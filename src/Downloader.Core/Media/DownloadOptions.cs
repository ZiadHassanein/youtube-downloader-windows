namespace YouTubeDownloader
{
    public sealed class DownloadOptions
    {
        public string Url, Folder;
        public string QualityKey;
        internal string FormatSelector;
        public bool Audio;
        public int Height = 1080;
        public int AudioBitrate = 320;
        public PlaylistInfo Playlist;
        public bool PlaylistItem;
        internal string ResumeDirectory { get; set; }
    }
}
