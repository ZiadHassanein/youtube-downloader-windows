namespace YouTubeDownloader
{
    public static class PlaylistScheduler
    {
        public static bool ShouldStop(string error)
        {
            error = (error ?? "").ToLowerInvariant();
            return DownloadErrors.IsDiskFull(error) || new[]
            {
                "429",
                "too many requests",
                "403",
                "confirm you're not a bot",
                "confirm you’re not a bot",
                "access is denied",
                "permission denied",
                "timed out",
                "connection",
                "resolve",
                "network",
                "missing tools"
            }.Any(error.Contains);
        }

        private static bool Unavailable(string error)
        {
            error = (error ?? "").ToLowerInvariant();
            return new[]
            {
                "unavailable",
                "private video",
                "removed",
                "age-restricted",
                "not available in your country",
                "requested format"
            }.Any(error.Contains);
        }

        public static DownloadResult Run(DownloadOptions options, IPlaylistStorage storage, Func<DownloadOptions, CancellationToken, DownloadResult> download, Action<int, int, string> itemStarted, CancellationToken token, Action<CancellationToken> pace = null)
        {
            var session = storage.Open(options);
            string folder = session.Folder;
            string profile = options.Audio ? "mp3-" + options.AudioBitrate : "mp4-" + options.Height;
            var completed = session.Completed;
            var result = new DownloadResult
            {
                Total = options.Playlist.Entries.Count
            };
            int done = 0, consecutiveFailures = 0;
            foreach (var entry in options.Playlist.Entries)
            {
                if (token.IsCancellationRequested)
                {
                    result.Cancelled = true;
                    break;
                }

                if (itemStarted != null)
                    itemStarted(done + 1, result.Total, entry.Title);
                string file;
                if (entry.Id != null && completed.TryGetValue(entry.Id, out file) && storage.IsCompleted(folder, file, options.Audio))
                {
                    result.Skipped++;
                    result.Completed++;
                    result.Files.Add(file);
                    result.FilePath = file;
                    done++;
                    continue;
                }

                try
                {
                    if (entry.Url == null)
                        throw new InvalidOperationException("This video is unavailable.");
                    // Waits are cancellable, including the gaps between processes.
                    if (done > 0)
                    {
                        if (pace != null)
                            pace(token);
                        else if (token.WaitHandle.WaitOne(2000))
                            token.ThrowIfCancellationRequested();
                    }

                    token.ThrowIfCancellationRequested();
                    var item = new DownloadOptions
                    {
                        Url = entry.Url,
                        Folder = folder,
                        Audio = options.Audio,
                        AudioBitrate = options.AudioBitrate,
                        Height = options.Height,
                        PlaylistItem = true
                    };
                    var saved = download(item, token);
                    if (saved.Cancelled)
                    {
                        result.Cancelled = true;
                        break;
                    }

                    file = saved.FilePath;
                    if (!storage.IsCompleted(folder, file, options.Audio))
                        throw new InvalidOperationException("The downloader did not produce a completed file.");
                    completed[entry.Id] = file;
                    result.Files.Add(file);
                    result.FilePath = file;
                    result.Completed++;
                    consecutiveFailures = 0;
                    try
                    {
                        session.Save();
                    }
                    catch (IOException)
                    {
                        result.StopReason = "Completed file saved, but resume information could not be saved.";
                        break;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        result.StopReason = "Completed file saved, but resume information could not be saved.";
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    result.Cancelled = true;
                    break;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    consecutiveFailures = Unavailable(ex.Message) ? 0 : consecutiveFailures + 1;
                    if (ShouldStop(ex.Message) || consecutiveFailures >= 3)
                    {
                        result.StopReason = ex.Message;
                        done++;
                        break;
                    }
                }

                done++;
            }

            result.Remaining = Math.Max(0, result.Total - result.Completed - result.Failed);
            result.Files = result.Files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return result;
        }
    }
}
