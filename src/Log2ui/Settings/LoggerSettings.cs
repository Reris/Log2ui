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

    public static LoggerSettings Default { get; } = new()
    {
        Columns = new EquatableArray<LogColumn>(
            new LogColumn("Level", LogPropertyType.LogLevel, nameof(LogMessage.Level)),
            new LogColumn("Time", LogPropertyType.DateTime, nameof(LogMessage.TimeStamp)),
            new LogColumn("Logger", LogPropertyType.LoggerName, nameof(LogMessage.LoggerName)),
            new LogColumn("Message", LogPropertyType.String, nameof(LogMessage.Message))),
    };

    [Category("Logging")]
    [DisplayName("Default Log Level")]
    [Description("Default Log Level at which the logger starts")]
    public LogLevel DefaultMinLogLevel
    {
        get;
        set => this.SetField(ref field, value);
    } = LogLevel.Trace;

    [Category("Logging")]
    [DisplayName("Message Cycle Count")]
    [Description("When greater than 0, the log messages are limited to that number.")]
    public uint MessageCycleCount
    {
        get;
        set => this.SetField(ref field, value);
    }

    [Category("Logging")]
    [DisplayName("TimeStamp Format String")]
    [Description("Defines the format to be used to display the log message timestamps (cf. DateTime.ToString(format) in the .NET Framework.")]
    public string TimeStampFormatString
    {
        get;
        set
        {
            try
            {
                _ = DateTime.Now.ToString(value); // If error, will throw FormatException
                this.SetField(ref field, value);
            }
            catch (FormatException ex)
            {
                MessageBoxManager.GetMessageBoxStandard("Error Configuring Columns", ex.Message, ButtonEnum.Ok, Icon.Error);
                field = LoggerSettings.DefaultTimeStampFormatString; // Back to default
            }
        }
    } = LoggerSettings.DefaultTimeStampFormatString;

    [Category("Logger Tree")]
    [DisplayName("Show the logger tree.")]
    [Description("Show the logger tree for contextual named loggers.")]
    public bool ShowLoggerTree
    {
        get;
        set => this.SetField(ref field, value);
    } = true;

    [Category("Logger Tree")]
    [DisplayName("Recursively Enable Loggers")]
    [Description("When a logger is enabled or disabled, do the same for all child loggers.")]
    public bool LoggerTreeEnableRecursivly
    {
        get;
        set => this.SetField(ref field, value);
    } = true;

    [Category("Message Details")]
    [DisplayName("Show message details")]
    [Description("Configure if the message details are shown")]
    public bool ShowMsgDetails
    {
        get;
        set => this.SetField(ref field, value);
    } = true;

    [Category("Message Details")]
    [DisplayName("Show Properties")]
    [Description("Show or hide the message properties in the message details panel.")]
    public bool ShowMsgDetailsProperties
    {
        get;
        set => this.SetField(ref field, value);
    } = true;

    [Category("Message Details")]
    [DisplayName("Show Exception")]
    [Description("Show or hide the exception in the message details panel.")]
    public bool ShowMsgDetailsException
    {
        get;
        set => this.SetField(ref field, value);
    } = true;

    [Category("Behavior")]
    [DisplayName("Auto Scroll to Last Log")]
    [Description("Automatically scroll to the last log message.")]
    public bool AutoScrollToLastLog
    {
        get;
        set => this.SetField(ref field, value);
    } = true;

    [Category("Behavior")]
    [DisplayName("Highlight Logger")]
    [Description("Highlight the Logger of the selected Log Message.")]
    public bool HighlightLogger
    {
        get;
        set => this.SetField(ref field, value);
    } = true;

    [Category("Behavior")]
    [DisplayName("Highlight Log Messages")]
    [Description("Highlight the Log Messages of the selected Logger.")]
    public bool HighlightLogMessages
    {
        get;
        set => this.SetField(ref field, value);
    } = true;

    [Category("Columns")]
    [DisplayName("Use Default Columns")]
    [Description("Use the app settings default columns")]
    [JsonIgnore]
    public bool UseDefaultColumns
    {
        get;
        set => this.SetField(ref field, value);
    } = true;

    [Category("Columns")]
    [DisplayName("Columns")]
    [Description("Customize shown columns")]
    public EquatableArray<LogColumn>? Columns
    {
        get;
        set => this.SetField(ref field, value);
    }

    [Category("Style")]
    [DisplayName("Use Default Style")]
    [Description("Use the app settings default style")]
    [JsonIgnore]
    public bool UseDefaultStyle
    {
        get;
        set => this.SetField(ref field, value);
    } = true;

    [Browsable(false)]
    public LoggerStyleSettings? Style
    {
        get;
        set => this.SetField(ref field, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public LoggerSettings DeepClone()
    {
        return this with
        {
            Style = this.Style?.DeepClone(),
            Columns = this.Columns is null ? null : [..this.Columns.Value.Select(a => a.DeepClone())],
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
