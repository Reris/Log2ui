using System;
using System.Threading.Tasks;

namespace Log2ui.Settings.Services;

public interface ISettingsService
{
    IObservable<AppSettings> AppSettings { get; }
    Theme? CurrentTheme { get; set; }
    Task LoadAsync();
    IObservable<NamedLoggerSettings> LoggerSettings(string loggerName);
    IObservable<LoggerStyleSettings> LoggerStyleSettingsFrom(string? loggerName);
    Task PrepareAsync(AppSettings settings);
    Task PrepareAsync(LoggerSettings settings);
    Task SaveAsync(AppSettings settings);
    Task SaveAsync(NamedLoggerSettings settings);
    Task DeleteAsync(NamedLoggerSettings settings);
}
