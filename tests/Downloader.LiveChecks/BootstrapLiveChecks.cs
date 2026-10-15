using YouTubeDownloader;

internal static class BootstrapLiveChecks
{
    internal static async Task<int> Run(string output, bool requireEmpty = true)
    {
        try
        {
            string root = Path.GetFullPath(output), cache = Path.Combine(root, "Tools");
            if (requireEmpty && Directory.Exists(cache)) throw new InvalidOperationException("This check requires a new, empty tool cache.");
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(12));
            using var manager = new DependencyManager(cache, Array.Empty<string>());
            string last = null;
            var progress = new InlineProgress(p =>
            {
                string text = p.Detail + " " + (int)(p.Percent / 20) * 20 + "%";
                if (text != last) { Console.WriteLine(text); last = text; }
            });
            var tools = await manager.EnsureAsync(progress, timeout.Token);
            if (!tools.Ready || new[] { tools.YtDlp, tools.Deno, tools.FFmpeg, tools.FFprobe }.Any(path => !File.Exists(path) || !Path.GetFullPath(path).StartsWith(cache + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
                throw new IOException("Tools were not acquired into the isolated cache.");
            Console.WriteLine("LIVE PASS: " + (requireEmpty ? "Empty cache and empty search locations automatically acquired" : "Isolated cache and empty search locations revalidated") + " and probed all four official executables.");
            manager.AcquireArtifact = (_, _, _) => throw new Exception("A second launch must not reacquire dependencies.");
            if (!(await manager.EnsureAsync(null, timeout.Token)).Ready) throw new IOException("Cached tools were not reusable.");
            Console.WriteLine("LIVE PASS: A second preparation reused the verified private tools without another download.");
            var refresh = await ToolRefresh.RefreshAsync(tools, cache, progress, timeout.Token);
            Console.WriteLine("LIVE PASS: Public yt-dlp and Deno release/checksum endpoints checked; " + refresh.Result.Version + ". Updated=" + refresh.Result.Updated);
            using var downloads = new VerifiedDownloads();
            foreach (var source in new[] { ToolReleaseSource.YtDlp, ToolReleaseSource.Deno })
            {
                var release = source.Read(await downloads.TextAsync(source.Api + "latest", source.Api, 512 * 1024, timeout.Token));
                string prefix = source.Prefix(release);
                string name = source.Kind == "Deno" ? source.Asset + ".sha256sum" : "SHA2-256SUMS";
                string hash = source.ReadChecksum(await downloads.TextAsync(prefix + name, prefix, 16384, timeout.Token));
                if (!string.Equals("sha256:" + hash, release.Digest, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Published checksums disagree.");
                Console.WriteLine("LIVE PASS: " + source.Kind + " published checksum format matches the official asset digest.");
            }
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("LIVE FAIL: " + ex); return 1; }
    }
    private sealed class InlineProgress(Action<DownloadProgress> report) : IProgress<DownloadProgress>
    { public void Report(DownloadProgress value) => report(value); }
}
