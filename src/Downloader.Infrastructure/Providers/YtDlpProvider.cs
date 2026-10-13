using System.Globalization;
using System.Text;

namespace YouTubeDownloader
{
    public sealed class YtDlpProvider : IMediaProvider
    {
        private ToolSet tools;
        private readonly DependencyManager dependencies;
        private readonly WindowsProcessRunner runner = new WindowsProcessRunner();
        private readonly Dictionary<string, Tuple<string, string>> choices = new Dictionary<string, Tuple<string, string>>();
        private readonly object gate = new object ();
        private volatile bool cancelled;
        public YtDlpProvider(DependencyManager dependencies)
        {
            this.dependencies = dependencies;
        }

        internal YtDlpProvider(ToolSet tools)
        {
            this.tools = tools;
        }

        public string Id
        {
            get
            {
                return "youtube";
            }
        }

        public bool IsReady
        {
            get
            {
                return tools != null && tools.Ready;
            }
        }

        public string Normalize(string url, bool playlist)
        {
            return VideoLink.Normalize(url, playlist);
        }

        public MediaTargets ResolveTargets(string input)
        {
            var link = VideoLink.Parse(input);
            return new(link.VideoUrl, link.PlaylistUrl);
        }

        public async Task EnsureReadyAsync(IProgress<DownloadProgress> progress, CancellationToken token)
        {
            if (!IsReady)
                tools = await dependencies.EnsureAsync(progress, token).ConfigureAwait(false);
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
                throw new InvalidOperationException(errors.LastOrDefault(x => x.StartsWith("ERROR:", StringComparison.Ordinal)) ?? errors.LastOrDefault() ?? "The download failed.");
            return capture.ToString();
        }

        public VideoInfo Inspect(string url, CancellationToken token = default(CancellationToken), IProgress<DownloadProgress> progress = null)
        {
            url = VideoLink.Normalize(url, false);
            var args = Arguments.Common(tools);
            args.AddRange(new[] { "--skip-download", "--dump-single-json", "--", url });
            Emit(progress, "Checking video", "Getting the title and available formats…", 0, true);
            var data = Json().Deserialize<Dictionary<string, object>>(Execute(args, null, token));
            var info = new VideoInfo
            {
                Title = Value(data, "title"),
                Uploader = Value(data, "uploader"),
                Duration = Number(data, "duration"),
                Live = Value(data, "is_live") == "True",
                Qualities = AvailableFormats.Read(data),
                HasAudio = AvailableFormats.HasAudio(data)
            };
            choices.Clear();
            foreach (var quality in info.Qualities)
            {
                quality.SelectionKey = Guid.NewGuid().ToString("N");
                choices[quality.SelectionKey] = Tuple.Create(url, quality.Selector);
            }

            return info;
        }

        public PlaylistInfo InspectPlaylist(string url, CancellationToken token = default(CancellationToken), IProgress<DownloadProgress> progress = null)
        {
            url = VideoLink.Normalize(url, true);
            var args = Arguments.Common(tools);
            args.Remove("--no-playlist");
            args.AddRange(new[] { "--yes-playlist", "--flat-playlist", "--skip-download", "--dump-single-json", "--sleep-requests", "0.75", "--", url });
            Emit(progress, "Checking playlist", "Getting the playlist titles…", 0, true);
            return PlaylistMetadata.Read(url, Json().Deserialize<Dictionary<string, object>>(Execute(args, null, token)));
        }

        public DownloadResult DownloadItem(DownloadOptions options, CancellationToken token = default(CancellationToken), IProgress<DownloadProgress> progress = null)
        {
            if (options.Playlist != null)
                throw new ArgumentException("Use the application playlist queue.");
            if (!options.Audio && !options.PlaylistItem && !String.IsNullOrEmpty(options.QualityKey))
            {
                Tuple<string, string> selected;
                if (!choices.TryGetValue(options.QualityKey, out selected) || selected.Item1 != VideoLink.Normalize(options.Url, false))
                    throw new ArgumentException("Check the video again to refresh its quality choices.");
                options.FormatSelector = selected.Item2;
            }

            var args = Arguments.ForDownload(tools, options);
            Directory.CreateDirectory(Path.GetFullPath(options.Folder));
            string saved = null;
            Emit(progress, "Connecting", "Preparing your download…", 0, true);
            try
            {
                Execute(args, delegate (string line)
                {
                    if (line.StartsWith("YTDL_FILE:"))
                        saved = Json().Deserialize<string>(line.Substring(10));
                    else if (line.StartsWith("YTDL_TITLE:"))
                    {
                        var h = Title;
                        if (h != null)
                            h(Json().Deserialize<string>(line.Substring(11)));
                    }
                    else if (line.StartsWith("YTDL_PROGRESS:"))
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
                            string detail = Size(bytes) + (total > 0 ? " of " + Size(total) : "") + (speed > 0 ? "  •  " + Size(speed) + "/s" : "") + (eta > 0 ? "  •  " + TimeSpan.FromSeconds(eta).ToString(@"m\:ss") + " remaining" : "");
                            Emit(progress, "Downloading", detail, total > 0 ? Math.Min(100, 100 * bytes / total) : 0, total <= 0);
                        }
                    }
                    else if (line.StartsWith("YTDL_POST:"))
                        Emit(progress, options.Audio ? "Making MP3" : "Preparing MP4", "Finishing your file. This can take a moment…", 100, true);
                }, token);
            }
            catch (OperationCanceledException)
            {
                return new DownloadResult
                {
                    Cancelled = true
                };
            }

            if (!MediaFiles.IsCompleted(options.Folder, saved, options.Audio))
                throw new InvalidOperationException(options.PlaylistItem ? "This video is live or unavailable." : "The downloader did not produce a completed file. Try again or choose another quality.");
            string root = Path.GetFullPath(options.Folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(saved).StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The downloaded file is outside the selected folder.");
            ValidateMedia(saved, options.Audio, token);
            Emit(progress, "Download complete", Path.GetFileName(saved), 100, false);
            return new DownloadResult
            {
                FilePath = saved,
                Files = new List<string>
                {
                    saved
                },
                Total = 1
            };
        }

        private void ValidateMedia(string file, bool audio, CancellationToken token)
        {
            var text = new StringBuilder();
            int code = runner.Run(tools.FFprobe, new[] { "-v", "error", "-show_entries", "format=format_name,duration:stream=codec_type", "-of", "json", "-i", file }, line => text.AppendLine(line), null, token, TimeSpan.FromSeconds(15), 1024 * 1024);
            using var document = System.Text.Json.JsonDocument.Parse(text.ToString());
            var root = document.RootElement;
            string format = root.GetProperty("format").GetProperty("format_name").GetString();
            string duration = root.GetProperty("format").GetProperty("duration").GetString();
            bool valid = code == 0 && double.TryParse(duration, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds > 0 && root.GetProperty("streams").EnumerateArray().Any(s => s.GetProperty("codec_type").GetString() == (audio ? "audio" : "video")) && (audio ? format == "mp3" : format.Split(',').Contains("mp4"));
            if (!valid)
                throw new InvalidDataException("The downloaded file could not be verified. Try another quality.");
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
}
