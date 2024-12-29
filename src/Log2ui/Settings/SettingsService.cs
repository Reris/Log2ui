using System;
using System.IO;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading.Tasks;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Settings;

public class SettingsService : ISettingsService, ISelfRegistering
{
    private readonly ReplaySubject<AppSettings> _appSettings = new(1);
    private readonly ReplaySubject<LogSettings> _logSettings = new(1);

    public static JsonSerializerOptions? JsonOptions { get; } = new(JsonSerializerOptions.Default)
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddSingleton<ISettingsService, SettingsService>();
    }

    public async Task LoadAsync()
    {
        var tasks = new
        {
            app = this.LoadFileAsync<Versioned<AppSettings>>("appSettings.json").SelectAsync(a => a?.Data ?? Settings.AppSettings.Default).AwaitInPool(),
            log = this.LoadFileAsync<Versioned<LogSettings>>("loggerSettings.json").SelectAsync(a => a?.Data ?? Settings.LogSettings.Default).AwaitInPool(),
        };

        this._appSettings.OnNext(await tasks.app);
        this._logSettings.OnNext(await tasks.log);
    }

    public IObservable<AppSettings> AppSettings => this._appSettings;
    public IObservable<LogSettings> LogSettings => this._logSettings;

    public async Task SaveAsync(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsService.GetDirectory());
        await this.SaveFileAsyc("appSettings.json", new Versioned<AppSettings>(1, settings)).AwaitInPool();
        this._appSettings.OnNext(settings);
    }

    public async Task SaveAsync(LogSettings settings)
    {
        Directory.CreateDirectory(SettingsService.GetDirectory());
        await this.SaveFileAsyc("loggerSettings.json", new Versioned<LogSettings>(1, settings)).AwaitInPool();
        this._logSettings.OnNext(settings);
    }

    private async Task<T?> LoadFileAsync<T>(string fileName)
    {
        var settingsFilePath = SettingsService.GetFilePath(fileName);
        if (!File.Exists(settingsFilePath))
        {
            return default;
        }

        await using var stream = File.OpenRead(settingsFilePath);
        return await JsonSerializer.DeserializeAsync<T>(stream, SettingsService.JsonOptions).AwaitInPool();
    }

    private async Task SaveFileAsyc<T>(string fileName, T settings)
    {
        var settingsFilePath = SettingsService.GetFilePath(fileName);
        await using var stream = File.OpenWrite(settingsFilePath);
        await JsonSerializer.SerializeAsync(stream, settings, SettingsService.JsonOptions).AwaitInPool();
        stream.SetLength(stream.Position);
    }

    private static string GetFilePath(string fileName)
    {
        var settingsPath = SettingsService.GetDirectory();
        var settingsFilePath = Path.Combine(settingsPath, fileName);
        return settingsFilePath;
    }

    private static string GetDirectory()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var settingsPath = Path.Combine(appDataPath, "Log2ui");
        return settingsPath;
    }

    protected record Versioned<T>(int Version, T Data);
}
