namespace YouTubeDownloader;

public sealed record VideoFormatChoice(string Label, VideoContainer Container)
{
    public override string ToString() => Label;
}

public sealed partial class MainViewModel
{
    private static readonly IReadOnlyList<VideoFormatChoice> VideoCatalog = new[]
    {
        new VideoFormatChoice("MP4", VideoContainer.Mp4), new VideoFormatChoice("MKV", VideoContainer.Mkv), new VideoFormatChoice("WebM", VideoContainer.WebM)
    };
    public IReadOnlyList<VideoFormatChoice> VideoFormats => videoInfo == null ? VideoCatalog : VideoCatalog.Where(choice => videoInfo.QualitiesFor(choice.Container).Count > 0).ToArray();
    public VideoFormatChoice SelectedVideoFormat
    {
        get => VideoCatalog.First(choice => choice.Container == state.VideoContainer);
        set
        {
            if (value == null || value.Container == state.VideoContainer || !VideoFormats.Contains(value)) return;
            state.VideoContainer = value.Container;
            RefreshQualities();
            Save();
        }
    }
    public string VideoFormatLabel => T("Video format");
    public bool HasAudioSource => videoInfo?.AudioSource != null;
    public string AudioSourceHint
    {
        get
        {
            if (playlistInfo != null || QueueRunning) return T("The source audio is checked for each video. Conversion cannot add detail.");
            var source = videoInfo?.AudioSource;
            if (source == null || !source.HasBitrate && !source.Lossless) return T("Source bitrate unavailable. Use automatic conversion.");
            string description = source.Lossless ? T("Lossless source") : F("Source estimate: {0} · {1} kbps", source.Codec, Math.Round(source.BitrateKbps.Value));
            return description + "\n" + T("Bitrates are conversion targets. Higher numbers cannot improve the source.");
        }
    }
    public IReadOnlyList<int> AvailableAudioBitrates => OutputProfiles.AudioBitrates(SelectedEncoding.Encoding, videoInfo?.AudioSource);
    private CaptionRequest CurrentCaption() => string.IsNullOrEmpty(SelectedCaption?.Language) ? null : new CaptionRequest(SelectedCaption.Language, SelectedCaption.Automatic, captionFormat);
    private CaptionRequest PreviewCaption => QueueRunning ? ActiveQueueEntry?.Options.Captions : CurrentCaption();
    public bool SubtitlesEnabled => CurrentCaption() != null;
    public bool HasPreviewCaptions => PreviewCaption != null;
    public string PreviewCaptions => PreviewCaption is not { } request ? "" :
        (CaptionChoices.FirstOrDefault(choice => choice.Language == request.Language && choice.Automatic == request.Automatic)?.Name ?? request.Language) + " · " + request.Format.ToString().ToUpperInvariant();

    private void RefreshCaptionSummary()
    {
        OnPropertyChanged(nameof(SubtitlesEnabled));
        OnPropertyChanged(nameof(HasPreviewCaptions));
        OnPropertyChanged(nameof(PreviewCaptions));
    }
}
