using System.Collections.Generic;
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
}
