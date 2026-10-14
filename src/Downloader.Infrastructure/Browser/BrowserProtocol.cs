using System.Buffers.Binary;
using System.Text.Json;

namespace YouTubeDownloader;
public static class BrowserProtocol
{
    public const int MaximumBytes = 65536;
    public static async Task<JsonDocument> ReadAsync(Stream input, CancellationToken token)
    {
        var header = new byte[4];
        await input.ReadExactlyAsync(header, token).ConfigureAwait(false);
        uint count = BinaryPrimitives.ReadUInt32LittleEndian(header);
        if (count == 0 || count > MaximumBytes) throw new InvalidDataException("The browser message is too large or empty.");
        var body = new byte[count];
        await input.ReadExactlyAsync(body, token).ConfigureAwait(false);
        return JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 8 });
    }
    public static async Task WriteAsync(Stream output, object value, CancellationToken token)
    {
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(value);
        if (body.Length > MaximumBytes) throw new InvalidDataException("The browser response is too large.");
        var header = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(header, (uint)body.Length);
        await output.WriteAsync(header, token).ConfigureAwait(false);
        await output.WriteAsync(body, token).ConfigureAwait(false);
        await output.FlushAsync(token).ConfigureAwait(false);
    }
    public static string ReadUrl(JsonElement message)
    {
        if (message.ValueKind != JsonValueKind.Object || message.EnumerateObject().Count() != 3 ||
            !message.TryGetProperty("version", out var version) || !version.TryGetInt32(out int number) || number != 1 ||
            !message.TryGetProperty("type", out var type) || type.GetString() != "open-link" ||
            !message.TryGetProperty("url", out var url) || url.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("The browser message is not supported.");
        return MediaLink.PublicUrl(url.GetString());
    }
}
