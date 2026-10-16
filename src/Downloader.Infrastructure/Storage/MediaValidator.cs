using System.Globalization;
using System.Text;
using System.Text.Json;

namespace YouTubeDownloader;
public static class MediaValidator
{
    public static void Validate(string file, DownloadOptions options, ToolSet tools, CancellationToken token)
    {
        var text = new StringBuilder();
        using var runner = new WindowsProcessRunner();
        int code = runner.Run(tools.FFprobe, new[] { "-v", "error", "-show_entries", "format=format_name,duration:stream=codec_type", "-of", "json", "-i", file }, line => text.AppendLine(line), null, token, TimeSpan.FromSeconds(15), 1024 * 1024);
        if (code != 0) throw new InvalidDataException("The downloaded file could not be verified. Try another quality.");
        using var document = JsonDocument.Parse(text.ToString());
        if (!Matches(document.RootElement, options)) throw new InvalidDataException("The downloaded file could not be verified. Try another quality.");
    }
    internal static bool Matches(JsonElement root, DownloadOptions options)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("format", out var format) || format.ValueKind != JsonValueKind.Object || !format.TryGetProperty("format_name", out var container) || !format.TryGetProperty("duration", out var duration) ||
            !root.TryGetProperty("streams", out var streams) || streams.ValueKind != JsonValueKind.Array) return false;
        string expected = options.Audio ? options.AudioEncoding switch { AudioEncoding.M4a => "m4a", AudioEncoding.Opus => "ogg", _ => OutputProfiles.AudioExtension(options.AudioEncoding) } : options.VideoContainer == VideoContainer.Mkv ? "matroska" : OutputProfiles.VideoExtension(options.VideoContainer);
        return container.ValueKind == JsonValueKind.String && container.GetString().Split(',').Contains(expected) &&
            double.TryParse(duration.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds) && seconds > 0 && double.IsFinite(seconds) &&
            streams.EnumerateArray().Any(s => s.ValueKind == JsonValueKind.Object && s.TryGetProperty("codec_type", out var type) && type.ValueKind == JsonValueKind.String && type.GetString() == (options.Audio ? "audio" : "video"));
    }
}
