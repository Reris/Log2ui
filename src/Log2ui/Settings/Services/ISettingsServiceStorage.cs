using System.Collections.Generic;
using System.Threading.Tasks;

namespace Log2ui.Settings.Services;

public interface ISettingsServiceStorage
{
    Task SaveAsync(Versioned<AppSettings> settings);
    Task SaveAsync(Versioned<AllReceiverSettings> settings);
    Task SaveAsync(Dictionary<string, Versioned<NamedLoggerSettings>> allSettings);
    Task<Versioned<AppSettings>?> LoadAppSettingsAsync();
    Task<Versioned<AllReceiverSettings>?> LoadAllReceiverSettingsAsync();
    Task<Dictionary<string, Versioned<NamedLoggerSettings>>> LoadLoggerSettingsAsync();
    Task DeleteLoggerSettingsAsync(Dictionary<string, Versioned<NamedLoggerSettings>> allSettings, NamedLoggerSettings[] deleted);
}
