using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Log2ui.Settings;

public record NamedLoggerSettings : LoggerSettings
{
    [Category("Logging")]
    [Description("Name of the logger")]
    [DisplayName("Logger Name")]
    [JsonIgnore]
    [Required(AllowEmptyStrings = false)]
    public string Name { get; set; } = "";

    [Browsable(false)]
    [JsonIgnore]
    public string OriginalName { get; init; } = "";

    public new NamedLoggerSettings DeepClone()
    {
        return this with { Style = this.Style?.DeepClone() };
    }
}
