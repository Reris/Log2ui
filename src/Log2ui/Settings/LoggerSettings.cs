using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Log2ui.Collections;
using Log2ui.Data;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Log2ui.Settings;

public record LoggerSettings : INotifyPropertyChanged
{
    private const string DefaultTimeStampFormatString = "yyyy-MM-dd HH:mm:ss.ffff";
    private bool _autoScrollToLastLog = true;
    private EquatableArray<LogColumn>? _columns;
    private bool _highlightLogger = true;
    private bool _highlightLogMessages = true;
    private bool _loggerTreeEnableRecursivly = true;
    private uint _messageCycleCount;
    private EquatableArray<ReceiverSettings> _receivers = [];
    private bool _showLoggerTree = true;
    private bool _showMsgDetails = true;
    private bool _showMsgDetailsException = true;
    private bool _showMsgDetailsProperties = true;
    private LoggerStyleSettings? _style;
    private string _timeStampFormatString = LoggerSettings.DefaultTimeStampFormatString;
    private bool _useDefaultColumns = true;
    private bool _useDefaultStyle = true;

    public static LoggerSettings Default { get; } = new()
    {
        Columns = new EquatableArray<LogColumn>(
            new LogColumn("Level", LogPropertyType.LogLevel, nameof(LogMessage.Level)),
            new LogColumn("Time", LogPropertyType.DateTime, nameof(LogMessage.TimeStamp)),
            new LogColumn("Logger", LogPropertyType.LoggerName, nameof(LogMessage.LoggerName)),
            new LogColumn("Message", LogPropertyType.String, nameof(LogMessage.Message))),
    };

    [Category("Logging")]
    [DisplayName("Message Cycle Count")]
    [Description("When greater than 0, the log messages are limited to that number.")]
    public uint MessageCycleCount
    {
        get => this._messageCycleCount;
        set => this.SetField(ref this._messageCycleCount, value);
    }

    [Category("Logging")]
    [DisplayName("TimeStamp Format String")]
    [Description("Defines the format to be used to display the log message timestamps (cf. DateTime.ToString(format) in the .NET Framework.")]
    public string TimeStampFormatString
    {
        get => this._timeStampFormatString;
        set
        {
            try
            {
                _ = DateTime.Now.ToString(value); // If error, will throw FormatException
                this.SetField(ref this._timeStampFormatString, value);
            }
            catch (FormatException ex)
            {
                MessageBoxManager.GetMessageBoxStandard("Error Configuring Columns", ex.Message, ButtonEnum.Ok, Icon.Error);
                this._timeStampFormatString = LoggerSettings.DefaultTimeStampFormatString; // Back to default
            }
        }
    }

    [Category("Logger Tree")]
    [DisplayName("Show the logger tree.")]
    [Description("Show the logger tree for contextual named loggers.")]
    public bool ShowLoggerTree
    {
        get => this._showLoggerTree;
        set => this.SetField(ref this._showLoggerTree, value);
    }

    [Category("Logger Tree")]
    [DisplayName("Recursively Enable Loggers")]
    [Description("When a logger is enabled or disabled, do the same for all child loggers.")]
    public bool LoggerTreeEnableRecursivly
    {
        get => this._loggerTreeEnableRecursivly;
        set => this.SetField(ref this._loggerTreeEnableRecursivly, value);
    }

    [Category("Message Details")]
    [DisplayName("Show message details")]
    [Description("Configure if the message details are shown")]
    public bool ShowMsgDetails
    {
        get => this._showMsgDetails;
        set => this.SetField(ref this._showMsgDetails, value);
    }

    [Category("Message Details")]
    [DisplayName("Show Properties")]
    [Description("Show or hide the message properties in the message details panel.")]
    public bool ShowMsgDetailsProperties
    {
        get => this._showMsgDetailsProperties;
        set => this.SetField(ref this._showMsgDetailsProperties, value);
    }

    [Category("Message Details")]
    [DisplayName("Show Exception")]
    [Description("Show or hide the exception in the message details panel.")]
    public bool ShowMsgDetailsException
    {
        get => this._showMsgDetailsException;
        set => this.SetField(ref this._showMsgDetailsException, value);
    }

    [Category("Behavior")]
    [DisplayName("Auto Scroll to Last Log")]
    [Description("Automatically scroll to the last log message.")]
    public bool AutoScrollToLastLog
    {
        get => this._autoScrollToLastLog;
        set => this.SetField(ref this._autoScrollToLastLog, value);
    }

    [Category("Behavior")]
    [DisplayName("Highlight Logger")]
    [Description("Highlight the Logger of the selected Log Message.")]
    public bool HighlightLogger
    {
        get => this._highlightLogger;
        set => this.SetField(ref this._highlightLogger, value);
    }

    [Category("Behavior")]
    [DisplayName("Highlight Log Messages")]
    [Description("Highlight the Log Messages of the selected Logger.")]
    public bool HighlightLogMessages
    {
        get => this._highlightLogMessages;
        set => this.SetField(ref this._highlightLogMessages, value);
    }

    [Category("Columns")]
    [DisplayName("Use Default Columns")]
    [Description("Use the app settings default columns")]
    [JsonIgnore]
    public bool UseDefaultColumns
    {
        get => this._useDefaultColumns;
        set => this.SetField(ref this._useDefaultColumns, value);
    }

    [Category("Columns")]
    [DisplayName("Columns")]
    [Description("Customize shown columns")]
    public EquatableArray<LogColumn>? Columns
    {
        get => this._columns;
        set => this.SetField(ref this._columns, value);
    }

    [Browsable(false)]
    public EquatableArray<ReceiverSettings> Receivers
    {
        get => this._receivers;
        set => this.SetField(ref this._receivers, value);
    }

    [Category("Style")]
    [DisplayName("Use Default Style")]
    [Description("Use the app settings default style")]
    [JsonIgnore]
    public bool UseDefaultStyle
    {
        get => this._useDefaultStyle;
        set => this.SetField(ref this._useDefaultStyle, value);
    }

    [Browsable(false)]
    public LoggerStyleSettings? Style
    {
        get => this._style;
        set => this.SetField(ref this._style, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public LoggerSettings DeepClone()
    {
        return this with
        {
            Style = this.Style?.DeepClone(),
            Columns = this.Columns is null ? null : [..this.Columns.Value.Select(a => a.DeepClone())],
            Receivers = [..this.Receivers.Select(a => a.DeepClone())],
        };
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        this.OnPropertyChanged(propertyName);
        return true;
    }
}
