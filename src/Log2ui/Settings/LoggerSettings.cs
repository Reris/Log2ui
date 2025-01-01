using System;
using System.ComponentModel;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Log2ui.Settings;

public record LoggerSettings : INotifyPropertyChanged
{
    private string _timeStampFormatString = "G";

    [Category("Logging")]
    [Description("When greater than 0, the log messages are limited to that number.")]
    [DisplayName("Message Cycle Count")]
    public uint MessageCycleCount { get; set; }

    [Category("Logging")]
    [Description("Defines the format to be used to display the log message timestamps (cf. DateTime.ToString(format) in the .NET Framework.")]
    [DisplayName("TimeStamp Format String")]
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

    [Category("Logging")]
    [Description("When a logger is enabled or disabled, do the same for all child loggers.")]
    [DisplayName("Recursively Enable Loggers")]
    public bool RecursivlyEnableLoggers { get; set; } = true;

    [Category("Message Details")]
    [DisplayName("Details information")]
    [Description("Configure which information to Display in the message details")]
    public FieldType[] MessageDetailConfiguration { get; set; }

    [Category("Message Details")]
    [Description("Show or hide the message properties in the message details panel.")]
    [DisplayName("Show Properties")]
    public bool ShowMsgDetailsProperties { get; set; } = true;

    [Category("Message Details")]
    [Description("Show or hide the exception in the message details panel.")]
    [DisplayName("Show Exception")]
    public bool ShowMsgDetailsException { get; set; } = true;

    [Category("Behavior")]
    [Description("Automatically scroll to the last log message.")]
    [DisplayName("Auto Scroll to Last Log")]
    public bool AutoScrollToLastLog { get; set; } = true;

    [Category("Behavior")]
    [Description("Highlight the Logger of the selected Log Message.")]
    [DisplayName("Highlight Logger")]
    public bool HighlightLogger { get; set; } = true;

    [Category("Behavior")]
    [Description("Highlight the Log Messages of the selected Logger.")]
    [DisplayName("Highlight Log Messages")]
    public bool HighlightLogMessages { get; set; } = true;

    [Category("Style")]
    [Description(".")]
    [DisplayName("Use Default Style")]
    public bool UseDefaultStyle
    {
        get => this.Style is null;
        set
        {
            if (this.UseDefaultStyle != value)
            {
                this.Style = value ? null : this.Style ?? LoggerStyleSettings.Default.DeepClone();
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.UseDefaultStyle)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Style)));
            }
        }
    }

    [Browsable(false)]
    public LoggerStyleSettings? Style { get; set; }

    public static LoggerSettings Default { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public LoggerSettings DeepClone()
    {
        return this with { Style = this.Style?.DeepClone() };
    }
}
