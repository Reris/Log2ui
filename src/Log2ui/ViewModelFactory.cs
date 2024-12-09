using DryIoc;
using Log2ui.Views;

namespace Log2ui;

public class ViewModelFactory(IContainer container) : IViewModelFactory
{
    public T Create<T>(params object[] dependencies)
        where T : IViewModel
    {
        var scope = container.CreateChild();
        return scope.Resolve<T>(dependencies);
    }
}
