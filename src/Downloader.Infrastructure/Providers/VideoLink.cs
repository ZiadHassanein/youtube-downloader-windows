using System.Text.RegularExpressions;

namespace YouTubeDownloader;
public static class VideoLink
{
    public static YouTubeLink Parse(string text)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0)
            throw new ArgumentException("Paste a YouTube video link first.");
        if (text.Length > 4096 || text.Any(Char.IsControl))
            throw new ArgumentException("Paste one complete YouTube video link.");
        if (!text.Contains("://"))
            text = "https://" + text;
        Uri uri;
        if (!Uri.TryCreate(text, UriKind.Absolute, out uri) || (uri.Scheme != "https" && uri.Scheme != "http") || !String.IsNullOrEmpty(uri.UserInfo) || !uri.IsDefaultPort)
            throw new ArgumentException("Enter a valid YouTube video link.");
        string host = uri.IdnHost.ToLowerInvariant(), id = null, list = null;
        foreach (string pair in uri.Query.TrimStart('?').Split('&'))
        {
            string[] entry = pair.Split('=', 2);
            if (entry.Length == 2 && entry[0] == "list")
                list = Uri.UnescapeDataString(entry[1]);
        }

        string[] parts = uri.AbsolutePath.Trim('/').Split('/');
        if (host == "youtu.be" || host == "www.youtu.be")
        {
            if (parts.Length == 1)
                id = parts[0];
        }
        else if (host == "youtube.com" || host == "www.youtube.com" || host == "m.youtube.com" || host == "music.youtube.com")
        {
            if (parts.Length == 1 && parts[0] == "watch")
            {
                foreach (string pair in uri.Query.TrimStart('?').Split('&'))
                {
                    string[] entry = pair.Split('=', 2);
                    if (entry.Length == 2 && entry[0] == "v")
                    {
                        id = Uri.UnescapeDataString(entry[1]);
                        break;
                    }
                }
            }
            else if (parts.Length == 2 && (parts[0] == "shorts" || parts[0] == "live" || parts[0] == "embed"))
                id = parts[1];
        }
        else
            throw new ArgumentException("Use a youtube.com or youtu.be video link.");
        bool validVideo = id != null && Regex.IsMatch(id, "^[A-Za-z0-9_-]{11}$");
        bool validList = list != null && Regex.IsMatch(list, "^[A-Za-z0-9_-]{2,150}$");
        bool playlistPath = parts.Length == 1 && parts[0] == "playlist";
        if (!validVideo && !(validList && playlistPath))
            throw new ArgumentException("Use a link to a YouTube video, Short, or playlist.");
        string videoUrl = validVideo ? "https://www.youtube.com/watch?v=" + id : null;
        string playlistUrl = validList ? "https://www.youtube.com/playlist?list=" + list : null;
        if (validList && list.StartsWith("RD", StringComparison.Ordinal))
        {
            string seed = validVideo ? id : list.Length == 13 ? list.Substring(2) : null;
            if (seed == null || !Regex.IsMatch(seed, "^[A-Za-z0-9_-]{11}$"))
                throw new ArgumentException("For a YouTube Mix, copy the link of a playing video in the mix.");
            playlistUrl = "https://www.youtube.com/watch?v=" + seed + "&list=" + list + "&start_radio=1";
        }
        return new YouTubeLink
        {
            VideoUrl = videoUrl,
            PlaylistId = validList ? list : null,
            PlaylistUrl = playlistUrl
        };
    }

    public static string Normalize(string text)
    {
        var link = Parse(text);
        return link.VideoUrl ?? link.PlaylistUrl;
    }

    public static string Normalize(string text, bool playlist)
    {
        var link = Parse(text);
        string result = playlist ? link.PlaylistUrl : link.VideoUrl;
        if (result == null)
            throw new ArgumentException(playlist ? "This link does not contain a playlist." : "Choose Whole playlist for a playlist-only link.");
        return result;
    }
}
