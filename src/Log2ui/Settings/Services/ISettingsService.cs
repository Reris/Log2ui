using System;
using System.Threading.Tasks;

namespace Log2ui.Settings.Services;

public interface ISettingsService
{
    Task LoadAsync();
    IObservable<AppSettings> AppSettings { get; }
    IObservable<LoggerSettings> LoggerSettings(string loggerName);
    Task SaveAsync(AppSettings settings);
    Task SaveAsync(LoggerSettings settings);
    Task DeleteAsync(LoggerSettings settings);
}
