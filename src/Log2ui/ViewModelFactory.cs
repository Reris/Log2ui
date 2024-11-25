using System;
using Log2ui.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui;

public class ViewModelFactory(IServiceProvider serviceProvider) : IViewModelFactory
{
    public T Create<T>(params object[] dependencies)
        where T : ViewModel
    {
        var scope = serviceProvider.CreateScope();
        return ActivatorUtilities.CreateInstance<T>(scope.ServiceProvider, dependencies);
    }
}
