using System.Collections;
using System.Text.RegularExpressions;

namespace YouTubeDownloader
{
    public static class PlaylistMetadata
    {
        public static PlaylistInfo Read(string url, Dictionary<string, object> data)
        {
            url = MediaLink.Normalize(url, true);
            bool youtube = new YouTubeLinkResolver().Supports(new Uri(url));
            var link = youtube ? VideoLink.Parse(url) : null;
            object entries, title;
            if ((youtube && link.PlaylistUrl == null) || !data.TryGetValue("entries", out entries) || !(entries is IEnumerable))
                throw new InvalidOperationException("The playlist could not be read. Check the link and try again.");
            var info = new PlaylistInfo
            {
                Id = youtube ? link.PlaylistId : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(url))).Substring(0, 24),
                Url = url,
                IsMix = link?.IsMix == true,
                ThumbnailUrl = Thumbnail(data),
                Title = data.TryGetValue("title", out title) && title != null ? Convert.ToString(title) : "Playlist"
            };
            foreach (object value in (IEnumerable)entries)
            {
                if (info.Entries.Count >= 1000) throw new InvalidOperationException("Choose a playlist with at most 1,000 videos.");
                var entry = value as Dictionary<string, object>;
                object id, name;
                string videoId = entry != null && entry.TryGetValue("id", out id) && id != null ? Convert.ToString(id) : null;
                if (videoId != null && !Regex.IsMatch(videoId, youtube ? "^[A-Za-z0-9_-]{11}$" : "^[A-Za-z0-9_.-]{1,96}$"))
                    videoId = null;
                if (info.IsMix && videoId != null && info.Entries.Any(item => item.Id == videoId)) continue;
                info.ThumbnailUrl ??= entry == null ? null : Thumbnail(entry);
                string mediaUrl = null;
                if (videoId != null)
                {
                    if (youtube) mediaUrl = "https://www.youtube.com/watch?v=" + videoId;
                    else if (entry.TryGetValue("webpage_url", out var page) || entry.TryGetValue("url", out page))
                        try { mediaUrl = MediaLink.Normalize(Convert.ToString(page), false); } catch (ArgumentException) { }
                }
                info.Entries.Add(new PlaylistEntry { Id = videoId, MediaUrl = mediaUrl, Title = entry != null && entry.TryGetValue("title", out name) && name != null ? Convert.ToString(name) : "Unavailable video" });
            }

            if (info.Entries.Count == 0)
                throw new InvalidOperationException("This playlist is empty or unavailable. Try a public playlist.");
            return info;
        }

        private static string Thumbnail(Dictionary<string, object> data)
        {
            if (data.TryGetValue("thumbnail", out var single) && single is string address) return address;
            return data.TryGetValue("thumbnails", out var many) && many is IEnumerable list
                ? list.OfType<Dictionary<string, object>>().Select(item => item.TryGetValue("url", out var value) ? value as string : null)
                    .LastOrDefault(value => !string.IsNullOrWhiteSpace(value)) : null;
        }
    }
}
