using YouTubeDownloader;

internal static class WorkflowLiveChecks
{
    internal static async Task<int> Run(string output)
    {
        string root = Path.GetFullPath(output); Directory.CreateDirectory(root);
        using var service = new DownloadService(new YtDlpProvider(new DependencyManager(Path.Combine(root, "Tools"))), new LocalPlaylistStorage(Path.Combine(root, "PlaylistState")));
        int verified = 0;
        foreach (var (site, url) in new[] { ("YouTube", "https://youtu.be/jNQXAC9IVRw"), ("Vimeo", "https://vimeo.com/76979871") })
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(4));
            try
            {
                var info = await service.InspectAsync(url, null, timeout.Token);
                var quality = info.Qualities.OrderBy(q => q.Resolution).First();
                var result = await service.DownloadAsync(new() { Url = url, Folder = Path.Combine(root, site), Height = quality.Resolution, Selection = quality.Selection }, null, timeout.Token);
                if (!File.Exists(result.FilePath) || result.Cancelled) throw new IOException("No completed media output");
                Console.WriteLine($"LIVE {site}: inspected actual qualities and verified {quality.Resolution}p MP4 ({new FileInfo(result.FilePath).Length} bytes)");
                verified++;
            }
            catch (Exception ex) { Console.WriteLine($"LIVE {site}: UNVERIFIED — {ex.Message}"); }
        }
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(40));
            Console.WriteLine("LIVE tool maintenance: " + await service.RefreshToolsAsync(null, timeout.Token));
        }
        catch (Exception ex) { Console.WriteLine("LIVE tool maintenance: UNVERIFIED — " + ex.Message); }
        return verified == 2 ? 0 : 2;
    }
}
