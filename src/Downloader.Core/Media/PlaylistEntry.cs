namespace YouTubeDownloader
{
    public sealed class PlaylistEntry
    {
        public string Id, Title, MediaUrl;
        public string Url
        {
            get
            {
                return MediaUrl;
            }
        }
    }
}
