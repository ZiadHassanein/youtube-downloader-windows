namespace YouTubeDownloader;
public interface IMediaMaintenance
{
    Task<string> RefreshToolsAsync(IProgress<DownloadProgress> progress, CancellationToken token);
}
