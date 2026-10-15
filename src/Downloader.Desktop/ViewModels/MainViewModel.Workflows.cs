using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace YouTubeDownloader;
public sealed record EncodingChoice(string Label, AudioEncoding Encoding)
{
    public override string ToString() => Label;
}
public sealed class QueueItemView : ObservableObject
{
    public QueueEntry Entry { get; }
    public Guid Id => Entry.Id;
    public string Title => Entry.Title;
    public bool HasArabicTitle => Title.Any(c => c is >= '\u0600' and <= '\u06ff' or >= '\u0750' and <= '\u077f' or >= '\u08a0' and <= '\u08ff' or >= '\ufb50' and <= '\ufdff' or >= '\ufe70' and <= '\ufeff');
    public string Url => Entry.Url;
    public string Status { get; private set; }
    public string Error { get; private set; }
    public double Percent => Entry.Percent;
    public bool IsActive => Entry.State is QueueState.Preparing or QueueState.Downloading;
    public bool IsPreparing => Entry.State == QueueState.Preparing;
    public bool CanRetry { get; private set; }
    public bool CanRemove => Entry.State is not (QueueState.Preparing or QueueState.Downloading);
    public bool HasError => Error.Length > 0;
    public QueueItemView(QueueEntry entry) => Entry = entry;
    public void Refresh(Func<string, string> translate, bool running)
    {
        Status = translate(Entry.State.ToString());
        Error = Entry.Error == null ? translate(Entry.Result?.Warning ?? "") : translate(MainViewModel.DescribeError(new InvalidOperationException(Entry.Error)));
        CanRetry = !running && Entry.State is QueueState.Failed or QueueState.Cancelled;
        OnPropertyChanged(string.Empty);
    }
}

public sealed partial class MainViewModel
{
    public DownloadQueue Queue { get; } = new();
    public ObservableCollection<QueueItemView> QueueItems { get; } = new();
    public ObservableCollection<CaptionChoice> CaptionChoices { get; } = new();
    public IReadOnlyList<EncodingChoice> Encodings { get; } = new[] { new EncodingChoice("MP3", AudioEncoding.Mp3), new EncodingChoice("M4A", AudioEncoding.M4a), new EncodingChoice("Opus", AudioEncoding.Opus), new EncodingChoice("WAV", AudioEncoding.Wav), new EncodingChoice("FLAC", AudioEncoding.Flac) };
    private EncodingChoice encoding;
    private CaptionChoice caption;
    private CaptionFormat captionFormat = CaptionFormat.Srt;
    private HashSet<string> selectedPlaylistIds;
    private List<CaptionChoice> playlistCaptions = new();
    public EncodingChoice SelectedEncoding
    {
        get => encoding ?? Encodings[0];
        set { if (value != null && SetProperty(ref encoding, value)) { state.AudioEncoding = value.Encoding; RefreshQualities(); OnPropertyChanged(nameof(AudioLabel)); Save(); } }
    }
    public CaptionChoice SelectedCaption { get => caption; set => SetProperty(ref caption, value); }
    public bool SrtCaptions { get => captionFormat == CaptionFormat.Srt; set { if (value) { captionFormat = CaptionFormat.Srt; OnPropertyChanged(nameof(SrtCaptions)); OnPropertyChanged(nameof(VttCaptions)); } } }
    public bool VttCaptions { get => captionFormat == CaptionFormat.Vtt; set { if (value) { captionFormat = CaptionFormat.Vtt; OnPropertyChanged(nameof(SrtCaptions)); OnPropertyChanged(nameof(VttCaptions)); } } }
    public bool OptionsNoticeVisible => NoticeVisible && noticeAnchor == "options";
    public bool MainNoticeVisible => NoticeVisible && noticeAnchor != "options";
    public bool CanSelectPlaylist => IsIdle && playlistInfo != null;
    public bool CanGetCaptionLanguages => IsIdle && playlistInfo != null;
    public bool HasQueue => QueueItems.Count > 0;
    public bool QueueRunning => Queue.IsRunning;
    public string QueueLabel => F("Queue ({0})", QueueItems.Count);
    public string PlaylistSelectionLabel => playlistInfo == null ? T("Choose videos") : F("Choose videos ({0}/{1})", selectedPlaylistIds?.Count ?? playlistInfo.Entries.Count, playlistInfo.Entries.Count);
    public string OptionsLabel => T("More options");
    public string DoneLabel => T("Done");
    public string CaptionFormatLabel => T("Subtitle file format");
    public string AudioFormatLabel => T("Audio format");
    public string CaptionsLabel => T("Subtitles");
    public string CaptionLanguagesLabel => T("Get subtitle languages");
    public string AddQueueLabel => T("Add to queue");
    public string StartQueueLabel => T("Start queue");
    public string RetryLabel => T("Retry");
    public string RemoveLabel => T("Remove");
    public string QueueHeading => T("Download queue");
    public string QueueHint => T("Enter a link, add more fields if needed, then choose Start queue.");
    public string QueueSettingsHint => T("Choose the format and quality in the main window before adding links.");
    public string QueueInputLabel => T("Video links");
    public string CloseLabel => T("Close");
    public string BrowserLabel => T("Browser integration");
    public string ToolRefreshLabel => T(IsRefreshingTools ? "Updating downloader…" : "Update downloader");
    public string ToolRefreshHint => T("ViFree checks for downloader and runtime updates automatically once a day when you use a link. You can check again here.");
    public string OutputHint => T("Audio conversion cannot improve the source quality. Subtitles are saved beside the media when available.");
    public string QueueMessage { get; private set; } = "";
    public IRelayCommand QueueCommand { get; private set; }
    public IRelayCommand OptionsCommand { get; private set; }
    public IRelayCommand AddQueueCommand { get; private set; }
    public IRelayCommand AddLinksCommand { get; private set; }
    public IAsyncRelayCommand QueueStartCommand { get; private set; }
    public IRelayCommand<Guid> RetryQueueCommand { get; private set; }
    public IRelayCommand<Guid> RemoveQueueCommand { get; private set; }
    public IRelayCommand PlaylistSelectionCommand { get; private set; }
    public IRelayCommand BrowserCommand { get; private set; }
    public IAsyncRelayCommand ToolRefreshCommand { get; private set; }
    public IAsyncRelayCommand CaptionLanguagesCommand { get; private set; }
    internal Func<string, string> Translate => T;
    internal static string DescribeError(Exception error) => FriendlyError(error);

