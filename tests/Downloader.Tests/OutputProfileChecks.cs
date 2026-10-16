using System.Text.Json;
using YouTubeDownloader;

internal static class OutputProfileChecks
{
    internal static void Run(string fixture)
    {
        int count = 0;
        void Check(bool condition, string label) { if (!condition) throw new Exception(label); count++; Console.WriteLine("PASS: " + label); }
        bool Reject(Action action) { try { action(); return false; } catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException) { return true; } }
        var json = new JsonCodec();
        const string directUrl = "https://www.w3schools.com/html/mov_bbb.mp4";
        Dictionary<string, object> Direct() => json.Deserialize<Dictionary<string, object>>("{\"extractor_key\":\"Generic\",\"formats\":[{\"format_id\":\"mp4\",\"url\":\"" + directUrl + "\",\"protocol\":\"https\",\"vcodec\":null}]}");
        var direct = Direct();
        Check(DirectMediaMetadata.NeedsProbe(direct, directUrl), "Generic direct MP4 metadata is probed instead of rejected or guessed");
        Check(!DirectMediaMetadata.NeedsProbe(direct, "https://example.com/other.mp4") && !DirectMediaMetadata.NeedsProbe(direct, "file:///C:/private.mp4"), "Direct-media probing is restricted to the same HTTP resource");
        var measured = json.Deserialize<Dictionary<string, object>>("{\"streams\":[{\"codec_type\":\"video\",\"codec_name\":\"h264\",\"width\":320,\"height\":176},{\"codec_type\":\"audio\",\"codec_name\":\"aac\",\"bit_rate\":\"160545\"}],\"format\":{\"duration\":\"10.026667\",\"size\":\"788493\",\"bit_rate\":\"629116\"}}");
        DirectMediaMetadata.Apply(direct, measured);
        Check(AvailableFormats.Read(direct).Single().Resolution == 176 && AvailableFormats.HasAudio(direct), "Direct file uses measured 176p H264/AAC streams without fictional qualities");
        Check(AudioMetadata.Read(direct).BitrateKbps == 160.545 && OutputProfiles.AudioBitrates(AudioEncoding.Mp3, AudioMetadata.Read(direct)).Max() == 160, "Measured direct-file audio bounds conversion targets independently of combined bitrate");
        Check(!DirectMediaMetadata.NeedsProbe(direct, directUrl) && AvailableFormats.Read(direct, VideoContainer.WebM).Count == 0, "Known direct media avoids redundant probing and incompatible WebM output");
        Check(Reject(() => DirectMediaMetadata.Apply(Direct(), json.Deserialize<Dictionary<string, object>>("{\"streams\":[{\"codec_type\":\"video\",\"codec_tag_string\":\"encv\"}]}"))), "Encrypted direct streams are not offered as ordinary video");
        Check(DirectMediaMetadata.ProbeArguments(directUrl).Contains("http,https,tcp,tls") && DirectMediaMetadata.ProbeArguments(directUrl).Contains("-tls_verify"), "Direct-media probing keeps bounded HTTP protocols and peer verification");
        var source = AudioMetadata.Read(json.Deserialize<Dictionary<string, object>>("{\"acodec\":\"opus\",\"vcodec\":\"none\",\"abr\":126.2,\"tbr\":140}"));
        Check(source.Codec == "opus" && source.BitrateKbps == 126.2 && !source.Lossless,
            "Audio source estimates use the selected audio stream's reported bitrate");
        var unknown = AudioMetadata.Read(json.Deserialize<Dictionary<string, object>>("{\"acodec\":\"aac\",\"vcodec\":\"h264\",\"tbr\":8000}"));
        Check(!unknown.HasBitrate && OutputProfiles.AudioBitrates(AudioEncoding.Mp3, unknown).Count == 0,
            "A combined video bitrate cannot masquerade as known audio quality");
        Check(OutputProfiles.AudioBitrates(AudioEncoding.Mp3, source).Max() == 112 && OutputProfiles.AudioBitrates(AudioEncoding.Mp3, null).Count == 0,
            "Uninspected audio offers no numeric targets and lossy estimates bound numeric choices");
        var lossless = new AudioSourceInfo("flac", null, true);
        Check(OutputProfiles.AudioBitrates(AudioEncoding.Mp3, lossless).Max() == 320,
            "Verified lossless sources may use the encoder's complete supported target range");
        Check(Reject(() => OutputProfiles.EffectiveAudioBitrate(new() { AudioEncoding = AudioEncoding.Mp3, AudioBitrate = 320, AudioSource = source })),
            "A changed source cannot silently accept a stale higher numeric target");
        Check(OutputProfiles.EffectiveAudioBitrate(new() { AudioBitrate = 0, AudioSource = unknown }) == 0,
            "Automatic conversion makes no fabricated numeric bitrate promise");
        var original = new DownloadOptions { VideoContainer = VideoContainer.WebM, AudioSource = source };
        var snapshot = original.Snapshot(); original.VideoContainer = VideoContainer.Mkv;
        Check(snapshot.VideoContainer == VideoContainer.WebM && snapshot.AudioSource == source && OutputProfiles.Key(original) != OutputProfiles.Key(snapshot),
            "Output snapshots and resume identities distinguish containers and retain immutable source facts");
        var metadata = json.Deserialize<Dictionary<string, object>>("{\"formats\":[{\"format_id\":\"mp4\",\"vcodec\":\"h264\",\"acodec\":\"aac\",\"height\":360},{\"format_id\":\"web\",\"vcodec\":\"vp9\",\"acodec\":\"none\",\"height\":720},{\"format_id\":\"audio\",\"vcodec\":\"none\",\"acodec\":\"opus\"}]}");
        var web = AvailableFormats.Read(metadata, VideoContainer.WebM);
        Check(web.Count == 1 && web[0].Resolution == 720 && !web[0].Selector.Contains("mp4") && web[0].Selector.Contains("acodec^=opus"),
            "WebM offers only compatible inspected video and audio combinations");
        Check(AvailableFormats.Read(json.Deserialize<Dictionary<string, object>>("{\"formats\":[{\"format_id\":\"h\",\"vcodec\":\"h264\",\"acodec\":\"aac\",\"height\":720}]}"), VideoContainer.WebM).Count == 0,
            "An incompatible source does not offer a fictional WebM conversion");
        var saved = new SettingsRepository(Path.Combine(AppContext.BaseDirectory, "output-settings-" + Guid.NewGuid().ToString("N")));
        saved.Save(new() { VideoContainer = VideoContainer.WebM, UnseenDownloads = 7 });
        Check(saved.Load().VideoContainer == VideoContainer.WebM && saved.Load().UnseenDownloads == 7,
            "The chosen container and unseen completion count persist independently");
        saved.Save(new() { VideoContainer = (VideoContainer)99, UnseenDownloads = -1 });
        Check(saved.Load().VideoContainer == VideoContainer.Mp4 && saved.Load().UnseenDownloads == 0,
            "Malformed saved output/count state recovers to safe defaults");
        string root = Path.Combine(AppContext.BaseDirectory, "output-profile-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        foreach (string file in Directory.GetFiles(fixture)) File.Copy(file, Path.Combine(root, Path.GetFileName(file)));
        foreach (string name in new[] { "yt-dlp.exe", "ffmpeg.exe", "ffprobe.exe", "deno.exe" }) File.Copy(Path.Combine(root, "FakeDownloader.exe"), Path.Combine(root, name));
        var tools = ToolSet.Validated(Path.Combine(root, "yt-dlp.exe"), Path.Combine(root, "ffmpeg.exe"), Path.Combine(root, "ffprobe.exe"), Path.Combine(root, "deno.exe"));
        using var provider = new YtDlpProvider(tools);
        const string url = "https://www.youtube.com/watch?v=jNQXAC9IVRw";
        var video = provider.Inspect(url);
        Check(video.QualitiesFor(VideoContainer.Mkv).Count == 1 && video.QualitiesFor(VideoContainer.WebM).Count == 0,
            "Provider inspection exposes only container-specific source choices");
        var mkv = new DownloadOptions { Url = url, Folder = root, VideoContainer = VideoContainer.Mkv, Height = 360, Selection = video.QualitiesFor(VideoContainer.Mkv).Single().Selection };
        var result = provider.DownloadItem(mkv);
        Check(result.FilePath.EndsWith(".mkv") && MediaFiles.IsCompleted(root, result.FilePath, mkv),
            "MKV reaches downloader arguments, output validation and completed-file handling");
        var wrong = mkv.Snapshot(); wrong.VideoContainer = VideoContainer.WebM;
        Check(Reject(() => provider.DownloadItem(wrong)), "Immutable selections cannot be reused under a different container");
        var audio = new DownloadOptions { Folder = root, Audio = true, AudioEncoding = AudioEncoding.M4a, AudioBitrate = 128, AudioSource = new("aac", 320) };
        string owned = Path.Combine(root, "owned.m4a"); File.WriteAllText(owned, "Original synthetic audio");
        File.WriteAllText(Path.Combine(root, "convert-fail.flag"), "fail");
        Check(Reject(() => AudioBitrateConversion.Apply(owned, audio, tools, CancellationToken.None)) && File.ReadAllText(owned) == "Original synthetic audio" && !Directory.GetFiles(root, ".vifree-audio-*").Any(),
            "Failed same-codec conversion preserves the original file and removes its private partial output");
        File.Delete(Path.Combine(root, "convert-fail.flag"));
        AudioBitrateConversion.Apply(owned, audio, tools, CancellationToken.None);
        Check(File.ReadAllText(owned) == "Original synthetic audio" && !Directory.GetFiles(root, ".vifree-audio-*").Any(),
            "Successful same-codec conversion verifies the staged output before replacement");
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        bool cancellationObserved = false;
        try { AudioBitrateConversion.Apply(owned, audio, tools, cancelled.Token); } catch (OperationCanceledException) { cancellationObserved = true; }
        Check(cancellationObserved && File.Exists(owned), "Cancelling audio conversion keeps the original file");
        foreach (VideoContainer container in Enum.GetValues<VideoContainer>())
        {
            using var probe = JsonDocument.Parse("{\"format\":{\"format_name\":\"" + (container == VideoContainer.Mp4 ? "mov,mp4" : "matroska,webm") + "\",\"duration\":\"1\"},\"streams\":[{\"codec_type\":\"video\"}]}");
            Check(MediaValidator.Matches(probe.RootElement, new() { VideoContainer = container }), "Actual container metadata validates for " + container);
        }
        Console.WriteLine(count + " output profile checks passed.");
    }
}
