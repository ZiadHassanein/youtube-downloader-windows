using System.Collections;
using System.Text.RegularExpressions;

namespace YouTubeDownloader
{
    public static class PlaylistMetadata
    {
        public static PlaylistInfo Read(string url, Dictionary<string, object> data)
        {
            var link = VideoLink.Parse(url);
            object entries, title;
            if (link.PlaylistUrl == null || !data.TryGetValue("entries", out entries) || !(entries is IEnumerable))
                throw new InvalidOperationException("The playlist could not be read. Check the link and try again.");
            var info = new PlaylistInfo
            {
                Id = link.PlaylistId,
                Url = link.PlaylistUrl,
                Title = data.TryGetValue("title", out title) && title != null ? Convert.ToString(title) : "Playlist"
            };
            foreach (object value in (IEnumerable)entries)
            {
                var entry = value as Dictionary<string, object>;
                object id, name;
                string videoId = entry != null && entry.TryGetValue("id", out id) && id != null ? Convert.ToString(id) : null;
                if (videoId != null && !Regex.IsMatch(videoId, "^[A-Za-z0-9_-]{11}$"))
                    videoId = null;
                info.Entries.Add(new PlaylistEntry { Id = videoId, MediaUrl = videoId == null ? null : "https://www.youtube.com/watch?v=" + videoId, Title = entry != null && entry.TryGetValue("title", out name) && name != null ? Convert.ToString(name) : "Unavailable video" });
            }

            if (info.Entries.Count == 0)
                throw new InvalidOperationException("This playlist is empty or unavailable. Try a public playlist.");
            return info;
        }
    }
}
