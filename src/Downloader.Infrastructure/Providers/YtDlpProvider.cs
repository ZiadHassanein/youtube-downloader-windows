using System.Globalization;
using System.Text;

namespace YouTubeDownloader;
public sealed class YtDlpProvider : IMediaProvider, IMediaMaintenance
{
    private ToolSet tools;
    private readonly DependencyManager dependencies;
    private readonly AutomaticToolUpdates automaticUpdates;
    private readonly MediaLinkRouter links;
    private readonly WindowsProcessRunner runner = new WindowsProcessRunner();
    private readonly Dictionary<string, MediaSelection> choices = new();
    private readonly object gate = new object ();
    private volatile bool cancelled;
    public YtDlpProvider(DependencyManager dependencies, MediaLinkRouter links = null)
    {
        this.dependencies = dependencies;
        automaticUpdates = new AutomaticToolUpdates(dependencies.CacheDirectory) { FindBaseline = dependencies.DiscoverBaseline };
        this.links = links ?? MediaLink.DefaultRouter;
    }

    internal YtDlpProvider(ToolSet tools, AutomaticToolUpdates automaticUpdates = null)
    {
        this.tools = tools;
        this.automaticUpdates = automaticUpdates;
        links = MediaLink.DefaultRouter;
    }

    public string Id
    {
        get
        {
            return "yt-dlp";
        }
    }

    public bool IsReady
    {
        get
        {
            return tools != null && tools.Ready && new[] { tools.YtDlp, tools.Deno, tools.FFmpeg, tools.FFprobe }.All(File.Exists);
        }
    }

    public string Normalize(string url, bool playlist)
    {
        return links.Normalize(url, playlist);
    }

    public MediaTargets ResolveTargets(string input)
    {
        return links.Resolve(input);
    }

    public async Task EnsureReadyAsync(IProgress<DownloadProgress> progress, CancellationToken token)
    {
        if (!IsReady)
            tools = await dependencies.EnsureAsync(progress, token).ConfigureAwait(false);
        if (automaticUpdates != null)
            tools = await automaticUpdates.CheckDailyAsync(tools, progress, token).ConfigureAwait(false);
    }

    public async Task<MediaToolUpdateResult> RefreshToolsAsync(IProgress<DownloadProgress> progress, CancellationToken token)
    {
        if (!IsReady) tools = await dependencies.EnsureAsync(progress, token).ConfigureAwait(false);
        var result = await automaticUpdates.RefreshAsync(tools, progress, token).ConfigureAwait(false);
        tools = result.Tools;
        return result.Result;
    }

    public event Action<string> Title;
    public event Action<string> Log;
    public bool IsBusy
    {
        get
        {
            return runner.IsBusy;
        }
    }

    private static JsonCodec Json()
    {
        return new JsonCodec
        {
            MaxJsonLength = 32 * 1024 * 1024
        };
    }

    private static void Emit(IProgress<DownloadProgress> progress, string stage, string detail, double percent, bool unknown)
    {
        progress?.Report(new DownloadProgress { Stage = stage, Detail = detail, Percent = percent, Indeterminate = unknown });
    }

