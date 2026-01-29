using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Log2ui.Collections;

namespace Log2ui.Settings.Services;

public interface ISettingsService : ILoading
{
    IObservable<AppSettings> AppSettings { get; }
    IObservable<AllReceiverSettings> AllReceiverSettings { get; }
    IReadOnlyList<string> AllLoggerNames { get; }
    Theme? CurrentTheme { get; set; }
    T QueryLoggers<T>(Func<NamedLoggerSettings[], T> query);
    Task LoadAsync();
    IObservable<NamedLoggerSettings> LoggerSettings(string loggerName);
    IObservable<LoggerStyleSettings> LoggerStyleSettingsFrom(string? loggerName);
    IObservable<EquatableArray<LogColumn>> LoggerColumnsFrom(string? loggerName);
    Task<bool> SaveAsync(AppSettings settings);
    Task<bool> SaveAsync(AllReceiverSettings settings);
    Task<bool> SaveAsync(NamedLoggerSettings settings);
    Task DeleteAsync(string loggerName);
}
