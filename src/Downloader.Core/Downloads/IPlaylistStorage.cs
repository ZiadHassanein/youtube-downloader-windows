namespace YouTubeDownloader;
public sealed class PlaylistSession
{
    public string Folder { get; init; }
    public Dictionary<string, string> Completed { get; init; }
    public Action Save { get; init; }
}

public interface IPlaylistStorage
{
    PlaylistSession Open(DownloadOptions options);
    bool IsCompleted(string folder, string file, DownloadOptions options, bool requireCaptions = false);
}
