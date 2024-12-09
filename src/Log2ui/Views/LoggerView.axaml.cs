using System.Linq;
using Avalonia.Controls;
using JetBrains.Annotations;
using Log2ui.Data;
using Log2ui.Extensions;

namespace Log2ui.Views;

[UsedImplicitly]
public partial class LoggerView : View
{
    public LoggerView()
    {
        this.InitializeComponent();

        this.DataGrid.SelectionChanged += this.ScrollToSelected;
        this.DataGrid.RegisterUnselector();
        this.DataGrid.BuildClassTrigger<LogMessageItem>()
            .Attach(a => a.Highlight, "highlighted");
    }

    private void ScrollToSelected(object? sender, SelectionChangedEventArgs e)
    {
        var item = e.AddedItems.Cast<object>().LastOrDefault();
        if (item is not null)
        {
            this.DataGrid.ScrollIntoView(item, this.DataGrid.CurrentColumn);
        }
    }
}
