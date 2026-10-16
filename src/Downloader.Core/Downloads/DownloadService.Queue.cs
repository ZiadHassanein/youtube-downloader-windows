namespace YouTubeDownloader;
public sealed partial class DownloadService
{
    public async Task<DownloadResult> DownloadQueueItemAsync(DownloadOptions options, IProgress<DownloadProgress> progress, CancellationToken token)
    {
        options = options.Snapshot();
        Action<DownloadProgress> report = p => progress?.Report(p);
        Progress += report;
        try
        {
            if (options.Playlist == null && options.Selection == null)
            {
                var targets = ResolveTargets(options.Url);
                if (targets.VideoUrl == null)
                    options.Playlist = await InspectPlaylistAsync(options.Url, progress, token).ConfigureAwait(false);
                else
                {
                    var metadata = await InspectAsync(options.Url, progress, token).ConfigureAwait(false);
                    if (metadata.Live) throw new InvalidOperationException("This video is live right now. Try again once the stream has ended.");
                    if (metadata.Collection != null) options.Playlist = metadata.Collection;
                    else if (options.Audio && !metadata.HasAudio) throw new InvalidOperationException("This video has no downloadable audio stream.");
                    else if (!options.Audio)
                    {
                        var quality = metadata.QualitiesFor(options.VideoContainer).FirstOrDefault(q => options.Height == 0 || q.Resolution <= options.Height) ?? throw new InvalidOperationException("The selected quality is no longer available.");
                        options.Selection = quality.Selection;
                        options.QualityKey = quality.SelectionKey;
                    }
                    progress?.Report(new DownloadProgress { Stage = "Checking video", ItemTitle = metadata.Title, Indeterminate = true });
                }
            }
            return await DownloadAsync(options, progress, token).ConfigureAwait(false);
        }
        finally { Progress -= report; }
    }
}
