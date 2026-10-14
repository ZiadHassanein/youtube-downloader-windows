using System.Buffers.Binary;
using System.Text.Json;
using YouTubeDownloader;

internal static class WorkflowChecks
{
    private static int count;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        count++;
        Console.WriteLine("PASS: " + name);
    }
    private static bool Reject(Action action)
    { try { action(); return false; } catch (ArgumentException) { return true; } catch (InvalidDataException) { return true; } catch (InvalidOperationException) { return true; } }
    internal static async Task Run(string fixtureDirectory)
    {
        const string youtube = "https://www.youtube.com/watch?v=jNQXAC9IVRw", vimeo = "https://vimeo.com/76979871";
        Check(MediaLink.Normalize("https://youtu.be/jNQXAC9IVRw", false) == youtube, "Link routing preserves canonical YouTube media identity");
        Check(MediaLink.Normalize(vimeo, false) == vimeo, "Link routing accepts public non-YouTube media");
        foreach (string bad in new[] { "file:///C:/private.mp4", "https://u:p@example.com/video", "http://127.0.0.1/video", "http://10.0.0.1/video", "http://[::1]/video", "http://[::ffff:192.168.1.1]/video", "https://example.com:8443/video", "https://example.com/video\n--exec=bad" })
            Check(Reject(() => MediaLink.PublicUrl(bad)), "Public link policy rejects unsafe protocol, address, credentials or control characters");
        var options = new DownloadOptions { Url = youtube, Folder = "C:\\fixture", Audio = true, AudioEncoding = AudioEncoding.Flac, Captions = new("en", false, CaptionFormat.Srt), Playlist = new() { Id = "one", Entries = new() { new() { Id = "first", MediaUrl = youtube } } } };
        var snapshot = options.Snapshot(); options.Playlist.Entries[0].Id = "changed"; options.AudioEncoding = AudioEncoding.Mp3;
        Check(snapshot.Playlist.Entries[0].Id == "first" && snapshot.AudioEncoding == AudioEncoding.Flac && snapshot.Captions.Language == "en", "Request snapshots isolate queue and playlist output profiles");
        Check(OutputProfiles.Key(snapshot) != OutputProfiles.Key(options), "Resume identity distinguishes audio encodings");
        var changedCaption = snapshot.Snapshot(); changedCaption.Captions = new("ar", false, CaptionFormat.Vtt);
        Check(OutputProfiles.Key(changedCaption) != OutputProfiles.Key(snapshot), "Resume identity distinguishes subtitle language and output");
        using (var malformed = JsonDocument.Parse("{\"format\": {\"format_name\": \"mp4\", \"duration\": \"2\"}, \"streams\": [null, {\"codec_type\": 1}]}"))
            Check(!MediaValidator.Matches(malformed.RootElement, new()), "Malformed stream metadata is rejected without leaking a parser failure");
        var silent = AvailableFormats.Read(new() { ["formats"] = new[] { new Dictionary<string, object> { ["format_id"] = "silent", ["vcodec"] = "h264", ["acodec"] = "none", ["height"] = 720, ["width"] = 1280 } } });
        Check(silent.Single().Selector == "silent", "A legitimate silent video does not require a nonexistent audio stream");

        string root = Path.Combine(AppContext.BaseDirectory, "workflow-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        foreach (string file in Directory.GetFiles(fixtureDirectory)) File.Copy(file, Path.Combine(root, Path.GetFileName(file)));
        foreach (string name in new[] { "yt-dlp.exe", "ffmpeg.exe", "ffprobe.exe", "deno.exe" }) File.Copy(Path.Combine(root, "FakeDownloader.exe"), Path.Combine(root, name));
        var tools = ToolSet.Validated(Path.Combine(root, "yt-dlp.exe"), Path.Combine(root, "ffmpeg.exe"), Path.Combine(root, "ffprobe.exe"), Path.Combine(root, "deno.exe"));
        using var provider = new YtDlpProvider(tools);
        var a = provider.Inspect(youtube); var selection = a.Qualities.Single().Selection;
        provider.Inspect(vimeo); provider.Inspect(youtube);
        var request = new DownloadOptions { Url = youtube, Folder = Path.Combine(root, "Media"), Height = selection.Resolution, Selection = selection };
        var result = provider.DownloadItem(request);
        Check(File.Exists(result.FilePath) && request.FormatSelector.Contains("youtube-fixture"), "A/B/A inspections retain the first video's immutable selected format");
        File.WriteAllText(Path.Combine(root, "missing-quality.flag"), "fixture");
        Check(Reject(() => provider.DownloadItem(request)), "Disappearing selected format fails instead of silently changing quality");
        File.Delete(Path.Combine(root, "missing-quality.flag"));
        var v = provider.Inspect(vimeo);
        var nonYoutube = new DownloadOptions { Url = vimeo, Folder = Path.Combine(root, "Vimeo"), Height = 360, Selection = v.Qualities.Single().Selection, Captions = new("en", false, CaptionFormat.Srt) };
        var nonYoutubeArgs = Arguments.ForDownload(tools, nonYoutube);
        Check(nonYoutubeArgs.Last() == vimeo && nonYoutubeArgs[^2] == "--", "Non-YouTube URL reaches download arguments without command injection");
        result = provider.DownloadItem(nonYoutube);
        Check(MediaFiles.IsCompleted(nonYoutube.Folder, result.FilePath, nonYoutube) && File.Exists(Path.ChangeExtension(result.FilePath, null) + ".en.srt"), "Vimeo fixture passes inspection, download, media validation and subtitle export");
        Check(v.Captions.Single().Language == "en" && !v.Captions.Single().Automatic, "Subtitle choices come from inspected metadata");
        var unavailableAudioCaptions = new DownloadOptions { Url = vimeo, Folder = Path.Combine(root, "RequiredAudioCaptions"), Audio = true, Captions = new("ar", false, CaptionFormat.Srt, Required: true) };
        Check(Reject(() => provider.DownloadItem(unavailableAudioCaptions)), "Required subtitles are refreshed for audio downloads as well as video");
        foreach (AudioEncoding encoding in Enum.GetValues<AudioEncoding>())
        {
            var audio = new DownloadOptions { Url = vimeo, Folder = Path.Combine(root, encoding.ToString()), Audio = true, AudioEncoding = encoding, AudioBitrate = 320 };
            var args = Arguments.ForDownload(tools, audio);
            result = provider.DownloadItem(audio);
            Check(args[args.IndexOf("--audio-format") + 1] == OutputProfiles.AudioExtension(encoding) && MediaFiles.IsCompleted(audio.Folder, result.FilePath, audio), "Audio profile passes argument generation and completed-container validation: " + encoding);
            Check(args.Contains("--audio-quality") != OutputProfiles.IsLossless(encoding), "Lossless profiles do not claim a lossy bitrate target: " + encoding);
        }
        nonYoutube.Captions = new("en.*|all", true, CaptionFormat.Srt);
        Check(Reject(() => Arguments.ForDownload(tools, nonYoutube)), "Subtitle language cannot inject a regex or option");
        Check(Reject(() => ToolRefresh.ReadRelease("{\"tag_name\":\"2026.08.19\",\"assets\":[{\"name\":\"yt-dlp.exe\",\"size\":2000000,\"browser_download_url\":\"https://evil.example/tool.exe\"}]}")), "Tool refresh rejects an unexpected publisher origin");
        Check(Reject(() => ToolRefresh.ReadChecksum("invalid yt-dlp.exe")), "Tool refresh requires a unique published SHA256");
        string hash = new('a', 64);
        Check(ToolRefresh.ReadChecksum(hash + "  yt-dlp.exe\n") == hash, "Tool refresh reads the official checksum format");
        using (var service = new DownloadService(new YtDlpProvider(tools), new LocalPlaylistStorage(Path.Combine(root, "PlaylistState"))))
        {
            var playlist = await service.InspectPlaylistAsync("https://www.youtube.com/playlist?list=PLFixture", null, CancellationToken.None);
            playlist.Entries = playlist.Entries.Where(e => e.Id == "jNQXAC9IVRw").ToList();
            var playlistRequest = new DownloadOptions { Url = playlist.Url, Playlist = playlist, Folder = Path.Combine(root, "SelectedPlaylist"), Audio = true, AudioEncoding = AudioEncoding.Opus, Captions = new("en", false, CaptionFormat.Srt) };
            var selected = await service.DownloadAsync(playlistRequest, null, CancellationToken.None);
            Check(selected.Total == 1 && selected.Completed == 1 && selected.Failed == 0 && selected.Files.Single().EndsWith(".opus"), "Selected playlist propagates audio encoding and subtitles through the scheduler");
            var resumed = await service.DownloadAsync(playlistRequest, null, CancellationToken.None);
            Check(resumed.Skipped == 1, "Matching audio/subtitle profile resumes completed selected entries");
            string caption = Path.ChangeExtension(selected.FilePath, null) + ".en.srt";
            File.Delete(caption);
            resumed = await service.DownloadAsync(playlistRequest, null, CancellationToken.None);
            Check(resumed.Skipped == 0 && File.Exists(caption), "Missing subtitle sidecar prevents an incomplete profile from being skipped");
        }
        using var pipeLifetime = new CancellationTokenSource();
        var received = new List<string>();
        var listener = BrowserLinkTransport.ListenAsync(link => { received.Add(link); return true; }, pipeLifetime.Token);
        using var pipeDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        Check(await BrowserLinkTransport.TrySendAsync(vimeo, 1000, pipeDeadline.Token) && received.Single() == vimeo, "Current-user pipe acknowledges reviewed link delivery without a download command");
        pipeLifetime.Cancel(); await listener;

        await QueueChecks();
        await BrowserChecks();
        Console.WriteLine(count + " media workflow checks passed.");
    }
    private static async Task QueueChecks()
    {
        var queue = new DownloadQueue();
        var firstOptions = new DownloadOptions { Url = "https://example.com/a", Folder = "fixture", Audio = true, AudioEncoding = AudioEncoding.Opus };
        queue.Add(firstOptions); firstOptions.AudioEncoding = AudioEncoding.Mp3;
        queue.Add(new() { Url = "https://example.com/b", Folder = "fixture" });
        Check(queue.Entries[0].Options.AudioEncoding == AudioEncoding.Opus, "Queue snapshots profiles before the form changes");
        Check(Reject(() => queue.Add(queue.Entries[0].Options)), "Queue rejects a duplicate pending output");
        var order = new List<string>(); int attempts = 0;
        await queue.RunAsync((request, progress, token) =>
        {
            order.Add(request.Url);
            if (++attempts == 1) throw new IOException("Connection reset");
            progress.Report(new() { Stage = "Downloading", Percent = 50 });
            return Task.FromResult(new DownloadResult());
        }, wait: (_, _) => Task.CompletedTask);
        Check(order.Count == 3 && order[0] == order[1] && order[2].EndsWith("/b") && queue.Entries.All(e => e.State == QueueState.Completed), "Queue retries transient failure once then preserves sequential order");
        queue = new(); queue.Add(new() { Url = "https://example.com/rate", Folder = "fixture" }); queue.Add(new() { Url = "https://example.com/later", Folder = "fixture" });
        attempts = 0;
        await queue.RunAsync((_, _, _) => { attempts++; throw new IOException("HTTP 429 Too Many Requests"); }, wait: (_, _) => Task.CompletedTask);
        Check(attempts == 1 && queue.Entries[0].State == QueueState.Failed && queue.Entries[1].State == QueueState.Pending, "Rate-limit failure halts queue without repeated requests");
        Check(queue.Retry(queue.Entries[0].Id) && queue.Entries[0].State == QueueState.Pending, "Failed job can be explicitly retried");
        IProgress<DownloadProgress> stale = null;
        await queue.RunAsync((_, sink, _) => { stale = sink; throw new IOException("HTTP 429 Too Many Requests"); });
        queue.Retry(queue.Entries[0].Id);
        stale.Report(new() { Stage = "Downloading", Percent = 95 });
        Check(queue.Entries[0].State == QueueState.Pending && queue.Entries[0].Percent == 0, "Progress from a previous queue attempt cannot corrupt a retried pending job");
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var active = queue.RunAsync(async (_, _, token) => { started.SetResult(); await Task.Delay(30000, token); return new(); });
        await started.Task;
        Check(!queue.Remove(queue.Entries[0].Id), "Active queue item cannot be removed during execution");
        Check(Reject(() => queue.RunAsync((_, _, _) => Task.FromResult(new DownloadResult())).GetAwaiter().GetResult()), "Queue permits only one dispatcher");
        queue.Stop(); await active;
        Check(!queue.IsRunning && queue.Entries[0].State == QueueState.Cancelled && queue.Entries[1].State == QueueState.Pending, "Cancellation tears down current job and retains pending items");
        queue = new(); for (int i = 0; i < DownloadQueue.Capacity; i++) queue.Add(new() { Url = "https://example.com/" + i });
        Check(Reject(() => queue.Add(new() { Url = "https://example.com/overflow" })), "Session queue enforces its capacity");
    }
    private static async Task BrowserChecks()
    {
        var stream = new MemoryStream();
        await BrowserProtocol.WriteAsync(stream, new { version = 1, type = "open-link", url = "https://vimeo.com/76979871" }, CancellationToken.None);
        using var chunked = new ChunkStream(stream.ToArray());
        using var message = await BrowserProtocol.ReadAsync(chunked, CancellationToken.None);
        Check(BrowserProtocol.ReadUrl(message.RootElement) == "https://vimeo.com/76979871", "Browser framing survives partial header and body reads");
        var oversized = new byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(oversized, BrowserProtocol.MaximumBytes + 1);
        bool rejected = false; try { await BrowserProtocol.ReadAsync(new MemoryStream(oversized), CancellationToken.None); } catch (InvalidDataException) { rejected = true; }
        Check(rejected, "Oversized browser message is rejected before allocation");
        using var wrong = JsonDocument.Parse("{\"version\":1,\"type\":\"download-now\",\"url\":\"https://example.com/a\"}");
        Check(Reject(() => BrowserProtocol.ReadUrl(wrong.RootElement)), "Browser protocol rejects automatic-download commands");
        using var local = JsonDocument.Parse("{\"version\":1,\"type\":\"open-link\",\"url\":\"file:///C:/secret\"}");
        Check(Reject(() => BrowserProtocol.ReadUrl(local.RootElement)), "Browser URL is validated again at the native boundary");
        Check(BrowserHost.IsAllowedCaller(new[] { "chrome-extension://" + BrowserIdentity.ChromiumId + "/", "--parent-window=0" }) && !BrowserHost.IsAllowedCaller(new[] { "chrome-extension://aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/" }), "Native host accepts only the configured Chromium extension origin");
        Check(BrowserHost.IsAllowedCaller(new[] { "C:\\fixture\\" + BrowserIdentity.HostName + ".firefox.json", BrowserIdentity.FirefoxId }) && !BrowserHost.IsAllowedCaller(new[] { "C:\\fixture\\" + BrowserIdentity.HostName + ".firefox.json", "other@extension" }), "Native host accepts only the configured Firefox extension identity");
    }
    private sealed class ChunkStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default) => base.ReadAsync(buffer[..Math.Min(1, buffer.Length)], token);
    }
}
