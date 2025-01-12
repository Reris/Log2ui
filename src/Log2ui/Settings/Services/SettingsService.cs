using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DryIoc;
using Log2ui.Collections.Observables;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Settings.Services;

public class SettingsService(ISettingsServiceStorage storage) : ISettingsService, ISelfRegistering
{
    private static readonly PropertyInfo OriginalNamePropertyInfo
        = typeof(NamedLoggerSettings).Property(nameof(NamedLoggerSettings.OriginalName)) is { CanWrite: true } p
              ? p
              : throw new NotSupportedException();

    private readonly Signal<AppSettings> _appSettings = new(Settings.AppSettings.Default);
    private readonly Dictionary<string, Signal<NamedLoggerSettings>> _loggerSettings = new();
    private Task? _loading;

    protected ISettingsServiceStorage Storage { get; } = storage ?? throw new ArgumentNullException(nameof(storage));

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddSingleton<ISettingsService, SettingsService>();
    }

    public IReadOnlyList<string> AllLoggerNames => this._loggerSettings.Select(a => a.Key).ToArray();
    public Theme? CurrentTheme { get; set; }
    public IObservable<AppSettings> AppSettings => this._appSettings.ToObservable();

    public IObservable<NamedLoggerSettings> LoggerSettings(string loggerName)
    {
        var result = this.GetLoggerSettingsSignal(loggerName);
        return result.ToObservable();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        await this.Storage.SaveAppSettingsAsync(new Versioned<AppSettings>(1, settings)).AwaitInPool();
        this._appSettings.OnNext(settings);
    }

    public async Task PrepareAsync(AppSettings settings)
    {
        await this.PrepareAsync(settings.LoggerDefaults);
    }

    public async Task PrepareAsync(LoggerSettings settings)
    {
        if (settings is { UseDefaultStyle: false, Style: null })
        {
            var name = settings is NamedLoggerSettings n ? n.OriginalName : null;
            settings.Style = await this.LoggerStyleSettingsFrom(name).GetCurrentAsync();
        }
        else if (settings is { UseDefaultStyle: true, Style: not null })
        {
            settings.Style = null;
        }
    }

    public IObservable<LoggerStyleSettings> LoggerStyleSettingsFrom(string? loggerName)
    {
        var appStyle = this.AppSettings.Select(a => a.LoggerDefaults.Style);
        var loggerSettings = loggerName is null
                                 ? appStyle
                                 : this.LoggerSettings(loggerName).Select(a => a.Style).CombineLatest(appStyle, (a, b) => a ?? b);
        return loggerSettings.Select(
            a => a ?? this.CurrentTheme switch
            {
                Theme.Dark => LoggerStyleSettings.Dark.DeepClone(),
                Theme.Light => LoggerStyleSettings.Light.DeepClone(),
                _ => null,
            }).NotNull();
    }

    public async Task SaveAsync(NamedLoggerSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.OriginalName);

        var allSettings = this._loggerSettings.ToDictionary(a => a.Key, a => new Versioned<NamedLoggerSettings>(1, a.Value.Current));
        allSettings.Remove(settings.OriginalName);
        allSettings[settings.Name] = new Versioned<NamedLoggerSettings>(1, settings);

        await this.Storage.SaveLoggerSettingsAsync(allSettings).AwaitInPool();
        var signal = this._loggerSettings[settings.OriginalName];
        this._loggerSettings.Remove(settings.OriginalName);
        this._loggerSettings[settings.Name] = signal;
        SettingsService.OriginalNamePropertyInfo.SetValue(settings, settings.Name);
        signal.OnNext(settings.DeepClone());
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

    protected virtual async Task LoadSettingsAsync()
    {
        var tasks = new
        {
            app = this.Storage.LoadAppSettingsAsync().AwaitInPool(),
            log = this.Storage.LoadLoggerSettingsAsync().AwaitInPool(),
        };

        if (await tasks.app is { } appSettings)
        {
            appSettings.Data.LoggerDefaults.UseDefaultStyle = appSettings.Data.LoggerDefaults.Style is null;
            this._appSettings.OnNext(appSettings.Data);
        }

        foreach (var (name, loaded) in await tasks.log)
        {
            loaded.Data.UseDefaultStyle = loaded.Data.Style is null;
            var signal = this.GetLoggerSettingsSignal(name);
            signal.OnNext(loaded.Data with { Name = name, OriginalName = name });
        }
    }
}
