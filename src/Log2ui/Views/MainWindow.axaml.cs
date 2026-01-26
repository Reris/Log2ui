using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Log2ui.Extensions;
using ReactiveUI.Avalonia;

namespace Log2ui.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>, ViewExtensions.IInvoking
{
    public MainWindow()
    {
        this.InitializeComponent();
    }

    IDictionary<string, IList<ViewExtensions.Invocation>> ViewExtensions.IInvoking.Invocations { get; }
        = new Dictionary<string, IList<ViewExtensions.Invocation>>();

    private void TabsControl_OnLoaded(object? sender, RoutedEventArgs e)
    {
        var tabsControl = sender as Control;
        var border = tabsControl!.GetVisualChildren().OfType<Border>().Single();
        border.Background = null;
    }
}
