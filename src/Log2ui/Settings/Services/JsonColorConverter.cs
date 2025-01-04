using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Media;

namespace Log2ui.Settings.Services;

public class JsonColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var hex = reader.GetString() ?? "0";
        var i = uint.Parse(hex, NumberStyles.HexNumber);
        var color = Color.FromUInt32(i);
        return color;
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUInt32().ToString("X"));
    }
}
