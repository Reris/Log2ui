using ReactiveUI;

namespace Log2ui.Views;

public interface IViewModel : IReactiveNotifyPropertyChanged<IReactiveObject>, IHandleObservableErrors, IReactiveObject;
