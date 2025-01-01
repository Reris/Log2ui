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
    private readonly Dictionary<string, BehaviorSubject<LoggerSettings>> _loggerSettings = new();

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
            this._appSettings.OnNext(appSettings);
        }

        foreach (var (name, loaded) in await tasks.log ?? [])
        {
            var subject = this.GetOrAddLoggerSettingsSubject(name);
            subject.OnNext(loaded);
        }
    }

    public IObservable<AppSettings> AppSettings => this._appSettings;

    public IObservable<LoggerSettings> LoggerSettings(string loggerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(loggerName);

        var subject = this.GetOrAddLoggerSettingsSubject(loggerName);
        return this.AppSettings.Select(a => a.LoggerDefaults.DeepClone() with { OriginalName = loggerName, Name = loggerName }).Take(1).Concat(subject);
    }

    public async Task SaveAsync(AppSettings settings)
    {
        await this.Storage.SaveAppSettingsAsync(settings).AwaitInPool();
        this._appSettings.OnNext(settings);
    }

    public async Task SaveAsync(LoggerSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.OriginalName);

        var allSettings = await this.GetAllLoggerSettingsAsync().AwaitInPool();
        allSettings.Remove(settings.OriginalName);
        allSettings[settings.Name] = settings;
        await this.Storage.SaveLoggerSettingsAsync(allSettings).AwaitInPool();
        var subject = this.GetOrAddLoggerSettingsSubject(settings.OriginalName);
        this._loggerSettings.Remove(settings.OriginalName);
        this._loggerSettings[settings.Name] = subject;
        subject.OnNext(settings with { OriginalName = settings.Name });
    }

    public async Task DeleteAsync(LoggerSettings settings)
    {
        var allSettings = await this.GetAllLoggerSettingsAsync();
        allSettings.Remove(settings.OriginalName);
        await this.Storage.DeleteLoggerSettingsAsync(allSettings, settings).AwaitInPool();
    }

    protected async Task<Dictionary<string, LoggerSettings>> GetAllLoggerSettingsAsync()
    {
        var allSettings = await Task.WhenAll(this._loggerSettings.Select(async a => (a.Key, await a.Value.FirstAsync())))
                                    .SelectAsync(a => a.ToDictionary()).AwaitInPool();
        return allSettings;
    }

    protected virtual BehaviorSubject<LoggerSettings> GetOrAddLoggerSettingsSubject(string name)
    {
        if (!this._loggerSettings.TryGetValue(name, out var subject))
        {
            this._loggerSettings[name] = subject = new BehaviorSubject<LoggerSettings>(Settings.AppSettings.Default.LoggerDefaults);
        }

        return subject;
    }
}
