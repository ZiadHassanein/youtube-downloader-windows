using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace YouTubeDownloader;

public static class ToolRefresh
{
    internal sealed record Release(string Version, string Url, long Bytes, string Digest);
    internal static Release ReadRelease(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        string version = root.GetProperty("tag_name").GetString();
        if (!DateTime.TryParseExact(version, "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || date < new DateTime(2026, 8, 19))
            throw new InvalidDataException("The media-tool release is invalid.");
        var asset = root.GetProperty("assets").EnumerateArray().Take(100).Single(a => a.GetProperty("name").GetString() == "yt-dlp.exe");
        string url = asset.GetProperty("browser_download_url").GetString();
        string expected = $"https://github.com/yt-dlp/yt-dlp/releases/download/{version}/yt-dlp.exe";
        long size = asset.GetProperty("size").GetInt64();
        if (url != expected || size is < 1000000 or > 60000000) throw new InvalidDataException("The media-tool asset is invalid.");
        string digest = asset.TryGetProperty("digest", out var hash) ? hash.GetString() : null;
        return new(version, url, size, digest);
    }
    internal static string ReadChecksum(string text)
    {
        var matches = Regex.Matches(text, @"(?m)^([A-Fa-f0-9]{64})\s+\*?yt-dlp\.exe\s*$");
        return matches.Count == 1 ? matches[0].Groups[1].Value : throw new InvalidDataException("The media-tool checksum is missing or invalid.");
    }

    public static async Task<(ToolSet Tools, MediaToolUpdateResult Result)> RefreshAsync(ToolSet current, string cache, IProgress<DownloadProgress> progress, CancellationToken token)
    {
        void Report(string text, double percent = 0) => progress?.Report(new DownloadProgress { Stage = "Preparing media tools", Detail = text, Percent = percent, Indeterminate = percent == 0 });
        Directory.CreateDirectory(cache);
        using var lease = new FileStream(Path.Combine(cache, "refresh.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var downloads = new VerifiedDownloads();
        Report("Checking for media-tool updates…");
        string json = await downloads.TextAsync("https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest", "https://api.github.com/repos/yt-dlp/yt-dlp/releases/", 512 * 1024, token).ConfigureAwait(false);
        var release = ReadRelease(json);
        var probe = new ToolProbe();
        string installed = await Task.Run(() => probe.Capture(current.YtDlp, token, "--version").Trim(), token).ConfigureAwait(false);
        if (string.CompareOrdinal(installed, release.Version) >= 0) return (current, new(false, installed));
        string prefix = $"https://github.com/yt-dlp/yt-dlp/releases/download/{release.Version}/";
        string hash = ReadChecksum(await downloads.TextAsync(prefix + "SHA2-256SUMS", prefix, 16384, token).ConfigureAwait(false));
        if (release.Digest != null && release.Digest != "sha256:" + hash.ToLowerInvariant()) throw new InvalidDataException("The media-tool checksums do not agree.");
        string root = Path.Combine(cache, "YtDlpUpdates");
        Directory.CreateDirectory(root);
        string stage = Path.Combine(root, "stage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        string file = Path.Combine(stage, "yt-dlp.exe");
        try
        {
            await downloads.DownloadAsync(release.Url, prefix, file, release.Bytes, hash, release.Bytes, n => Report("Downloading the downloader update…", 100.0 * n / release.Bytes), token).ConfigureAwait(false);
            if (!await Task.Run(() => probe.Accept(file, "yt-dlp", token), token).ConfigureAwait(false)) throw new InvalidDataException("The new media tool failed its compatibility check.");
            token.ThrowIfCancellationRequested();
            string target = Path.Combine(root, release.Version + "-" + Guid.NewGuid().ToString("N"));
            Directory.Move(stage, target);
            string marker = Path.Combine(root, "active.json"), temporary = marker + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, JsonSerializer.Serialize(new { Directory = Path.GetFileName(target), release.Bytes, Hash = hash }));
                token.ThrowIfCancellationRequested();
                File.Move(temporary, marker, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            return (ToolSet.Validated(Path.Combine(target, "yt-dlp.exe"), current.FFmpeg, current.FFprobe, current.Deno), new(true, release.Version));
        }
        finally
        {
            // This unique directory is created above and can contain only our single acquired file.
            if (File.Exists(file)) File.Delete(file);
            if (Directory.Exists(stage)) Directory.Delete(stage);
        }
    }
    internal static string FindVerified(string cache, CancellationToken token)
    {
        try
        {
            string root = Path.Combine(cache, "YtDlpUpdates"), marker = Path.Combine(root, "active.json");
            if (!File.Exists(marker) || new FileInfo(marker).Length > 2048) return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(marker));
            var record = doc.RootElement;
            long size = record.GetProperty("Bytes").GetInt64();
            if (size is < 1000000 or > 60000000) return null;
            string name = record.GetProperty("Directory").GetString();
            if (!Regex.IsMatch(name ?? "", @"^\d{4}\.\d{2}\.\d{2}-[a-f0-9]{32}$")) return null;
            string file = Path.Combine(root, name, "yt-dlp.exe");
            if (!File.Exists(file) || (File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0 || (File.GetAttributes(Path.GetDirectoryName(file)) & FileAttributes.ReparsePoint) != 0) return null;
            using var input = File.OpenRead(file);
            if (input.Length != record.GetProperty("Bytes").GetInt64()) return null;
            token.ThrowIfCancellationRequested();
            if (!string.Equals(Convert.ToHexString(SHA256.HashData(input)), record.GetProperty("Hash").GetString(), StringComparison.OrdinalIgnoreCase)) return null;
            return new ToolProbe().Accept(file, "yt-dlp", token) ? file : null;
        }
        catch (OperationCanceledException) { throw; }
        catch { return null; }
    }
}
