using System.Diagnostics;
using System.IO.Pipes;
using YouTubeDownloader;

internal static class ReviewRegressionChecks
{
    private static int count;
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        count++;
        Console.WriteLine("PASS: " + message);
    }

    internal static async Task Run(string fixtures)
    {
        string root = Path.Combine(AppContext.BaseDirectory, "review-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        CleanupChecks(root);
        await QueueInspectionChecks(root, fixtures);
        await PipeRecoveryChecks();
        Console.WriteLine(count + " review regression checks passed.");
    }

    private static void CleanupChecks(string root)
    {
        string updates = Path.Combine(root, "updates");
        Directory.CreateDirectory(updates);
        UpdateTransaction.RootOverride = updates;
        string Stage(string phase, int pid = 0, long start = 0, int parentPid = 0, long parentStart = 0)
        {
            string stage = Path.Combine(updates, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stage);
            File.WriteAllText(Path.Combine(stage, "update.zip"), "package");
            File.WriteAllText(Path.Combine(stage, "transaction.json"), new JsonCodec().Serialize(new UpdateJournal
            {
                Id = Guid.NewGuid().ToString("N"), Target = Path.Combine(root, "other.exe"), Stage = stage,
                Backup = Path.Combine(stage, "previous"), Phase = phase, HelperPid = pid, HelperStart = start,
                ParentPid = parentPid, ParentStart = parentStart
            }));
            return stage;
        }
        try
        {
            string committed = Stage("committed"), rolledBack = Stage("rolled-back"), recovery = Stage("recovery-required"), prepared = Stage("prepared");
            string stalePrepared = Stage("prepared"); Directory.SetLastWriteTimeUtc(stalePrepared, DateTime.UtcNow.AddDays(-8));
            using var self = Process.GetCurrentProcess();
            string active = Stage("committed", self.Id, self.StartTime.ToUniversalTime().Ticks);
            string activePreparation = Stage("prepared", parentPid: self.Id, parentStart: self.StartTime.ToUniversalTime().Ticks);
            Directory.SetLastWriteTimeUtc(activePreparation, DateTime.UtcNow.AddDays(-8));
            string invalid = Stage("committed"); File.WriteAllText(Path.Combine(invalid, "transaction.json"), "malformed");
            string abandoned = Path.Combine(updates, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(abandoned);
            Directory.SetLastWriteTimeUtc(abandoned, DateTime.UtcNow.AddDays(-8));
            string recent = Path.Combine(updates, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(recent);
            string unrelated = Path.Combine(updates, "not-an-app-stage"); Directory.CreateDirectory(unrelated);
            Directory.SetLastWriteTimeUtc(unrelated, DateTime.UtcNow.AddDays(-8));
            Check(!UpdateTransaction.RecoverFor(Path.Combine(root, "current.exe")), "Stage cleanup does not request recovery for another app target");
            Check(!Directory.Exists(committed) && !Directory.Exists(rolledBack) && !Directory.Exists(abandoned) && !Directory.Exists(stalePrepared), "Later startup removes completed transactions and abandoned week-old preparations");
            Check(new[] { recovery, prepared, active, activePreparation, invalid, recent, unrelated }.All(Directory.Exists), "Cleanup retains recovery, recent preparation, live parent/helper, invalid, recent and unowned stages");
            string outside = Path.Combine(root, "outside"); Directory.CreateDirectory(outside);
            File.WriteAllText(Path.Combine(outside, "keep.txt"), "keep");
            OwnedDirectoryCleanup.TryDelete(updates, outside);
            Check(File.Exists(Path.Combine(outside, "keep.txt")), "Cleanup cannot remove a directory outside its exact owned root");
        }
        finally { UpdateTransaction.RootOverride = null; }

        var store = new ToolUpdateStore(Path.Combine(root, "tools"));
        ToolUpdateStore.Selection Version()
        {
            string folder = Path.Combine(store.Root, "version-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "yt-dlp.exe"), "tool");
            File.WriteAllText(Path.Combine(folder, "deno.exe"), "runtime");
            return new(ToolUpdateStore.Record(folder, Path.Combine(folder, "yt-dlp.exe"), "fixture"), ToolUpdateStore.Record(folder, Path.Combine(folder, "deno.exe"), "fixture"));
        }
        var first = Version(); var second = Version(); var third = Version();
        var state = new ToolUpdateStore.State(third, second);
        store.Save(state, default); store.Prune(state);
        Check(Directory.GetDirectories(store.Root).Length == 2 && store.Verify(second.Deno, "deno.exe", default) != null && store.Verify(third.YtDlp, "yt-dlp.exe", default) != null,
            "Tool pruning retains the active and previous verified sets after successive promotions");
        string quarantineCache = Path.Combine(root, "quarantine"); Directory.CreateDirectory(quarantineCache);
        string stale = Path.Combine(quarantineCache, "tools.unusable-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(stale);
        Directory.SetLastWriteTimeUtc(stale, DateTime.UtcNow.AddDays(-8));
        string fresh = Path.Combine(quarantineCache, "tools.unusable-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(fresh);
        using var manager = new DependencyManager(quarantineCache, Array.Empty<string>());
        manager.PruneQuarantine();
        Check(!Directory.Exists(stale) && Directory.Exists(fresh), "Quarantine cleanup keeps recent tools and removes only owned directories older than a week");
    }

    private static async Task QueueInspectionChecks(string root, string fixtures)
    {
        string toolsRoot = Path.Combine(root, "queue-tools"); Directory.CreateDirectory(toolsRoot);
        foreach (string file in Directory.GetFiles(fixtures)) File.Copy(file, Path.Combine(toolsRoot, Path.GetFileName(file)));
        foreach (string name in new[] { "yt-dlp.exe", "ffmpeg.exe", "ffprobe.exe", "deno.exe" })
            File.Copy(Path.Combine(fixtures, "FakeDownloader.exe"), Path.Combine(toolsRoot, name));
        var tools = ToolSet.Validated(Path.Combine(toolsRoot, "yt-dlp.exe"), Path.Combine(toolsRoot, "ffmpeg.exe"), Path.Combine(toolsRoot, "ffprobe.exe"), Path.Combine(toolsRoot, "deno.exe"));
        using var provider = new YtDlpProvider(tools);
        using var service = new DownloadService(provider, new LocalPlaylistStorage(Path.Combine(root, "playlists")));
        string log = Path.Combine(toolsRoot, "metadata-inspections.txt");
        int Inspections() => File.Exists(log) ? File.ReadAllLines(log).Length : 0;
        foreach (bool audio in new[] { false, true })
        {
            int before = Inspections();
            var request = new DownloadOptions { Url = "https://vimeo.com/76979871", Folder = Path.Combine(root, "queue-" + audio), Audio = audio, Height = 0 };
            var result = await service.DownloadQueueItemAsync(request, null, default);
            Check(File.Exists(result.FilePath) && Inspections() == before + 1, "A queued " + (audio ? "audio" : "video") + " download uses exactly one real metadata process");
        }
        var metadata = await service.InspectAsync("https://vimeo.com/76979871", null, default);
        int prior = Inspections();
        await service.DownloadAsync(new() { Url = "https://vimeo.com/76979871", Folder = Path.Combine(root, "main-download"), Selection = metadata.Qualities[0].Selection }, null, default);
        Check(Inspections() == prior + 1, "Main-window selections still revalidate metadata before downloading");
        prior = Inspections();
        var playlistResult = await service.DownloadAsync(new()
        {
            Url = "https://vimeo.com/76979871", Audio = true, Folder = Path.Combine(root, "playlist-audio"),
            FreshMetadata = new VideoInfo { HasAudio = false },
            Playlist = new PlaylistInfo { Id = "review-playlist", Url = "https://vimeo.com/76979871", Title = "Review playlist", Entries = new() { new() { Id = "one", MediaUrl = "https://vimeo.com/76979871" } } }
        }, null, default);
        Check(playlistResult.Completed == 1 && Inspections() == prior + 1, "Playlist entries inspect their own audio rather than inheriting collection metadata");
    }

    private static async Task PipeRecoveryChecks()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using (var lifetime = new CancellationTokenSource())
        {
            var listener = BrowserLinkTransport.ListenAsync(_ => false, lifetime.Token);
            int launches = 0; var clock = Stopwatch.StartNew();
            var delivery = await BrowserHost.DeliverAsync("https://example.com/video", () => launches++, deadline.Token);
            Check(delivery == BrowserDelivery.Rejected && launches == 0 && clock.Elapsed < TimeSpan.FromSeconds(2), "A busy listener rejects immediately without launching or retrying a second app");
            lifetime.Cancel(); await listener;
        }
        using var recoveryLifetime = new CancellationTokenSource();
        var competing = new NamedPipeServerStream(BrowserLinkTransport.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        var recovering = BrowserLinkTransport.ListenAsync(_ => true, recoveryLifetime.Token);
        try
        {
            await Task.Delay(100);
            Check(!recovering.IsCompleted, "An occupied pipe does not fault or terminate the listener");
            competing.Dispose();
            await Task.Delay(1100);
            Check(await BrowserLinkTransport.TrySendAsync("https://example.com/video", 1500, deadline.Token) == BrowserDelivery.Accepted, "The listener recovers and accepts links after the competing owner exits");
        }
        finally { competing.Dispose(); recoveryLifetime.Cancel(); await recovering; }
    }
}
