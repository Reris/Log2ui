using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Settings.Services;

public class SettingsService(ISettingsServiceStorage storage) : ISettingsService, ISelfRegistering
{
    private readonly BehaviorSubject<AppSettings> _appSettings = new(Settings.AppSettings.Default);
    private readonly Dictionary<string, BehaviorSubject<NamedLoggerSettings>> _loggerSettings = new();

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
            this._appSettings.OnNext(appSettings.Data);
        }

        foreach (var (name, loaded) in await tasks.log)
        {
            var subject = this.GetOrAddLoggerSettingsSubject(name);
            subject.OnNext(loaded.Data with { Name = name, OriginalName = name });
        }
    }

    public IObservable<AppSettings> AppSettings => this._appSettings;

    public IObservable<NamedLoggerSettings> LoggerSettings(string loggerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(loggerName);

        var subject = this.GetOrAddLoggerSettingsSubject(loggerName);
        return subject;
    }

    public async Task SaveAsync(AppSettings settings)
    {
        await this.Storage.SaveAppSettingsAsync(new Versioned<AppSettings>(1, settings)).AwaitInPool();
        this._appSettings.OnNext(settings);
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
        subject.OnNext(settings with { OriginalName = settings.Name });
    }

    public async Task DeleteAsync(NamedLoggerSettings settings)
    {
        var allSettings = await this.GetAllLoggerSettingsAsync();
        allSettings.Remove(settings.OriginalName);
        var versionedSettings = allSettings.ToDictionary(a => a.Key, a => new Versioned<NamedLoggerSettings>(1, a.Value));
        await this.Storage.DeleteLoggerSettingsAsync(versionedSettings, settings).AwaitInPool();
    }

    protected async Task<Dictionary<string, NamedLoggerSettings>> GetAllLoggerSettingsAsync()
    {
        var allSettings = await Task.WhenAll(this._loggerSettings.Select(async a => (a.Key, await a.Value.FirstAsync())))
                                    .SelectAsync(a => a.ToDictionary()).AwaitInPool();
        return allSettings;
    }

    protected virtual BehaviorSubject<NamedLoggerSettings> GetOrAddLoggerSettingsSubject(string loggerName)
    {
        if (!this._loggerSettings.TryGetValue(loggerName, out var subject))
        {
            var value = Mapper.DefaultToNamed(Settings.AppSettings.Default.LoggerDefaults, loggerName, loggerName);
            this._loggerSettings[loggerName] = subject = new BehaviorSubject<NamedLoggerSettings>(value);
        }

        return subject;
    }
}
