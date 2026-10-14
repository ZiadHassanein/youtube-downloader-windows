namespace YouTubeDownloader
{
    public sealed class DownloadResult
    {
        public string FilePath;
        public bool Cancelled;
        public List<string> Files = new List<string>();
        public int Total, Completed, Failed, Skipped, Remaining;
        public string StopReason;
        public string Warning;
    }
}
