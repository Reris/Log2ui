using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Log2ui.Settings;

public record LoggerSettings : INotifyPropertyChanged
{
    private ImmutableArray<ReceiverSettings> _receivers = [];
    private LoggerStyleSettings? _style;
    private string _timeStampFormatString = "G";

    [Category("Logging")]
    [DisplayName("Message Cycle Count")]
    [Description("When greater than 0, the log messages are limited to that number.")]
    public uint MessageCycleCount { get; set; }

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
                this._timeStampFormatString = value;
            }
            catch (FormatException ex)
            {
                MessageBoxManager.GetMessageBoxStandard("Error Configuring Columns", ex.Message, ButtonEnum.Ok, Icon.Error);
                this._timeStampFormatString = "G"; // Back to default
            }
        }
    }

    [Category("Logger Tree")]
    [DisplayName("Show the logger tree.")]
    [Description("Show the logger tree for contextual named loggers.")]
    public bool ShowLoggerTree { get; set; } = true;

    [Category("Logger Tree")]
    [DisplayName("Recursively Enable Loggers")]
    [Description("When a logger is enabled or disabled, do the same for all child loggers.")]
    public bool LoggerTreeEnableRecursivly { get; set; } = true;

    [Category("Message Details")]
    [DisplayName("Show message details")]
    [Description("Configure if the message details are shown")]
    public bool ShowMsgDetails { get; set; } = true;

    [Category("Message Details")]
    [DisplayName("Show Properties")]
    [Description("Show or hide the message properties in the message details panel.")]
    public bool ShowMsgDetailsProperties { get; set; } = true;

    [Category("Message Details")]
    [DisplayName("Show Exception")]
    [Description("Show or hide the exception in the message details panel.")]
    public bool ShowMsgDetailsException { get; set; } = true;

    [Category("Behavior")]
    [DisplayName("Auto Scroll to Last Log")]
    [Description("Automatically scroll to the last log message.")]
    public bool AutoScrollToLastLog { get; set; } = true;

    [Category("Behavior")]
    [DisplayName("Highlight Logger")]
    [Description("Highlight the Logger of the selected Log Message.")]
    public bool HighlightLogger { get; set; } = true;

    [Category("Behavior")]
    [DisplayName("Highlight Log Messages")]
    [Description("Highlight the Log Messages of the selected Logger.")]
    public bool HighlightLogMessages { get; set; } = true;

    [Category("Style")]
    [DisplayName("Use Default Style")]
    [Description(".")]
    [JsonIgnore]
    public bool UseDefaultStyle { get; set; } = true;

    [Browsable(false)]
    public LoggerStyleSettings? Style
    {
        get => this._style;
        set => this.SetField(ref this._style, value);
    }

    [Browsable(false)]
    public ImmutableArray<ReceiverSettings> Receivers
    {
        get => this._receivers;
        set => this.SetField(ref this._receivers, value);
    }

    public static LoggerSettings Default { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public LoggerSettings DeepClone()
    {
        return this with { Style = this.Style?.DeepClone(), Receivers = [..this.Receivers.Select(a => a.DeepClone())] };
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
