using System;
using Log2ui.Dependencies;
using Log2ui.Settings;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace Log2ui.Views;

public class AppSettingsViewModel : ViewModel, ICaptionedViewModel, ISelfRegistering
{
    private string _caption = "App settings";

    public AppSettingsViewModel(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        this.Settings = settings;
    }

    public AppSettings Settings { get; }

    public string Caption
    {
        get => this._caption;
        set => this.RaiseAndSetIfChanged(ref this._caption, value);
    }

    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddTransient<AppSettingsViewModel>();
    }
}
