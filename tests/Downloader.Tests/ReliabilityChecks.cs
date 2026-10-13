using System.Diagnostics;
using YouTubeDownloader;

internal static class ReliabilityChecks
{
    private static void Check(bool value, string name)
    {
        if (!value)
            throw new Exception(name);
        Console.WriteLine("PASS: " + name);
    }

    internal static async Task Run(string fixtureDirectory)
    {
        string fixture = Path.GetFullPath(Path.Combine(fixtureDirectory, "FakeDownloader.exe"));
        using (var runner = new WindowsProcessRunner())
        {
            var output = new List<string>();
            Check(runner.Run(fixture, new[] { "--echo", "a quote\" and trailing\\" }, output.Add, null, CancellationToken.None, TimeSpan.FromSeconds(5)) == 0 && output.Single() == "a quote\" and trailing\\", "Contained runner preserves Windows argument quoting and UTF-8 output");
            using var token = new CancellationTokenSource(200);
            bool cancelled = false;
            try
            {
                runner.Run(fixture, new[] { "--hang" }, null, null, token.Token);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }

            Check(cancelled && !runner.IsBusy, "Cancellation confirms teardown and permits another operation");
            bool timedOut = false;
            try
            {
                runner.Run(fixture, new[] { "--hang" }, null, null, CancellationToken.None, TimeSpan.FromMilliseconds(200));
            }
            catch (TimeoutException)
            {
                timedOut = true;
            }

            Check(timedOut && !runner.IsBusy, "Hung tool probes are bounded");
            string root = Path.Combine(AppContext.BaseDirectory, "containment-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string writer = Path.Combine(root, "writer.txt"), pidFile = Path.Combine(root, "child.pid");
            using var cancel = new CancellationTokenSource();
            var run = Task.Run(() =>
            {
                try
                {
                    runner.Run(fixture, new[] { "--spawn-writer", writer, pidFile }, null, null, cancel.Token);
                }
                catch (OperationCanceledException)
                {
                }
            });
            for (int n = 0; n < 100 && (!File.Exists(pidFile) || !File.Exists(writer)); n++)
                await Task.Delay(20);
            Check(File.Exists(pidFile), "Immediate child fixture actually spawned a descendant");
            int pid = int.Parse(File.ReadAllText(pidFile));
            cancel.Cancel();
            await run;
            long length = new FileInfo(writer).Length;
            await Task.Delay(150);
            bool alive = false;
            try
            {
                alive = !Process.GetProcessById(pid).HasExited;
            }
            catch (ArgumentException)
            {
            }

            Check(!alive && new FileInfo(writer).Length == length, "Independent child PID and file growth stop after cancellation");
            runner.RejectAssignment = true;
            bool failed = false;
            try
            {
                runner.Run(fixture, new[] { "--write-marker", Path.Combine(root, "must-not-exist") }, null, null, CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                failed = true;
            }

            Check(failed && !File.Exists(Path.Combine(root, "must-not-exist")), "Failed containment never resumes the executable");
        }

        string settingsRoot = Path.Combine(AppContext.BaseDirectory, "settings-" + Guid.NewGuid().ToString("N"));
        var settings = new SettingsRepository(settingsRoot);
        var saved = new SavedState
        {
            Theme = "Light",
            Language = "ar",
            Folder = Path.GetFullPath("."),
            Recent = new()
            {
                null,
                "not-a-path",
                Path.GetFullPath("saved.mp4")
            }
        };
        settings.Save(saved);
        settings.Save(saved);
        File.WriteAllText(settings.FilePath, "truncated");
        Check(settings.Load().Language == "ar" && settings.Load().Recent.Count == 1, "Damaged settings recover valid backup and sanitize history");
        settings.Save(saved);
        string before = File.ReadAllText(settings.FilePath);
        settings.BeforeReplace = () => throw new IOException("Injected replacement failure");
        try
        {
            settings.Save(new SavedState());
        }
        catch (IOException)
        {
        }

        Check(File.ReadAllText(settings.FilePath) == before, "Failed settings commit preserves prior valid bytes");
        settings.BeforeReplace = null;
        string legacyDirectory = Path.Combine(settingsRoot, "previous-app");
        Directory.CreateDirectory(legacyDirectory);
        string legacy = Path.Combine(legacyDirectory, "settings.json");
        File.WriteAllText(legacy, "{\"Language\":\"ar\",\"Theme\":\"Light\",\"AudioBitrate\":192,\"LastUrl\":\"https://youtu.be/jNQXAC9IVRw\",\"Recent\":[null]}");
        string source = File.ReadAllText(legacy);
        var migratedSettings = new SettingsRepository(Path.Combine(settingsRoot, "migrated-preferences"));
        Check(migratedSettings.Load(legacyDirectory).AudioBitrate == 192 && File.ReadAllText(legacy) == source && !File.ReadAllText(migratedSettings.FilePath).Contains("LastUrl"), "Automatic legacy migration preserves original settings without persisting a URL");
        var invalidRoot = Path.Combine(settingsRoot, "invalid-tools");
        Directory.CreateDirectory(invalidRoot);
        File.WriteAllText(Path.Combine(invalidRoot, "yt-dlp.exe"), "not executable");
        var manager = new DependencyManager(Path.Combine(settingsRoot, "cache"), new[] { "", invalidRoot });
        Check(manager.Discover(CancellationToken.None) == null, "Existing unusable executable is never ready");
        var validRoot = Path.Combine(settingsRoot, "fake-tools");
        Directory.CreateDirectory(validRoot);
        foreach (string file in Directory.GetFiles(fixtureDirectory))
            File.Copy(file, Path.Combine(validRoot, Path.GetFileName(file)));
        foreach (string name in new[]
        {
            "yt-dlp.exe",
            "deno.exe",
            "ffmpeg.exe"
        }

        )
            File.Copy(fixture, Path.Combine(validRoot, name));
        manager = new DependencyManager(Path.Combine(settingsRoot, "cache"), new[] { validRoot });
        Check(manager.Discover(CancellationToken.None) == null, "Missing ffprobe prevents a usable tool set");
        File.Copy(fixture, Path.Combine(validRoot, "ffprobe.exe"));
        Check(manager.Discover(CancellationToken.None)?.Ready == true, "Versions and coherent codec/muxer capabilities enable compatible reuse");
        manager.AcquireArtifact = (_, _, _) => throw new Exception("Should never acquire");
        Check((await manager.EnsureAsync(null, CancellationToken.None)).Ready, "Compatible installed tools require zero acquisition requests");
    }
}
