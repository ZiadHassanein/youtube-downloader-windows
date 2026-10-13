namespace YouTubeDownloader;
public sealed class ToolSet
{
    public string YtDlp { get; }
    public string FFmpeg { get; }
    public string FFprobe { get; }
    public string Deno { get; }
    public bool Ready { get; }

    private ToolSet(string ytDlp, string ffmpeg, string ffprobe, string deno)
    {
        YtDlp = ytDlp;
        FFmpeg = ffmpeg;
        FFprobe = ffprobe;
        Deno = deno;
        Ready = true;
    }

    internal static ToolSet Validated(string ytDlp, string ffmpeg, string ffprobe, string deno) => new(ytDlp, ffmpeg, ffprobe, deno);
}
