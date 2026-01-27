using System;
using System.Threading.Tasks;
using Log2ui.Collections;
using Log2ui.Settings;

namespace Log2ui.Views;

public interface ILoggerSettingsViewModel
{
    IObservable<NamedLoggerSettings> LoggerSettings { get; }
    IObservable<LoggerStyleSettings> StyleSettings { get; }
    IObservable<AllReceiverSettings> AllReceiverSettings { get; }
    IObservable<EquatableArray<LogColumn>> Columns { get; }
    Task<bool> AddReceiverAsync(ReceiverSettings receiverSettings);
    Task<bool> RemoveReceiverAsync(string receiverKey);
    Task RemoveAsync();
    Task SaveAsync();
}
