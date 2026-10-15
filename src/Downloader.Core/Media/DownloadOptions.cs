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
        public AudioEncoding AudioEncoding = AudioEncoding.Mp3;
        public CaptionRequest Captions;
        public MediaSelection Selection;
        public PlaylistInfo Playlist;
        public bool PlaylistItem;
        internal string ResumeDirectory { get; set; }

        public DownloadOptions Snapshot() => new()
        {
            Url = Url, Folder = Folder, QualityKey = QualityKey, FormatSelector = FormatSelector,
            Audio = Audio, AudioBitrate = AudioBitrate, AudioEncoding = AudioEncoding,
            Height = Height, Captions = Captions, Selection = Selection, PlaylistItem = PlaylistItem,
            ResumeDirectory = ResumeDirectory,
            Playlist = Playlist == null ? null : new PlaylistInfo
            {
                Id = Playlist.Id, Url = Playlist.Url, Title = Playlist.Title, ThumbnailUrl = Playlist.ThumbnailUrl,
                Entries = Playlist.Entries.Select(e => new PlaylistEntry { Id = e.Id, Title = e.Title, MediaUrl = e.MediaUrl }).ToList()
            }
        };
    }
}
