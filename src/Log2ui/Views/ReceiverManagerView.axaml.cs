using System.Diagnostics.CodeAnalysis;
using Avalonia.Interactivity;
using Log2ui.Extensions;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

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

    [SuppressMessage("ReSharper", "AsyncVoidEventHandlerMethod")]
    private async void DeleteReceiver(object? sender, RoutedEventArgs e)
    {
        var box = MessageBoxManager.GetMessageBoxStandard("Receiver", "Are you sure you would like to delete this receiver?", ButtonEnum.YesNo);

        if (await box.ShowAsync() == ButtonResult.Yes)
        {
        }
    }
}
