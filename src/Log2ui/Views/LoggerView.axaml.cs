using System.Linq;
using Avalonia;
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

    private void DataGridCellLogLevel_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        LoggerView.LogLevelAsClass(e, (e.OldValue as LogMessageItem)?.Message.Level, (e.NewValue as LogMessageItem)?.Message.Level);
    }

    private void StackPanelMinLogLevel_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        LoggerView.LogLevelAsClass(e, (e.OldValue as LogLevelInfo)?.Level, (e.NewValue as LogLevelInfo)?.Level);
    }

    private static void LogLevelAsClass(AvaloniaPropertyChangedEventArgs e, LogLevel? oldLevel, LogLevel? newLevel)
    {
        if (e.Property != StyledElement.DataContextProperty || e.Sender is not StyledElement element)
        {
            return;
        }

        if (oldLevel.HasValue)
        {
            element.Classes.Remove(oldLevel.Value.ToString());
        }

        if (newLevel.HasValue)
        {
            element.Classes.Add(newLevel.Value.ToString());
        }
    }
}
