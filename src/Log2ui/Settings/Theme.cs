using System.Text.Json.Serialization;

namespace Log2ui.Settings;

[JsonConverter(typeof(JsonStringEnumConverter<Theme>))]
public enum Theme
{
    Default,
    Light,
    Dark,
}
