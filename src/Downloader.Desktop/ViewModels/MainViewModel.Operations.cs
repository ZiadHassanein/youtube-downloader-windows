using System.Net.Http;

namespace YouTubeDownloader;
public sealed partial class MainViewModel
{
    private long inputGeneration;
    private void ParseScope()
    {
        try
        {
            var parsed = downloads.ResolveTargets(Url);
            HasPlaylist = parsed.PlaylistUrl != null;
            canSingle = parsed.VideoUrl != null;
            wholePlaylist = HasPlaylist && !canSingle;
        }
        catch (ArgumentException)
        {
            HasPlaylist = false;
            canSingle = false;
            wholePlaylist = false;
        }

        OnPropertyChanged(nameof(WholePlaylist));
        OnPropertyChanged(nameof(SingleVideo));
    }

    private void RefreshQualities()
    {
        var previous = SelectedQuality;
        Qualities.Clear();
        if (Audio && OutputProfiles.IsLossless(SelectedEncoding.Encoding))
            Qualities.Add(new QualityChoice(T("Source quality"), Bitrate: state.AudioBitrate));
        else if (Audio)
        {
            state.AudioBitrate = OutputProfiles.NormalizeAudioBitrate(SelectedEncoding.Encoding, state.AudioBitrate);
            Qualities.Add(new QualityChoice(T("Best available")));
            foreach (int bitrate in OutputProfiles.AudioBitrates(SelectedEncoding.Encoding).Reverse())
                Qualities.Add(new QualityChoice(bitrate + " kbps", Bitrate: bitrate));
        }
        else if (WholePlaylist && playlistInfo != null)
            foreach (int cap in new[]
            {
                0,
                2160,
                1440,
                1080,
                720,
                480,
                360
            }

            )
                Qualities.Add(new QualityChoice(cap == 0 ? T("Best available") : F("Up to {0}p", cap), Height: cap));
        else if (videoInfo != null)
            foreach (var choice in videoInfo.Qualities)
                Qualities.Add(new QualityChoice(choice.Resolution + "p", choice.SelectionKey, choice.Resolution, Selection: choice.Selection));
        if (Qualities.Count == 0)
            Qualities.Add(new QualityChoice(T("Quality")));
        SelectedQuality = Audio ? Qualities.First(q => q.Bitrate == state.AudioBitrate) : Qualities.FirstOrDefault(q => previous != null && q.Key == previous.Key && q.Height == previous.Height) ?? (WholePlaylist && playlistInfo != null ? Qualities.First(q => q.Height == 1080) : videoInfo?.Qualities.Count > 0 ? Qualities[videoInfo.RecommendedIndex] : Qualities[0]);
        RefreshAvailability();
    }

    private void ScheduleInspection()
    {
        debounce?.Cancel();
        var previous = inspection;
        var timer = new CancellationTokenSource();
        debounce = timer;
        long generation = ++inputGeneration;
        inspection = DebouncedInspect(previous, timer, generation);
    }

