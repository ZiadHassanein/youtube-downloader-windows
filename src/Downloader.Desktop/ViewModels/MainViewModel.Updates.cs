namespace YouTubeDownloader;
public sealed partial class MainViewModel
{
    private AppRelease available;
    private bool downloadingUpdate;
    private bool manualUpdateFallback;
    public string UpdateLabel => T(downloadingUpdate ? "Downloading update…" : manualUpdateFallback ? "Download update" : available == null ? "Check for updates" : "Install v" + available.Version.ToString(3));

    private async Task UpdateAsync(CancellationToken token)
    {
        using var operation = downloads.Operations.Begin(OperationState.Updating, token);
        try
        {
            if (manualUpdateFallback)
            {
                actions.OpenReleaseDownload();
                return;
            }

            if (available == null)
            {
                ShowNotice("Checking for updates", "Looking for the latest app release…", false, "update");
                available = await AppUpdates.CheckAsync(operation.Token);
                if (available == null)
                    ShowNotice("You're up to date", T("No newer app release is available."), false, "update");
                else
                    ShowNotice("A new version is ready", F("Version {0} is available. Choose Install to update the app.", available.Version.ToString(3)), false, "update");
            }
            else
            {
                downloadingUpdate = true;
                OnPropertyChanged(nameof(UpdateLabel));
                ShowNotice("Preparing your update", "The app will restart when the verified update is ready.", false, "update");
                string prepared = await AppUpdates.PrepareAsync(available, operation.Token);
                AppUpdates.InstallAndRestart(prepared);
                actions.Dispatch(() => System.Windows.Application.Current.MainWindow.Close());
            }
        }
        catch (OperationCanceledException)
        {
            ShowNotice("Update cancelled", "Your current app is ready to use.", false, "update");
        }
        catch (UnauthorizedAccessException)
        {
            manualUpdateFallback = true;
            ShowNotice("Update couldn't be installed", "Download the new ZIP and extract it into a writable folder. Your current app is ready to use.", true, "update");
        }
        catch
        {
            ShowNotice(available == null ? "Couldn't check for updates" : "Update couldn't be installed", "Your current app is ready to use. Check your connection and try again.", true, "update");
        }
        finally
        {
            downloadingUpdate = false;
            OnPropertyChanged(nameof(UpdateLabel));
            RefreshAvailability();
        }
    }
}
