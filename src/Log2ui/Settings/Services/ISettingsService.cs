using System;
using System.Threading.Tasks;

namespace Log2ui.Settings.Services;

public interface ISettingsService
{
    Task LoadAsync();
    IObservable<AppSettings> AppSettings { get; }
    IObservable<NamedLoggerSettings> LoggerSettings(string loggerName);
    Task SaveAsync(AppSettings settings);
    Task SaveAsync(NamedLoggerSettings settings);
    Task DeleteAsync(NamedLoggerSettings settings);
}
