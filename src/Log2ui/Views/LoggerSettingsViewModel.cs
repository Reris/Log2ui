using System;
using System.Linq;
using System.Threading.Tasks;
using Log2ui.Collections;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Log2ui.Settings;
using Log2ui.Settings.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Views;

public class LoggerSettingsViewModel : ViewModel, ISelfRegistering, ILoggerSettingsViewModel
{
    private readonly ISettingsService _settingsService;

    public LoggerSettingsViewModel(string name, ISettingsService settingsService)
    {
        ArgumentNullException.ThrowIfNull(settingsService);

        this._settingsService = settingsService;
        this.LoggerSettings = this._settingsService.LoggerSettings(name)
                                  .SelectExceptCurrent(a => a.DeepClone())
                                  .UseCurrent();

        this.StyleSettings = this._settingsService.LoggerStyleSettingsFrom(name);
        this.Columns = this._settingsService.LoggerColumnsFrom(name);
    }

    public IObservable<NamedLoggerSettings> LoggerSettings { get; }
    public IObservable<LoggerStyleSettings> StyleSettings { get; }
    public IObservable<EquatableArray<LogColumn>> Columns { get; }

    public async Task<bool> AddReceiverAsync(ReceiverSettings receiverSettings)
    {
        var settings = await this.LoggerSettings.GetCurrentAsync();
        if (settings.Receivers.Any(a => a.GetType() == receiverSettings.GetType() && a.ValueKey == receiverSettings.ValueKey))
        {
            return false;
        }

        settings.Receivers = settings.Receivers.Append(receiverSettings);
        await this.SaveAsync();
        return true;
    }

    public async Task RemoveAsync()
    {
        var current = await this.LoggerSettings.GetCurrentAsync();
        await this._settingsService.DeleteAsync(current.OriginalName);
    }

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<ILoggerSettingsViewModel, LoggerSettingsViewModel>();
    }

    public async Task SaveAsync()
    {
        var current = await this.LoggerSettings.GetCurrentAsync();
        await this._settingsService.SaveAsync(current);
    }
}
