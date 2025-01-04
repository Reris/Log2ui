using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Log2ui.Settings;
using Log2ui.Settings.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Views;

public class AppSettingsViewModel : ViewModel, ICaptionedViewModel, ISelfRegistering
{
    private readonly ISettingsService _settingsService;

    public AppSettingsViewModel(ISettingsService settingsService)
    {
        ArgumentNullException.ThrowIfNull(settingsService);

        this._settingsService = settingsService;
        this.AppSettings = settingsService.AppSettings
                                          .SelectExceptCurrent(a => a.DeepClone())
                                          .UseCurrent();
    }

    public IObservable<AppSettings> AppSettings { get; }

    public IObservable<string> Caption { get; } = Observable.Return("App settings");

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<AppSettingsViewModel>();
    }

    public async Task SaveAsync()
    {
        var current = await this.AppSettings.GetCurrentAsync();
        await this._settingsService.SaveAsync(current);
    }
}
