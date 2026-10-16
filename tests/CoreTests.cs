using YouTubeDownloader;

public static class CoreTests
{
    private static int checks;
    private static void Check(bool condition, string name)
    {
        if (!condition)
            throw new Exception(name);
        checks++;
        Console.WriteLine("PASS: " + name);
    }

    public static int Run()
    {
        try
        {
            const string expected = "https://www.youtube.com/watch?v=jNQXAC9IVRw";
            foreach (string url in new[]
            {
                expected,
                expected + "&list=example",
                "https://youtu.be/jNQXAC9IVRw?t=3",
                "https://youtube.com/shorts/jNQXAC9IVRw"
            }

            )
                Check(VideoLink.Normalize(url) == expected, "Normalize video URL");
            foreach (string url in new[]
            {
                "",
                "https://example.com/watch?v=jNQXAC9IVRw",
                "https://youtube.com.evil.invalid/watch?v=jNQXAC9IVRw",
                "file:///C:/Windows/system32/cmd.exe",
                "https://youtube.com/playlist?list=bad%26--exec",
                "https://youtu.be/jNQXAC9IVRw\n--exec"
            }

            )
            {
                try
                {
                    VideoLink.Normalize(url);
                    throw new Exception("Invalid link was accepted");
                }
                catch (ArgumentException)
                {
                    checks++;
                }
            }

            var serializer = new JsonCodec();
            var metadata = serializer.Deserialize<Dictionary<string, object>>(@"{""formats"":[{""format_id"":""hd"",""height"":720,""width"":1280,""vcodec"":""avc1"",""acodec"":""none""},{""format_id"":""sd"",""height"":480,""width"":854,""vcodec"":""avc1"",""acodec"":""none""},{""format_id"":""audio"",""vcodec"":""none"",""acodec"":""aac""}]}");
            var qualities = AvailableFormats.Read(metadata);
            Check(qualities.Select(x => x.Resolution).SequenceEqual(new[] { 720, 480 }), "Only actual video qualities are offered");
            Check(qualities[AvailableFormats.RecommendedIndex(qualities)].Resolution == 720, "Do not suggest unavailable 1080p");
            Check(AvailableFormats.HasAudio(metadata), "Recognize an audio stream");
            var portrait = serializer.Deserialize<Dictionary<string, object>>(@"{""formats"":[{""format_id"":""short"",""height"":1920,""width"":1080,""vcodec"":""avc1"",""acodec"":""none""},{""format_id"":""locked"",""height"":2160,""width"":3840,""vcodec"":""avc1"",""has_drm"":true}]}");
            Check(AvailableFormats.Read(portrait).Single().Resolution == 1080, "Portrait quality and protected-format filtering");
            var tools = ToolSet.Validated("yt-dlp.exe", "ffmpeg.exe", "ffprobe.exe", "deno.exe");
            var options = new DownloadOptions
            {
                Url = expected,
                Folder = Environment.CurrentDirectory,
                Height = 720,
                FormatSelector = qualities[0].Selector
            };
            var args = Arguments.ForDownload(tools, options);
            Check(args.Contains(qualities[0].Selector), "Use actual format IDs");
            Check(args.Contains("%(title).100B [%(id)s] - 720p.%(ext)s"), "Separate filenames by selected quality");
            Check(args.Contains("--ignore-config") && args.Contains("--no-playlist") && args.Contains("--no-overwrites"), "Keep predictable single-video behavior");
            Check(args.Last() == expected && args[args.Count - 2] == "--", "Safe final URL argument");
            options.Audio = true;
            Check(Arguments.ForDownload(tools, options).Contains("--extract-audio"), "MP3 extraction");
            options.AudioBitrate = 320;
            Check(Arguments.ForDownload(tools, options).Contains("320K") && Arguments.ForDownload(tools, options).Contains("%(title).100B [%(id)s] - 320kbps.%(ext)s"), "320 kbps MP3 uses the chosen encoding and a distinct filename");
            options.AudioBitrate = 192;
            Check(Arguments.ForDownload(tools, options).Contains("192K"), "192 kbps MP3 remains available");
            Check(Arguments.Quote("a\"b") == "\"a\\\"b\"", "Windows argument quoting");
            Check(VideoLink.Normalize(expected + "&list=PLExample", true) == "https://www.youtube.com/playlist?list=PLExample", "A mixed link has an explicit whole-playlist target");
            Check(VideoLink.Parse("https://youtu.be/jNQXAC9IVRw?list=PLExample").VideoUrl == expected, "Shared playlist links retain the single-video choice");
            Check(VideoLink.Parse("https://www.youtube.com/playlist?list=PLExample").VideoUrl == null, "Playlist-only links cannot pretend to be a single video");
            const string mix = "https://www.youtube.com/watch?v=YKMJXdNBvNQ&list=RDYKMJXdNBvNQ&start_radio=1";
            Check(VideoLink.Normalize(mix, true) == mix && VideoLink.Normalize(VideoLink.Normalize(mix, true), true) == mix,
                "YouTube Mix normalization preserves its starting video and is idempotent");
            Check(VideoLink.Normalize(mix, false) == "https://www.youtube.com/watch?v=YKMJXdNBvNQ" &&
                VideoLink.Normalize("https://www.youtube.com/playlist?list=RDYKMJXdNBvNQ", true) == mix,
                "Mix links retain single-video scope and recover an embedded starting video");
            bool rejectsUnseededMix = false;
            try { VideoLink.Parse("https://www.youtube.com/playlist?list=RDMM"); }
            catch (ArgumentException error) { rejectsUnseededMix = error.Message.Contains("playing video"); }
            Check(rejectsUnseededMix, "A Mix without a starting video provides an actionable validation error");
            PlaylistChecks(serializer, tools);
            Console.WriteLine(checks + " core checks passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void PlaylistChecks(JsonCodec json, ToolSet tools)
    {
        string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "playlist-fixture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            const string url = "https://www.youtube.com/playlist?list=PLFixture";
            var info = PlaylistMetadata.Read(url, json.Deserialize<Dictionary<string, object>>("{\"title\":\"Test\",\"entries\":[{\"id\":\"jNQXAC9IVRw\"},null,{\"id\":\"aqz-KE-bpKQ\"}]}"));
            var mixInfo = PlaylistMetadata.Read("https://www.youtube.com/watch?v=YKMJXdNBvNQ&list=RDYKMJXdNBvNQ", json.Deserialize<Dictionary<string, object>>(
                "{\"title\":\"Mix\",\"entries\":[{\"id\":\"jNQXAC9IVRw\",\"thumbnails\":[{\"url\":\"https://i.ytimg.com/fixture.jpg\"}]},{\"id\":\"jNQXAC9IVRw\"},{\"id\":\"aqz-KE-bpKQ\"}]}"));
            Check(mixInfo.IsMix && mixInfo.Entries.Count == 2 && mixInfo.ThumbnailUrl == "https://i.ytimg.com/fixture.jpg" &&
                new DownloadOptions { Playlist = mixInfo }.Snapshot().Playlist.IsMix,
                "Mix metadata deduplicates videos, retains entry artwork and preserves identity in request snapshots");
            Check(info.Entries.Count == 3 && info.Entries[1].Url == null, "Flat metadata retains unavailable entries for an honest count");
            var options = new DownloadOptions
            {
                Url = url,
                Folder = root,
                Playlist = info,
                Height = 720,
                FormatSelector = "wrong-format-from-other-video",
                ResumeDirectory = Path.Combine(root, "private-state")
            };
            var args = Arguments.ForDownload(tools, options);
            Check(args.Contains("--yes-playlist") && !args.Contains("--no-playlist") && !args.Contains(options.FormatSelector), "Playlist arguments use the playlist target and per-video quality selection");
            Check(args.Contains("--sleep-requests") && args.Contains("--continue") && args.Contains("--match-filters"), "Playlist items use request pacing, partial-file resume, and live-stream filtering");
            Check(args[args.IndexOf("--format") + 1].Contains("width<=720") && args[args.IndexOf("--format") + 1].Contains("height<=720"), "Maximum quality handles portrait and landscape media");
            int downloads = 0;
            Func<DownloadOptions, CancellationToken, DownloadResult> save = delegate (DownloadOptions item, CancellationToken token)
            {
                downloads++;
                string file = Path.Combine(item.Folder, VideoLink.Parse(item.Url).VideoUrl.Substring(item.Url.Length - 11) + ".mp4");
                File.WriteAllText(file, "fixture");
                return new DownloadResult
                {
                    FilePath = file
                };
            };
            Action<CancellationToken> noDelay = delegate (CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
            };
            var first = PlaylistScheduler.Run(options, new LocalPlaylistStorage(options.ResumeDirectory), save, null, CancellationToken.None, noDelay);
            Check(first.Completed == 2 && first.Failed == 1 && first.Remaining == 0 && first.Files.Count == 2 && downloads == 2, "Queue continues past unavailable entries and counts completed files");
            string playlistFolder = LocalPlaylistStorage.OutputFolder(options);
            Check(Directory.GetFiles(playlistFolder, "*.json").Length == 0 && Directory.GetFiles(options.ResumeDirectory, "*.json").Length == 1, "Completed playlists put resume JSON in private state rather than the media folder");
            Check(PlaylistRecords.RecordPath(playlistFolder, "mp4-720", options.ResumeDirectory) == PlaylistRecords.RecordPath(playlistFolder.ToUpperInvariant() + "\\", "mp4-720", options.ResumeDirectory), "Resume identity is stable across Windows path casing and trailing separators");
            var second = PlaylistScheduler.Run(options, new LocalPlaylistStorage(options.ResumeDirectory), save, null, CancellationToken.None, noDelay);
            Check(second.Skipped == 2 && downloads == 2, "Repeat runs skip completed files before contacting YouTube");
            File.Delete(first.Files[0]);
            var third = PlaylistScheduler.Run(options, new LocalPlaylistStorage(options.ResumeDirectory), save, null, CancellationToken.None, noDelay);
            Check(third.Skipped == 1 && downloads == 3, "Deleted completed files are downloaded again");
            var legacyData = new Dictionary<string, string>
            {
                {
                    "jNQXAC9IVRw",
                    first.Files[0]
                },
                {
                    "aqz-KE-bpKQ",
                    first.Files[1]
                }
            };
            options.Height = 1080;
            string legacy = Path.Combine(playlistFolder, ".yd-completed-mp4-1080.json");
            File.WriteAllText(legacy, json.Serialize(legacyData));
            string otherLegacy = Path.Combine(playlistFolder, ".yd-completed-mp3-192.json");
            File.WriteAllText(otherLegacy, json.Serialize(legacyData));
            var migrated = PlaylistScheduler.Run(options, new LocalPlaylistStorage(options.ResumeDirectory), save, null, CancellationToken.None, noDelay);
            Check(migrated.Skipped == 2 && downloads == 3 && !File.Exists(legacy) && File.Exists(PlaylistRecords.RecordPath(playlistFolder, "mp4-1080", options.ResumeDirectory)), "Legacy visible records migrate without redownloading completed videos");
            Check(!File.Exists(otherLegacy) && File.Exists(PlaylistRecords.RecordPath(playlistFolder, "mp3-192", options.ResumeDirectory)), "Using one profile migrates other legacy profiles out of the same media folder too");
            options.Height = 1200;
            string blocked = Path.Combine(root, "blocked-state");
            File.WriteAllText(blocked, "fixture file blocks directory creation");
            legacy = Path.Combine(playlistFolder, ".yd-completed-mp4-1200.json");
            File.WriteAllText(legacy, json.Serialize(legacyData));
            string originalCache = options.ResumeDirectory;
            options.ResumeDirectory = blocked;
            bool preserved = false;
            try
            {
                PlaylistScheduler.Run(options, new LocalPlaylistStorage(options.ResumeDirectory), save, null, CancellationToken.None, noDelay);
            }
            catch (IOException)
            {
                preserved = File.Exists(legacy) && downloads == 3;
            }

            Check(preserved, "Failed private-state creation preserves the legacy record and makes no download requests");
            options.ResumeDirectory = originalCache;
            options.Height = 1250;
            legacy = Path.Combine(playlistFolder, ".yd-completed-mp4-1250.json");
            File.WriteAllText(legacy, "corrupt legacy fixture");
            var backedUp = PlaylistRecords.Open(playlistFolder, "mp4-1250", originalCache);
            Check(!File.Exists(legacy) && File.ReadAllText(backedUp.FilePath + ".legacy") == "corrupt legacy fixture", "Malformed legacy state is preserved privately instead of left beside downloaded media");
            Check(PlaylistRecords.RecordPath(playlistFolder, "mp4-720", originalCache) != PlaylistRecords.RecordPath(Path.Combine(root, "other-output"), "mp4-720", originalCache), "Different download destinations keep separate resume state");
            options.Audio = true;
            options.AudioBitrate = 320;
            var fourth = PlaylistScheduler.Run(options, new LocalPlaylistStorage(options.ResumeDirectory), save, null, CancellationToken.None, noDelay);
            Check(fourth.Skipped == 0 && downloads == 5, "Different formats have separate resume records");
            options.Audio = false;
            options.Height = 360;
            using (var cancel = new CancellationTokenSource())
            {
                var stopped = PlaylistScheduler.Run(options, new LocalPlaylistStorage(options.ResumeDirectory), delegate (DownloadOptions item, CancellationToken token)
                {
                    var saved = save(item, token);
                    cancel.Cancel();
                    return saved;
                }, null, cancel.Token, noDelay);
                Check(stopped.Cancelled && stopped.Completed == 1 && stopped.Remaining == 2, "Cancellation retains completed files and the unprocessed count");
            }

            options.Height = 480;
            var limited = PlaylistScheduler.Run(options, new LocalPlaylistStorage(options.ResumeDirectory), delegate (DownloadOptions item, CancellationToken token)
            {
                throw new InvalidOperationException("HTTP 429 Too Many Requests");
            }, null, CancellationToken.None, noDelay);
            Check(limited.Failed == 1 && limited.Remaining == 2 && limited.StopReason != null, "Rate-limit errors stop further requests instead of hammering the playlist");
            string outside = Path.Combine(root, "outside.mp4");
            File.WriteAllText(outside, "fixture");
            options.Height = 1440;
            var escaped = PlaylistScheduler.Run(options, new LocalPlaylistStorage(options.ResumeDirectory), delegate (DownloadOptions item, CancellationToken token)
            {
                return new DownloadResult
                {
                    FilePath = outside
                };
            }, null, CancellationToken.None, noDelay);
            Check(escaped.Completed == 0 && escaped.Files.Count == 0, "Paths outside the playlist output folder are not accepted or recorded");
            options.Playlist = new PlaylistInfo
            {
                Id = info.Id,
                Url = info.Url,
                Entries = new System.Collections.Generic.List<PlaylistEntry>
                {
                    new PlaylistEntry
                    {
                        Id = "jNQXAC9IVRw",
                        MediaUrl = "https://www.youtube.com/watch?v=jNQXAC9IVRw"
                    },
                    new PlaylistEntry
                    {
                        Id = "aqz-KE-bpKQ",
                        MediaUrl = "https://www.youtube.com/watch?v=aqz-KE-bpKQ"
                    },
                    new PlaylistEntry
                    {
                        Id = "pc5IlcEn8vw",
                        MediaUrl = "https://www.youtube.com/watch?v=pc5IlcEn8vw"
                    }
                }
            };
            int profile = 3000;
            foreach (string error in new[]
            {
                "[WinError 112] The disk operation failed",
                "There is not enough space on the disk"
            }

            )
            {
                options.Height = profile++;
                int calls = 0;
                var full = PlaylistScheduler.Run(options, new LocalPlaylistStorage(options.ResumeDirectory), delegate (DownloadOptions item, CancellationToken token)
                {
                    calls++;
                    if (calls == 2)
                        throw new IOException(error);
                    return save(item, token);
                }, null, CancellationToken.None, noDelay);
                Check(calls == 2 && full.Completed == 1 && full.Failed == 1 && full.Remaining == 1 && full.Files.Count == 1 && File.Exists(full.Files[0]) && full.StopReason == error, "Windows disk-full wording stops subsequent requests and preserves the completed file: " + error);
            }

            string fixture = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FakeDownloader.exe");
            using (var engine = new YtDlpProvider(ToolSet.Validated(fixture, fixture, fixture, fixture)))
            {
                var read = engine.InspectPlaylist(url);
                options.Playlist = read;
                options.Height = 2160;
                var service = new DownloadService(engine, new LocalPlaylistStorage(options.ResumeDirectory));
                int highest = 0;
                bool countSeen = false;
                service.Progress += delegate (DownloadProgress p)
                {
                    highest = Math.Max(highest, p.Item);
                    if (p.Total == 3 && p.Item == 3 && p.Percent > 66)
                        countSeen = true;
                };
                var process = service.DownloadAsync(options, null, CancellationToken.None).GetAwaiter().GetResult();
                Check(read.Entries.Count == 3 && process.Completed == 2 && process.Failed == 1 && highest == 3 && countSeen, "Actual child-process output drives playlist progress and partial success");
                read.Entries = new System.Collections.Generic.List<PlaylistEntry>
                {
                    new PlaylistEntry
                    {
                        Id = "slowVideo01",
                        MediaUrl = "https://www.youtube.com/watch?v=slowVideo01",
                        Title = "Slow fixture"
                    }
                };
                options.Height = 2000;
                using (var cancel = new CancellationTokenSource())
                {
                    cancel.CancelAfter(200);
                    var result = service.DownloadAsync(options, null, cancel.Token).GetAwaiter().GetResult();
                    Check(result.Cancelled && result.Remaining == 1 && !engine.IsBusy, "Cancellation stops the active child process and retains resumable queue state");
                }
            }
        }
        finally
        {
            if (Path.GetFullPath(root).StartsWith(AppDomain.CurrentDomain.BaseDirectory, StringComparison.OrdinalIgnoreCase))
                Directory.Delete(root, true);
        }
    }
}
