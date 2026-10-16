using System.Globalization;
using System.Text.Json;
using YouTubeDownloader;

internal static class OutputMediaLiveChecks
{
    internal static async Task<int> Run(string output)
    {
        try
        {
            string root = Path.GetFullPath(output), folder = Path.Combine(root, "OutputMediaPreview27");
            Directory.CreateDirectory(folder);
            using var dependencies = new DependencyManager(Path.Combine(root, "Tools"), Array.Empty<string>());
            var tools = dependencies.Discover(CancellationToken.None) ?? throw new IOException("Prepare the isolated tool cache first.");
            using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var runner = new WindowsProcessRunner();
            void Encode(params string[] args)
            {
                if (runner.Run(tools.FFmpeg, new[] { "-nostdin", "-hide_banner", "-loglevel", "error", "-y" }.Concat(args).ToArray(), null, Console.Error.WriteLine, deadline.Token, TimeSpan.FromSeconds(30)) != 0)
                    throw new IOException("Synthetic media encoding failed.");
            }
            foreach (var (encoding, encoder, codec, sourceRate) in new[]
            {
                (AudioEncoding.Mp3, "libmp3lame", "mp3", 320), (AudioEncoding.M4a, "aac", "aac", 256), (AudioEncoding.Opus, "libopus", "opus", 256)
            })
            {
                string file = Path.Combine(folder, "synthetic-" + codec + "." + OutputProfiles.AudioExtension(encoding));
                Encode("-f", "lavfi", "-i", "anoisesrc=color=pink:amplitude=0.2:sample_rate=48000:duration=4", "-ac", "2", "-c:a", encoder, "-b:a", sourceRate + "k", file);
                long before = new FileInfo(file).Length;
                var options = new DownloadOptions { Folder = folder, Audio = true, AudioEncoding = encoding, AudioBitrate = 128, AudioSource = new(codec, sourceRate) };
                AudioBitrateConversion.Apply(file, options, tools, deadline.Token);
                string json = new ToolProbe().Capture(tools.FFprobe, deadline.Token, "-v", "error", "-show_entries", "stream=codec_name:format=duration,bit_rate", "-of", "json", file);
                using var info = JsonDocument.Parse(json);
                double duration = double.Parse(info.RootElement.GetProperty("format").GetProperty("duration").GetString(), CultureInfo.InvariantCulture);
                double rate = double.Parse(info.RootElement.GetProperty("format").GetProperty("bit_rate").GetString(), CultureInfo.InvariantCulture) / 1000;
                if (info.RootElement.GetProperty("streams")[0].GetProperty("codec_name").GetString() != codec || duration is < 3.9 or > 4.5 || rate is < 75 or > 180 || new FileInfo(file).Length >= before)
                    throw new IOException("The selected lower bitrate did not produce valid smaller audio: " + codec);
                Console.WriteLine($"LIVE PASS: Real same-codec {encoding} conversion targeted128kbps; FFprobe measured {rate:F1}kbps/{duration:F2}s and verified a smaller file.");
            }
            string mp4 = Path.Combine(folder, "synthetic.mp4"), mkv = Path.Combine(folder, "synthetic.mkv"), webm = Path.Combine(folder, "synthetic.webm");
            Encode("-f", "lavfi", "-i", "testsrc2=duration=0.6:size=320x180:rate=12", "-f", "lavfi", "-i", "sine=frequency=440:duration=0.6", "-shortest", "-c:v", "libx264", "-preset", "ultrafast", "-c:a", "aac", mp4);
            Encode("-i", mp4, "-c", "copy", mkv);
            Encode("-i", mp4, "-c:v", "libvpx-vp9", "-deadline", "realtime", "-cpu-used", "8", "-c:a", "libopus", webm);
            foreach (var (container, file) in new[] { (VideoContainer.Mp4, mp4), (VideoContainer.Mkv, mkv), (VideoContainer.WebM, webm) })
            {
                MediaValidator.Validate(file, new() { VideoContainer = container }, tools, deadline.Token);
                Console.WriteLine("LIVE PASS: FFprobe verified actual synthetic " + container + " video output.");
            }
            using var service = new DownloadService(new YtDlpProvider(tools), new LocalPlaylistStorage(Path.Combine(folder, "PlaylistState")));
            const string url = "https://youtu.be/jNQXAC9IVRw";
            var metadata = await service.InspectAsync(url, null, deadline.Token);
            foreach (var container in Enum.GetValues<VideoContainer>())
            {
                var quality = metadata.QualitiesFor(container).OrderBy(choice => choice.Resolution).FirstOrDefault();
                if (quality == null) { Console.WriteLine("LIVE: " + container + " correctly unavailable for this source."); continue; }
                var result = await service.DownloadAsync(new() { Url = url, Folder = Path.Combine(folder, "YouTube"), VideoContainer = container, Height = quality.Resolution, Selection = quality.Selection }, null, deadline.Token);
                if (result.Cancelled || !File.Exists(result.FilePath)) throw new IOException("Live container output was not completed.");
                Console.WriteLine($"LIVE PASS: Anonymous YouTube {container}/{quality.Resolution}p download passed the real adapter and output validation ({new FileInfo(result.FilePath).Length}bytes).");
            }
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("LIVE FAIL: " + error); return 1; }
    }
}
