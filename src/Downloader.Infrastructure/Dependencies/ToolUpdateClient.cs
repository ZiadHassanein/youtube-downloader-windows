using System.Text.RegularExpressions;

namespace YouTubeDownloader;

internal sealed class ToolUpdateClient(string cache) : IToolUpdateClient
{
    internal Func<VerifiedDownloads> OpenDownloads { get; set; } = () => new();
    internal Func<string, string, CancellationToken, bool> Accept { get; set; } = (file, kind, token) => new ToolProbe().Accept(file, kind, token);
    internal Func<string, CancellationToken, string> VersionOf { get; set; } = (file, token) =>
    {
        string text = new ToolProbe().Capture(file, token, "--version").Trim();
        return Path.GetFileName(file) == "deno.exe" ? Regex.Match(text, @"^deno (\d+\.\d+\.\d+)").Groups[1].Value : text;
    };

    public async Task<(ToolSet Tools, MediaToolUpdateResult Result)> RefreshAsync(ToolSet current, IProgress<DownloadProgress> progress, CancellationToken token)
    {
        Directory.CreateDirectory(cache);
        using var lease = new FileStream(Path.Combine(cache, "refresh.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var downloads = OpenDownloads();
        var store = new ToolUpdateStore(cache);
        Directory.CreateDirectory(store.Root);
        if (ToolUpdateStore.IsLink(store.Root)) throw new InvalidDataException("The media-tool folder is invalid.");
        var state = store.Read();
        var active = store.VerifiedSelection(token);
        var candidates = new List<(ToolReleaseSource Source, ToolRelease Release)>();
        foreach (var source in new[] { ToolReleaseSource.YtDlp, ToolReleaseSource.Deno })
        {
            var release = source.Read(await downloads.TextAsync(source.Api + "latest", source.Api, 512 * 1024, token).ConfigureAwait(false));
            string installed = await Task.Run(() => VersionOf(source.Kind == "Deno" ? current.Deno : current.YtDlp, token), token).ConfigureAwait(false);
            string rejected = source.Kind == "Deno" ? state.RejectedDeno : state.RejectedYtDlp;
            if (source.IsNewer(release.Version, installed) && release.Version != rejected) candidates.Add((source, release));
        }
        if (candidates.Count == 0) return (current, new(false, await Describe(current, token).ConfigureAwait(false)));

        string stage = Path.Combine(store.Root, "stage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        string ownedDirectory = stage;
        bool committed = false;
        try
        {
            foreach (var (source, release) in candidates)
            {
                string prefix = source.Prefix(release);
                string checksums = source.Kind == "Deno" ? source.Asset + ".sha256sum" : "SHA2-256SUMS";
                string hash = source.ReadChecksum(await downloads.TextAsync(prefix + checksums, prefix, 16 * 1024, token).ConfigureAwait(false));
                if (release.Digest != null && !string.Equals(release.Digest, "sha256:" + hash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The media-tool checksums do not agree.");
                string archive = Path.Combine(stage, source.Asset);
                await downloads.DownloadAsync(release.Url, prefix, archive, release.Bytes, hash, release.Bytes,
                    n => progress?.Report(new() { Stage = "Preparing media tools", Detail = "Updating " + source.Kind + "…", Percent = 100.0 * n / release.Bytes }), token).ConfigureAwait(false);
                if (source.Kind == "Deno")
                {
                    await Task.Run(() => SafeArchives.Extract(archive, stage, new Dictionary<string, string> { ["deno.exe"] = "deno.exe" }, 500_000_000, token), token).ConfigureAwait(false);
                    File.Delete(archive);
                }
                string file = Path.Combine(stage, source.Executable);
                if (!await Task.Run(() => Accept(file, source.Kind, token) && VersionOf(file, token) == release.Version, token).ConfigureAwait(false))
                    throw new InvalidDataException("The new media tool failed its compatibility check.");
            }
            token.ThrowIfCancellationRequested();
            string target = Path.Combine(store.Root, "version-" + Guid.NewGuid().ToString("N"));
            Directory.Move(stage, target);
            ownedDirectory = target;
            var selection = active;
            foreach (var (source, release) in candidates)
            {
                var record = ToolUpdateStore.Record(target, Path.Combine(target, source.Executable), release.Version);
                selection = source.Kind == "Deno" ? selection with { Deno = record } : selection with { YtDlp = record };
            }
            var updated = ToolSet.Validated(store.Verify(selection.YtDlp, "yt-dlp.exe", token) ?? current.YtDlp, current.FFmpeg, current.FFprobe,
                store.Verify(selection.Deno, "deno.exe", token) ?? current.Deno);
            string description = await Describe(updated, token).ConfigureAwait(false);
            var promoted = new ToolUpdateStore.State(selection, active, state.RejectedYtDlp, state.RejectedDeno);
            store.Save(promoted, token);
            committed = true;
            store.Prune(promoted);
            return (updated, new(true, description));
        }
        finally
        {
            // Unique app-created stage; extraction only writes allowlisted flat files.
            if (!committed && Directory.Exists(ownedDirectory))
            {
                foreach (string file in Directory.GetFiles(ownedDirectory)) File.Delete(file);
                Directory.Delete(ownedDirectory);
            }
        }
    }

    private async Task<string> Describe(ToolSet tools, CancellationToken token) => await Task.Run(() =>
        "yt-dlp " + VersionOf(tools.YtDlp, token) + " · Deno " + VersionOf(tools.Deno, token), token).ConfigureAwait(false);

    public Task<ToolSet> RollbackAsync(ToolSet current, ToolSet fallback, CancellationToken token) => Task.Run(() =>
    {
        using var lease = new FileStream(Path.Combine(cache, "refresh.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var store = new ToolUpdateStore(cache);
        var state = store.Read();
        // Do not roll back another process's concurrent promotion.
        if (state.Previous == null || (store.Verify(state.Active.YtDlp, "yt-dlp.exe", token) is string yt && yt != current.YtDlp) ||
            (store.Verify(state.Active.Deno, "deno.exe", token) is string deno && deno != current.Deno)) return current;
        var restored = new ToolUpdateStore.State(state.Previous, null,
            state.Active.YtDlp != state.Previous.YtDlp ? state.Active.YtDlp?.Version : state.RejectedYtDlp,
            state.Active.Deno != state.Previous.Deno ? state.Active.Deno?.Version : state.RejectedDeno);
        store.Save(restored, token);
        store.Prune(restored);
        return ToolSet.Validated(store.Verify(state.Previous.YtDlp, "yt-dlp.exe", token) ?? fallback.YtDlp, fallback.FFmpeg, fallback.FFprobe,
            store.Verify(state.Previous.Deno, "deno.exe", token) ?? fallback.Deno);
    }, token);
}