    private string Execute(List<string> args, Action<string> output, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!IsReady)
            throw new InvalidOperationException("Media tools are not ready. Paste the link again to retry preparation.");
        cancelled = false;
        var capture = new StringBuilder();
        var errors = new List<string>();
        int code = runner.Run(tools.YtDlp, args, line =>
        {
            if (output != null)
                output(line);
            else
                capture.AppendLine(line);
        }, line =>
        {
            if (!String.IsNullOrWhiteSpace(line))
            {
                errors.Add(line);
                if (errors.Count > 80)
                    errors.RemoveAt(0);
                Log?.Invoke(line);
            }
        }, token, args.Contains("--dump-single-json") ? TimeSpan.FromMinutes(2) : null);
        if (cancelled || token.IsCancellationRequested)
            throw new OperationCanceledException();
        if (code != 0)
            throw new MediaToolExecutionException(string.Join("\n", errors.TakeLast(12).DefaultIfEmpty("The download failed.")));
        return capture.ToString();
    }

    public VideoInfo Inspect(string url, CancellationToken token = default(CancellationToken), IProgress<DownloadProgress> progress = null)
        => Recover(() => InspectCore(url, token, progress), progress, token);

    private VideoInfo InspectCore(string url, CancellationToken token, IProgress<DownloadProgress> progress)
    {
        url = Normalize(url, false);
        var args = Arguments.Common(tools);
        args.AddRange(new[] { "--flat-playlist", "--playlist-end", "1001", "--format", "bestaudio/best/bestvideo", "--skip-download", "--dump-single-json", "--", url });
        Emit(progress, "Checking video", "Getting the title and available formats…", 0, true);
        var data = Json().Deserialize<Dictionary<string, object>>(Execute(args, null, token));
        if (data.ContainsKey("entries"))
            return new VideoInfo { Title = Value(data, "title"), Collection = PlaylistMetadata.Read(url, data) };
        if (DirectMediaMetadata.NeedsProbe(data, url))
        {
            var probe = new StringBuilder();
            var errors = new StringBuilder();
            int code = runner.Run(tools.FFprobe, DirectMediaMetadata.ProbeArguments(url), line => probe.AppendLine(line),
                line => errors.AppendLine(line), token, TimeSpan.FromSeconds(25), 1024 * 1024);
            token.ThrowIfCancellationRequested();
            if (cancelled) throw new OperationCanceledException();
            if (code != 0) throw new MediaToolExecutionException(errors.ToString());
            DirectMediaMetadata.Apply(data, Json().Deserialize<Dictionary<string, object>>(probe.ToString()));
        }
        var info = new VideoInfo
        {
            Title = Value(data, "title"),
            Uploader = Value(data, "uploader"),
            ThumbnailUrl = Value(data, "thumbnail"),
            Duration = Number(data, "duration"),
            Live = Value(data, "is_live") == "True",
            Qualities = AvailableFormats.Read(data),
            HasAudio = AvailableFormats.HasAudio(data),
            AudioSource = AudioMetadata.Read(data),
            Captions = CaptionMetadata.Read(data)
        };
        choices.Clear();
        foreach (var container in Enum.GetValues<VideoContainer>())
        {
            if (container != VideoContainer.Mp4) info.AlternativeQualities[container] = AvailableFormats.Read(data, container);
            foreach (var quality in info.QualitiesFor(container))
            {
                quality.SelectionKey = Guid.NewGuid().ToString("N");
                quality.Selection = new MediaSelection(url, quality.Resolution, quality.Selector, container);
                choices[quality.SelectionKey] = quality.Selection;
            }
        }

        return info;
    }

    public PlaylistInfo InspectPlaylist(string url, CancellationToken token = default(CancellationToken), IProgress<DownloadProgress> progress = null)
        => Recover(() => InspectPlaylistCore(url, token, progress), progress, token);

    private PlaylistInfo InspectPlaylistCore(string url, CancellationToken token, IProgress<DownloadProgress> progress)
    {
        url = Normalize(url, true);
        var args = Arguments.Common(tools);
        args.Remove("--no-playlist");
        bool mix = new YouTubeLinkResolver().Supports(new Uri(url)) && VideoLink.Parse(url).IsMix;
        args.AddRange(new[] { "--yes-playlist", "--flat-playlist", "--playlist-end", mix ? PlaylistInfo.MixSnapshotLimit.ToString(CultureInfo.InvariantCulture) : "1001", "--skip-download", "--dump-single-json", "--sleep-requests", "0.75", "--", url });
        Emit(progress, "Checking playlist", "Getting the playlist titles…", 0, true);
        return PlaylistMetadata.Read(url, Json().Deserialize<Dictionary<string, object>>(Execute(args, null, token)));
    }

    public DownloadResult DownloadItem(DownloadOptions options, CancellationToken token = default(CancellationToken), IProgress<DownloadProgress> progress = null)
        => Recover(() => DownloadItemCore(options, token, progress), progress, token);

    private T Recover<T>(Func<T> action, IProgress<DownloadProgress> progress, CancellationToken token) => automaticUpdates == null ? action() :
        automaticUpdates.RunAsync(action, () => tools, value => tools = value, progress, token).GetAwaiter().GetResult();

    private DownloadResult DownloadItemCore(DownloadOptions options, CancellationToken token, IProgress<DownloadProgress> progress)
    {
        if (options.Playlist != null)
            throw new ArgumentException("Use the application playlist queue.");
        VideoInfo fresh = null;
        if (options.Audio || !options.PlaylistItem && (options.Selection != null || options.Captions?.Required == true) || options.VideoContainer != VideoContainer.Mp4)
        {
            fresh = !options.PlaylistItem && options.FreshMetadata is { } queued &&
                (options.Audio || options.Captions != null || queued.QualitiesFor(options.VideoContainer).Any(q => q.Selection == options.Selection))
                ? queued : InspectCore(options.Url, token, progress);
            if (fresh.Live) throw new InvalidOperationException("This video is live right now. Try again once the stream has ended.");
            if (options.Captions?.Required == true && !fresh.Captions.Any(c => c.Language == options.Captions.Language && c.Automatic == options.Captions.Automatic))
                throw new InvalidOperationException("The selected subtitles are no longer available. Refresh output options or choose no subtitles.");
        }
        if (options.Audio)
        {
            if (!fresh.HasAudio) throw new InvalidOperationException("This video has no downloadable audio stream.");
            options.AudioSource = fresh.AudioSource ?? new AudioSourceInfo("", null);
            _ = OutputProfiles.EffectiveAudioBitrate(options);
        }
        if (!options.PlaylistItem && options.Selection != null && !options.Audio)
        {
            var selected = options.Selection;
            if (selected.MediaUrl != Normalize(options.Url, false) || selected.Container != options.VideoContainer)
                throw new ArgumentException("Check this video again to refresh its quality choices.");
            var available = fresh.QualitiesFor(options.VideoContainer).FirstOrDefault(q => q.Resolution == selected.Resolution);
            var alternatives = available?.Selector.Split('/').ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>();
            options.FormatSelector = string.Join('/', selected.Selector.Split('/').Where(alternatives.Contains));
            if (options.FormatSelector.Length == 0)
                throw new InvalidOperationException("The selected quality is no longer available. Check the link again and choose a new quality.");
        }
        else if (!options.Audio && options.VideoContainer != VideoContainer.Mp4)
        {
            options.FormatSelector = fresh.QualitiesFor(options.VideoContainer).FirstOrDefault(q => options.Height == 0 || q.Resolution <= options.Height)?.Selector
                ?? throw new InvalidOperationException("This video has no compatible streams for this format. Choose another video format.");
        }
        else if (!options.Audio && !options.PlaylistItem && !String.IsNullOrEmpty(options.QualityKey))
        {
            if (!choices.TryGetValue(options.QualityKey, out var selected) || selected.MediaUrl != Normalize(options.Url, false) || selected.Container != options.VideoContainer)
                throw new ArgumentException("Check this video again to refresh its quality choices.");
            options.FormatSelector = selected.Selector;
        }

        var args = Arguments.ForDownload(tools, options);
        Directory.CreateDirectory(Path.GetFullPath(options.Folder));
        string saved = null;
        Emit(progress, "Connecting", "Preparing your download…", 0, true);
        try
        {
            Execute(args, delegate (string line)
            {
                if (line.StartsWith("YTDL_FILE:", StringComparison.Ordinal))
                    saved = Json().Deserialize<string>(line.Substring(10));
                else if (line.StartsWith("YTDL_TITLE:", StringComparison.Ordinal))
                {
                    var h = Title;
                    if (h != null)
                        h(Json().Deserialize<string>(line.Substring(11)));
                }
                else if (line.StartsWith("YTDL_PROGRESS:", StringComparison.Ordinal))
                {
                    var p = Json().Deserialize<Dictionary<string, object>>(line.Substring(14));
                    double total = Number(p, "total_bytes");
                    if (total <= 0)
                        total = Number(p, "total_bytes_estimate");
                    double bytes = Number(p, "downloaded_bytes"), speed = Number(p, "speed"), eta = Number(p, "eta");
                    if (Value(p, "status") == "finished")
                        Emit(progress, "Finishing download", "Preparing the final file…", 100, true);
                    else
                    {
                        string detail = Size(bytes) + (total > 0 ? " of " + Size(total) : "") + (speed > 0 ? "  •  " + Size(speed) + "/s" : "") + (eta > 0 ? "  •  " + TimeSpan.FromSeconds(eta).ToString(@"m\:ss", System.Globalization.CultureInfo.InvariantCulture) + " remaining" : "");
                        Emit(progress, "Downloading", detail, total > 0 ? Math.Min(100, 100 * bytes / total) : 0, total <= 0);
                    }
                }
                else if (line.StartsWith("YTDL_POST:", StringComparison.Ordinal))
                    Emit(progress, options.Audio ? "Preparing audio" : "Preparing video", "Finishing your file. This can take a moment…", 100, true);
            }, token);
        }
        catch (OperationCanceledException)
        {
            return new DownloadResult
            {
                Cancelled = true
            };
        }

        if (!MediaFiles.IsCompleted(options.Folder, saved, options))
            throw new InvalidOperationException(options.PlaylistItem ? "This video is live or unavailable." : "The downloader did not produce a completed file. Try again or choose another quality.");
        string root = Path.GetFullPath(options.Folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(saved).StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The downloaded file is outside the selected folder.");
        AudioBitrateConversion.Apply(saved, options, tools, token);
        MediaValidator.Validate(saved, options, tools, token);
        Emit(progress, "Download complete", Path.GetFileName(saved), 100, false);
        return new DownloadResult
        {
            FilePath = saved,
            Warning = MediaFiles.HasCaptions(saved, options.Captions) ? null : "Requested subtitles were unavailable. The media file was saved.",
            Files = new List<string>
            {
                saved
            },
            Total = 1
        };
    }

    private static string Value(Dictionary<string, object> data, string key)
    {
        object v;
        return data.TryGetValue(key, out v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : "";
    }

    private static double Number(Dictionary<string, object> data, string key)
    {
        double n;
        return Double.TryParse(Value(data, key), NumberStyles.Any, CultureInfo.InvariantCulture, out n) ? n : 0;
    }

    public static string Size(double bytes)
    {
        string[] names =
        {
            "B",
            "KB",
            "MB",
            "GB"
        };
        int unit = 0;
        while (bytes >= 1024 && unit < 3)
        {
            bytes /= 1024;
            unit++;
        }

        return bytes.ToString(unit == 0 ? "0" : "0.0", CultureInfo.InvariantCulture) + " " + names[unit];
    }

    public void Cancel()
    {
        cancelled = true;
        runner.Cancel();
    }

    public void Dispose()
    {
        Cancel();
        runner.Dispose();
        dependencies?.Dispose();
    }
}
