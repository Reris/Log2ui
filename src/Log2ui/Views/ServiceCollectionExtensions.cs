using System.Collections.Generic;
using Avalonia.Threading;
using Log2ui.Collections;
using Log2ui.Data;
using Log2ui.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui.Views;

public static class ServiceCollectionExtensions
{
    public static void AddMainServices(this IServiceCollection collection)
    {
        collection.AddSingleton<IMainDispatcher>(new MainDispatcher(Dispatcher.UIThread));
        collection.AddSingleton<IViewModelFactory, ViewModelFactory>();
        collection.AddTransient<MainWindowViewModel>();
    }

    public static void AddViewModels(this IServiceCollection collection)
    {
        collection.AddTransient<ILoggerViewModel, LoggerViewModel>();
        collection.AddTransient<LogSearchViewModel>();
        collection.AddScoped<ICollectionView<LogMessageItem>, TypedCollectionView<LogMessageItem>>();
        collection.AddScoped<ICollectionView<LogMessageItem, IList<LogMessageItem>>>(a => a.GetRequiredService<ICollectionView<LogMessageItem>>());
    }
}
