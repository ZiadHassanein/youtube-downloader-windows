using System.Collections;
using System.Globalization;

namespace YouTubeDownloader;

internal static class AudioMetadata
{
    internal static AudioSourceInfo Read(Dictionary<string, object> data)
    {
        // Inspection requests bestaudio/best, so the root describes the actual selected audio.
        var source = HasAudio(data) ? data : data.TryGetValue("formats", out var formats) && formats is IEnumerable list
            ? list.OfType<Dictionary<string, object>>().Where(HasAudio).OrderBy(f => Text(f, "vcodec") == "none").LastOrDefault() : null;
        if (source == null) return null;
        string codec = Text(source, "acodec").ToLowerInvariant();
        double bitrate = Number(source, "abr");
        if (bitrate <= 0 && Text(source, "vcodec") == "none") bitrate = Number(source, "tbr");
        return new AudioSourceInfo(codec, bitrate is > 0 and <= 100000 && double.IsFinite(bitrate) ? bitrate : null,
            codec is "flac" or "alac" || codec.StartsWith("pcm_", StringComparison.Ordinal));
    }
    private static bool HasAudio(Dictionary<string, object> data) => Text(data, "acodec") is not ("" or "none") && Text(data, "has_drm") != "True";
    private static string Text(Dictionary<string, object> data, string key) => data.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? "" : "";
    private static double Number(Dictionary<string, object> data, string key) => double.TryParse(Text(data, key), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : 0;
}
