using System.Collections;
using System.Globalization;

namespace YouTubeDownloader;

// Generic direct-file extraction can omit codecs, dimensions and audio entirely.
// Probe that same resource; never infer these facts from its file extension.
internal static class DirectMediaMetadata
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        { ".mp4", ".m4v", ".mov", ".mkv", ".webm", ".mp3", ".m4a", ".aac", ".ogg", ".opus", ".wav", ".flac" };

    internal static bool NeedsProbe(Dictionary<string, object> data, string requestedUrl)
    {
        if (Text(data, "extractor_key") != "Generic" && Text(data, "extractor") != "generic") return false;
        if (!Uri.TryCreate(requestedUrl, UriKind.Absolute, out var url) || url.Scheme is not ("https" or "http") ||
            url.UserInfo.Length != 0 || !url.IsDefaultPort || !Extensions.Contains(Path.GetExtension(url.AbsolutePath))) return false;
        var formats = Formats(data);
        if (formats.Length != 1) return false;
        var format = formats[0];
        if (Text(data, "has_drm") == "True" || Text(format, "has_drm") == "True" ||
            !Uri.TryCreate(Text(format, "url"), UriKind.Absolute, out var media) || media.AbsoluteUri != url.AbsoluteUri) return false;
        string video = Text(format, "vcodec"), audio = Text(format, "acodec");
        return video is "" or "unknown" || audio is "" or "unknown" || video != "none" && Number(format, "height") <= 0;
    }

    internal static string[] ProbeArguments(string url) => new[] {
        "-v", "error", "-tls_verify", "1", "-rw_timeout", "10000000", "-probesize", "5000000", "-analyzeduration", "5000000",
        "-protocol_whitelist", "http,https,tcp,tls", "-show_entries",
        "stream=codec_type,codec_name,codec_tag_string,width,height,bit_rate,duration:format=duration,bit_rate,size", "-of", "json", "-i", url
    };

    internal static void Apply(Dictionary<string, object> data, Dictionary<string, object> probe)
    {
        var streams = probe.TryGetValue("streams", out var list) && list is IEnumerable many
            ? many.OfType<Dictionary<string, object>>().Take(64).ToArray() : Array.Empty<Dictionary<string, object>>();
        if (streams.Any(s => Text(s, "codec_tag_string") is "encv" or "enca")) throw new InvalidDataException("This video is protected.");
        var video = streams.FirstOrDefault(s => Text(s, "codec_type") == "video");
        var audio = streams.FirstOrDefault(s => Text(s, "codec_type") == "audio");
        if (video == null && audio == null) throw new InvalidDataException("The site did not return downloadable formats for this video.");
        var format = Formats(data).Single();
        foreach (var target in new[] { format, data })
        {
            target["vcodec"] = video == null ? "none" : Text(video, "codec_name");
            target["acodec"] = audio == null ? "none" : Text(audio, "codec_name");
            if (video != null) { target["width"] = Number(video, "width"); target["height"] = Number(video, "height"); }
            if (audio != null && Number(audio, "bit_rate") > 0) target["abr"] = Number(audio, "bit_rate") / 1000;
            else target.Remove("abr");
        }
        if (probe.TryGetValue("format", out var metadata) && metadata is Dictionary<string, object> details)
        {
            if (Number(details, "duration") > 0) data["duration"] = Number(details, "duration");
            if (Number(details, "size") > 0) format["filesize"] = Number(details, "size");
            if (Number(details, "bit_rate") > 0) format["tbr"] = Number(details, "bit_rate") / 1000;
        }
    }

    private static Dictionary<string, object>[] Formats(Dictionary<string, object> data) =>
        data.TryGetValue("formats", out var value) && value is IEnumerable list ? list.OfType<Dictionary<string, object>>().ToArray() : Array.Empty<Dictionary<string, object>>();
    private static string Text(Dictionary<string, object> data, string key) => data.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? "" : "";
    private static double Number(Dictionary<string, object> data, string key) => double.TryParse(Text(data, key), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) && value >= 0 ? value : 0;
}
