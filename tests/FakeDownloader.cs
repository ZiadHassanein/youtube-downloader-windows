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
            Console.WriteLine("{\"format\":{\"format_name\":\"mov,mp4,m4a,3gp,3g2,mj2\",\"duration\":\"1.0\"},\"streams\":[{\"codec_type\":\"video\"}]}");
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
            Console.WriteLine(" A..... libmp3lame  MP3\n A..... aac AAC");
            return 0;
        }

        if (args.Contains("-muxers"))
        {
            Console.WriteLine(" E mp4 MP4\n E mp3 MP3");
            return 0;
        }

        string url = args.Last();
        if (args.Contains("--dump-single-json"))
        {
            Console.WriteLine("{\"title\":\"Test playlist\",\"entries\":[{\"id\":\"jNQXAC9IVRw\",\"title\":\"First\"},{\"id\":\"abcdefghijk\",\"title\":\"Unavailable\"},{\"id\":\"aqz-KE-bpKQ\",\"title\":\"Last\"}]}");
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
        string file = Path.Combine(folder, "fixture " + url.Substring(url.Length - 11) + ".mp4");
        Console.WriteLine("YTDL_TITLE:" + System.Text.Json.JsonSerializer.Serialize("Fixture title"));
        Console.WriteLine("YTDL_PROGRESS:{\"downloaded_bytes\":512,\"total_bytes\":1024,\"speed\":128,\"eta\":4}");
        File.WriteAllText(file, "Local fixture only");
        Console.WriteLine("YTDL_FILE:" + System.Text.Json.JsonSerializer.Serialize(file));
        return 0;
    }
}
