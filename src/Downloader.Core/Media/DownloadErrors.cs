using System.Text.RegularExpressions;

namespace YouTubeDownloader;
public static class DownloadErrors
{
    private static readonly string[] BlockedMarkers = { "429", "too many requests", "403", "not a bot", "sign in", "login required", "logged-in", "log in", "authentication", "permission denied", "access is denied" };
    private static readonly string[] TransientMarkers = { "timed out", "timeout", "connection reset", "connection aborted", "connection refused", "network", "name resolution", "resolve host", "502", "503", "504" };

    public static bool IsBlocked(string message)
    {
        string error = (message ?? "").ToLowerInvariant();
        return IsDiskFull(error) || BlockedMarkers.Any(error.Contains);
    }
    public static bool IsTransient(string message)
    {
        string error = (message ?? "").ToLowerInvariant();
        return TransientMarkers.Any(error.Contains);
    }
    public static bool IsDiskFull(string message)
    {
        string error = (message ?? "").ToLowerInvariant();
        return error.Contains("no space") || error.Contains("disk full") || error.Contains("not enough space") || error.Contains("enospc") || Regex.IsMatch(error, @"\b(?:winerror\s*112|errno\s*28)\b");
    }
}
