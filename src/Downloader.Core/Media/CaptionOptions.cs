namespace YouTubeDownloader;

public enum CaptionFormat { Srt, Vtt }
public sealed record CaptionChoice(string Language, string Name, bool Automatic = false)
{
    public override string ToString() => Name;
}
public sealed record CaptionRequest(string Language, bool Automatic, CaptionFormat Format, bool Required = false);
