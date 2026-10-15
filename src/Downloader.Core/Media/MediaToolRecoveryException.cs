namespace YouTubeDownloader;

public sealed class MediaToolRecoveryException(bool checkedLatest, Exception inner) : InvalidOperationException(
    checkedLatest ? "The latest downloader still couldn't read this link. The website may have changed; try again later." :
        "The downloader couldn't be updated. Check your internet connection and try again.", inner)
{
    public bool CheckedLatest { get; } = checkedLatest;
}
