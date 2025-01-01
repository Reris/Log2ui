using System.Collections.Generic;
using System.Threading.Tasks;

namespace Log2ui.Settings.Services;

public interface ISettingsServiceStorage
{
    Task SaveAppSettingsAsync(Versioned<AppSettings> settings);
    Task SaveLoggerSettingsAsync(Dictionary<string, Versioned<NamedLoggerSettings>> allSettings);
    Task<Versioned<AppSettings>?> LoadAppSettingsAsync();
    Task<Dictionary<string, Versioned<NamedLoggerSettings>>> LoadLoggerSettingsAsync();
    Task DeleteLoggerSettingsAsync(Dictionary<string, Versioned<NamedLoggerSettings>> allSettings, NamedLoggerSettings deleted);
}
