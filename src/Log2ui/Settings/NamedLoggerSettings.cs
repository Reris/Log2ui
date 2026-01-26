using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using Log2ui.Collections;
using Log2ui.Settings.Validations;

namespace Log2ui.Settings;

public record NamedLoggerSettings : LoggerSettings
{
    [Category("Logging")]
    [Description("Name of the logger")]
    [DisplayName("Logger Name")]
    [Required(AllowEmptyStrings = false)]
    [ValidateLoggerName]
    public string Name { get; set; } = "";

    [Browsable(false)]
    [JsonIgnore]
    public string OriginalName { get; init; } = "";

    [Browsable(false)]
    public EquatableArray<string> ReceiverKeys { get; set; } = new();

    public new NamedLoggerSettings DeepClone()
    {
        return this with
        {
            Style = this.Style?.DeepClone(),
            Columns = this.Columns is null ? null : new EquatableArray<LogColumn>([.. this.Columns.Value.Select(a => a.DeepClone())]),
            ReceiverKeys = EquatableArray.Create(this.ReceiverKeys),
        };
    }
}
