namespace YouTubeDownloader;

public sealed partial class MainViewModel
{
    public int RecentDownloadCount => state.UnseenDownloads;
    public bool HasRecentDownloadsBadge => RecentDownloadCount > 0;
    public string RecentDownloadsBadge => RecentDownloadCount > 99 ? "99+" : RecentDownloadCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public string HistoryAccessibleName => RecentDownloadCount == 0 ? HistoryLabel : F("Recent downloads, {0} new files", RecentDownloadCount);
    private void RefreshRecentBadge()
    {
        OnPropertyChanged(nameof(RecentDownloadCount));
        OnPropertyChanged(nameof(HasRecentDownloadsBadge));
        OnPropertyChanged(nameof(RecentDownloadsBadge));
        OnPropertyChanged(nameof(HistoryAccessibleName));
    }
}
