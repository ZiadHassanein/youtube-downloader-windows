using System.Text.Json;
using YouTubeDownloader;

internal static class AudioProfileLiveChecks
{
    internal static async Task<int> Run(string root)
    {
        try
        {
            using var manager = new DependencyManager(Path.Combine(Path.GetFullPath(root), "Tools"), Array.Empty<string>());
            var tools = manager.Discover(CancellationToken.None) ?? throw new IOException("Prepare the isolated tool cache first.");
            string output = Path.Combine(Path.GetFullPath(root), "SyntheticAudio"); Directory.CreateDirectory(output);
            using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            using var runner = new WindowsProcessRunner();
            int count = 0;
            foreach (var encoding in Enum.GetValues<AudioEncoding>())
            {
                var (encoder, codec) = encoding switch
                {
                    AudioEncoding.Mp3 => ("libmp3lame", "mp3"), AudioEncoding.M4a => ("aac", "aac"),
                    AudioEncoding.Opus => ("libopus", "opus"), AudioEncoding.Wav => ("pcm_s16le", "pcm_s16le"), _ => ("flac", "flac")
                };
                foreach (int bitrate in OutputProfiles.AudioBitrates(encoding).Prepend(0))
                {
                    string file = Path.Combine(output, encoding + "-" + bitrate + "." + OutputProfiles.AudioExtension(encoding));
                    var args = new List<string> { "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", "sine=frequency=440:sample_rate=48000:duration=0.2", "-ac", "2", "-c:a", encoder };
                    if (bitrate != 0) args.AddRange(new[] { "-b:a", bitrate + "k" });
                    args.AddRange(new[] { "-y", file });
                    if (runner.Run(tools.FFmpeg, args, null, Console.Error.WriteLine, deadline.Token, TimeSpan.FromSeconds(15)) != 0) throw new IOException("Encoding failed: " + encoding + "/" + bitrate);
                    string json = new ToolProbe().Capture(tools.FFprobe, deadline.Token, "-v", "error", "-show_entries", "stream=codec_name:format=duration", "-of", "json", file);
                    using var info = JsonDocument.Parse(json);
                    if (info.RootElement.GetProperty("streams")[0].GetProperty("codec_name").GetString() != codec || new FileInfo(file).Length == 0)
                        throw new IOException("Encoded output did not validate: " + encoding + "/" + bitrate);
                    count++;
                }
            }
            Console.WriteLine("LIVE PASS: " + count + " audio profiles encoded and probed a synthetic local tone with the automatically acquired FFmpeg/ffprobe pair.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("LIVE FAIL: " + ex); return 1; }
    }
}
