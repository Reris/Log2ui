using System;
using System.Threading.Tasks;

namespace Log2ui.Settings;

public interface ISettingsService
{
    Task LoadAsync();
    IObservable<AppSettings> AppSettings { get; }
    IObservable<LogSettings> LogSettings { get; }
    Task SaveAsync(AppSettings settings);
    Task SaveAsync(LogSettings settings);
}
