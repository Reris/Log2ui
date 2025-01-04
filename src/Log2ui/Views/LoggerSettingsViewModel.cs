using System;
using System.Threading.Tasks;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Log2ui.Settings;
using Log2ui.Settings.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Views;

public class LoggerSettingsViewModel : ViewModel, ISelfRegistering
{
    private readonly ISettingsService _settingsService;

    public LoggerSettingsViewModel(string name, ISettingsService settingsService)
    {
        ArgumentNullException.ThrowIfNull(settingsService);

        this._settingsService = settingsService;
        this.AppSettings = this._settingsService.AppSettings;
        this.LoggerSettings = this._settingsService.LoggerSettings(name)
                                  .SelectExceptCurrent(a => a.DeepClone())
                                  .UseCurrent();

        this.StyleSettings = this._settingsService.LoggerStyleSettingsFrom(name);
    }

    public IObservable<AppSettings> AppSettings { get; }
    public IObservable<NamedLoggerSettings> LoggerSettings { get; }
    public IObservable<LoggerStyleSettings> StyleSettings { get; }

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<LoggerSettingsViewModel>();
    }

    public async Task SaveAsync()
    {
        var current = await this.LoggerSettings.GetCurrentAsync();
        await this._settingsService.PrepareAsync(current);
        await this._settingsService.SaveAsync(current);
    }
}
