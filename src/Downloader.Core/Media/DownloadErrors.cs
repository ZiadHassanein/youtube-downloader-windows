using System.Text.RegularExpressions;

namespace YouTubeDownloader
{
    public static class DownloadErrors
    {
        public static bool IsBlocked(string message)
        {
            string error = (message ?? "").ToLowerInvariant();
            return IsDiskFull(error) || new[] { "429", "too many requests", "403", "not a bot", "sign in", "login required", "logged-in", "log in", "authentication", "permission denied", "access is denied" }.Any(error.Contains);
        }
        public static bool IsTransient(string message)
        {
            string error = (message ?? "").ToLowerInvariant();
            return new[] { "timed out", "timeout", "connection reset", "connection aborted", "connection refused", "network", "name resolution", "resolve host", "502", "503", "504" }.Any(error.Contains);
        }
        public static bool IsDiskFull(string message)
        {
            string error = (message ?? "").ToLowerInvariant();
            return error.Contains("no space") || error.Contains("disk full") || error.Contains("not enough space") || error.Contains("enospc") || Regex.IsMatch(error, @"\b(?:winerror\s*112|errno\s*28)\b");
        }
    }
}
