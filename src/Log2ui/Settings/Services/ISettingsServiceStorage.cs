using System.Collections.Generic;
using System.Threading.Tasks;

namespace Log2ui.Settings.Services;

public interface ISettingsServiceStorage
{
    Task SaveAppSettingsAsync(AppSettings settings);
    Task SaveLoggerSettingsAsync(Dictionary<string, LoggerSettings> allSettings);
    Task<AppSettings?> LoadAppSettingsAsync();
    Task<Dictionary<string, LoggerSettings>?> LoadLoggerSettingsAsync();
    Task DeleteLoggerSettingsAsync(Dictionary<string, LoggerSettings> allSettings, LoggerSettings deleted);
}
