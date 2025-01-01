using System.ComponentModel;

namespace Log2ui.Settings;

public record AppSettings
{
    public static AppSettings Default { get; } = new();

    [Category("Appearance")]
    [Description("The Log2ui theme.")]
    [DisplayName("Theme")]
    public Theme Theme { get; set; }

    [Category("Appearance")]
    [Description("The Log2ui window will remain on top of all other windows.")]
    [DisplayName("Always On Top")]
    public bool AlwaysOnTop { get; set; }

    [Browsable(false)]
    public LoggerSettings LoggerDefaults { get; set; } = LoggerSettings.Default;

    public AppSettings DeepClone()
    {
        return this with { LoggerDefaults = this.LoggerDefaults.DeepClone() };
    }
}
