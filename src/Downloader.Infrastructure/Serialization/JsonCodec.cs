using System.Text.Json;
using System.Text.Json.Serialization;

namespace YouTubeDownloader;
// Explicitly maps untyped provider JSON to bounded primitive/container values.
public sealed class JsonCodec
{
    public int MaxJsonLength { get; set; } = 32 * 1024 * 1024;

    private static readonly JsonSerializerOptions Options = CreateOptions();
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            IncludeFields = true,
            MaxDepth = 64,
            PropertyNameCaseInsensitive = false
        };
        options.Converters.Add(new PrimitiveConverter());
        return options;
    }

    public T Deserialize<T>(string json)
    {
        if (json == null || json.Length > MaxJsonLength)
            throw new InvalidDataException("The data is too large.");
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    public string Serialize(object value)
    {
        var json = JsonSerializer.Serialize(value, Options);
        if (json.Length > MaxJsonLength)
            throw new InvalidDataException("The data is too large.");
        return json;
    }

    private sealed class PrimitiveConverter : JsonConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            return Value(document.RootElement);
        }

        private static object Value(JsonElement value) => value.ValueKind switch
        {
            JsonValueKind.Object => value.EnumerateObject().ToDictionary(p => p.Name, p => Value(p.Value)),
            JsonValueKind.Array => value.EnumerateArray().Select(Value).ToArray(),
            JsonValueKind.String => value.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => value.TryGetInt64(out var integer) ? (object)integer : value.GetDouble(),
            _ => null
        };
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
