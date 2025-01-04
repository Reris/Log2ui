using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DryIoc;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Settings.Services;

public class SettingsService(ISettingsServiceStorage storage) : ISettingsService, ISelfRegistering
{
    private static readonly PropertyInfo OriginalNamePropertyInfo = typeof(NamedLoggerSettings).Property(nameof(NamedLoggerSettings.OriginalName));
    private readonly BehaviorSubject<AppSettings> _appSettings = new(Settings.AppSettings.Default);
    private readonly Dictionary<string, ReplaySubject<NamedLoggerSettings>> _loggerSettings = new();

    protected ISettingsServiceStorage Storage { get; } = storage ?? throw new ArgumentNullException(nameof(storage));

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddSingleton<ISettingsService, SettingsService>();
    }

    public async Task LoadAsync()
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
            var subject = this.GetOrAddLoggerSettingsSubject(name);
            subject.OnNext(loaded.Data with { Name = name, OriginalName = name });
        }
    }

    public Theme? CurrentTheme { get; set; }
    public IObservable<AppSettings> AppSettings => this._appSettings;

    public IObservable<NamedLoggerSettings> LoggerSettings(string loggerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(loggerName);

        var subject = this.AppSettings.FirstAsync().Select(a => Mapper.DefaultToNamed(a.LoggerDefaults, loggerName))
                          .Concat(this.GetOrAddLoggerSettingsSubject(loggerName));
        return subject;
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

        var allSettings = await this.GetAllLoggerSettingsAsync().AwaitInPool();
        allSettings.Remove(settings.OriginalName);
        allSettings[settings.Name] = settings;
        var versionedSettings = allSettings.ToDictionary(a => a.Key, a => new Versioned<NamedLoggerSettings>(1, a.Value));
        await this.Storage.SaveLoggerSettingsAsync(versionedSettings).AwaitInPool();
        var subject = this.GetOrAddLoggerSettingsSubject(settings.OriginalName);
        this._loggerSettings.Remove(settings.OriginalName);
        this._loggerSettings[settings.Name] = subject;
        SettingsService.OriginalNamePropertyInfo.SetValue(settings, settings.Name);
        subject.OnNext(settings with { });
    }

    public async Task DeleteAsync(NamedLoggerSettings settings)
    {
        var allSettings = await this.GetAllLoggerSettingsAsync();
        var toRemove = this._loggerSettings.Where(a => !a.Value.HasObservers).Select(a => a.Key).Append(settings.OriginalName)
                           .Join(allSettings, a => a, a => a.Key, (_, b) => b.Value)
                           .ToArray();
        foreach (var dead in toRemove)
        {
            allSettings.Remove(dead.OriginalName);
        }

        var versionedSettings = allSettings.ToDictionary(a => a.Key, a => new Versioned<NamedLoggerSettings>(1, a.Value));
        await this.Storage.DeleteLoggerSettingsAsync(versionedSettings, toRemove).AwaitInPool();
    }

    protected async Task<Dictionary<string, NamedLoggerSettings>> GetAllLoggerSettingsAsync()
    {
        var allSettings = await Task.WhenAll(this._loggerSettings.Select(async a => (a.Key, await this.LoggerSettings(a.Key).FirstAsync())))
                                    .SelectAsync(a => a.ToDictionary()).AwaitInPool();
        return allSettings;
    }

    protected virtual ReplaySubject<NamedLoggerSettings> GetOrAddLoggerSettingsSubject(string loggerName)
    {
        if (!this._loggerSettings.TryGetValue(loggerName, out var subject))
        {
            this._loggerSettings[loggerName] = subject = new ReplaySubject<NamedLoggerSettings>(1);
        }

        return subject;
    }
}
