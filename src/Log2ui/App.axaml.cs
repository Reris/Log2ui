using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Log2ui.Data;
using Log2ui.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        LogLevels.Init();

        var iocFactory = new DryIocServiceProviderFactory();

        // Register all the services needed for the application to run
        var collection = new ServiceCollection();
        collection.AddMainServices();
        collection.AddViewModels();

        var container = iocFactory.CreateBuilder(collection);

        // Creates a ServiceProvider containing services from the provided IServiceCollection
        var services = iocFactory.CreateServiceProvider(container);

        var vm = services.GetRequiredService<MainWindowViewModel>();
        switch (this.ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow
                {
                    DataContext = vm
                };
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainWindow
                {
                    DataContext = vm
                };
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
