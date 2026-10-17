namespace YouTubeDownloader
{
    public sealed class DownloadOptions
    {
        public string Url, Folder;
        public string QualityKey;
        internal string FormatSelector;
        internal VideoInfo FreshMetadata;
        public bool Audio;
        public int Height = 1080;
        public int AudioBitrate;
        public AudioEncoding AudioEncoding = AudioEncoding.Mp3;
        public VideoContainer VideoContainer;
        public AudioSourceInfo AudioSource;
        public CaptionRequest Captions;
        public MediaSelection Selection;
        public PlaylistInfo Playlist;
        public bool PlaylistItem;
        internal string ResumeDirectory { get; set; }

        public DownloadOptions Snapshot() => new()
        {
            Url = Url, Folder = Folder, QualityKey = QualityKey, FormatSelector = FormatSelector, FreshMetadata = FreshMetadata,
            Audio = Audio, AudioBitrate = AudioBitrate, AudioEncoding = AudioEncoding, VideoContainer = VideoContainer, AudioSource = AudioSource,
            Height = Height, Captions = Captions, Selection = Selection, PlaylistItem = PlaylistItem,
            ResumeDirectory = ResumeDirectory,
            Playlist = Playlist == null ? null : new PlaylistInfo
            {
                Id = Playlist.Id, Url = Playlist.Url, Title = Playlist.Title, ThumbnailUrl = Playlist.ThumbnailUrl, IsMix = Playlist.IsMix,
                Entries = Playlist.Entries.Select(e => new PlaylistEntry { Id = e.Id, Title = e.Title, MediaUrl = e.MediaUrl }).ToList()
            }
        };
    }
}
