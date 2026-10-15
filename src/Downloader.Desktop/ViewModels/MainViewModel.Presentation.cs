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
    public ImageSource PreviewImage => previewImage;
    public bool SettingsVisible
    {
        get => settingsVisible;
        set => SetProperty(ref settingsVisible, value);
    }
    public IRelayCommand SettingsCommand { get; private set; }
    public IRelayCommand NewDownloadCommand { get; private set; }
    public IReadOnlyList<string> VideoContainers { get; } = new[] { "MP4" };
    public string NewDownloadLabel => T("New download");
    public string RecentLabel => T("Recent");
    public string SettingsLabel => T("Settings");
    public string VideoKindLabel => T("Video");
    public string AudioKindLabel => T("Audio");
    public string ContainerLabel => T("Format");
    public string OptionsHint => T("Subtitles and file options");
    public string ChangeFolderLabel => T("Change");
    public string BrowserSettingsHint => T("Send a video link from your browser to ViFree.");
    public string UpdateSectionLabel => T("App updates");
    public string DownloaderSectionLabel => T("Download support");
    public string PreviewLabel => T("Video preview");
    public bool ShowDownloadActivity => IsBusy && !IsRefreshingTools && HasStatus;
    public string FolderDisplay => string.IsNullOrWhiteSpace(Folder) ? T("Choose a folder") :
        Path.GetFileName(Path.TrimEndingDirectorySeparator(Folder)) is { Length: > 0 } name ? name : Folder;
    public string FolderAccessibleName => T("Save to") + ": " + Folder;
    public string PreviewTitle => videoInfo?.Title ?? playlistInfo?.Title ?? T("Your next download");
    public FlowDirection PreviewTitleDirection => PreviewTitle.Any(c => c is >= '\u0600' and <= '\u06ff')
        ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    public string PreviewSubtitle => videoInfo?.Uploader ?? (playlistInfo == null ? T("Paste a link to see its details.") : T("Playlist"));
    public bool HasPreviewDuration => videoInfo?.Duration is > 0 and < 8640000;
    public string PreviewDuration => !HasPreviewDuration ? "" : videoInfo.Duration >= 3600
        ? TimeSpan.FromSeconds(videoInfo.Duration).ToString(@"h\:mm\:ss")
        : TimeSpan.FromSeconds(videoInfo.Duration).ToString(@"m\:ss");
    public string PreviewOutput => Audio ? SelectedEncoding.Label + " · " + SelectedQuality?.Label
        : "MP4 · " + (SelectedQuality?.Key != null || playlistInfo != null ? SelectedQuality?.Label : T("Quality"));
    public string PreviewStatus => HasStatus ? StatusHeading : CanDownload ? T("Ready to download") : T("Paste a video or playlist link");
    public Brush PreviewStatusBrush => (Brush)Application.Current.Resources[statusError ? "PreviewError" : CanDownload ? "PreviewSuccess" : "PreviewInk"];

    private void InitializePresentation()
    {
        SettingsCommand = new RelayCommand(() => SettingsVisible = !SettingsVisible);
        NewDownloadCommand = new RelayCommand(() => SettingsVisible = false);
    }

    private void RefreshMediaPresentation()
    {
        foreach (string property in new[]
        {
            nameof(PreviewTitle), nameof(PreviewSubtitle), nameof(PreviewTitleDirection),
            nameof(PreviewDuration), nameof(HasPreviewDuration), nameof(PreviewOutput),
            nameof(PreviewStatus), nameof(PreviewStatusBrush), nameof(ShowDownloadActivity)
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
