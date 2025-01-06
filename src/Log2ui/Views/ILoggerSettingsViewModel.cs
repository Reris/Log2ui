using System;
using System.Threading.Tasks;
using Log2ui.Settings;

namespace Log2ui.Views;

public interface ILoggerSettingsViewModel
{
    IObservable<NamedLoggerSettings> LoggerSettings { get; }
    IObservable<LoggerStyleSettings> StyleSettings { get; }
    Task<bool> AddReceiverAsync(ReceiverSettings receiverSettings);
}
