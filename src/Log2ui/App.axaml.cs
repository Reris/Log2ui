using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using DryIoc;
using Log2ui.Dependencies;
using Log2ui.Extensions;
using Log2ui.Settings.Services;
using Log2ui.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using Serilog.Events;

namespace Log2ui;

public class App : Application
{
    private static IServiceProvider? _serviceLocator;

    private readonly TaskCompletionSource _initializedTcs = new();
    private ISettingsService? _settingsService;

    /// <summary>
    /// Antipattern. Use with caution!
    /// </summary>
    public static IServiceProvider ServiceLocator => App._serviceLocator ?? throw new NotInitializedException(nameof(App.ServiceLocator));

    private ISettingsService SettingsService => this._settingsService ?? throw new NotInitializedException(nameof(this.SettingsService));
    public IObservable<LogEvent> ObservableLog { get; set; } = Observable.Empty<LogEvent>();

    public IObservable<ThemeVariant> Theme => Observable.DeferAsync(async _ =>
    {
        await this._initializedTcs.Task;
        return this.SettingsService.AppSettings.Select(a => a.Theme switch
        {
            Settings.Theme.Default => ThemeVariant.Default,
            Settings.Theme.Light => ThemeVariant.Light,
            Settings.Theme.Dark => ThemeVariant.Dark,
            _ => throw new SwitchExpressionException(a.Theme),
        });
    }).DistinctUntilChanged();

    public static IList<IViewModel> ViewModelStack { get; } = [];


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
        try
        {
            var builderContainer = Registry.Register();

            builderContainer.RegisterInstance(this.ObservableLog);

            App._serviceLocator = builderContainer.Resolve<IServiceProvider>();
            this._settingsService = builderContainer.Resolve<ISettingsService>();
            var vm = builderContainer.Resolve<MainWindowViewModel>();

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

            this.Application_SetCurrentTheme();
        }
        catch (Exception e)
        {
            App.EarlyStop(e).FireAndForget();
            return;
        }

        base.OnFrameworkInitializationCompleted();
        this._initializedTcs.SetResult();

        App.ToRxAppUnhandledExceptionAsync(this.LoadAsync);
    }

    private static async Task EarlyStop(Exception exception)
    {
        if (exception is TargetInvocationException ti)
        {
            exception = ti.InnerException ?? ti;
        }

        var box = MessageBoxManager.GetMessageBoxStandard("Start up exception", exception.ToString(), ButtonEnum.Ok, Icon.Error);
        await box.ShowAsync();
        Environment.Exit(-1);
    }

    private static async void ToRxAppUnhandledExceptionAsync(Func<Task> func)
    {
        try
        {
            await func();
        }
        catch (Exception ex)
        {
            RxApp.DefaultExceptionHandler.OnNext(ex);
            RxApp.DefaultExceptionHandler.OnNext(ex);
        }
    }

    private void Application_SetCurrentTheme(object? sender = null, EventArgs? e = null)
    {
        this._settingsService!.CurrentTheme = this.ActualThemeVariant.Key switch
        {
            nameof(ThemeVariant.Light) => Settings.Theme.Light,
            nameof(ThemeVariant.Dark) => Settings.Theme.Dark,
            _ => throw new SwitchExpressionException(this.ActualThemeVariant.Key),
        };
    }
}
