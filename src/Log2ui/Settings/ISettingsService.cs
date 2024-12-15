using System.Threading.Tasks;

namespace Log2ui.Settings;

public interface ISettingsService
{
    Task LoadAsync();
    AppSettings AppSettings { get; }
    LogSettings LogSettings { get; }
    Task SaveAsync(AppSettings settings);
    Task SaveAsync(LogSettings settings);
}
