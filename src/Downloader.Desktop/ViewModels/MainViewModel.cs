using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace YouTubeDownloader;
public sealed record QualityChoice(string Label, string Key = null, int Height = 0, int Bitrate = 0, MediaSelection Selection = null)
{
    public override string ToString() => Label;
}

public sealed partial class MainViewModel : ObservableObject
{
    private readonly DownloadService downloads;
    private readonly SettingsRepository settings;
    private readonly DesktopActions actions;
    private SavedState state;
    private string url = "", folder, checkedUrl, lastFile;
    private VideoInfo videoInfo;
    private PlaylistInfo playlistInfo;
    private bool audio, wholePlaylist, hasPlaylist, canSingle;
    private QualityChoice selectedQuality;
    private CancellationTokenSource debounce;
    private Task inspection = Task.CompletedTask;
    private string statusHeading = "", statusDetail = "", noticeHeading = "", noticeDetail = "";
    private bool statusError, noticeError, noticeVisible, showProgress, indeterminate;
    private double progress;
    private CancellationTokenSource noticeLifetime;
    private string noticeAnchor;
    public MainViewModel(DownloadService downloads, SettingsRepository settings, DesktopActions actions, IMediaThumbnailLoader thumbnailLoader = null)
    {
        this.downloads = downloads;
        this.settings = settings;
        this.actions = actions;
        this.thumbnailLoader = thumbnailLoader ?? new MediaThumbnailLoader();
        InitializePresentation();
        state = settings.Load();
        encoding = Encodings.FirstOrDefault(e => e.Encoding == state.AudioEncoding) ?? Encodings[0];
        folder = AppPaths.InitialFolder(state.Folder, state.FolderVersion == 0);
        state.FolderVersion = 1;
        audio = state.Format == 1;
        ThemeResources.Apply(IsDark);
        RefreshQualities();
        PasteCommand = new AsyncRelayCommand(PasteAsync, () => IsIdle);
        DownloadCommand = new AsyncRelayCommand(DownloadAsync, () => CanDownload);
        CancelCommand = new RelayCommand(() =>
        {
            Queue.Stop();
            downloads.Cancel();
            OnPropertyChanged(nameof(CancelLabel));
        }, () => IsBusy);
        ThemeCommand = new RelayCommand(() =>
        {
            state.Theme = IsDark ? "Light" : "Dark";
            state.ThemeSelected = true;
            ThemeResources.Apply(IsDark, animate: true);
            NotifyPresentation();
            Save();
        });
        LanguageCommand = new RelayCommand(() =>
        {
            SelectLanguageCommand.Execute(IsArabic ? "en" : "ar");
        }, () => IsIdle);
        BrowseCommand = new RelayCommand(() =>
        {
            var chosen = actions.BrowseFolder(Folder);
            if (chosen != null)
                Folder = chosen;
        }, () => IsIdle);
        OpenFolderCommand = new RelayCommand(() => OpenFolder(), () => IsIdle);
        OpenFileCommand = new RelayCommand(() => OpenMedia(false), () => HasFile && IsIdle);
        RevealCommand = new RelayCommand(() => OpenMedia(true), () => HasFile && IsIdle);
        InitializeDownloadAction();
        HistoryCommand = new RelayCommand(() => actions.ShowHistory(state.Recent, T), () => IsIdle);
        SocialCommand = new RelayCommand<string>(kind =>
        {
            try
            {
                actions.OpenSocial(kind);
            }
            catch
            {
                ShowNotice("Couldn't open this link", "Choose a default browser or email app in Windows, then try again.", true, "update");
            }
        });
        DismissNoticeCommand = new RelayCommand(() => NoticeVisible = false);
        UpdateCommand = new AsyncRelayCommand(UpdateAsync, () => IsIdle);
        InitializeWorkflows();
        downloads.Progress += p => actions.Dispatch(() =>
        {
            if (IsBusy && p.OperationId == downloads.Operations.CurrentId)
                ApplyProgress(p);
        });
        downloads.Operations.Changed += () => actions.Dispatch(RefreshAvailability);
        if (settings.RecoveryMessage != null)
            ShowNotice("Settings recovered", settings.RecoveryMessage, false, "folder");
        if (AppUpdates.RecoveryMessage != null)
            SetStatus("Update needs recovery", AppUpdates.RecoveryMessage, true);
    }

