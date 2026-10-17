namespace YouTubeDownloader;
public sealed class VideoInfo
{
    public List<CaptionChoice> Captions = new();
    public PlaylistInfo Collection;
    public string Title, Uploader, ThumbnailUrl;
    public double Duration;
    public bool Live;
    public List<VideoQuality> Qualities = new List<VideoQuality>();
    public bool HasAudio;
    public AudioSourceInfo AudioSource;
    public Dictionary<VideoContainer, List<VideoQuality>> AlternativeQualities = new();
    public IReadOnlyList<VideoQuality> QualitiesFor(VideoContainer container) => container == VideoContainer.Mp4 ? Qualities :
        AlternativeQualities.TryGetValue(container, out var formats) ? formats : Array.Empty<VideoQuality>();
    public int RecommendedIndex => Qualities.Count == 0 ? -1 : Math.Max(0, Qualities.FindIndex(q => q.Resolution <= 1080));
}
