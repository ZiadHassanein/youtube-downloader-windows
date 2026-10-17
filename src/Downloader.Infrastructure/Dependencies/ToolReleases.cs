using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace YouTubeDownloader;

internal sealed record ToolRelease(string Version, string Url, long Bytes, string Digest);

internal sealed record ToolReleaseSource(string Kind, string Repository, string Asset, string Executable, long MaximumBytes)
{
    internal static readonly ToolReleaseSource YtDlp = new("yt-dlp", "yt-dlp/yt-dlp", "yt-dlp.exe", "yt-dlp.exe", 60_000_000);
    internal static readonly ToolReleaseSource Deno = new("Deno", "denoland/deno", "deno-x86_64-pc-windows-msvc.zip", "deno.exe", 200_000_000);
    internal string Api => $"https://api.github.com/repos/{Repository}/releases/";
    internal static string Prefix(ToolRelease release) => release.Url[..(release.Url.LastIndexOf('/') + 1)];

    internal ToolRelease Read(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if ((root.TryGetProperty("draft", out var draft) && draft.GetBoolean()) ||
            (root.TryGetProperty("prerelease", out var preview) && preview.GetBoolean()))
            throw new InvalidDataException("Only stable media-tool releases are supported.");
        string tag = root.GetProperty("tag_name").GetString();
        string version = Kind == "Deno" && tag?.StartsWith('v') == true ? tag[1..] : tag;
        bool valid = Kind == "Deno"
            ? Regex.IsMatch(tag ?? "", @"^v\d+\.\d+\.\d+$") && Version.Parse(version) >= new Version(2, 9, 7)
            : DateTime.TryParseExact(version, "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) && date >= new DateTime(2026, 8, 19);
        if (!valid) throw new InvalidDataException("The media-tool release is invalid.");
        var assets = root.GetProperty("assets").EnumerateArray().Take(101).ToArray();
        if (assets.Length > 100) throw new InvalidDataException("The media-tool release has too many assets.");
        var matches = assets.Where(a => a.GetProperty("name").GetString() == Asset).ToArray();
        if (matches.Length != 1) throw new InvalidDataException("The media-tool asset is missing or ambiguous.");
        var asset = matches[0];
        string url = asset.GetProperty("browser_download_url").GetString();
        long size = asset.GetProperty("size").GetInt64();
        if (url != $"https://github.com/{Repository}/releases/download/{tag}/{Asset}" || size < 1_000_000 || size > MaximumBytes)
            throw new InvalidDataException("The media-tool asset is invalid.");
        string digest = asset.TryGetProperty("digest", out var hash) ? hash.GetString() : null;
        if (digest != null && !Regex.IsMatch(digest, @"^sha256:[a-fA-F0-9]{64}$"))
            throw new InvalidDataException("The media-tool digest is invalid.");
        return new(version, url, size, digest);
    }

    internal string ReadChecksum(string text)
    {
        var matches = Regex.Matches(text, @"(?m)^([A-Fa-f0-9]{64})\s+\*?" + Regex.Escape(Asset) + @"\s*$");
        if (matches.Count == 1) return matches[0].Groups[1].Value;
        // Deno's Windows publisher emits Get-FileHash's list format rather than sha256sum format.
        if (Kind == "Deno" && matches.Count == 0 &&
            Regex.Count(text, @"(?m)^Algorithm\s*:\s*SHA256\s*$") == 1 &&
            Regex.Count(text, @"(?m)^Path\s*:\s*[^\r\n]*[\\/]" + Regex.Escape(Asset) + @"\s*$") == 1)
        {
            var hashes = Regex.Matches(text, @"(?m)^Hash\s*:\s*([A-Fa-f0-9]{64})\s*$");
            if (hashes.Count == 1) return hashes[0].Groups[1].Value;
        }
        throw new InvalidDataException("The media-tool checksum is missing or invalid.");
    }

    internal bool IsNewer(string candidate, string installed) => Kind == "Deno"
        ? Version.Parse(candidate) > Version.Parse(installed)
        : string.CompareOrdinal(candidate, installed) > 0;
}
