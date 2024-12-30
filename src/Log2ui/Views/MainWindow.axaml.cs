using System.Collections.Generic;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Log2ui.Extensions;

namespace Log2ui.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>, ViewExtensions.IInvoking
{
    public MainWindow()
    {
        this.InitializeComponent();
    }

    IDictionary<string, IList<ViewExtensions.Invocation>> ViewExtensions.IInvoking.Invocations { get; }
        = new Dictionary<string, IList<ViewExtensions.Invocation>>();

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        this.IsEnabled = false;
        this.InvokeLatest(
            this.ViewModel,
            async vm =>
            {
                await vm.LoadAsync();
                this.IsEnabled = true;
            });
    }
}
