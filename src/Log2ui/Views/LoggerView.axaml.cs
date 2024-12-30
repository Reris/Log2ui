using System.Linq;
using Avalonia.Controls;
using Log2ui.Data;
using Log2ui.Extensions;

namespace Log2ui.Views;

public partial class LoggerView : View<LoggerViewModel>
{
    public LoggerView()
    {
        this.InitializeComponent();

        this.MessageDataGrid.SelectionChanged += this.ScrollToSelected;
        this.MessageDataGrid.RegisterUnselector();
        this.MessageDataGrid.BuildClassTrigger<LogMessageItem>()
            .Attach(a => a.Highlight, "highlighted");
    }

    private void ScrollToSelected(object? sender, SelectionChangedEventArgs e)
    {
        var item = e.AddedItems.Cast<object>().LastOrDefault();
        if (item is not null)
        {
            this.MessageDataGrid.ScrollIntoView(item, this.MessageDataGrid.CurrentColumn);
        }
    }

    private void MessageDataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        (this.DataContext as LoggerViewModel)?.UpdateSelectedMessageText();
    }
}
