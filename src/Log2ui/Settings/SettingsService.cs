using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Settings;

public class SettingsService : ISettingsService, ISelfRegistering
{
    private AppSettings? _appSettings;
    private LogSettings? _logSettings;

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddSingleton<ISettingsService, SettingsService>();
    }

    public async Task LoadAsync()
    {
        var tasks = new
        {
            app = this.LoadFileAsync<Versioned<AppSettings>>("appSettings.json").SelectAsync(a => a?.Data ?? AppSettings.Default).AwaitInPool(),
            log = this.LoadFileAsync<Versioned<LogSettings>>("loggerSettings.json").SelectAsync(a => a?.Data ?? LogSettings.Default).AwaitInPool(),
        };

        this._appSettings = await tasks.app;
        this._logSettings = await tasks.log;
    }

    public AppSettings AppSettings => this._appSettings ?? throw new SettingNotLoadedException(nameof(this.AppSettings));
    public LogSettings LogSettings => this._logSettings ?? throw new SettingNotLoadedException(nameof(this.LogSettings));

    public async Task SaveAsync(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsService.GetDirectory());
        await this.SaveFileAsyc("appSettings.json", settings).AwaitInPool();
    }

    public async Task SaveAsync(LogSettings settings)
    {
        Directory.CreateDirectory(SettingsService.GetDirectory());
        await this.SaveFileAsyc("loggerSettings.json", settings).AwaitInPool();
    }

    private async Task<T?> LoadFileAsync<T>(string fileName)
    {
        var settingsFilePath = SettingsService.GetFilePath(fileName);
        if (!File.Exists(settingsFilePath))
        {
            return default;
        }

        await using var stream = File.OpenRead(settingsFilePath);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonSerializerOptions.Web).AwaitInPool();
    }

    private async Task SaveFileAsyc<T>(string fileName, T settings)
    {
        var settingsFilePath = SettingsService.GetFilePath(fileName);
        await using var stream = File.OpenWrite(settingsFilePath);
        await JsonSerializer.SerializeAsync(stream, settings, JsonSerializerOptions.Web).AwaitInPool();
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

    public class SettingNotLoadedException(string settingsName) : Exception(
        $"Settings for {settingsName} are not loaded. Ensure {nameof(SettingsService.LoadAsync)} has been called.");

    protected record Versioned<T>(int Version, T Data);
}
