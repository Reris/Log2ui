using System.ComponentModel;
using Avalonia.Media;

namespace Log2ui.Settings;

public record LoggerStyleSettings
{
    public static LoggerStyleSettings Dark { get; } = new()
    {
        LogListBackColor = Colors.Transparent,
        LogMessageBackColor = Colors.Transparent,
        TraceLevelColor = Colors.Gray,
        DebugLevelColor = Colors.Black,
        InfoLevelColor = Colors.Green,
        WarnLevelColor = Colors.Orange,
        ErrorLevelColor = Colors.Red,
        FatalLevelColor = Colors.Purple,
    };

    public static LoggerStyleSettings Light { get; } = new()
    {
        LogListBackColor = Colors.Transparent,
        LogMessageBackColor = Colors.Transparent,
        TraceLevelColor = Colors.Gray,
        DebugLevelColor = Colors.White,
        InfoLevelColor = Colors.Green,
        WarnLevelColor = Colors.Orange,
        ErrorLevelColor = Colors.Red,
        FatalLevelColor = Colors.Purple,
    };

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
