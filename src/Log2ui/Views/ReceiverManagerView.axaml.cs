using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
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
        var vm = this.ViewModel;
        var wnd = (Window?)this.GetVisualRoot();
        var current = vm?.CurrentSettings;
        if (vm is null || wnd is null || current is null)
        {
            return;
        }

        var count = vm.CountAttachedLoggers(current.Key);
        var loggers = count == 1 ? "1 logger" : $"{count} loggers";
        var box = MessageBoxManager.GetMessageBoxStandard(
            "Receiver",
            $"""
             Are you sure you would like to delete this receiver?
             '{current.DisplayName}' is currently attached to {loggers}.

             """,
            ButtonEnum.YesNo,
            Icon.Question);
        if (await box.ShowWindowDialogAsync(wnd) == ButtonResult.Yes)
        {
            await vm.DeleteCurrentReceiverAsync();
        }
    }
}
