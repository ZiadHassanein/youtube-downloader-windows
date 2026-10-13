namespace YouTubeDownloader;
public interface IMediaProvider : IDisposable
{
    string Id { get; }

    bool IsReady { get; }

    Task EnsureReadyAsync(IProgress<DownloadProgress> progress, CancellationToken token);
    string Normalize(string url, bool playlist);
    MediaTargets ResolveTargets(string input);
    VideoInfo Inspect(string url, CancellationToken token, IProgress<DownloadProgress> progress = null);
    PlaylistInfo InspectPlaylist(string url, CancellationToken token, IProgress<DownloadProgress> progress = null);
    DownloadResult DownloadItem(DownloadOptions options, CancellationToken token, IProgress<DownloadProgress> progress = null);
    void Cancel();
}
