using System.Text.RegularExpressions;

namespace YouTubeDownloader
{
    public static class DownloadErrors
    {
        public static bool IsDiskFull(string message)
        {
            string error = (message ?? "").ToLowerInvariant();
            return error.Contains("no space") || error.Contains("disk full") || error.Contains("not enough space") || error.Contains("enospc") || Regex.IsMatch(error, @"\b(?:winerror\s*112|errno\s*28)\b");
        }
    }
}
