namespace YouTubeDownloader;

// Only inspected metadata may create a selector. Front ends carry it without interpreting it.
public sealed record MediaSelection
{
    public string MediaUrl { get; }
    public int Resolution { get; }
    internal string Selector { get; }
    internal MediaSelection(string mediaUrl, int resolution, string selector)
        => (MediaUrl, Resolution, Selector) = (mediaUrl, resolution, selector);
}
