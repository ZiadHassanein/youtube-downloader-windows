namespace YouTubeDownloader;

internal interface IToolUpdateClient
{
    Task<(ToolSet Tools, MediaToolUpdateResult Result)> RefreshAsync(ToolSet current, IProgress<DownloadProgress> progress, CancellationToken token);
    Task<ToolSet> RollbackAsync(ToolSet current, ToolSet fallback, CancellationToken token);
}
