using System.Globalization;
using System.Text;

namespace YouTubeDownloader
{
    public static class Arguments
    {
        // Windows CommandLineToArgvW quoting, including quotes and trailing slashes.
        public static string Quote(string value)
        {
            var result = new StringBuilder("\"");
            int slashes = 0;
            foreach (char ch in value)
            {
                if (ch == '\\')
                {
                    slashes++;
                    continue;
                }

                if (ch == '"')
                {
                    result.Append('\\', slashes * 2 + 1).Append(ch);
                    slashes = 0;
                }
                else
                {
                    result.Append('\\', slashes).Append(ch);
                    slashes = 0;
                }
            }

            result.Append('\\', slashes * 2).Append('"');
            return result.ToString();
        }

        public static List<string> Common(ToolSet tools)
        {
            return new List<string>
            {
                "--ignore-config",
                "--no-update",
                "--no-plugin-dirs",
                "--no-remote-components",
                "--no-js-runtimes",
                "--no-playlist",
                "--encoding",
                "utf-8",
                "--socket-timeout",
                "20",
                "--retries",
                "3",
                "--fragment-retries",
                "3",
                "--retry-sleep",
                "http:exp=1:8",
                "--retry-sleep",
                "fragment:exp=1:8",
                "--js-runtimes",
                "deno:" + tools.Deno,
                "--ffmpeg-location",
                tools.FFmpeg
            };
        }

        public static List<string> ForDownload(ToolSet tools, DownloadOptions options)
        {
            string url = MediaLink.Normalize(options.Url, options.Playlist != null);
            if (String.IsNullOrWhiteSpace(options.Folder) || !Path.IsPathFullyQualified(options.Folder))
                throw new ArgumentException("Choose a folder for your download.");
            string folder = Path.GetFullPath(options.Folder);
            if (folder.StartsWith(@"\\"))
                throw new ArgumentException("Choose a folder on this computer.");
            if (options.Height < 0 || options.Height > 10000)
                throw new ArgumentException("Choose a supported video quality.");
            if (!Enum.IsDefined(options.AudioEncoding) || !Enum.IsDefined(options.Captions?.Format ?? CaptionFormat.Srt))
                throw new ArgumentException("Choose a supported output format.");
            if (options.Audio && !OutputProfiles.SupportsAudioBitrate(options.AudioEncoding, options.AudioBitrate))
                throw new ArgumentException("Choose a supported audio quality.");
            var args = Common(tools);
            bool playlist = options.Playlist != null || options.PlaylistItem;
            if (options.Playlist != null)
            {
                args.Remove("--no-playlist");
                args.Add("--yes-playlist");
            }

            if (playlist)
                args.AddRange(new[] { "--sleep-requests", "0.75", "--sleep-interval", "2", "--max-sleep-interval", "4", "--match-filters", "!is_live" });
            string filename = "%(title).100B [%(id)s]" + (options.Audio ? " - " + (OutputProfiles.IsLossless(options.AudioEncoding) ? OutputProfiles.AudioExtension(options.AudioEncoding) : options.AudioBitrate == 0 ? "best" : options.AudioBitrate + "kbps") : " - " + (options.Height > 0 ? (playlist ? "max" : "") + options.Height + "p" : "best")) + ".%(ext)s";
            args.AddRange(new[] { "--no-simulate", "--continue", "--part", "--newline", "--progress", "--progress-delta", "0.3", "--windows-filenames", "--no-overwrites", "--trim-filenames", "180", "--paths", folder, "--output", filename, "--progress-template", "download:YTDL_PROGRESS:%(progress)j", "--progress-template", "postprocess:YTDL_POST:%(progress)j", "--print", "before_dl:YTDL_TITLE:%(title)j", "--print", "after_move:YTDL_FILE:%(filepath)j" });
            if (options.Audio)
            {
                args.AddRange(new[] { "--format", "bestaudio/best", "--extract-audio", "--audio-format", OutputProfiles.AudioExtension(options.AudioEncoding) });
                if (!OutputProfiles.IsLossless(options.AudioEncoding)) args.AddRange(new[] { "--audio-quality", options.AudioBitrate == 0 ? "0" : options.AudioBitrate + "K" });
            }
            else
            {
                string limit = options.Height == 0 ? "" : "[height<=" + options.Height.ToString(CultureInfo.InvariantCulture) + "]";
                string selector = playlist && options.Height > 0 ? "(bv*[height<=" + options.Height + "]/bv*[width<=" + options.Height + "])+ba/(b[height<=" + options.Height + "]/b[width<=" + options.Height + "])" : String.IsNullOrEmpty(options.FormatSelector) ? "bv*" + limit + "+ba/b" + limit : options.FormatSelector;
                if (selector.Length > 18000 || selector.Any(Char.IsControl))
                    throw new ArgumentException("The video's format information is invalid. Check the video again.");
                args.AddRange(new[] { "--format", selector, "--format-sort", "res,vcodec:h264,acodec:aac", "--merge-output-format", "mp4", "--remux-video", "mp4" });
            }

            if (options.Captions != null)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(options.Captions.Language ?? "", "^[A-Za-z0-9_.-]{1,48}$") || options.Captions.Language == "live_chat")
                    throw new ArgumentException("Choose a supported subtitle language.");
                args.AddRange(new[] { options.Captions.Automatic ? "--write-auto-subs" : "--write-subs", "--sub-langs", "^" + System.Text.RegularExpressions.Regex.Escape(options.Captions.Language) + "$", "--sub-format", "vtt/best", "--sleep-subtitles", "2" });
                args.AddRange(new[] { "--convert-subs", options.Captions.Format.ToString().ToLowerInvariant() });
            }
            args.Add("--");
            args.Add(url);
            return args;
        }
    }
}
