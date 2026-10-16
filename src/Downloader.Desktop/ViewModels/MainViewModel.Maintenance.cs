namespace YouTubeDownloader;

public sealed partial class MainViewModel
{
    private bool refreshingTools, toolRefreshIndeterminate;
    private double toolRefreshProgress;
    public bool IsRefreshingTools
    {
        get => refreshingTools;
        private set
        {
            if (SetProperty(ref refreshingTools, value)) OnPropertyChanged(nameof(ToolRefreshLabel));
        }
    }
    public bool ToolRefreshIndeterminate { get => toolRefreshIndeterminate; private set => SetProperty(ref toolRefreshIndeterminate, value); }
    public double ToolRefreshProgress { get => toolRefreshProgress; private set => SetProperty(ref toolRefreshProgress, value); }

    private async Task RefreshDownloaderAsync(CancellationToken token)
    {
        IsRefreshingTools = true;
        ToolRefreshIndeterminate = true;
        ToolRefreshProgress = 0;
        ShowNotice("Checking downloader updates", "Looking for an update to the tool that reads video links and downloads files…", false, "tools", autoDismiss: false);
        var progress = new Progress<DownloadProgress>(value =>
        {
            if (!IsRefreshingTools || value.OperationId != downloads.Operations.CurrentId || noticeAnchor != "tools" || !NoticeVisible) return;
            if (!string.IsNullOrWhiteSpace(value.Detail)) noticeDetail = value.Detail;
            if (!value.Indeterminate && noticeHeading != "Updating downloader…")
            {
                noticeHeading = "Updating downloader…";
                OnPropertyChanged(nameof(NoticeHeading));
            }
            ToolRefreshIndeterminate = value.Indeterminate;
            ToolRefreshProgress = Math.Clamp(value.Percent, 0, 100);
            OnPropertyChanged(nameof(NoticeDetail));
        });
        try
        {
            var result = await downloads.RefreshToolsAsync(progress, token);
            ShowNotice(result.Updated ? "Downloader updated" : "Downloader is up to date",
                result.Updated ? "The update is ready. Try the video link that was not working again." : "No new downloader update is available. You can use Vydrop as usual.", false, "tools");
        }
        catch (OperationCanceledException)
        {
            ShowNotice("Downloader update cancelled", "You can check again whenever you need to.", false, "tools");
        }
        catch (Exception)
        {
            ShowNotice("Could not update the downloader", "Check your internet connection and free disk space, then try again.", true, "tools");
        }
        finally
        {
            IsRefreshingTools = false;
            RefreshAvailability();
        }
    }
}
