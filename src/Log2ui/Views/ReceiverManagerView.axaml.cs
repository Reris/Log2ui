using Avalonia.Interactivity;
using Log2ui.Extensions;

namespace Log2ui.Views;

public partial class ReceiverManagerView : View<ReceiverManagerViewModel>
{
    public ReceiverManagerView()
    {
        this.InitializeComponent();
    }

    private void PropertyGrid_OnCommandExecuted(object? sender, RoutedEventArgs e)
    {
        this.InvokeLatest(vm => vm.SaveAsync());
    }
}
