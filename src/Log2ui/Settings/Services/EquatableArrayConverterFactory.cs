using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Log2ui.Collections;

namespace Log2ui.Settings.Services;

public class EquatableArrayConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
        {
            return false;
        }

        return typeToConvert.GetGenericTypeDefinition() == typeof(EquatableArray<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var elementType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(EquatableArrayConverter<>).MakeGenericType(elementType);
        return (JsonConverter?)Activator.CreateInstance(converterType) ?? throw new InvalidOperationException("Create Instance failed");
    }

    private class EquatableArrayConverter<T> : JsonConverter<EquatableArray<T>>
        where T : IEquatable<T>
    {
        public override EquatableArray<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected start of array.");
            }

            var array = JsonSerializer.Deserialize<T[]>(ref reader, options);
            return new EquatableArray<T>(array ?? []);
        }

        public override void Write(Utf8JsonWriter writer, EquatableArray<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
            {
                JsonSerializer.Serialize(writer, item, options);
            }

            writer.WriteEndArray();
        }
    }
}
