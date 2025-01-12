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
    [DisplayName("Log List Background")]
    [Description("Set the Background Color of the Log List View.")]
    public Color LogListBackColor { get; set; }

    [Category("Colors")]
    [DisplayName("Log Details Background")]
    [Description("Set the Background Color of the Log Message Details.")]
    public Color LogMessageBackColor { get; set; }

    [Category("Log Level Colors")]
    [DisplayName("1 - Trace")]
    [Description("Set the Trace Level Color.")]
    public Color TraceLevelColor { get; set; }

    [Category("Log Level Colors")]
    [DisplayName("2 - Debug")]
    [Description("Set the Debug Level Color.")]
    public Color DebugLevelColor { get; set; }

    [Category("Log Level Colors")]
    [DisplayName("3 - Info")]
    [Description("Set the Info Level Color.")]
    public Color InfoLevelColor { get; set; }

    [Category("Log Level Colors")]
    [DisplayName("4 - Warning")]
    [Description("Set the Warning Level Color.")]
    public Color WarnLevelColor { get; set; }

    [Category("Log Level Colors")]
    [DisplayName("5 - Error")]
    [Description("Set the Error Level Color.")]
    public Color ErrorLevelColor { get; set; }

    [Category("Log Level Colors")]
    [DisplayName("6 - Fatal")]
    [Description("Set the Fatal Level Color.")]
    public Color FatalLevelColor { get; set; }

    public LoggerStyleSettings DeepClone()
    {
        return this with { };
    }
}
