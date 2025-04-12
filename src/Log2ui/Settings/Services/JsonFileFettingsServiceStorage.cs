using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using DynamicData;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PropertyModels.Extensions;
using Serilog;

namespace Log2ui.Settings.Services;

public class JsonFileFettingsServiceStorage : ISettingsServiceStorage, ISelfRegistering
{
    private static readonly ILogger Logger = Log.ForContext<JsonFileFettingsServiceStorage>();

    public JsonFileFettingsServiceStorage(ReceiverSettingsDiscriminator[] settingsTypes)
    {
        JsonFileFettingsServiceStorage.JsonOptions ??= new JsonSerializerOptions(JsonSerializerOptions.Default)
        {
            TypeInfoResolver = JsonFileFettingsServiceStorage.CreateTypeInfoResilver(settingsTypes),
            Converters = { new JsonColorConverter(), new EquatableArrayConverterFactory() },
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }

    public static JsonSerializerOptions? JsonOptions { get; private set; }

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
        await Task.WhenAll(allSettings.Select(a => this.SaveFileAsyc($"loggerSettings.{this.SafeName(a.Key)}.json", a.Value))).AwaitInPool();

        var allLoggerFiles = Directory.GetFiles(JsonFileFettingsServiceStorage.GetDirectory(), "loggerSettings.*.json").Select(a => new FileInfo(a));
        var orphaned = allLoggerFiles.ExceptBy(allSettings.Select(a => $"loggerSettings.{this.SafeName(a.Key)}.json"), a => a.Name).ToArray();
        this.DeleteLoggerSettingFiles(orphaned.Select(a => a.Name));
    }

    public async Task<Versioned<AppSettings>?> LoadAppSettingsAsync()
    {
        var result = await this.LoadFileAsync<Versioned<AppSettings>>("appSettings.json");
        return result;
    }

    public async Task<Dictionary<string, Versioned<NamedLoggerSettings>>> LoadLoggerSettingsAsync()
    {
        var allLoggerFiles = Directory.GetFiles(JsonFileFettingsServiceStorage.GetDirectory(), "loggerSettings.*.json").Select(a => new FileInfo(a)).ToArray();
        var settings = await Task.WhenAll(allLoggerFiles.Select(a => this.LoadFileAsync<Versioned<NamedLoggerSettings>>(a.Name))).AwaitInPool();
        var result = settings.Where(a => a?.Data is not null).ToDictionary(a => a!.Data.Name, a => a!);
        return result;
    }

    public Task DeleteLoggerSettingsAsync(Dictionary<string, Versioned<NamedLoggerSettings>> allSettings, NamedLoggerSettings[] deleted)
    {
        this.DeleteLoggerSettingFiles(deleted.Select(a => $"loggerSettings.{this.SafeName(a.OriginalName)}.json"));
        return Task.CompletedTask;
    }

    private void DeleteLoggerSettingFiles(IEnumerable<string> fileNames)
    {
        var files = fileNames.Select(a => new FileInfo(JsonFileFettingsServiceStorage.GetFilePath(a)));
        foreach (var file in files)
        {
            if (file.Exists)
            {
                file.Delete();
            }
        }
    }

    private string SafeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var result = new string(name.Where(a => !invalid.Contains(a)).ToArray());
        return result;
    }

    private static IJsonTypeInfoResolver CreateTypeInfoResilver(ReceiverSettingsDiscriminator[] settingsTypes)
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(
            info =>
            {
                if (info.Type != typeof(ReceiverSettings))
                {
                    if (info.Type.IsAssignableTo(typeof(ReceiverSettings)))
                    {
                        info.Properties.Remove(a => a.Name == nameof(ReceiverSettings.ValueKey));
                    }

                    return;
                }

                var poly = info.PolymorphismOptions ??= new JsonPolymorphismOptions();
                var derrived = settingsTypes.Select(a => new JsonDerivedType(a.Type, a.TypeKey));
                poly.DerivedTypes.AddRange(derrived);
            });

        return resolver;
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
            var result = await JsonSerializer.DeserializeAsync<T>(stream, JsonFileFettingsServiceStorage.JsonOptions).AwaitInPool();
            return result;
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
