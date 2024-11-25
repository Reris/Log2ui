using Log2ui.Settings;
using ReactiveUI;

namespace Log2ui.Data;

/// <summary>
/// Describes a Log Message.
/// TODO: Make it disposable to dereference Item?
/// </summary>
public class LogMessageItem : ReactiveObject
{
    private bool _enabled = true;

    private bool _highlight;

    /// <summary>
    /// Logger Item Parent.
    /// </summary>
    public LoggerItem Parent;

    /// <summary>
    /// The item before this one, allow to retrieve the order of arrival (time is not reliable here).
    /// The previous item is not necessary a sibling in the logger tree, only in the message list view.
    /// </summary>
    public LogMessageItem Previous;


    public LogMessageItem(LoggerItem parent, LogMessage logMessage)
    {
        this.Parent = parent;
        this.Message = logMessage;
        var toolTip = string.Empty;
        var msg = logMessage.Message?.Replace("\r\n", " ")?.Replace("\n", " ");
        toolTip = msg;

        // TODO: Die an die richtige Stelle
        //logMessage.TimeStamp.ToString(UserSettings.Instance.TimeStampFormatString);
        //logMessage.ExceptionString.Replace("\r\n", " ").Replace("\n", " ");

        //Add all the Properties in the Message to the ListViewItem
        foreach (var property in logMessage.Properties)
        {
            var propertyKey = property.Key;
            if (UserSettings.Instance.ColumnProperties.ContainsKey(propertyKey))
            {
                var propertyColumnNumber = UserSettings.Instance.ColumnProperties[propertyKey];
            }
        }

        this.ToolTipText = toolTip;
    }

    /// <summary>
    /// Indicates if this Log Message Item is enable.
    /// When disabled the List View Item is not in the Log List View.
    /// </summary>
    public bool Enabled
    {
        get => this._enabled;
        set => this.RaiseAndSetIfChanged(ref this._enabled, value);
    }

    /// <summary>
    /// Log Message.
    /// </summary>
    public LogMessage Message { get; }

    public string ToolTipText { get; set; }

    public bool Highlight
    {
        get => this._highlight;
        set => this.RaiseAndSetIfChanged(ref this._highlight, value);
    }
}
