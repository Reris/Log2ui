using Avalonia.PropertyGrid.Controls;
using Log2ui.Extensions;

namespace Log2ui.Views;

public partial class LoggerSettingsView : View<LoggerSettingsViewModel>
{
    public LoggerSettingsView()
    {
        this.InitializeComponent();
    }

    private void PropertyGrid_OnCommandExecuted(object? sender, RoutedCommandExecutedEventArgs e)
    {
        this.InvokeLatest(vm => vm.SaveAsync());
    }
}
