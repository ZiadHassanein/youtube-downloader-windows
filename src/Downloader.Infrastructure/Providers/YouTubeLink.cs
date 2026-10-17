namespace YouTubeDownloader;
public sealed class YouTubeLink
{
    public string VideoUrl, PlaylistUrl, PlaylistId;
    public bool IsMix => PlaylistId?.StartsWith("RD", StringComparison.Ordinal) == true;
}
