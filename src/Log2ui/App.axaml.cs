using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DryIoc;
using Log2ui.Data;
using Log2ui.Dependencies;
using Log2ui.Receivers;
using Log2ui.Views;
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

        var builderContainer = Registry.Register();
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
    }
}
