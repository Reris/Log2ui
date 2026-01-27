using Log2ui.Collections;
using Log2ui.Settings;
using System;
using System.Threading.Tasks;

namespace Log2ui.Views;

public interface ILoggerSettingsViewModel
{
    IObservable<NamedLoggerSettings> LoggerSettings { get; }
    IObservable<LoggerStyleSettings> StyleSettings { get; }
    IObservable<AllReceiverSettings> AllReceiverSettings { get; }
    IObservable<EquatableArray<LogColumn>> Columns { get; }
    Task<bool> AddReceiverAsync(ReceiverSettings receiverSettings);
    Task RemoveAsync();
    Task SaveAsync();
}
