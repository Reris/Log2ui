using Log2ui.Views;

namespace Log2ui;

public interface IViewModelFactory
{
    T Create<T>(params object[] dependencies)
        where T : ViewModel;
}
