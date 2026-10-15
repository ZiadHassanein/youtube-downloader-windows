namespace YouTubeDownloader
{
    public sealed class PlaylistInfo
    {
        public string Id, Url, Title, ThumbnailUrl;
        public List<PlaylistEntry> Entries = new List<PlaylistEntry>();
    }
}
