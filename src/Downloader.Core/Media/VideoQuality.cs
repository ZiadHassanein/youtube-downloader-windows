namespace YouTubeDownloader
{
    public sealed class VideoQuality
    {
        public int Resolution;
        public string SelectionKey;
        internal string Selector;
        public override string ToString()
        {
            return Resolution + "p";
        }
    }
}
