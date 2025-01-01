using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace Log2ui.Settings.Services;

public class JsonFileFettingsServiceStorage : ISettingsServiceStorage, ISelfRegistering
{
    private static readonly ILogger Logger = Log.ForContext<JsonFileFettingsServiceStorage>();

    public static JsonSerializerOptions? JsonOptions { get; } = new(JsonSerializerOptions.Default)
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static void RegisterServices(Registry registry)
    {
        registry.Collection.TryAddSingleton<ISettingsServiceStorage, JsonFileFettingsServiceStorage>();
    }

    public async Task SaveAppSettingsAsync(Versioned<AppSettings> settings)
    {
        await this.SaveFileAsyc("appSettings.json", settings).AwaitInPool();
    }

    public async Task SaveLoggerSettingsAsync(Dictionary<string, Versioned<NamedLoggerSettings>> allSettings)
    {
        await this.SaveFileAsyc("loggerSettings.json", allSettings).AwaitInPool();
    }

    public async Task<Versioned<AppSettings>?> LoadAppSettingsAsync()
    {
        return await this.LoadFileAsync<Versioned<AppSettings>>("appSettings.json");
    }

    public async Task<Dictionary<string, Versioned<NamedLoggerSettings>>> LoadLoggerSettingsAsync()
    {
        return await this.LoadFileAsync<Dictionary<string, Versioned<NamedLoggerSettings>>>("loggerSettings.json") ?? [];
    }

    public async Task DeleteLoggerSettingsAsync(Dictionary<string, Versioned<NamedLoggerSettings>> allSettings, NamedLoggerSettings[] deleted)
    {
        await this.SaveLoggerSettingsAsync(allSettings);
    }

    protected async Task<T?> LoadFileAsync<T>(string fileName)
    {
        var settingsFilePath = JsonFileFettingsServiceStorage.GetFilePath(fileName);
        if (!File.Exists(settingsFilePath))
        {
            return default;
        }

        await using var stream = File.OpenRead(settingsFilePath);
        try
        {
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonFileFettingsServiceStorage.JsonOptions).AwaitInPool();
        }
        catch (JsonException e)
        {
            JsonFileFettingsServiceStorage.Logger.Error(e, "Failed to load file {File}", settingsFilePath);
            return default;
        }
    }

    protected async Task SaveFileAsyc<T>(string fileName, T settings)
    {
        Directory.CreateDirectory(JsonFileFettingsServiceStorage.GetDirectory());
        var settingsFilePath = JsonFileFettingsServiceStorage.GetFilePath(fileName);
        await using var stream = File.OpenWrite(settingsFilePath);
        try
        {
            await JsonSerializer.SerializeAsync(stream, settings, JsonFileFettingsServiceStorage.JsonOptions).AwaitInPool();
        }
        catch (JsonException e)
        {
            JsonFileFettingsServiceStorage.Logger.Error(e, "Failed to load file {File}", settingsFilePath);
            throw;
        }
        finally
        {
            stream.SetLength(stream.Position);
        }
    }

    private static string GetFilePath(string fileName)
    {
        var settingsPath = JsonFileFettingsServiceStorage.GetDirectory();
        var settingsFilePath = Path.Combine(settingsPath, fileName);
        return settingsFilePath;
    }

    private static string GetDirectory()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var settingsPath = Path.Combine(appDataPath, "Log2ui");
        return settingsPath;
    }
}
