using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System;
using System.ComponentModel;

namespace Log2ui.Settings;

public record AppSettings
{
    public static AppSettings Default { get; } = new();

    [Category("Appearance")]
    [Description("The Log2ui window will remain on top of all other windows.")]
    [DisplayName("Always On Top")]
    public bool AlwaysOnTop { get; set; }

    [Category("Logging")]
    [Description("The Log2ui window will remain on top of all other windows.")]
    [DisplayName("Always On Top")]
    public LoggerSettings LoggerDefaults { get; set; }
}

public record LoggerSettings
{
    private string _timeStampFormatString;

    [Category("Notification")]
    [Description("Automatically scroll to the last log message.")]
    [DisplayName("Auto Scroll to Last Log")]
    public bool AutoScrollToLastLog { get; set; }

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
    public bool RecursivlyEnableLoggers { get; set; }

    [Category("Message Details")]
    [DisplayName("Details information")]
    [Description("Configure which information to Display in the message details")]
    public FieldType[] MessageDetailConfiguration { get; set; }

    [Category("Message Details")]
    [Description("Show or hide the exception in the message details panel.")]
    [DisplayName("Show Exception")]
    public bool ShowMsgDetailsException { get; set; }
}
