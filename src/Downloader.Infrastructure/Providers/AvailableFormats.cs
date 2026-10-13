using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

namespace YouTubeDownloader
{
    public static class AvailableFormats
    {
        private sealed class Source
        {
            public string Id;
            public int Resolution;
            public bool Audio, H264, Direct;
            public double Fps, Bitrate;
        }

        public static List<VideoQuality> Read(Dictionary<string, object> info)
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
                int height = (int)Number(f, "height"), width = (int)Number(f, "width");
                int resolution = width > 0 ? Math.Min(width, height) : height;
                if (resolution <= 0)
                    continue;
                sources.Add(new Source { Id = id, Resolution = resolution, Audio = Text(f, "acodec") != "" && Text(f, "acodec") != "none", H264 = codec.StartsWith("avc", StringComparison.OrdinalIgnoreCase) || codec.StartsWith("h264", StringComparison.OrdinalIgnoreCase), Direct = Text(f, "protocol") == "https" || Text(f, "protocol") == "http", Fps = Number(f, "fps"), Bitrate = Number(f, "tbr") });
            }

            var qualities = new List<VideoQuality>();
            foreach (var group in sources.GroupBy(f => f.Resolution).OrderByDescending(g => g.Key))
            {
                var alternatives = new List<string>();
                foreach (var source in group.OrderByDescending(s => s.H264).ThenByDescending(s => s.Direct).ThenByDescending(s => s.Fps).ThenByDescending(s => s.Bitrate))
                {
                    if (source.Audio)
                        alternatives.Add(source.Id);
                    else
                    {
                        alternatives.Add(source.Id + "+bestaudio[ext=m4a]");
                        alternatives.Add(source.Id + "+bestaudio");
                    }
                }

                qualities.Add(new VideoQuality { Resolution = group.Key, Selector = String.Join("/", alternatives.Distinct()) });
            }

            return qualities;
        }

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
}