    private string T(string text) => Texts.For(state.Language, text)?.Replace("&", "");
    private string F(string text, params object[] values) => Texts.Format(state.Language, text, values);
    public bool IsArabic => state.Language == "ar";
    public GridLength NoticeLeadingWidth => IsArabic ? new GridLength(32) : new GridLength(1, GridUnitType.Star);
    public GridLength NoticeTrailingWidth => IsArabic ? new GridLength(1, GridUnitType.Star) : new GridLength(32);
    public int NoticeTextColumn => IsArabic ? 1 : 0;
    public int NoticeDismissColumn => IsArabic ? 0 : 1;
    public TextAlignment NoticeAlignment => IsArabic ? TextAlignment.Right : TextAlignment.Left;
    public FontFamily ContentFont => (FontFamily)Application.Current.Resources[IsArabic ? "ArabicFont" : "LatinFont"];
    public bool IsDark => !state.ThemeSelected || state.Theme == "Dark";
    public FlowDirection ContentDirection => IsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    public bool IsBusy => downloads.Operations.IsBusy || Queue.IsRunning;
    public bool IsIdle => !IsBusy;

    public bool HasPlaylist
    {
        get => hasPlaylist;
        private set
        {
            SetProperty(ref hasPlaylist, value);
            OnPropertyChanged(nameof(CanSelectSingle));
        }
    }

    public bool CanSelectSingle => canSingle && IsIdle;

    public string Url
    {
        get => url;
        set
        {
            if (!SetProperty(ref url, value ?? ""))
                return;
            if (IsIdle)
            {
                lastFile = null;
                OnPropertyChanged(nameof(HasFile));
            }
            if (downloadActionState != DownloadActionState.Downloading)
                SetDownloadAction(DownloadActionState.Ready);
            videoInfo = null;
            playlistInfo = null;
            selectedPlaylistIds = null;
            playlistCaptions = new();
            RefreshCaptions();
            checkedUrl = null;
            ParseScope();
            RefreshQualities();
            ScheduleInspection();
            RefreshAvailability();
        }
    }

    public string Folder
    {
        get => folder;
        set
        {
            if (SetProperty(ref folder, value))
            {
                OnPropertyChanged(nameof(FolderDisplay));
                OnPropertyChanged(nameof(FolderAccessibleName));
                Save();
            }
        }
    }

    public bool Audio
    {
        get => audio;
        set
        {
            if (!SetProperty(ref audio, value))
                return;
            OnPropertyChanged(nameof(Video));
            state.Format = value ? 1 : 0;
            RefreshQualities();
            Save();
        }
    }

    public bool Video
    {
        get => !audio;
        set
        {
            if (value)
                Audio = false;
        }
    }

    public bool WholePlaylist
    {
        get => wholePlaylist;
        set
        {
            if (!SetProperty(ref wholePlaylist, value))
                return;
            OnPropertyChanged(nameof(SingleVideo));
            videoInfo = null;
            playlistInfo = null;
            checkedUrl = null;
            RefreshQualities();
            ScheduleInspection();
        }
    }

    public bool SingleVideo
    {
        get => !wholePlaylist;
        set
        {
            if (value)
                WholePlaylist = false;
        }
    }

    public ObservableCollection<QualityChoice> Qualities { get; } = new();

    public QualityChoice SelectedQuality
    {
        get => selectedQuality;
        set
        {
            if (!SetProperty(ref selectedQuality, value))
                return;
            if (Audio && value != null && !OutputProfiles.IsLossless(SelectedEncoding.Encoding))
            {
                state.AudioBitrate = value.Bitrate;
                Save();
            }

            RefreshAvailability();
        }
    }

