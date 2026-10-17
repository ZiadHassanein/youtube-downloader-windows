using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using YouTubeDownloader;

internal static class AutomaticToolChecks
{
    private static int count;
    private static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
        count++;
        Console.WriteLine("PASS: " + name);
    }
    private static string Root() => Path.Combine(AppContext.BaseDirectory, "auto-tools-" + Guid.NewGuid().ToString("N"));
    private static ToolSet Tools(string name) => ToolSet.Validated(name + "/yt-dlp.exe", name + "/ffmpeg.exe", name + "/ffprobe.exe", name + "/deno.exe");
    internal static async Task Run(string fixtureDirectory)
    {
        await ScheduleAndRecovery();
        await VerifiedTransactions();
        AdapterRecovery(fixtureDirectory);
        Console.WriteLine(count + " automatic tool checks passed.");
    }

    private static void AdapterRecovery(string fixtureDirectory)
    {
        ToolSet Prepare(string folder)
        {
            Directory.CreateDirectory(folder);
            foreach (string file in Directory.GetFiles(fixtureDirectory)) File.Copy(file, Path.Combine(folder, Path.GetFileName(file)));
            foreach (string name in new[] { "yt-dlp.exe", "deno.exe", "ffmpeg.exe", "ffprobe.exe" }) File.Copy(Path.Combine(fixtureDirectory, "FakeDownloader.exe"), Path.Combine(folder, name));
            return ToolSet.Validated(Path.Combine(folder, "yt-dlp.exe"), Path.Combine(folder, "ffmpeg.exe"), Path.Combine(folder, "ffprobe.exe"), Path.Combine(folder, "deno.exe"));
        }
        string root = Root(); var old = Prepare(Path.Combine(root, "old")); var candidate = Prepare(Path.Combine(root, "new"));
        File.WriteAllText(Path.Combine(root, "old", "compatibility-failure.flag"), "fixture only");
        var fake = new FakeUpdates { Candidate = candidate };
        using (var provider = new YtDlpProvider(old, new AutomaticToolUpdates(Path.Combine(root, "inspect-cache"), fake)))
        {
            var info = provider.Inspect("https://youtu.be/jNQXAC9IVRw");
            Check(info.Title == "Public media fixture" && fake.Checks == 1, "The actual adapter rebuilds inspection arguments and invokes the newly activated executable on recovery");
        }
        fake = new() { Candidate = candidate };
        using (var provider = new YtDlpProvider(old, new AutomaticToolUpdates(Path.Combine(root, "download-cache"), fake)))
        {
            var result = provider.DownloadItem(new() { Url = "https://youtu.be/jNQXAC9IVRw", Folder = Path.Combine(root, "media"), Audio = true, AudioBitrate = 192 });
            Check(File.Exists(result.FilePath) && fake.Checks == 1 && result.FilePath.EndsWith(".mp3", StringComparison.Ordinal), "A download retry uses the new tool paths and completes media validation through the adapter");
        }
    }

    private sealed class FakeUpdates : IToolUpdateClient
    {
        internal int Checks, Rollbacks;
        internal Exception Failure;
        internal bool Updated = true;
        internal bool WaitForCancellation;
        internal ToolSet Candidate = Tools("new");
        public async Task<(ToolSet Tools, MediaToolUpdateResult Result)> RefreshAsync(ToolSet current, IProgress<DownloadProgress> progress, CancellationToken token)
        {
            Checks++;
            token.ThrowIfCancellationRequested();
            if (WaitForCancellation) await Task.Delay(Timeout.Infinite, token);
            if (Failure != null) throw Failure;
            return (Updated ? Candidate : current, new MediaToolUpdateResult(Updated, "fixture"));
        }
        public Task<ToolSet> RollbackAsync(ToolSet current, ToolSet fallback, CancellationToken token)
        { Rollbacks++; token.ThrowIfCancellationRequested(); return Task.FromResult(fallback); }
    }

    private static async Task ScheduleAndRecovery()
    {
        string root = Root();
        var old = Tools("old");
        var fake = new FakeUpdates();
        var now = DateTimeOffset.Parse("2026-10-16T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var automatic = new AutomaticToolUpdates(root, fake) { UtcNow = () => now };
        var active = await automatic.CheckDailyAsync(old, null, default);
        Check(active == fake.Candidate && fake.Checks == 1, "First use silently checks and activates a verified update without a consent prompt");
        await automatic.CheckDailyAsync(active, null, default);
        automatic = new AutomaticToolUpdates(root, fake) { UtcNow = () => now.AddHours(23) };
        await automatic.CheckDailyAsync(active, null, default);
        Check(fake.Checks == 1, "The daily check survives app restarts and does not repeat within 24 hours");
        automatic.UtcNow = () => now.AddDays(1);
        fake.Updated = false;
        await automatic.CheckDailyAsync(active, null, default);
        Check(fake.Checks == 2, "The next day performs a fresh public update check");
        fake.Failure = new HttpRequestException("Offline");
        automatic.UtcNow = () => now.AddDays(2);
        Check(await automatic.CheckDailyAsync(active, null, default) == active, "Offline daily checks retain working tools and do not block normal use");
        await automatic.CheckDailyAsync(active, null, default);
        Check(fake.Checks == 3, "Failed daily checks are throttled instead of polling every link");
        using (var cancel = new CancellationTokenSource())
        {
            cancel.Cancel(); bool cancelled = false;
            try { await new AutomaticToolUpdates(Root(), fake).CheckDailyAsync(old, null, cancel.Token); }
            catch (OperationCanceledException) { cancelled = true; }
            Check(cancelled, "Daily maintenance preserves explicit user cancellation");
        }
        foreach (string blocked in new[] { "HTTP Error 403: Forbidden", "HTTP Error 429", "Sign in to confirm you're not a bot", "Private video", "Requested format is not available", "Disk full", "Connection timed out", "Unsupported URL", "DRM protected" })
            Check(!AutomaticToolUpdates.IsRecoverable(blocked + " signature extraction failed"), "No compatibility retry for access, storage, format or transport failure: " + blocked);
        foreach (string eligible in new[] { "Signature extraction failed", "nsig extraction failed", "Unable to extract player response", "No supported JavaScript runtime", "Challenge solving failed" })
            Check(AutomaticToolUpdates.IsRecoverable(eligible), "Recognized extractor/runtime compatibility failure: " + eligible);

        fake = new(); automatic = new AutomaticToolUpdates(Root(), fake); active = old;
        int attempts = 0;
        int result = await automatic.RunAsync(() => ++attempts == 1 ? throw new MediaToolExecutionException("Signature extraction failed") : 42,
            () => active, value => active = value, null, default);
        Check(result == 42 && attempts == 2 && fake.Checks == 1 && active == fake.Candidate, "A compatible failure updates and retries the operation exactly once");
        fake = new(); automatic = new AutomaticToolUpdates(Root(), fake); active = old; attempts = 0;
        bool unavailable = false;
        try { await automatic.RunAsync<int>(() => { attempts++; throw new MediaToolExecutionException("Signature extraction failed"); }, () => active, value => active = value, null, default); }
        catch (MediaToolRecoveryException ex) { unavailable = ex.CheckedLatest; }
        Check(unavailable && attempts == 2 && fake.Checks == 1 && fake.Rollbacks == 1 && active == old, "A failed updated retry rolls back, stops looping and explains that no working fix is available");
        fake = new() { Updated = false }; automatic = new AutomaticToolUpdates(Root(), fake); active = old; attempts = 0;
        try { await automatic.RunAsync<int>(() => { attempts++; throw new MediaToolExecutionException("Signature extraction failed"); }, () => active, value => active = value, null, default); }
        catch (MediaToolRecoveryException ex) { unavailable = ex.CheckedLatest; }
        Check(unavailable && attempts == 1 && fake.Checks == 1, "An already-current extractor reports the upstream problem without repeating the same failing request");
        fake = new() { Failure = new HttpRequestException("rate limited", null, HttpStatusCode.Forbidden) }; automatic = new AutomaticToolUpdates(Root(), fake); attempts = 0;
        bool updateFailed = false;
        try { await automatic.RunAsync<int>(() => { attempts++; throw new MediaToolExecutionException("Signature extraction failed"); }, () => old, _ => { }, null, default); }
        catch (MediaToolRecoveryException ex) { updateFailed = !ex.CheckedLatest; }
        Check(updateFailed && attempts == 1, "A failed update does not claim the latest tools were checked or request credentials");
        fake = new(); automatic = new AutomaticToolUpdates(Root(), fake); attempts = 0;
        try { await automatic.RunAsync<int>(() => { attempts++; throw new MediaToolExecutionException("HTTP Error 403: Forbidden"); }, () => old, _ => { }, null, default); }
        catch (MediaToolExecutionException) { }
        Check(attempts == 1 && fake.Checks == 0, "Permission/access failures never trigger automatic updating or retries");
        fake = new() { Updated = false }; active = Tools("new");
        automatic = new AutomaticToolUpdates(Root(), fake) { FindBaseline = _ => old }; attempts = 0;
        result = await automatic.RunAsync(() => ++attempts == 1 ? throw new MediaToolExecutionException("Signature extraction failed") : 42,
            () => active, value => active = value, null, default);
        Check(result == 42 && active == old && fake.Rollbacks == 1, "Recovery can restore the retained baseline after an application restart");
        fake = new() { WaitForCancellation = true }; automatic = new AutomaticToolUpdates(Root(), fake) { UpdateTimeout = TimeSpan.FromMilliseconds(20) };
        Check(await automatic.CheckDailyAsync(old, null, default) == old, "A timed-out silent update keeps the previous tools usable");
        bool timedOut = false;
        try { await automatic.RefreshAsync(old, null, default); } catch (TimeoutException) { timedOut = true; }
        Check(timedOut, "An update deadline is reported as a timeout, not a user cancellation");
        var queue = new DownloadQueue();
        queue.Add(new() { Url = "https://example.com/first" }); queue.Add(new() { Url = "https://example.com/later" }); attempts = 0;
        await queue.RunAsync((_, _, _) => { attempts++; throw new MediaToolRecoveryException(false, new IOException()); }, wait: (_, _) => Task.CompletedTask);
        Check(attempts == 1 && queue.Entries[1].State == QueueState.Pending, "Queue transport retry does not multiply an exhausted compatibility-recovery attempt");
        var request = new DownloadOptions { Folder = Root(), Playlist = new() { Entries = new() { new() { Id = "first", MediaUrl = "https://example.com/first" }, new() { Id = "later", MediaUrl = "https://example.com/later" } } } };
        attempts = 0;
        var playlist = PlaylistScheduler.Run(request, new EmptyStorage(), (_, _) => { attempts++; throw new MediaToolRecoveryException(true, new IOException()); }, null, default);
        Check(attempts == 1 && playlist.StopQueue && playlist.Failed == 1 && playlist.Remaining == 1, "An unfixed compatibility failure stops its playlist before requesting another item");
        queue = new(); queue.Add(new() { Url = "https://example.com/first" }); queue.Add(new() { Url = "https://example.com/later" }); attempts = 0;
        await queue.RunAsync((_, _, _) => { attempts++; return Task.FromResult(playlist); }, wait: (_, _) => Task.CompletedTask);
        Check(attempts == 1 && queue.Entries[1].State == QueueState.Pending, "Playlist stop status also halts later queue jobs without losing them");
    }

    private sealed class EmptyStorage : IPlaylistStorage
    {
        public PlaylistSession Open(DownloadOptions options) => new() { Folder = options.Folder, Completed = new(), Save = () => { } };
        public bool IsCompleted(string folder, string file, DownloadOptions options, bool requireCaptions = false) => false;
    }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { token.ThrowIfCancellationRequested(); return Task.FromResult(respond(request)); }
    }

    private static async Task VerifiedTransactions()
    {
        var yt = new byte[1_100_000]; RandomNumberGenerator.Fill(yt);
        var deno = new byte[1_100_000]; RandomNumberGenerator.Fill(deno);
        byte[] zip;
        using (var memory = new MemoryStream())
        {
            using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
            using (var output = archive.CreateEntry("deno.exe", CompressionLevel.NoCompression).Open()) output.Write(deno);
            zip = memory.ToArray();
        }
        bool corruptDeno = false, badProbe = false, requestHadCredentials = false;
        int assetDownloads = 0;
        string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        HttpResponseMessage Respond(HttpRequestMessage request)
        {
            requestHadCredentials |= request.Headers.Authorization != null;
            string url = request.RequestUri.AbsoluteUri;
            bool isDeno = url.Contains("denoland/deno");
            var source = isDeno ? ToolReleaseSource.Deno : ToolReleaseSource.YtDlp;
            byte[] bytes = isDeno ? zip : yt;
            string tag = isDeno ? "v2.10.0" : "2026.10.16";
            string assetUrl = $"https://github.com/{source.Repository}/releases/download/{tag}/{source.Asset}";
            if (url.EndsWith("/latest", StringComparison.Ordinal)) return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new
                { tag_name = tag, draft = false, prerelease = false, assets = new[] { new { name = source.Asset, size = bytes.Length, browser_download_url = assetUrl, digest = "sha256:" + Hash(bytes) } } })) };
            if (url.EndsWith("SHA2-256SUMS", StringComparison.Ordinal) || url.EndsWith(".sha256sum", StringComparison.Ordinal)) return new(HttpStatusCode.OK) { Content = new StringContent(Hash(bytes) + "  " + source.Asset + "\n") };
            assetDownloads++;
            return new(HttpStatusCode.OK) { Content = new ByteArrayContent(isDeno && corruptDeno ? new byte[zip.Length] : bytes) };
        }
        ToolUpdateClient Client(string root) => new(root)
        {
            OpenDownloads = () => new(new Handler(Respond)),
            Accept = (_, _, _) => !badProbe,
            VersionOf = (file, _) => file.StartsWith("old/", StringComparison.Ordinal) ? (file.EndsWith("deno.exe", StringComparison.Ordinal) ? "2.9.7" : "2026.08.19") : file.EndsWith("deno.exe", StringComparison.Ordinal) ? "2.10.0" : "2026.10.16"
        };
        string root = Root(); var client = Client(root); var old = Tools("old");
        string obsolete = Path.Combine(new ToolUpdateStore(root).Root, "version-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(obsolete);
        File.WriteAllText(Path.Combine(obsolete, "yt-dlp.exe"), "superseded fixture");
        var updated = await client.RefreshAsync(old, null, default);
        var store = new ToolUpdateStore(root); var state = store.Read();
        Check(updated.Result.Updated && File.Exists(updated.Tools.YtDlp) && File.Exists(updated.Tools.Deno) && state.Previous != null, "Both verified tools activate together with a retained previous selection");
        Check(!Directory.Exists(obsolete), "A real tool promotion prunes superseded owned versions after saving its verified selection");
        Check(!requestHadCredentials && assetDownloads == 2, "Update acquisition is anonymous and downloads only the two newer official assets");
        Check(store.Verify(state.Active.YtDlp, "yt-dlp.exe", default) == updated.Tools.YtDlp && store.Verify(state.Active.Deno, "deno.exe", default) == updated.Tools.Deno,
            "The persisted selection revalidates executable sizes and SHA256 after extraction");
        var restored = await client.RollbackAsync(updated.Tools, old, default);
        Check(restored.YtDlp == old.YtDlp && restored.Deno == old.Deno && !File.Exists(updated.Tools.YtDlp) && !File.Exists(updated.Tools.Deno), "Rollback restores the previous pair and prunes the rejected version");
        int before = assetDownloads;
        var skipped = await client.RefreshAsync(old, null, default);
        Check(!skipped.Result.Updated && assetDownloads == before, "A rejected version is not silently reinstalled the following day");
        foreach (string failure in new[] { "checksum", "probe" })
        {
            root = Root(); client = Client(root); corruptDeno = failure == "checksum"; badProbe = failure == "probe";
            bool rejected = false;
            try { await client.RefreshAsync(old, null, default); } catch (InvalidDataException) { rejected = true; }
            Check(rejected && !File.Exists(Path.Combine(root, "MediaToolUpdates", "active.json")) && Directory.GetDirectories(Path.Combine(root, "MediaToolUpdates")).Length == 0,
                "A failed " + failure + " never promotes either component and cleans staging");
        }
        corruptDeno = badProbe = false; root = Root(); client = Client(root);
        updated = await client.RefreshAsync(old, null, default); store = new(root); state = store.Read();
        File.WriteAllText(updated.Tools.Deno, "tampered");
        Check(store.Verify(state.Active.Deno, "deno.exe", default) == null && store.VerifiedSelection(default) == state.Previous,
            "Tampering with one active executable makes discovery fall back to the previous pair");
        var unsafeRecord = state.Active.YtDlp with { Directory = "../elsewhere" };
        Check(store.Verify(unsafeRecord, "yt-dlp.exe", default) == null, "An update marker cannot select an executable outside its owned version directories");
        bool prerelease = false;
        try { ToolReleaseSource.Deno.Read("{\"tag_name\":\"v2.10.0\",\"prerelease\":true}"); } catch (InvalidDataException) { prerelease = true; }
        Check(prerelease, "Automatic updates reject prereleases");
        string windowsChecksum = "\r\nAlgorithm : SHA256\r\nHash : " + Hash(zip).ToUpperInvariant() + "\r\nPath : C:\\a\\deno\\target\\release\\deno-x86_64-pc-windows-msvc.zip\r\n";
        Check(string.Equals(ToolReleaseSource.Deno.ReadChecksum(windowsChecksum), Hash(zip), StringComparison.OrdinalIgnoreCase), "Deno's actual Windows Get-FileHash checksum format is supported");
        bool ambiguous = false;
        try { ToolReleaseSource.Deno.ReadChecksum(windowsChecksum + "Hash : " + Hash(zip) + "\r\n"); } catch (InvalidDataException) { ambiguous = true; }
        Check(ambiguous, "Multiple Windows checksum values are rejected");
    }
}
