using DryIoc;
using Log2ui.Dependencies;
using Log2ui.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Log2ui;

public class ViewModelFactory(IContainer container) : IViewModelFactory, ISelfRegistering
{
    static void ISelfRegistering.RegisterServices(Registry registry)
    {
        registry.Collection.AddSingleton<IViewModelFactory, ViewModelFactory>();
    }

    public T Create<T>(params object[] dependencies)
        where T : IViewModel
    {
        var scope = container.CreateChild();
        return scope.Resolve<T>(dependencies);
    }
}
