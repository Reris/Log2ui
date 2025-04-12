using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Log2ui.Collections;

namespace Log2ui.Settings.Services;

public interface ISettingsService : ILoading
{
    IObservable<AppSettings> AppSettings { get; }
    IReadOnlyList<string> AllLoggerNames { get; }
    Theme? CurrentTheme { get; set; }
    Task LoadAsync();
    IObservable<NamedLoggerSettings> LoggerSettings(string loggerName);
    IObservable<LoggerStyleSettings> LoggerStyleSettingsFrom(string? loggerName);
    IObservable<EquatableArray<LogColumn>> LoggerColumnsFrom(string? loggerName);
    Task<bool> SaveAsync(AppSettings settings);
    Task<bool> SaveAsync(NamedLoggerSettings settings);
    Task DeleteAsync(string loggerName);
}
