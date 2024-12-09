using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Log2ui.Data;
using Log2ui.Receivers;
using Log2ui.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;

namespace Log2ui;

public class App : Application
{
    public IObservable<LogEvent> ObservableLog { get; set; } = Observable.Empty<LogEvent>();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        LogLevels.Init();

        var iocFactory = new DryIocServiceProviderFactory();

        // Register all the services needed for the application to run
        var collection = new ServiceCollection();
        collection.AddMainServices();
        collection.AddViewModels();

        var container = iocFactory.CreateBuilder(collection);
        var vm = container.Resolve<MainWindowViewModel>([new ObservableReceiver(this.ObservableLog)]);

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
    }
}
