namespace YouTubeDownloader;
public sealed class DependencyManager : IDisposable
{
    private readonly string cache;
    private readonly string[] locations;
    private readonly ToolProbe probe = new();
    private readonly VerifiedDownloads downloads;
    public string CacheDirectory => cache;
    internal Func<ToolArtifact, string, CancellationToken, Task> AcquireArtifact { get; set; }
    internal Action BeforePromote { get; set; }

    public DependencyManager(string cache = null, IEnumerable<string> searchLocations = null) : this(cache, searchLocations, new VerifiedDownloads())
    {
    }

    internal DependencyManager(string cache, IEnumerable<string> searchLocations, VerifiedDownloads downloads)
    {
        this.cache = cache ?? Path.Combine(AppPaths.UserData, "Tools");
        this.downloads = downloads;
        locations = new[]
        {
            Path.Combine(this.cache, ToolManifest.Identity)
        }.Concat(searchLocations ?? DefaultLocations()).Where(p => !string.IsNullOrWhiteSpace(p) && Path.IsPathFullyQualified(p.Trim('"'))).Select(p => p.Trim('"')).Distinct(StringComparer.OrdinalIgnoreCase).Take(64).ToArray();
    }

    private IEnumerable<string> DefaultLocations()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "tools");
        yield return Path.Combine(cache, ToolManifest.Identity);
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Links");
        foreach (string value in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
            yield return value;
    }

    public async Task<ToolSet> EnsureAsync(IProgress<DownloadProgress> progress, CancellationToken token)
    {
        void Report(string detail, double percent = 0, bool indeterminate = true) => progress?.Report(new DownloadProgress { Stage = "Preparing media tools", Detail = detail, Percent = percent, Indeterminate = indeterminate });
        Report("Checking available media tools…");
        var existing = await Task.Run(() => Discover(token), token).ConfigureAwait(false);
        if (existing != null)
            return existing;
        Directory.CreateDirectory(cache);
        using var preparation = await AcquireLock(token).ConfigureAwait(false);
        existing = await Task.Run(() => Discover(token), token).ConfigureAwait(false);
        if (existing != null)
            return existing;
        string target = Path.Combine(cache, ToolManifest.Identity);
        string stage = Path.Combine(cache, "stage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        try
        {
            var pieces = await Task.Run(() => DiscoverPieces(token), token).ConfigureAwait(false);
            // Move-owned cache files are copied before their directory is quarantined.
            string Preserve(string file)
            {
                if (file == null || !file.StartsWith(target + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return file;
                string copy = Path.Combine(stage, Path.GetFileName(file));
                File.Copy(file, copy);
                return copy;
            }

            pieces = (Preserve(pieces.Yt), Preserve(pieces.Ffmpeg), Preserve(pieces.Ffprobe), Preserve(pieces.Deno));
            var missing = ToolManifest.Artifacts.Where(a => a.Name == "yt-dlp" ? pieces.Yt == null : a.Name == "Deno" ? pieces.Deno == null : pieces.Ffmpeg == null).ToArray();
            int index = 0;
            foreach (var artifact in missing)
            {
                token.ThrowIfCancellationRequested();
                string archive = Path.Combine(stage, artifact.Name + ".download");
                Report("Downloading " + artifact.Name + "…", 100.0 * index / missing.Length, false);
                for (int attempt = 0;; attempt++)
                {
                    try
                    {
                        if (AcquireArtifact != null)
                            await AcquireArtifact(artifact, archive, token).ConfigureAwait(false);
                        else
                            await downloads.DownloadAsync(artifact.Url, artifact.Url.Substring(0, artifact.Url.LastIndexOf('/') + 1), archive, artifact.Bytes, artifact.Hash, artifact.Bytes, bytes => Report("Downloading " + artifact.Name + "…", 100.0 * (index + (double)bytes / artifact.Bytes) / missing.Length, false), token).ConfigureAwait(false);
                        break;
                    }
                    catch (HttpRequestException ex)when (attempt < 2 && (ex.StatusCode == null || (int)ex.StatusCode >= 500))
                    {
                        if (File.Exists(archive))
                            File.Delete(archive);
                        await Task.Delay(TimeSpan.FromSeconds(attempt + 1), token).ConfigureAwait(false);
                    }
                }

                // Test acquisition uses the same mandatory hash and size gate.
                using (var input = File.OpenRead(archive))
                    if (input.Length != artifact.Bytes || !string.Equals(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(input)), artifact.Hash, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("The media tools did not pass their integrity check.");
                if (artifact.Url.EndsWith(".exe", StringComparison.Ordinal))
                    File.Move(archive, Path.Combine(stage, "yt-dlp.exe"));
                else
                {
                    await Task.Run(() => SafeArchives.Extract(archive, stage, artifact.Entries, artifact.ExpandedBytes, token), token).ConfigureAwait(false);
                    File.Delete(archive);
                }

                index++;
            }

            Report("Checking prepared media tools…");
            pieces = (pieces.Yt ?? Path.Combine(stage, "yt-dlp.exe"), pieces.Ffmpeg ?? Path.Combine(stage, "ffmpeg.exe"), pieces.Ffprobe ?? Path.Combine(stage, "ffprobe.exe"), pieces.Deno ?? Path.Combine(stage, "deno.exe"));
            bool valid = await Task.Run(() => probe.Accept(pieces.Yt, "yt-dlp", token) && probe.Accept(pieces.Deno, "Deno", token) && probe.AcceptPair(pieces.Ffmpeg, pieces.Ffprobe, token), token).ConfigureAwait(false);
            if (!valid)
                throw new InvalidDataException("The prepared tools are not compatible with this PC.");
            using (var notices = typeof(DependencyManager).Assembly.GetManifestResourceStream("YouTubeDownloader.ThirdPartyNotices"))
            using (var copy = File.Create(Path.Combine(stage, "THIRD-PARTY-NOTICES.txt")))
                notices.CopyTo(copy);
            token.ThrowIfCancellationRequested();
            BeforePromote?.Invoke();
            // A previous version remains untouched; only this known invalid version is quarantined.
            if (Directory.Exists(target))
                Directory.Move(target, target + ".unusable-" + Guid.NewGuid().ToString("N"));
            Directory.Move(stage, target);
            Report("Media tools are ready.", 100, false);
            string Activated(string file) => file.StartsWith(stage + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? Path.Combine(target, Path.GetFileName(file)) : file;
            return ToolSet.Validated(Activated(pieces.Yt), Activated(pieces.Ffmpeg), Activated(pieces.Ffprobe), Activated(pieces.Deno));
        }
        finally
        {
            CleanupStaging(stage);
        }
    }

    private async Task<FileStream> AcquireLock(CancellationToken token)
    {
        var until = DateTime.UtcNow.AddMinutes(2);
        while (true)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(Path.Combine(cache, "preparation.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)when (DateTime.UtcNow < until)
            {
                await Task.Delay(200, token).ConfigureAwait(false);
            }
        }
    }

    internal ToolSet Discover(CancellationToken token)
    {
        var pieces = DiscoverPieces(token);
        return pieces.Yt != null && pieces.Deno != null && pieces.Ffmpeg != null ? ToolSet.Validated(pieces.Yt, pieces.Ffmpeg, pieces.Ffprobe, pieces.Deno) : null;
    }

    internal ToolSet DiscoverBaseline(CancellationToken token)
    {
        var pieces = DiscoverPieces(token, false);
        return pieces.Yt != null && pieces.Deno != null && pieces.Ffmpeg != null ? ToolSet.Validated(pieces.Yt, pieces.Ffmpeg, pieces.Ffprobe, pieces.Deno) : null;
    }

    private (string Yt, string Ffmpeg, string Ffprobe, string Deno) DiscoverPieces(CancellationToken token, bool includeUpdates = true)
    {
        string yt = includeUpdates ? ToolRefresh.FindVerified(cache, token) : null,
            deno = includeUpdates ? ToolRefresh.FindVerified(cache, token, "Deno") : null, ffmpeg = null, ffprobe = null;
        foreach (string folder in locations)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                if (yt == null)
                {
                    string file = Path.Combine(folder, "yt-dlp.exe");
                    if (probe.Accept(file, "yt-dlp", token))
                        yt = file;
                }

                if (deno == null)
                {
                    string file = Path.Combine(folder, "deno.exe");
                    if (probe.Accept(file, "Deno", token))
                        deno = file;
                }

                if (ffmpeg == null)
                {
                    string file = Path.Combine(folder, "ffmpeg.exe"), pair = Path.Combine(folder, "ffprobe.exe");
                    if (probe.AcceptPair(file, pair, token))
                    {
                        ffmpeg = file;
                        ffprobe = pair;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            if (yt != null && deno != null && ffmpeg != null)
                break;
        }

        return (yt, ffmpeg, ffprobe, deno);
    }

    public void Dispose() => downloads.Dispose();
    private void CleanupStaging(string stage)
    {
        // Only files created under the exact private staging path may be removed.
        string full = Path.GetFullPath(stage), parent = Path.GetFullPath(cache).TrimEnd('\\') + "\\";
        if (!full.StartsWith(parent, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(full).StartsWith("stage-", StringComparison.Ordinal) || !Directory.Exists(full))
            return;
        try
        {
            foreach (string file in Directory.GetFiles(full))
                File.Delete(file);
            Directory.Delete(full);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
