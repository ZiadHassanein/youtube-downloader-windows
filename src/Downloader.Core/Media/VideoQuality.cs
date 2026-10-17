namespace YouTubeDownloader;
public sealed class VideoQuality
{
    public int Resolution;
    public string SelectionKey;
    public MediaSelection Selection { get; internal set; }
    internal string Selector;
    public override string ToString()
    {
        return Resolution + "p";
    }
}
