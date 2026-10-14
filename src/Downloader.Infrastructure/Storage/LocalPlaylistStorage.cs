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
        string canonical = MediaLink.Normalize(options.Url, true);
        if (options.Playlist == null || options.Playlist.Url != canonical || string.IsNullOrWhiteSpace(options.Playlist.Id))
            throw new ArgumentException("Check the playlist link again before downloading.");
        if (new YouTubeLinkResolver().Supports(new Uri(canonical)) && VideoLink.Parse(canonical).PlaylistId != options.Playlist.Id)
            throw new ArgumentException("Check the playlist link again before downloading.");
        if (string.IsNullOrWhiteSpace(options.Folder))
            throw new ArgumentException("Choose a folder for your download.");
        string root = Path.GetFullPath(options.Folder);
        if (root.StartsWith(@"\\"))
            throw new ArgumentException("Choose a folder on this computer.");
        string safeId = new string(options.Playlist.Id.Where(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_').Take(96).ToArray());
        if (safeId.Length == 0) throw new ArgumentException("Invalid playlist identity.");
        return Path.Combine(root, "Playlist [" + safeId + "]");
    }

    public PlaylistSession Open(DownloadOptions options)
    {
        string folder = OutputFolder(options);
        Directory.CreateDirectory(folder);
        string profile = OutputProfiles.Key(options);
        var records = PlaylistRecords.Open(folder, profile, directory ?? options.ResumeDirectory);
        return new PlaylistSession
        {
            Folder = folder,
            Completed = records.Completed,
            Save = records.Save
        };
    }

    public bool IsCompleted(string folder, string file, DownloadOptions options, bool requireCaptions = false) => MediaFiles.IsCompleted(folder, file, options) && (!requireCaptions || MediaFiles.HasCaptions(file, options.Captions));
}
