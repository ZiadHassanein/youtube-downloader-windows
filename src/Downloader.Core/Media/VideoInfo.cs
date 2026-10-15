namespace YouTubeDownloader
{
    public sealed class VideoInfo
    {
        public List<CaptionChoice> Captions = new();
        public PlaylistInfo Collection;
        public string Title, Uploader, ThumbnailUrl;
        public double Duration;
        public bool Live;
        public List<VideoQuality> Qualities = new List<VideoQuality>();
        public bool HasAudio;
        public int RecommendedIndex => Qualities.Count == 0 ? -1 : Math.Max(0, Qualities.FindIndex(q => q.Resolution <= 1080));
    }
}
