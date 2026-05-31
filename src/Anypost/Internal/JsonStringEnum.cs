using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anypost.Internal;

/// <summary>
/// Maps an enum member to its wire string (e.g. <c>email.sent</c>,
/// <c>send_only</c>). Members without this attribute are not serializable; an
/// unrecognized wire value deserializes to the enum's <c>Unknown</c> member when
/// one exists, so a server that adds a new value does not break deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
internal sealed class JsonStringValueAttribute : Attribute
{
    public string Value { get; }

    public JsonStringValueAttribute(string value) => Value = value;
}

/// <summary>Maps a string-backed enum value to its wire string, for query parameters.</summary>
internal static class EnumWire
{
    internal static string? Value<TEnum>(TEnum? value) where TEnum : struct, Enum
    {
        if (value is null)
        {
            return null;
        }

        var field = typeof(TEnum).GetField(value.Value.ToString());
        return field?.GetCustomAttribute<JsonStringValueAttribute>()?.Value;
    }
}

/// <summary>
/// Produces a <see cref="StringEnumConverter{TEnum}"/> for any enum whose members
/// carry <see cref="JsonStringValueAttribute"/>. Registered once on the shared
/// <see cref="JsonSerializerOptions"/>.
/// </summary>
internal sealed class StringEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsEnum)
        {
            return false;
        }

        foreach (var field in typeToConvert.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetCustomAttribute<JsonStringValueAttribute>() is not null)
            {
                return true;
            }
        }

        return false;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(StringEnumConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>
/// Serializes a string-backed enum using its <see cref="JsonStringValueAttribute"/>
/// values. Unknown wire values map to the <c>Unknown</c> member when present.
/// </summary>
internal sealed class StringEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private static readonly Dictionary<string, TEnum> FromWire = new(StringComparer.Ordinal);
    private static readonly Dictionary<TEnum, string> ToWire = new();
    private static readonly bool HasUnknown;
    private static readonly TEnum UnknownValue;

    static StringEnumConverter()
    {
        foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var value = (TEnum)field.GetValue(null)!;
            var attr = field.GetCustomAttribute<JsonStringValueAttribute>();
            if (attr is not null)
            {
                FromWire[attr.Value] = value;
                ToWire[value] = attr.Value;
            }
            else if (field.Name == "Unknown")
            {
                HasUnknown = true;
                UnknownValue = value;
            }
        }
    }

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        if (raw is not null && FromWire.TryGetValue(raw, out var value))
        {
            return value;
        }

        if (HasUnknown)
        {
            return UnknownValue;
        }

        throw new JsonException($"Unexpected value '{raw}' for {typeof(TEnum).Name}.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        if (ToWire.TryGetValue(value, out var raw))
        {
            writer.WriteStringValue(raw);
            return;
        }

        throw new JsonException($"Cannot serialize {typeof(TEnum).Name}.{value}; it has no wire representation.");
    }
}
