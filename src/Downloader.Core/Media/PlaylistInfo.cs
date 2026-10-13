namespace YouTubeDownloader
{
    public sealed class PlaylistInfo
    {
        public string Id, Url, Title;
        public List<PlaylistEntry> Entries = new List<PlaylistEntry>();
    }
}
