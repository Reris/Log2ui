using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using DryIoc;
using Log2ui.Data;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Log2ui.Receivers;
using Log2ui.Settings.Services;
using Log2ui.Views;
using Serilog.Events;

namespace Log2ui;

public class App : Application
{
    private readonly TaskCompletionSource _initializedTcs = new();
    private ISettingsService? _settingsService;
    private ISettingsService SettingsService => this._settingsService ?? throw new NotInitializedException(nameof(this.SettingsService));
    public IObservable<LogEvent> ObservableLog { get; set; } = Observable.Empty<LogEvent>();

    public IObservable<ThemeVariant> Theme => Observable.DeferAsync(
        async _ =>
        {
            await this._initializedTcs.Task;
            return this.SettingsService.AppSettings.Select(
                a => a.Theme switch
                {
                    Settings.Theme.Default => ThemeVariant.Default,
                    Settings.Theme.Light => ThemeVariant.Light,
                    Settings.Theme.Dark => ThemeVariant.Dark,
                    _ => throw new SwitchExpressionException(a.Theme),
                });
        }).DistinctUntilChanged();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public async Task LoadAsync()
    {
        await Task.Factory.AwaitInPool();
        await this.SettingsService.LoadAsync();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        LogLevels.Init();

        var builderContainer = Registry.Register();
        this._settingsService = builderContainer.Resolve<ISettingsService>();
        var vm = builderContainer.Resolve<MainWindowViewModel>([new ObservableReceiver(this.ObservableLog)]);

        switch (this.ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow
                {
                    DataContext = vm,
                };
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainWindow
                {
                    DataContext = vm,
                };
                break;
        }

        base.OnFrameworkInitializationCompleted();
        this._initializedTcs.SetResult();

        this.LoadAsync().FireAndForget();
    }
}
