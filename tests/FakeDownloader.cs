using System.Diagnostics;

// Deterministic process fixture. These are text files, never real downloaded media.
internal static class FakeDownloader
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
            return 0;
        if (args[0] == "--update-ack")
        {
            var root = System.Text.Json.JsonDocument.Parse(File.ReadAllText(args[1])).RootElement;
            string directory = AppContext.BaseDirectory;
            if (File.Exists(Path.Combine(directory, "no-ack.flag")))
            {
                var start = new ProcessStartInfo(Environment.ProcessPath)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                start.ArgumentList.Add("--writer");
                start.ArgumentList.Add(Path.Combine(directory, "candidate-writer.txt"));
                using var child = Process.Start(start);
                File.WriteAllText(Path.Combine(directory, "candidate-child.pid"), child.Id.ToString());
                Thread.Sleep(60000);
                return 0;
            }

            using var self = Process.GetCurrentProcess();
            File.WriteAllText(args[1] + ".ack", root.GetProperty("Id").GetString() + "|" + self.Id + "|" + self.StartTime.ToUniversalTime().Ticks);
            Thread.Sleep(60000);
            return 0;
        }

        if (args[0] == "--echo")
        {
            Console.WriteLine(args[1]);
            return 0;
        }

        if (args[0] == "--write-marker")
        {
            File.WriteAllText(args[1], "ran");
            return 0;
        }

        if (args[0] == "--hang")
        {
            Thread.Sleep(60000);
            return 0;
        }

        if (args[0] == "--writer")
        {
            while (true)
            {
                File.AppendAllText(args[1], "x");
                Thread.Sleep(10);
            }
        }

        if (args[0] == "--spawn-writer")
        {
            using var child = Process.Start(new ProcessStartInfo(Environment.ProcessPath, "--writer \"" + args[1] + "\"") { UseShellExecute = false, CreateNoWindow = true });
            File.WriteAllText(args[2], child.Id.ToString());
            Thread.Sleep(60000);
            return 0;
        }

        string tool = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
        if (args.Contains("-show_entries"))
        {
            string extension = Path.GetExtension(args.Last());
            string container = extension switch { ".mp3" => "mp3", ".opus" => "ogg", ".wav" => "wav", ".flac" => "flac", _ => "mov,mp4,m4a,3gp,3g2,mj2" };
            string stream = extension == ".mp4" ? "video" : "audio";
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { format = new { format_name = container, duration = "1.0" }, streams = new[] { new { codec_type = stream } } }));
            return 0;
        }

        if (args.Contains("--version") || args.Contains("-version"))
        {
            Console.WriteLine(tool == "deno" ? "deno 2.9.7" : tool == "ffmpeg" ? "ffmpeg version 9.0.2-test" : tool == "ffprobe" ? "ffprobe version 9.0.2-test" : "2026.08.19");
            return 0;
        }

        if (args.Contains("--help"))
        {
            Console.WriteLine("--no-plugin-dirs --no-remote-components --js-runtimes");
            return 0;
        }

        if (args.Contains("-encoders"))
        {
            Console.WriteLine(" A..... libmp3lame  MP3\n A..... aac AAC\n A..... libopus Opus\n A..... flac FLAC\n A..... pcm_s16le PCM");
            return 0;
        }

        if (args.Contains("-muxers"))
        {
            Console.WriteLine(" E mp4 MP4\n E mp3 MP3\n E ogg Ogg\n E wav WAV\n E flac FLAC");
            return 0;
        }

        string url = args.Last();
        if (File.Exists(Path.Combine(AppContext.BaseDirectory, "compatibility-failure.flag")))
        {
            Console.Error.WriteLine("ERROR: Signature extraction failed");
            return 1;
        }
        if (args.Contains("--dump-single-json"))
        {
            if (url.Contains("list=RD") && (args[Array.IndexOf(args, "--playlist-end") + 1] != "50" || !url.Contains("watch?v=")))
            {
                Console.Error.WriteLine("ERROR: Mix inspection requires a seeded watch link and a bounded snapshot.");
                return 1;
            }
            if (args.Contains("--yes-playlist"))
                Console.WriteLine("{\"title\":\"Test playlist\",\"entries\":[{\"id\":\"jNQXAC9IVRw\",\"title\":\"First\"},{\"id\":\"abcdefghijk\",\"title\":\"Unavailable\"},{\"id\":\"aqz-KE-bpKQ\",\"title\":\"Last\"}]}");
            else
            {
                string id = url.Contains("vimeo.com") ? "vimeo-fixture" : "youtube-fixture";
                int height = File.Exists(Path.Combine(AppContext.BaseDirectory, "missing-quality.flag")) ? 720 : 360;
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { title = "Public media fixture", uploader = "Fixture", duration = 1, formats = new[] { new { format_id = id, vcodec = "h264", acodec = "aac", height, width = height * 2, protocol = "https" } }, subtitles = new Dictionary<string, object> { ["en"] = new[] { new { ext = "vtt", name = "English", url = "https://example.com/subtitle.vtt" } } } }));
            }
            return 0;
        }

        if (url.EndsWith("abcdefghijk"))
        {
            Console.Error.WriteLine("ERROR: Video unavailable");
            return 1;
        }

        if (url.EndsWith("rateLimit01"))
        {
            Console.Error.WriteLine("ERROR: HTTP 429 Too Many Requests");
            return 1;
        }

        if (url.EndsWith("slowVideo01"))
            Thread.Sleep(10000);
        int index = Array.IndexOf(args, "--paths");
        string folder = args[index + 1];
        Directory.CreateDirectory(folder);
        int formatIndex = Array.IndexOf(args, "--audio-format");
        string extensionName = formatIndex >= 0 ? args[formatIndex + 1] : "mp4";
        string file = Path.Combine(folder, "fixture " + url.Substring(Math.Max(0, url.Length - 11)).Replace('/', '_').Replace('?', '_') + "." + extensionName);
        Console.WriteLine("YTDL_TITLE:" + System.Text.Json.JsonSerializer.Serialize("Fixture title"));
        Console.WriteLine("YTDL_PROGRESS:{\"downloaded_bytes\":512,\"total_bytes\":1024,\"speed\":128,\"eta\":4}");
        File.WriteAllText(file, "Local fixture only");
        int subtitleIndex = Array.IndexOf(args, "--sub-langs");
        if (subtitleIndex >= 0)
        {
            string language = System.Text.RegularExpressions.Regex.Unescape(args[subtitleIndex + 1].Trim('^', '$'));
            int conversion = Array.IndexOf(args, "--convert-subs");
            string subtitle = Path.ChangeExtension(file, null) + "." + language + "." + (conversion >= 0 ? args[conversion + 1] : "vtt");
            File.WriteAllText(subtitle, "1\n00:00:00,000 --> 00:00:01,000\nLocal subtitle fixture\n");
        }
        Console.WriteLine("YTDL_FILE:" + System.Text.Json.JsonSerializer.Serialize(file));
        return 0;
    }
}
