using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace YouTubeDownloader;

public static class ToolRefresh
{
    internal static ToolRelease ReadRelease(string json) => ToolReleaseSource.YtDlp.Read(json);
    internal static string ReadChecksum(string text) => ToolReleaseSource.YtDlp.ReadChecksum(text);

    public static Task<(ToolSet Tools, MediaToolUpdateResult Result)> RefreshAsync(ToolSet current, string cache, IProgress<DownloadProgress> progress, CancellationToken token) =>
        new ToolUpdateClient(cache).RefreshAsync(current, progress, token);

    internal static string FindVerified(string cache, CancellationToken token, string kind = "yt-dlp")
    {
        var store = new ToolUpdateStore(cache);
        var selection = store.VerifiedSelection(token);
        string file = store.Verify(kind == "Deno" ? selection.Deno : selection.YtDlp, kind == "Deno" ? "deno.exe" : "yt-dlp.exe", token);
        if (file != null && ToolProbe.Accept(file, kind, token)) return file;
        return kind == "yt-dlp" ? FindLegacyVerified(cache, token) : null;
    }

    // Older previews stored only the extractor. Preserve that verified cache during migration.
    private static string FindLegacyVerified(string cache, CancellationToken token)
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
            if (!File.Exists(file) || ToolUpdateStore.IsLink(root) || ToolUpdateStore.IsLink(marker) || ToolUpdateStore.IsLink(file) || ToolUpdateStore.IsLink(Path.GetDirectoryName(file))) return null;
            using var input = File.OpenRead(file);
            if (input.Length != size) return null;
            token.ThrowIfCancellationRequested();
            if (!string.Equals(Convert.ToHexString(SHA256.HashData(input)), record.GetProperty("Hash").GetString(), StringComparison.OrdinalIgnoreCase)) return null;
            return ToolProbe.Accept(file, "yt-dlp", token) ? file : null;
        }
        catch (OperationCanceledException) { throw; }
        catch { return null; }
    }
}
