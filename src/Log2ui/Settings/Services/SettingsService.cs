using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DryIoc;
using Log2ui.Collections;
using Log2ui.Collections.Observables;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Settings.Services;

public class SettingsService(ISettingsServiceStorage storage, IValidator validator) : ISettingsService, ISelfRegistering
{
    private static readonly PropertyInfo OriginalNamePropertyInfo
        = typeof(NamedLoggerSettings).Property(nameof(NamedLoggerSettings.OriginalName)) is { CanWrite: true } p
              ? p
              : throw new NotSupportedException();

    private readonly Signal<AllReceiverSettings> _allReceiverSettings = new(new AllReceiverSettings());

    private readonly Signal<AppSettings> _appSettings = new(Settings.AppSettings.Default);

    private readonly Dictionary<string, Signal<NamedLoggerSettings>> _loggerSettings = new();
    private Task? _loading;

    protected ISettingsServiceStorage Storage { get; } = storage ?? throw new ArgumentNullException(nameof(storage));

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddSingleton<ISettingsService, SettingsService>();
    }

    public IObservable<AllReceiverSettings> AllReceiverSettings => this._allReceiverSettings.AsObservable();
    public IReadOnlyList<string> AllLoggerNames => this._loggerSettings.Select(a => a.Key).ToArray();
    public Theme? CurrentTheme { get; set; }
    public IObservable<AppSettings> AppSettings => this._appSettings.AsObservable();

    public IObservable<NamedLoggerSettings> LoggerSettings(string loggerName)
    {
        var result = this.GetLoggerSettingsSignal(loggerName);
        return result.AsObservable();
    }

    public IObservable<LoggerStyleSettings> LoggerStyleSettingsFrom(string? loggerName)
    {
        var appStyle = this.AppSettings.Select(a => a.LoggerDefaults.Style);
        var loggerSettings = loggerName is null
                                 ? appStyle
                                 : this.LoggerSettings(loggerName).Select(a => a.Style).CombineLatest(appStyle, (a, b) => a ?? b);
        return loggerSettings.Select(a => a ?? this.CurrentTheme switch
        {
            Theme.Dark => LoggerStyleSettings.Dark.DeepClone(),
            Theme.Light => LoggerStyleSettings.Light.DeepClone(),
            _ => null,
        }).NotNull();
    }

    public IObservable<EquatableArray<LogColumn>> LoggerColumnsFrom(string? loggerName)
    {
        var appColumns = this.AppSettings.Select(a => a.LoggerDefaults.Columns ?? Settings.LoggerSettings.Default.Columns ?? []);
        var loggerColumns = loggerName is null
                                ? appColumns
                                : this.LoggerSettings(loggerName).Select(a => a.Columns).CombineLatest(appColumns, (a, b) => a ?? b);
        return loggerColumns.NotNull();
    }

    public async Task<bool> SaveAsync(AppSettings settings)
    {
        await this.PrepareSaveAsync(settings);

        if (!validator.IsValid(settings))
        {
            return false;
        }

        await this.Storage.SaveAsync(new Versioned<AppSettings>(1, settings)).AwaitInPool();
        this._appSettings.OnNext(settings);
        return true;
    }

    public async Task<bool> SaveAsync(AllReceiverSettings settings)
    {
        if (!validator.IsValid(settings))
        {
            return false;
        }

        var allLoggers = await Task.WhenAll(this._loggerSettings.Values.Select(a => a.GetCurrentAsync().AsTask()));
        var drops = allLoggers.Where(a => this.DropUnknownReceivers(a, settings));

        await this.Storage.SaveAsync(new Versioned<AllReceiverSettings>(1, settings)).AwaitInPool();

        this._allReceiverSettings.OnNext(settings);
        await Task.WhenAll(drops.Select(this.SaveAsync));
        return true;
    }

    public async Task<bool> SaveAsync(NamedLoggerSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.OriginalName);

        await this.PrepareSaveAsync(settings);
        this.DropUnknownReceivers(settings, await this.AllReceiverSettings.GetCurrentAsync());

        if (!validator.IsValid(settings))
        {
            return false;
        }

        var allSettings = this._loggerSettings.ToDictionary(a => a.Key, a => new Versioned<NamedLoggerSettings>(1, a.Value.Current));
        allSettings.Remove(settings.OriginalName);
        allSettings[settings.Name] = new Versioned<NamedLoggerSettings>(1, settings);

        await this.Storage.SaveAsync(allSettings).AwaitInPool();
        var signal = this._loggerSettings[settings.OriginalName];
        this._loggerSettings.Remove(settings.OriginalName);
        this._loggerSettings[settings.Name] = signal;
        SettingsService.OriginalNamePropertyInfo.SetValue(settings, settings.Name);
        signal.OnNext(settings);
        return true;
    }

    public async Task DeleteAsync(string loggerName)
    {
        var allSettings = this._loggerSettings.ToDictionary(a => a.Key, a => a.Value.Current);
        var toRemove = this._loggerSettings.Where(a => !a.Value.HasObservers).Select(a => a.Key).Append(loggerName)
                           .Join(allSettings, a => a, a => a.Key, (_, b) => b.Value)
                           .ToArray();
        foreach (var dead in toRemove)
        {
            allSettings.Remove(dead.OriginalName);
        }

        var versionedSettings = allSettings.ToDictionary(a => a.Key, a => new Versioned<NamedLoggerSettings>(1, a.Value));
        await this.Storage.DeleteLoggerSettingsAsync(versionedSettings, toRemove).AwaitInPool();

        foreach (var dead in toRemove)
        {
            this._loggerSettings.Remove(dead.OriginalName);
        }
    }

    public Task Loading => this._loading ??= this.LoadAsync();

    public Task LoadAsync()
    {
        return this._loading ??= this.LoadSettingsAsync();
    }

    private bool DropUnknownReceivers(NamedLoggerSettings settings, AllReceiverSettings receivers)
    {
        var filtered = EquatableArray.Create(settings.ReceiverKeys.Where(a => receivers.Receivers.Any(b => b.Key == a)));
        if (filtered.Count == settings.ReceiverKeys.Count)
        {
            return false;
        }

        settings.ReceiverKeys = filtered;
        return true;
    }

    private async Task PrepareSaveAsync(AppSettings settings)
    {
        await this.PrepareSaveAsync(settings.LoggerDefaults);
    }

    private Task PrepareSaveAsync(LoggerSettings settings)
    {
        settings.Columns = settings switch
        {
            { UseDefaultColumns: false, Columns: null } =>
            [
                ..(this._appSettings.Current.LoggerDefaults.Columns ?? Settings.LoggerSettings.Default.Columns ?? []).Select(a => a.DeepClone()),
            ],
            { UseDefaultColumns: true, Columns: not null } => null,
            _ => settings.Columns,
        };

        settings.Style = settings switch
        {
            { UseDefaultStyle: false, Style: null } => this._appSettings.Current.LoggerDefaults.Style,
            { UseDefaultStyle: true, Style: not null } => null,
            _ => settings.Style,
        };

        return Task.CompletedTask;
    }

    private Signal<NamedLoggerSettings> GetLoggerSettingsSignal(string loggerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(loggerName);

        if (!this._loggerSettings.TryGetValue(loggerName, out var result))
        {
            var fromAppSettings = Mapper.DefaultToNamed(this._appSettings.Current.LoggerDefaults, loggerName);
            this._loggerSettings[loggerName] = result = new Signal<NamedLoggerSettings>(fromAppSettings);
        }

        return result;
    }

    [SuppressMessage("ReSharper", "NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract", Justification = "Potential old data")]
    protected virtual async Task LoadSettingsAsync()
    {
        var tasks = new
        {
            app = this.Storage.LoadAppSettingsAsync().AwaitInPool(),
            receivers = this.Storage.LoadAllReceiverSettingsAsync().AwaitInPool(),
            log = this.Storage.LoadLoggerSettingsAsync().AwaitInPool(),
        };

        if (await tasks.app is { Data: not null } appSettings)
        {
            appSettings.Data.LoggerDefaults ??= this._appSettings.Current.LoggerDefaults;
            SettingsService.AfterLoad(appSettings.Data.LoggerDefaults);
            this._appSettings.OnNext(appSettings.Data);
        }

        if (await tasks.receivers is { Data: not null } receiverSettings)
        {
            this._allReceiverSettings.OnNext(receiverSettings.Data);
        }

        foreach (var (name, loaded) in await tasks.log)
        {
            SettingsService.AfterLoad(loaded.Data);
            var signal = this.GetLoggerSettingsSignal(name);
            signal.OnNext(loaded.Data with { Name = name, OriginalName = name });
        }
    }

    private static void AfterLoad(LoggerSettings appSettings)
    {
        appSettings.UseDefaultStyle = appSettings.Style is null;
        appSettings.UseDefaultColumns = appSettings.Columns is null;
    }
}
