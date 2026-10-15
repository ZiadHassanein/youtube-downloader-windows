namespace YouTubeDownloader;
public sealed record MediaToolUpdateResult(bool Updated, string Version);

public interface IMediaMaintenance
{
    Task<MediaToolUpdateResult> RefreshToolsAsync(IProgress<DownloadProgress> progress, CancellationToken token);
}
