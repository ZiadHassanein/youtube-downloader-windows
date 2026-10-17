using System.Text.RegularExpressions;

namespace YouTubeDownloader;
public static class CaptionMetadata
{
    public static List<CaptionChoice> Read(Dictionary<string, object> data)
    {
        var result = new List<CaptionChoice>();
        foreach (var (field, automatic) in new[] { ("subtitles", false), ("automatic_captions", true) })
        {
            if (!data.TryGetValue(field, out var value) || value is not Dictionary<string, object> tracks) continue;
            foreach (var track in tracks.Take(200))
            {
                if (track.Key == "live_chat" || !Regex.IsMatch(track.Key, "^[A-Za-z0-9_.-]{1,48}$")) continue;
                if (track.Value is not System.Collections.IEnumerable formats) continue;
                string name = track.Key;
                bool available = false;
                foreach (var item in formats)
                    if (item is Dictionary<string, object> format)
                    {
                        available = true;
                        if (format.TryGetValue("name", out var text) && text != null) name = new string(Convert.ToString(text, System.Globalization.CultureInfo.InvariantCulture).Where(c => !char.IsControl(c)).Take(80).ToArray());
                    }
                if (available) result.Add(new(track.Key, name, automatic));
            }
        }
        return result.OrderBy(c => c.Automatic).ThenBy(c => c.Language, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