    private async Task DebouncedInspect(Task previous, CancellationTokenSource timer, long generation)
    {
        try
        {
            await Task.Delay(450, timer.Token);
            await previous;
            timer.Token.ThrowIfCancellationRequested();
            if (generation != inputGeneration || string.IsNullOrWhiteSpace(Url) || !IsIdle)
                return;
            try
            {
                downloads.Normalize(Url, WholePlaylist);
            }
            catch (ArgumentException)
            {
                return;
            }

            await InspectAsync(generation, timer.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            timer.Dispose();
            if (ReferenceEquals(debounce, timer))
                debounce = null;
        }
    }

    private async Task PasteAsync()
    {
        string pasted = actions.ClipboardText();
        if (pasted != null)
            Url = pasted.Trim();
        debounce?.Cancel();
        await inspection;
        if (!string.IsNullOrWhiteSpace(Url))
            await InspectAsync(inputGeneration, CancellationToken.None);
    }

    private IProgress<DownloadProgress> PreparationProgress(long generation) => new Progress<DownloadProgress>(p =>
    {
        if (generation == inputGeneration && IsBusy && p.OperationId == downloads.Operations.CurrentId)
            ApplyProgress(p);
    });
    private async Task InspectAsync(long generation, CancellationToken token)
    {
        string candidate = Url.Trim();
        bool playlist = WholePlaylist;
        try
        {
            SetStatus(playlist ? "Checking playlist" : "Checking video", "Preparing your download…", false, true);
            if (playlist)
            {
                var result = await downloads.InspectPlaylistAsync(candidate, PreparationProgress(generation), token);
                if (generation != inputGeneration || candidate != Url.Trim() || playlist != WholePlaylist)
                    return;
                playlistInfo = result;
                videoInfo = null;
            }
            else
            {
                var result = await downloads.InspectAsync(candidate, PreparationProgress(generation), token);
                if (generation != inputGeneration || candidate != Url.Trim() || playlist != WholePlaylist)
                    return;
                if (result.Collection != null)
                {
                    playlistInfo = result.Collection;
                    videoInfo = null;
                    hasPlaylist = true;
                    canSingle = false;
                    wholePlaylist = true;
                    selectedPlaylistIds = null;
                    OnPropertyChanged(nameof(HasPlaylist));
                    OnPropertyChanged(nameof(WholePlaylist));
                    OnPropertyChanged(nameof(SingleVideo));
                }
                else
                {
                if (result.Live)
                    throw new InvalidOperationException("This video is live right now. Try again once the stream has ended.");
                if (result.Qualities.Count == 0 && !result.HasAudio)
                    throw new InvalidOperationException("The site did not return downloadable formats for this video.");
                videoInfo = result;
                playlistInfo = null;
                }
            }

            checkedUrl = candidate;
            statusHeading = statusDetail = "";
            ShowProgress = false;
            NotifyStatus();
            RefreshQualities();
            RefreshCaptions();
        }
        catch (OperationCanceledException)
        {
            if (generation == inputGeneration)
                SetStatus("Check cancelled", "You can paste another link or try again.", false);
        }
        catch (Exception ex)
        {
            if (generation == inputGeneration)
            {
                checkedUrl = null;
                videoInfo = null;
                playlistInfo = null;
                SetStatus(playlist ? "Could not check playlist" : "Could not check video", FriendlyError(ex), true);
                RefreshQualities();
            }
        }
        finally
        {
            RefreshAvailability();
        }
    }

    private async Task DownloadAsync()
    {
        if (!CanDownload)
            return;
        DownloadOptions request = null;
        lastFile = null;
        OnPropertyChanged(nameof(HasFile));
        try
        {
            request = CurrentRequest();
            if (string.IsNullOrWhiteSpace(request.Folder) || !Path.IsPathFullyQualified(request.Folder))
                throw new ArgumentException("Choose a valid folder on this computer with Browse.");
            SetStatus("Connecting", "Preparing your download…", false, true);
            var result = await downloads.DownloadAsync(request, PreparationProgress(inputGeneration), CancellationToken.None);
            if (result.Cancelled)
                SetStatus("Download cancelled", "Partial files are kept so the same download can resume later.", false);
            else if (request.Playlist != null)
            {
                bool complete = result.Failed == 0 && result.Remaining == 0 && result.StopReason == null;
                string summary = F("{0} saved · {1} already downloaded · {2} failed · {3} remaining", result.Completed - result.Skipped, result.Skipped, result.Failed, result.Remaining);
                SetStatus(complete ? "Playlist complete" : result.Remaining > 0 ? "Playlist paused" : "Playlist finished with errors", summary + (complete ? "" : "\n" + T(result.StopReason ?? "Run the same playlist again to resume. Completed files are kept.")) + (result.Warning == null ? "" : "\n" + T(result.Warning)), !complete);
                if (complete)
                    ClearLink();
            }
            else
            {
                SetStatus("Download complete", Path.GetFileName(result.FilePath) + (result.Warning == null ? "" : "\n" + T(result.Warning)), false);
                ClearLink();
            }

            foreach (string file in result.Files)
            {
                state.Recent.RemoveAll(p => string.Equals(p, file, StringComparison.OrdinalIgnoreCase));
                state.Recent.Insert(0, file);
            }

            state.Recent = state.Recent.Take(5).ToList();
            lastFile = result.FilePath;
            Save();
            OnPropertyChanged(nameof(HasFile));
            if (!result.Cancelled)
            {
                ShowProgress = true;
                Indeterminate = false;
                Progress = request.Playlist == null ? 100 : 100.0 * (result.Completed + result.Failed) / Math.Max(1, result.Total);
            }
        }
        catch (OperationCanceledException)
        {
            SetStatus("Download cancelled", "You can start another download.", false);
        }
        catch (Exception ex)
        {
            SetStatus("Download failed", FriendlyError(ex), true);
        }
        finally
        {
            RefreshAvailability();
        }
    }

    private void ClearLink()
    {
        debounce?.Cancel();
        Url = "";
    }

    private void ApplyProgress(DownloadProgress value)
    {
        statusHeading = value.Stage;
        statusDetail = (value.Total > 0 ? F("Video {0} of {1}", value.Item, value.Total) + " · " + value.ItemTitle + "\n" : "") + value.Detail;
        statusError = false;
        ShowProgress = true;
        Indeterminate = value.Indeterminate;
        Progress = Math.Clamp(value.Percent, 0, 100);
        NotifyStatus();
    }

    private void SetStatus(string heading, string detail, bool error, bool progress = false)
    {
        statusHeading = heading;
        statusDetail = detail;
        statusError = error;
        ShowProgress = progress;
        Indeterminate = progress;
        NotifyStatus();
    }

    private void NotifyStatus()
    {
        OnPropertyChanged(nameof(HasStatus));
        OnPropertyChanged(nameof(StatusHeading));
        OnPropertyChanged(nameof(StatusDetail));
        OnPropertyChanged(nameof(StatusBrush));
        RefreshMediaPresentation();
    }

    private void ShowNotice(string heading, string detail, bool error, string anchor, bool autoDismiss = true)
    {
        noticeLifetime?.Cancel();
        noticeLifetime = autoDismiss ? new CancellationTokenSource() : null;
        noticeAnchor = anchor;
        noticeHeading = heading;
        noticeDetail = detail;
        noticeError = error;
        NoticeVisible = true;
        OnPropertyChanged(nameof(NoticeHeading));
        OnPropertyChanged(nameof(NoticeDetail));
        OnPropertyChanged(nameof(NoticeBrush));
        OnPropertyChanged(nameof(MainNoticeVisible));
        OnPropertyChanged(nameof(OptionsNoticeVisible));
        NoticeRequested?.Invoke(anchor);
        if (noticeLifetime != null)
            _ = ExpireNotice(noticeLifetime);
    }

    private async Task ExpireNotice(CancellationTokenSource lifetime)
    {
        try
        {
            await Task.Delay(5000, lifetime.Token);
            NoticeVisible = false;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            lifetime.Dispose();
            if (ReferenceEquals(noticeLifetime, lifetime))
                noticeLifetime = null;
        }
    }

    private void OpenFolder()
    {
        try
        {
            if (!Directory.Exists(Folder))
                throw new IOException();
            actions.OpenFolder(Folder);
        }
        catch
        {
            ShowNotice("Folder is not available", "Choose an existing folder or download a video first.", true, "folder");
        }
    }

    private void OpenMedia(bool reveal)
    {
        try
        {
            if (!File.Exists(lastFile) || !new[]
            {
                ".mp4",
                ".mp3", ".m4a", ".opus", ".wav", ".flac"
            }.Contains(Path.GetExtension(lastFile), StringComparer.OrdinalIgnoreCase))
                throw new IOException();
            actions.OpenMedia(lastFile, reveal);
        }
        catch
        {
            ShowNotice("File is not available", "Open the save folder and try opening the file there.", true, "folder");
        }
    }

    public async Task CloseAsync()
    {
        thumbnailCancellation?.Cancel();
        thumbnailCancellation?.Dispose();
        debounce?.Cancel();
        noticeLifetime?.Cancel();
        Queue.Stop();
        downloads.Cancel();
        await inspection;
        await downloads.Operations.WaitForIdleAsync();
        if (DownloadCommand.ExecutionTask != null)
            await DownloadCommand.ExecutionTask;
        if (UpdateCommand.ExecutionTask != null)
            await UpdateCommand.ExecutionTask;
        if (QueueStartCommand.ExecutionTask != null) await QueueStartCommand.ExecutionTask;
        if (ToolRefreshCommand.ExecutionTask != null) await ToolRefreshCommand.ExecutionTask;
        if (CaptionLanguagesCommand.ExecutionTask != null) await CaptionLanguagesCommand.ExecutionTask;
        Save();
        downloads.Dispose();
    }

    private static string FriendlyError(Exception exception, string networkRecovery = "Check your internet connection, then paste the link again to retry.")
    {
        string text = exception.Message.ToLowerInvariant();
        if (exception is ArgumentException)
            return exception.Message;
        if (text.Contains("selected subtitles")) return "The selected subtitles are no longer available. Refresh output options or choose no subtitles.";
        if (text.Contains("429") || text.Contains("too many requests"))
            return "This site is limiting downloads right now. Wait a few minutes, then try again.";
        if (text.Contains("no longer available") || text.Contains("requested format"))
            return "That quality is no longer available. Paste the link again to refresh the options.";
        if (text.Contains("unsupported url") || text.Contains("no suitable extractor")) return "This site or link is not supported. Try a public video link.";
        if (text.Contains("private") || text.Contains("sign in") || text.Contains("logged-in") || text.Contains("login") || text.Contains("age-restricted"))
            return "This video requires sign-in. Try a publicly available video.";
        if (text.Contains("403"))
            return "This site refused this download. Wait a little, then paste the link again.";
        if (DownloadErrors.IsDiskFull(text))
            return "The drive is full. Free some space or choose another save folder.";
        if (exception is UnauthorizedAccessException || text.Contains("permission") || text.Contains("access is denied"))
            return "We can't save in that folder. Choose another folder with Browse.";
        if (exception is HttpRequestException or TimeoutException || text.Contains("network") || text.Contains("connection"))
            return networkRecovery;
        if (text.Contains("integrity") || text.Contains("compatible") || text.Contains("media tool"))
            return "Media tools could not be prepared. Paste the link again to retry.";
        if (text.Contains("unavailable") || text.Contains("removed"))
            return "This video is unavailable. Try another video link.";
        if (text.Contains("requested format"))
            return "That quality is no longer available. Paste the link again to refresh the options.";
        return "We couldn't finish this action. Check the link and save folder, then try again.";
    }
}
