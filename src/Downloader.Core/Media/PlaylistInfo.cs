namespace YouTubeDownloader
{
    public sealed class PlaylistInfo
    {
        public string Id, Url, Title, ThumbnailUrl;
        public const int MixSnapshotLimit = 50;
        public bool IsMix;
        public List<PlaylistEntry> Entries = new List<PlaylistEntry>();
    }
}
