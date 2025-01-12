using System.ComponentModel;

namespace Log2ui.Settings;

public record AppSettings
{
    public static AppSettings Default { get; } = new();

    [Category("Appearance")]
    [DisplayName("Theme")]
    [Description("The Log2ui theme.")]
    public Theme Theme { get; set; }

    [Category("Appearance")]
    [DisplayName("Always On Top")]
    [Description("The Log2ui window will remain on top of all other windows.")]
    public bool AlwaysOnTop { get; set; }

    [Browsable(false)]
    public LoggerSettings LoggerDefaults { get; set; } = LoggerSettings.Default;

    public AppSettings DeepClone()
    {
        return this with { LoggerDefaults = this.LoggerDefaults.DeepClone() };
    }
}
