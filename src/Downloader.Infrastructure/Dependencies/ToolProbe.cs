using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace YouTubeDownloader;
internal sealed class ToolProbe
{
    internal string Capture(string executable, CancellationToken token, params string[] arguments)
    {
        using var runner = new WindowsProcessRunner();
        var output = new StringBuilder();
        int code = runner.Run(executable, arguments, line => output.AppendLine(line), line => output.AppendLine(line), token, TimeSpan.FromSeconds(10), 2 * 1024 * 1024);
        if (code != 0)
            throw new InvalidDataException("The media tool did not pass its readiness check.");
        return output.ToString();
    }

    internal bool Accept(string file, string kind, CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            if (!IsX64(file))
                return false;
            if (kind == "yt-dlp")
            {
                string version = Capture(file, token, "--version").Trim();
                if (!DateTime.TryParseExact(version, "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || date < new DateTime(2026, 8, 19))
                    return false;
                string help = Capture(file, token, "--help");
                return new[]
                {
                    "--no-plugin-dirs",
                    "--no-remote-components",
                    "--js-runtimes"
                }.All(help.Contains);
            }

            if (kind == "Deno")
            {
                var version = Regex.Match(Capture(file, token, "--version"), @"^deno (\d+\.\d+\.\d+)");
                return version.Success && Version.Parse(version.Groups[1].Value) >= new Version(2, 3, 0);
            }

            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    internal bool AcceptPair(string ffmpeg, string ffprobe, CancellationToken token)
    {
        try
        {
            if (!IsX64(ffmpeg) || !IsX64(ffprobe))
                return false;
            string a = Regex.Match(Capture(ffmpeg, token, "-hide_banner", "-version"), @"ffmpeg version (\S+)").Groups[1].Value;
            string b = Regex.Match(Capture(ffprobe, token, "-hide_banner", "-version"), @"ffprobe version (\S+)").Groups[1].Value;
            if (a.Length == 0 || a != b)
                return false;
            if (!a.StartsWith("N-") && (!int.TryParse(a.Split('.')[0], out int major) || major < 6))
                return false;
            string encoders = Capture(ffmpeg, token, "-hide_banner", "-encoders"), muxers = Capture(ffmpeg, token, "-hide_banner", "-muxers");
            return new[] { "libmp3lame", "libopus", "flac", "pcm_s16le" }.All(encoders.Contains) && Regex.IsMatch(encoders, @"\baac\s") && new[] { "mp4", "mp3", "ogg", "wav", "flac" }.All(format => Regex.IsMatch(muxers, @"\b" + format + @"\b"));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    internal static bool IsX64(string file)
    {
        if (!Path.IsPathFullyQualified(file) || !File.Exists(file))
            return false;
        using var stream = File.OpenRead(file);
        using var reader = new BinaryReader(stream);
        if (stream.Length < 64 || reader.ReadUInt16() != 0x5A4D)
            return false;
        stream.Position = 60;
        int offset = reader.ReadInt32();
        if (offset < 64 || offset > stream.Length - 6)
            return false;
        stream.Position = offset;
        return reader.ReadUInt32() == 0x4550 && reader.ReadUInt16() == 0x8664;
    }
}
