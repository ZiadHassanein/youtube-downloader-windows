using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace YouTubeDownloader;

// One atomic marker selects both tools. Previous files are retained for rollback; installed tools are never overwritten.
internal sealed class ToolUpdateStore
{
    internal sealed record FileRecord(string Directory, string Name, long Bytes, string Hash, string Version);
    internal sealed record Selection(FileRecord YtDlp = null, FileRecord Deno = null);
    internal sealed record State(Selection Active, Selection Previous = null, string RejectedYtDlp = null, string RejectedDeno = null);
    private readonly string root;
    private string Marker => Path.Combine(root, "active.json");
    internal ToolUpdateStore(string cache) => root = Path.Combine(cache, "MediaToolUpdates");
    internal string Root => root;

    internal State Read()
    {
        try
        {
            if (!File.Exists(Marker) || new FileInfo(Marker).Length > 8192 || IsLink(root) || IsLink(Marker)) return new(new());
            return JsonSerializer.Deserialize<State>(File.ReadAllText(Marker)) is { Active: not null } state ? state : new(new());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return new(new()); }
    }

    internal void Save(State state, CancellationToken token)
    {
        Directory.CreateDirectory(root);
        if (IsLink(root)) throw new InvalidDataException("The media-tool folder is invalid.");
        string temporary = Marker + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(state));
            token.ThrowIfCancellationRequested();
            File.Move(temporary, Marker, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    internal string Verify(FileRecord record, string expectedName, CancellationToken token)
    {
        if (record == null) return null;
        try
        {
            if (!Regex.IsMatch(record.Directory ?? "", @"^version-[a-f0-9]{32}$") || record.Name != expectedName ||
                record.Bytes is < 1 or > 500_000_000 || !Regex.IsMatch(record.Hash ?? "", @"^[a-fA-F0-9]{64}$") || IsLink(root)) return null;
            string directory = Path.Combine(root, record.Directory), file = Path.Combine(directory, expectedName);
            if (!File.Exists(file) || IsLink(directory) || IsLink(file)) return null;
            token.ThrowIfCancellationRequested();
            using var input = File.OpenRead(file);
            return input.Length == record.Bytes && string.Equals(Convert.ToHexString(SHA256.HashData(input)), record.Hash, StringComparison.OrdinalIgnoreCase) ? file : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return null; }
    }

    internal Selection VerifiedSelection(CancellationToken token)
    {
        var state = Read();
        bool Valid(Selection selection) => selection != null &&
            (selection.YtDlp == null || Verify(selection.YtDlp, "yt-dlp.exe", token) != null) &&
            (selection.Deno == null || Verify(selection.Deno, "deno.exe", token) != null);
        return Valid(state.Active) ? state.Active : Valid(state.Previous) ? state.Previous : new();
    }

    internal static bool IsLink(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
    internal static FileRecord Record(string directory, string file, string version)
    {
        using var input = File.OpenRead(file);
        return new(Path.GetFileName(directory), Path.GetFileName(file), input.Length, Convert.ToHexString(SHA256.HashData(input)), version);
    }
}
