using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

namespace YouTubeDownloader;
public static class AvailableFormats
{
    private static readonly string[] WebMVideoCodecs = { "vp8", "vp9", "vp09", "av1", "av01" };
    private static readonly string[] Mp4VideoCodecs = { "avc", "h264", "hevc", "h265", "hev1", "hvc1", "av1", "av01", "vp9", "vp09", "mpeg4", "mp4v" };
    private static readonly string[] Mp4AudioCodecs = { "aac", "mp4a", "mp3", "ac3", "eac3", "alac", "opus" };
    private sealed class Source
    {
        public string Id;
        public int Resolution;
        public bool Audio, H264, Direct;
        public double Fps, Bitrate;
    }

    public static List<VideoQuality> Read(Dictionary<string, object> info, VideoContainer container = VideoContainer.Mp4)
    {
        object formats;
        var sources = new List<Source>();
        if (!info.TryGetValue("formats", out formats) || !(formats is IEnumerable))
            return new List<VideoQuality>();
        foreach (object item in (IEnumerable)formats)
        {
            var f = item as Dictionary<string, object>;
            if (f == null)
                continue;
            string codec = Text(f, "vcodec"), id = Text(f, "format_id");
            if (codec == "" || codec == "none" || codec == "images" || Text(f, "protocol") == "mhtml" || Text(f, "has_drm") == "True" || !Regex.IsMatch(id, "^[A-Za-z0-9_.-]+$"))
                continue;
            if (!CompatibleVideo(codec, container) || (Text(f, "acodec") is not ("" or "none") && !CompatibleAudio(Text(f, "acodec"), container))) continue;
            int height = (int)Number(f, "height"), width = (int)Number(f, "width");
            int resolution = width > 0 ? Math.Min(width, height) : height;
            if (resolution <= 0)
                continue;
            sources.Add(new Source { Id = id, Resolution = resolution, Audio = Text(f, "acodec") != "" && Text(f, "acodec") != "none", H264 = codec.StartsWith("avc", StringComparison.OrdinalIgnoreCase) || codec.StartsWith("h264", StringComparison.OrdinalIgnoreCase), Direct = Text(f, "protocol") == "https" || Text(f, "protocol") == "http", Fps = Number(f, "fps"), Bitrate = Number(f, "tbr") });
        }

        var qualities = new List<VideoQuality>();
        bool hasAudio = HasAudio(info);
        bool compatibleAudio = ((IEnumerable)formats).OfType<Dictionary<string, object>>().Any(f => Text(f, "has_drm") != "True" && Text(f, "vcodec") == "none" && CompatibleAudio(Text(f, "acodec"), container));
        foreach (var group in sources.GroupBy(f => f.Resolution).OrderByDescending(g => g.Key))
        {
            var alternatives = new List<string>();
            foreach (var source in group.OrderByDescending(s => s.H264).ThenByDescending(s => s.Direct).ThenByDescending(s => s.Fps).ThenByDescending(s => s.Bitrate))
            {
                if (source.Audio || !hasAudio)
                    alternatives.Add(source.Id);
                else
                {
                    if (!compatibleAudio) continue;
                    if (container == VideoContainer.WebM)
                    {
                        alternatives.Add(source.Id + "+bestaudio[acodec^=opus]");
                        alternatives.Add(source.Id + "+bestaudio[acodec^=vorbis]");
                    }
                    else
                    {
                        if (container == VideoContainer.Mp4)
                        {
                            alternatives.Add(source.Id + "+bestaudio[ext=m4a]");
                            foreach (string codec in new[] { "aac", "mp4a", "mp3", "ac3", "eac3", "alac", "opus" })
                                alternatives.Add(source.Id + "+bestaudio[acodec^=" + codec + "]");
                        }
                        else alternatives.Add(source.Id + "+bestaudio");
                    }
                }
            }

            if (alternatives.Count > 0) qualities.Add(new VideoQuality { Resolution = group.Key, Selector = String.Join("/", alternatives.Distinct()) });
        }

        return qualities;
    }

    private static bool CompatibleVideo(string codec, VideoContainer container) => container switch
    {
        VideoContainer.Mkv => true,
        VideoContainer.WebM => WebMVideoCodecs.Any(prefix => codec.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)),
        _ => Mp4VideoCodecs.Any(prefix => codec.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    };
    private static bool CompatibleAudio(string codec, VideoContainer container) => codec is not ("" or "none") && (container switch
    {
        VideoContainer.Mkv => true,
        VideoContainer.WebM => codec.StartsWith("opus", StringComparison.OrdinalIgnoreCase) || codec.StartsWith("vorbis", StringComparison.OrdinalIgnoreCase),
        _ => Mp4AudioCodecs.Any(prefix => codec.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    });

    public static int RecommendedIndex(List<VideoQuality> qualities)
    {
        if (qualities.Count == 0)
            return -1;
        int index = qualities.FindIndex(q => q.Resolution <= 1080);
        return index >= 0 ? index : 0;
    }

    public static bool HasAudio(Dictionary<string, object> info)
    {
        object formats;
        if (!info.TryGetValue("formats", out formats) || !(formats is IEnumerable))
            return false;
        foreach (object item in (IEnumerable)formats)
        {
            var f = item as Dictionary<string, object>;
            if (f != null && Text(f, "acodec") != "" && Text(f, "acodec") != "none" && Text(f, "has_drm") != "True")
                return true;
        }

        return false;
    }

    private static string Text(Dictionary<string, object> data, string key)
    {
        object v;
        return data.TryGetValue(key, out v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : "";
    }

    private static double Number(Dictionary<string, object> data, string key)
    {
        double n;
        return Double.TryParse(Text(data, key), NumberStyles.Any, CultureInfo.InvariantCulture, out n) ? n : 0;
    }
}
