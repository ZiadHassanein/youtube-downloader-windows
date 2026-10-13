namespace YouTubeDownloader;
public sealed class LocalPlaylistStorage : IPlaylistStorage
{
    private readonly string directory;
    public LocalPlaylistStorage(string directory = null)
    {
        this.directory = directory;
    }

    public static string OutputFolder(DownloadOptions options)
    {
        var link = VideoLink.Parse(options.Url);
        if (options.Playlist == null || link.PlaylistId != options.Playlist.Id || options.Playlist.Url != link.PlaylistUrl)
            throw new ArgumentException("Check the playlist link again before downloading.");
        if (string.IsNullOrWhiteSpace(options.Folder))
            throw new ArgumentException("Choose a folder for your download.");
        string root = Path.GetFullPath(options.Folder);
        if (root.StartsWith(@"\\"))
            throw new ArgumentException("Choose a folder on this computer.");
        return Path.Combine(root, "Playlist [" + link.PlaylistId + "]");
    }

    public PlaylistSession Open(DownloadOptions options)
    {
        string folder = OutputFolder(options);
        Directory.CreateDirectory(folder);
        string profile = options.Audio ? "mp3-" + options.AudioBitrate : "mp4-" + options.Height;
        var records = PlaylistRecords.Open(folder, profile, directory ?? options.ResumeDirectory);
        return new PlaylistSession
        {
            Folder = folder,
            Completed = records.Completed,
            Save = records.Save
        };
    }

    public bool IsCompleted(string folder, string file, bool audio) => MediaFiles.IsCompleted(folder, file, audio);
}
