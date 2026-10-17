using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;

namespace YouTubeDownloader;

public sealed partial class MainViewModel
{
    private readonly IMediaThumbnailLoader thumbnailLoader;
    private CancellationTokenSource thumbnailCancellation;
    private string thumbnailAddress;
    private ImageSource previewImage;
    private bool settingsVisible;
    private bool notifiedHasMediaPreview;
    private bool inlineMediaPreview;
    private bool quietCompletion;
    public bool HasMediaContext => HasMediaPreview || QueueRunning;
    public bool InlineMediaPreview
    {
        get => inlineMediaPreview;
        internal set { if (SetProperty(ref inlineMediaPreview, value)) OnPropertyChanged(nameof(ShowInlineActivity)); }
    }
    private QueueEntry ActiveQueueEntry => Queue.Entries.FirstOrDefault(entry => entry.State is QueueState.Preparing or QueueState.Downloading);
    private bool QueueMatchesPreview
    {
        get { try { return checkedUrl != null && downloads.Normalize(checkedUrl, WholePlaylist) == ActiveQueueEntry?.Url; } catch (ArgumentException) { return false; } }
    }
    public ImageSource PreviewImage => QueueRunning && !QueueMatchesPreview ? null : previewImage;
    public bool SettingsVisible
    {
        get => settingsVisible;
        set
        {
            if (SetProperty(ref settingsVisible, value))
                OnPropertyChanged(nameof(ShowInlineActivity));
        }
    }
    public IRelayCommand SettingsCommand { get; private set; }
    public IRelayCommand NewDownloadCommand { get; private set; }
    public IRelayCommand<string> SelectLanguageCommand { get; private set; }
    public string CurrentLanguageLabel => IsArabic ? "العربية" : "English";
    public string LanguageLabel => T("Language");
    public bool IsEnglish => !IsArabic;
    public string NewDownloadLabel => T("New download");
    public string SettingsLabel => T("Settings");
    public string ContactLabel => T("Contact");
    public string EmailLabel => T("Email");
    public string OptionsHint => T("Choose a subtitle language and file format.");
    public string ChangeFolderLabel => T("Change");
    public string BrowserSettingsHint => T("Send a video link from your browser to Vydrop.");
    public string UpdateSectionLabel => T("App updates");
    public string DownloaderSectionLabel => T("Download support");
    public string PreviewLabel => T("Video preview");
    public bool HasMediaPreview => videoInfo != null || playlistInfo != null;
    public bool ShowInlineActivity => HasStatus && !quietCompletion && (!HasMediaContext || InlineMediaPreview) && !SettingsVisible;
    public bool ShowDownloadActivity => HasStatus && !IsRefreshingTools;
    public string FolderDisplay => string.IsNullOrWhiteSpace(Folder) ? T("Choose a folder") :
        Path.GetFileName(Path.TrimEndingDirectorySeparator(Folder)) is { Length: > 0 } name ? name : Folder;
    public string FolderAccessibleName => T("Save to") + ": " + Folder;
    public string PreviewTitle => QueueRunning ? ActiveQueueEntry?.Title ?? T("Download queue") : videoInfo?.Title ?? playlistInfo?.Title ?? T("Your next download");
    public FlowDirection PreviewTitleDirection => PreviewTitle.Any(c => c is >= '\u0600' and <= '\u06ff')
        ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    public string PreviewSubtitle => QueueRunning ? T("Download queue") : videoInfo?.Uploader ?? (playlistInfo == null ? T("Paste a link to see its details.") : playlistInfo.IsMix ? F("YouTube Mix · {0} videos from this changing mix", playlistInfo.Entries.Count) : T("Playlist"));
    public bool HasPreviewDuration => (!QueueRunning || QueueMatchesPreview) && videoInfo?.Duration is > 0 and < 8640000;
    public bool PreviewIsAudio => QueueRunning && ActiveQueueEntry is { } entry ? entry.Options.Audio : Audio;
    public string PreviewDuration => !HasPreviewDuration ? "" : videoInfo.Duration >= 3600
        ? TimeSpan.FromSeconds(videoInfo.Duration).ToString(@"h\:mm\:ss", System.Globalization.CultureInfo.InvariantCulture)
        : TimeSpan.FromSeconds(videoInfo.Duration).ToString(@"m\:ss", System.Globalization.CultureInfo.InvariantCulture);
    public string PreviewOutput => QueueRunning && ActiveQueueEntry is { } active ? QueueOutput(active.Options) : Audio ? SelectedEncoding.Label + " · " + SelectedQuality?.Label
        : SelectedVideoFormat.Label + " · " + (SelectedQuality?.Key != null || playlistInfo != null ? SelectedQuality?.Label : T("Quality"));
    private string QueueOutput(DownloadOptions request) => request.Audio
        ? OutputProfiles.AudioExtension(request.AudioEncoding).ToUpperInvariant() + " · " + (OutputProfiles.IsLossless(request.AudioEncoding) ? T("No bitrate setting") : request.AudioBitrate == 0 ? T("Automatic") : request.AudioBitrate + " kbps")
        : OutputProfiles.VideoExtension(request.VideoContainer).ToUpperInvariant() + " · " + (request.Height == 0 ? T("Best available") : request.Height + "p");
    public string PreviewStatus => HasStatus ? StatusHeading : CanDownload ? T("Ready to download") : T("Paste a video or playlist link");

