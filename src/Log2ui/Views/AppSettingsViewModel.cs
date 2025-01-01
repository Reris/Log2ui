using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Log2ui.Settings;
using Log2ui.Settings.Services;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace Log2ui.Views;

public class AppSettingsViewModel : ViewModel, ICaptionedViewModel, ISelfRegistering
{
    private readonly ISettingsService _settingsService;
    private string _caption = "App settings";

    public AppSettingsViewModel(ISettingsService settingsService)
    {
        ArgumentNullException.ThrowIfNull(settingsService);

        this._settingsService = settingsService;
        this.AppSettings = settingsService.AppSettings.Select(a => a.DeepClone()).UseCurrent();
        this.SaveCommand = ReactiveCommand.CreateFromTask(this.SaveAsync);
    }

    public IObservable<AppSettings> AppSettings { get; }

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    public string Caption
    {
        get => this._caption;
        set => this.RaiseAndSetIfChanged(ref this._caption, value);
    }

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
