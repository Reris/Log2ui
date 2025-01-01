using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Avalonia.Media;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using PropertyModels.ComponentModel.DataAnnotations;

namespace Log2ui.Settings;

public record LoggerSettings : INotifyPropertyChanged
{
    private string _timeStampFormatString = "G";

    public static LoggerSettings Default { get; } = new();

    [Category("Logging")]
    [Description("Name of the logger")]
    [DisplayName("Logger Name")]
    [JsonIgnore]
    [VisibilityPropertyCondition(nameof(LoggerSettings.OriginalName), null!, LogicType = ConditionLogicType.Not)]
    public string? Name { get; set; }

    [Browsable(false)]
    [JsonIgnore]
    public string OriginalName { get; init; } = null!;

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
    [Description("Show or hide the message properties in the message details panel.")]
    [DisplayName("Show Properties")]
    public bool ShowMsgDetailsProperties { get; set; }

    [Category("Message Details")]
    [Description("Show or hide the exception in the message details panel.")]
    [DisplayName("Show Exception")]
    public bool ShowMsgDetailsException { get; set; }

    [Category("Behavior")]
    [Description("Automatically scroll to the last log message.")]
    [DisplayName("Auto Scroll to Last Log")]
    public bool AutoScrollToLastLog { get; set; }

    [Category("Behavior")]
    [Description("Highlight the Logger of the selected Log Message.")]
    [DisplayName("Highlight Logger")]
    public bool HighlightLogger { get; set; }

    [Category("Behavior")]
    [Description("Highlight the Log Messages of the selected Logger.")]
    [DisplayName("Highlight Log Messages")]
    public bool HighlightLogMessages { get; set; }

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

    public event PropertyChangedEventHandler? PropertyChanged;

    public LoggerSettings DeepClone()
    {
        return this with { Style = this.Style?.DeepClone() };
    }
}

public record LoggerStyleSettings
{
    public static LoggerStyleSettings Default { get; } = new();

    [Category("Colors")]
    [Description("Set the Background Color of the Log List View.")]
    [DisplayName("Log List Background")]
    public Color LogListBackColor { get; set; }

    [Category("Colors")]
    [Description("Set the Background Color of the Log Message Details.")]
    [DisplayName("Log Details Background")]
    public Color LogMessageBackColor { get; set; }

    [Category("Log Level Colors")]
    [Description("Set the Trace Level Color.")]
    [DisplayName("1 - Trace")]
    public Color TraceLevelColor { get; set; }

    [Category("Log Level Colors")]
    [Description("Set the Debug Level Color.")]
    [DisplayName("2 - Debug")]
    public Color DebugLevelColor { get; set; }

    [Category("Log Level Colors")]
    [Description("Set the Info Level Color.")]
    [DisplayName("3 - Info")]
    public Color InfoLevelColor { get; set; }

    [Category("Log Level Colors")]
    [Description("Set the Warning Level Color.")]
    [DisplayName("4 - Warning")]
    public Color WarnLevelColor { get; set; }

    [Category("Log Level Colors")]
    [Description("Set the Error Level Color.")]
    [DisplayName("5 - Error")]
    public Color ErrorLevelColor { get; set; }

    [Category("Log Level Colors")]
    [Description("Set the Fatal Level Color.")]
    [DisplayName("6 - Fatal")]
    public Color FatalLevelColor { get; set; }

    public LoggerStyleSettings DeepClone()
    {
        return this with { };
    }
}
