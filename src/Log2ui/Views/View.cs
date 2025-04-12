using System.Collections.Generic;
using Avalonia;
using Avalonia.ReactiveUI;
using Log2ui.Extensions;

namespace Log2ui.Views;

public class View<TViewModel> : ReactiveUserControl<TViewModel>, ViewExtensions.IInvoking
    where TViewModel : class, IViewModel
{
    IDictionary<string, IList<ViewExtensions.Invocation>> ViewExtensions.IInvoking.Invocations { get; }
        = new Dictionary<string, IList<ViewExtensions.Invocation>>();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ReactiveUserControl<TViewModel>.ViewModelProperty && change.OldValue != change.NewValue)
        {
            this.OnViewModelChanged((TViewModel?)change.OldValue, (TViewModel?)change.NewValue);
        }
    }

    protected virtual void OnViewModelChanged(TViewModel? oldValue, TViewModel? newValue)
    {
        if (oldValue is not null)
        {
            App.ViewModelStack.Remove(oldValue);
        }

        if (newValue is not null)
        {
            App.ViewModelStack.Add(newValue);
        }

        if (newValue is ILoading loadable)
        {
            this.IsEnabled = false;
            this.InvokeLatest(
                loadable,
                async vm =>
                {
                    await vm.Loading;
                    this.IsEnabled = true;
                });
        }
    }
}
