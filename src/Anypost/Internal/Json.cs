using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anypost.Internal;

/// <summary>
/// The single <see cref="JsonSerializerOptions"/> used for every request and
/// response: snake_case property names, omit nulls when writing, tolerate unknown
/// response fields, and map string-backed enums via their wire values.
/// </summary>
internal static class Json
{
    internal static readonly JsonSerializerOptions Options = Build();

    private static JsonSerializerOptions Build()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DictionaryKeyPolicy = null, // dictionary keys (headers, variables) pass through verbatim
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };
        options.Converters.Add(new StringEnumConverterFactory());
        return options;
    }

    internal static byte[] SerializeToUtf8Bytes<T>(T value) =>
        JsonSerializer.SerializeToUtf8Bytes(value, Options);

    internal static T? Deserialize<T>(byte[] utf8Json) =>
        JsonSerializer.Deserialize<T>(utf8Json, Options);
}