    private void InitializePresentation()
    {
        SettingsCommand = new RelayCommand(() => SettingsVisible = !SettingsVisible);
        NewDownloadCommand = new RelayCommand(() => SettingsVisible = false);
        SelectLanguageCommand = new RelayCommand<string>(SetLanguage, language => (language is "en" or "ar") && IsIdle);
    }

    private void SetLanguage(string language)
    {
        if (!IsIdle || language == state.Language || language is not ("en" or "ar")) return;
        state.Language = language;
        RefreshQualities();
        NotifyPresentation();
        Save();
    }

    private void RefreshMediaPresentation()
    {
        if (notifiedHasMediaPreview != HasMediaPreview)
        {
            notifiedHasMediaPreview = HasMediaPreview;
            OnPropertyChanged(nameof(HasMediaPreview));
        }
        foreach (string property in new[]
        {
            nameof(HasMediaContext), nameof(PlaylistLabel), nameof(PreviewImage), nameof(HasPreviewCaptions), nameof(PreviewCaptions), nameof(AudioSourceHint), nameof(PreviewIsAudio),
            nameof(PreviewTitle), nameof(PreviewSubtitle), nameof(PreviewTitleDirection),
            nameof(PreviewDuration), nameof(HasPreviewDuration), nameof(PreviewOutput),
            nameof(PreviewStatus), nameof(ShowInlineActivity), nameof(ShowDownloadActivity)
        }) OnPropertyChanged(property);
        string address = videoInfo?.ThumbnailUrl ?? playlistInfo?.ThumbnailUrl;
        if (thumbnailAddress == address) return;
        thumbnailAddress = address;
        thumbnailCancellation?.Cancel();
        thumbnailCancellation?.Dispose();
        thumbnailCancellation = null;
        previewImage = null;
        OnPropertyChanged(nameof(PreviewImage));
        if (!string.IsNullOrWhiteSpace(address))
        {
            thumbnailCancellation = new CancellationTokenSource();
            _ = LoadThumbnailAsync(address, thumbnailCancellation.Token);
        }
    }

    private async Task LoadThumbnailAsync(string address, CancellationToken token)
    {
        try
        {
            var image = await thumbnailLoader.LoadAsync(address, token);
            if (token.IsCancellationRequested || address != thumbnailAddress) return;
            previewImage = image;
            OnPropertyChanged(nameof(PreviewImage));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Artwork is optional; its failure must never turn a playable link into an error.
        }
    }
}