    public bool CanChooseQuality => IsIdle && (Audio ? !OutputProfiles.IsLossless(SelectedEncoding.Encoding) : playlistInfo != null || videoInfo?.Qualities.Count > 0);
    public bool CanDownload => IsIdle && checkedUrl == Url.Trim() && (WholePlaylist ? playlistInfo != null : videoInfo != null && (Audio ? videoInfo.HasAudio : SelectedQuality?.Key != null));
    public string LinkLabel => T("Video link");
    public string LinkPlaceholder => T("Videos, Shorts, and playlists");
    public string PasteLabel => T("Paste");
    public string SingleLabel => T("This video");
    public string PlaylistLabel => T(playlistInfo?.IsMix == true ? "YouTube Mix (up to 50 videos)" : "Whole playlist");
    public string FormatLabel => T("Save as");
    public string VideoLabel => T("Video · MP4");
    public string AudioLabel => T("Audio") + " · " + SelectedEncoding.Label;
    public string QualityLabel => T("Quality");
    public string FolderLabel => T("Save to");
    public string BrowseLabel => T("Browse…");
    public string OpenFolderLabel => T("Open folder");
    public string DownloadLabel => T("Download");
    public string HistoryLabel => T("Recent downloads");
    public string CancelLabel => T(downloads.Operations.State == OperationState.Cancelling ? "Cancelling…" : "Cancel");
    public string OpenFileLabel => T("Open file");
    public string RevealLabel => T("Show in folder");
    public string DismissLabel => T("Dismiss update message");
    public string LanguageAction => IsArabic ? "English" : "العربية";
    public string ThemeAction => T(IsDark ? "Switch to light theme" : "Switch to dark theme");
    public string ThemeIcon => IsDark ? "M 12 0 L 12 3 M 12 21 L 12 24 M 0 12 L 3 12 M 21 12 L 24 12 M 3 3 L 5 5 M 19 19 L 21 21 M 3 21 L 5 19 M 19 5 L 21 3 M 16 12 A 4 4 0 1 1 8 12 A 4 4 0 1 1 16 12" : "M 12 3 C 7.03 3 3 7.03 3 12 C 3 16.97 7.03 21 12 21 C 16.3 21 19.9 18 20.8 14 C 17 15 13 13.6 11.2 10.3 C 9.9 7.9 10.25 4.8 12 3 Z";
    public string VersionLabel => "v" + AppUpdates.Current.ToString(3) + (ReleaseChannel.IsPreview ? " · Preview" : "");
    public bool HasFile => lastFile != null;
    public bool HasStatus => statusHeading.Length > 0;
    public string StatusHeading => T(statusHeading);
    public string StatusDetail => Texts.Progress(state.Language, statusDetail);
    public Brush StatusBrush => (Brush)Application.Current.Resources[statusError ? "Error" : "Ink"];
    public bool ShowProgress { get => showProgress; private set => SetProperty(ref showProgress, value); }
    public bool Indeterminate { get => indeterminate; private set => SetProperty(ref indeterminate, value); }
    public double Progress { get => progress; private set => SetProperty(ref progress, value); }
    public bool NoticeVisible
    {
        get => noticeVisible;
        private set
        {
            if (SetProperty(ref noticeVisible, value))
            {
                OnPropertyChanged(nameof(MainNoticeVisible));
                OnPropertyChanged(nameof(OptionsNoticeVisible));
            }
        }
    }
    public string NoticeHeading => T(noticeHeading);
    public string NoticeDetail => T(noticeDetail);
    public Brush NoticeBrush => (Brush)Application.Current.Resources[noticeError ? "Error" : "Ink"];

    public event Action<string> NoticeRequested;
    public IAsyncRelayCommand PasteCommand { get; }
    public IAsyncRelayCommand DownloadCommand { get; }
    public IAsyncRelayCommand UpdateCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand ThemeCommand { get; }
    public IRelayCommand LanguageCommand { get; }
    public IRelayCommand BrowseCommand { get; }
    public IRelayCommand OpenFolderCommand { get; }
    public IRelayCommand OpenFileCommand { get; }
    public IRelayCommand RevealCommand { get; }
    public IRelayCommand HistoryCommand { get; }
    public IRelayCommand<string> SocialCommand { get; }
    public IRelayCommand DismissNoticeCommand { get; }

    private void RefreshAvailability()
    {
        foreach (string property in new[]
        {
            nameof(IsBusy),
            nameof(IsIdle),
            nameof(CanDownload),
            nameof(CanChooseQuality),
            nameof(CanSelectSingle),
            nameof(CancelLabel)
        }

        )
            OnPropertyChanged(property);
        PasteCommand?.NotifyCanExecuteChanged();
        DownloadCommand?.NotifyCanExecuteChanged();
        CancelCommand?.NotifyCanExecuteChanged();
        LanguageCommand?.NotifyCanExecuteChanged();
        SelectLanguageCommand?.NotifyCanExecuteChanged();
        BrowseCommand?.NotifyCanExecuteChanged();
        OpenFolderCommand?.NotifyCanExecuteChanged();
        OpenFileCommand?.NotifyCanExecuteChanged();
        RevealCommand?.NotifyCanExecuteChanged();
        DownloadActionCommand?.NotifyCanExecuteChanged();
        HistoryCommand?.NotifyCanExecuteChanged();
        UpdateCommand?.NotifyCanExecuteChanged();
        RefreshWorkflowAvailability();
        RefreshMediaPresentation();
    }

    private void NotifyPresentation()
    {
        RefreshQueueLinkLabels();
        RefreshCaptions();
        SyncQueue();
        OnPropertyChanged(string.Empty);
        RefreshAvailability();
    }

    private void Save()
    {
        state.Folder = Folder;
        try
        {
            settings.Save(state);
        }
        catch
        {
            ShowNotice("Settings could not be saved", "Your preferences could not be saved. Check available storage and try again.", true, "folder");
        }
    }
}