    private void InitializeWorkflows()
    {
        QueueCommand = new RelayCommand(() => actions.ShowQueue(this));
        OptionsCommand = new RelayCommand(() => actions.ShowOutputOptions(this), () => IsIdle);
        AddQueueCommand = new RelayCommand(() =>
        {
            try
            {
                Queue.Add(CurrentRequest(), videoInfo?.Title ?? playlistInfo?.Title);
                QueueAdditionVersion++;
                OnPropertyChanged(nameof(QueueAdditionVersion));
                ShowNotice("Added to queue", T("Open the queue to start or manage downloads."), false, "folder");
            }
            catch (Exception ex) { ShowNotice("Could not add download", ex.Message, true, "folder"); }
        }, () => CanDownload);
        QueueStartCommand = new AsyncRelayCommand(StartQueueAsync, () => IsIdle && Queue.Entries.Any(e => e.State == QueueState.Pending));
        InitializeQueueInputs();
        RetryQueueCommand = new RelayCommand<Guid>(id => Queue.Retry(id), _ => !Queue.IsRunning);
        RemoveQueueCommand = new RelayCommand<Guid>(id => Queue.Remove(id));
        PlaylistSelectionCommand = new RelayCommand(() =>
        {
            var selected = actions.SelectPlaylist(playlistInfo, selectedPlaylistIds, T);
            if (selected != null) { selectedPlaylistIds = selected; OnPropertyChanged(nameof(PlaylistSelectionLabel)); }
        }, () => CanSelectPlaylist);
        BrowserCommand = new RelayCommand(() => actions.ShowBrowserIntegration(T), () => IsIdle);
        ToolRefreshCommand = new AsyncRelayCommand(RefreshDownloaderAsync, () => IsIdle);
        CaptionLanguagesCommand = new AsyncRelayCommand(async token =>
        {
            try
            {
                var first = playlistInfo.Entries.FirstOrDefault(e => e.MediaUrl != null) ?? throw new InvalidOperationException("This video is unavailable.");
                var metadata = await downloads.InspectAsync(first.MediaUrl, PreparationProgress(inputGeneration), token);
                playlistCaptions = metadata.Captions;
                RefreshCaptions();
                SetStatus("", "", false);
                ShowNotice("Subtitles", "Languages shown come from the first available playlist video. Availability may vary for other videos.", false, "options");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception ex)
            {
                ShowNotice("Subtitles", FriendlyError(ex, "Check your internet connection, then choose Get subtitle languages to try again."), true, "options");
            }
            finally { RefreshAvailability(); }
        }, () => CanGetCaptionLanguages);
        Queue.Changed += () => actions.Dispatch(SyncQueue);
        RefreshCaptions();
    }

    private void RefreshCaptions()
    {
        var previous = SelectedCaption;
        CaptionChoices.Clear();
        CaptionChoices.Add(new CaptionChoice("", T("No subtitles")));
        foreach (var choice in (videoInfo?.Captions ?? (playlistInfo != null ? playlistCaptions : new List<CaptionChoice>())))
            CaptionChoices.Add(choice with { Name = choice.Name + (choice.Automatic ? " · " + T("Automatic") : "") });
        SelectedCaption = CaptionChoices.FirstOrDefault(c => c.Language == previous?.Language && c.Automatic == previous?.Automatic) ?? CaptionChoices[0];
        OnPropertyChanged(nameof(PlaylistSelectionLabel));
    }
    private DownloadOptions CurrentRequest()
    {
        var request = OutputRequest();
        request.Url = downloads.Normalize(Url, WholePlaylist);
        request.Playlist = WholePlaylist ? playlistInfo : null;
        if (request.Captions != null) request.Captions = request.Captions with { Required = !WholePlaylist };
        request = request.Snapshot();
        if (request.Playlist != null && selectedPlaylistIds != null)
            request.Playlist.Entries = request.Playlist.Entries.Where(e => e.Id != null && selectedPlaylistIds.Contains(e.Id)).ToList();
        return request;
    }
    private DownloadOptions OutputRequest() => new()
        {
            Folder = Folder?.Trim(), Audio = Audio,
            AudioBitrate = state.AudioBitrate, AudioEncoding = SelectedEncoding.Encoding,
            Height = SelectedQuality?.Height ?? 1080, QualityKey = SelectedQuality?.Key,
            Selection = SelectedQuality?.Selection,
            Captions = string.IsNullOrEmpty(SelectedCaption?.Language) ? null : new CaptionRequest(SelectedCaption.Language, SelectedCaption.Automatic, captionFormat)
        };
    internal void ClearQueueMessage()
    {
        QueueMessage = "";
        OnPropertyChanged(nameof(QueueMessage));
    }

    private void SyncQueue()
    {
        var entries = Queue.Entries;
        for (int i = QueueItems.Count - 1; i >= 0; i--) if (!entries.Any(e => e.Id == QueueItems[i].Id)) QueueItems.RemoveAt(i);
        foreach (var entry in entries)
        {
            var row = QueueItems.FirstOrDefault(q => q.Id == entry.Id);
            if (row == null) { row = new QueueItemView(entry); QueueItems.Add(row); }
            row.Refresh(T, Queue.IsRunning);
        }
        OnPropertyChanged(nameof(QueueLabel));
        OnPropertyChanged(nameof(HasQueue));
        OnPropertyChanged(nameof(QueueRunning));
        RefreshAvailability();
    }
    private async Task StartQueueAsync()
    {
        SetDownloadAction(DownloadActionState.Ready);
        SetStatus("Download queue", "Preparing your download…", false, true);
        await Queue.RunAsync(async (request, progress, token) =>
        {
                var result = await downloads.DownloadQueueItemAsync(request, progress, token);
                actions.Dispatch(() =>
                {
                    RememberFiles(result);
                    if (!result.Cancelled && result.Failed == 0 && result.Remaining == 0 && result.StopReason == null && Url.Trim() == request.Url) ClearLink();
                });
                return result;
        });
        bool pending = Queue.Entries.Any(e => e.State == QueueState.Pending);
        SetStatus(pending ? "Queue paused" : "Queue finished", pending ? "Review failed items before starting the remaining downloads." : "Open the queue to review completed or failed downloads.", false);
        SyncQueue();
    }
    private void RememberFiles(DownloadResult result)
    {
        foreach (string file in result.Files) { state.Recent.RemoveAll(p => string.Equals(p, file, StringComparison.OrdinalIgnoreCase)); state.Recent.Insert(0, file); }
        state.Recent = state.Recent.Take(5).ToList();
        lastFile = result.FilePath ?? lastFile;
        OnPropertyChanged(nameof(HasFile));
        Save();
    }
    private void RefreshWorkflowAvailability()
    {
        AddQueueCommand?.NotifyCanExecuteChanged();
        OptionsCommand?.NotifyCanExecuteChanged();
        QueueStartCommand?.NotifyCanExecuteChanged();
        RefreshQueueInputAvailability();
        RetryQueueCommand?.NotifyCanExecuteChanged();
        PlaylistSelectionCommand?.NotifyCanExecuteChanged();
        BrowserCommand?.NotifyCanExecuteChanged();
        ToolRefreshCommand?.NotifyCanExecuteChanged();
        CaptionLanguagesCommand?.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanSelectPlaylist));
        OnPropertyChanged(nameof(CanGetCaptionLanguages));
    }
    public bool ReceiveBrowserLink(string link)
    {
        if (!IsIdle) return false;
        Url = link;
        return true;
    }
}
