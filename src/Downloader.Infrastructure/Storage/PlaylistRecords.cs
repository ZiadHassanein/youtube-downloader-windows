using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace YouTubeDownloader;
internal sealed class PlaylistRecords
{
    internal readonly string FilePath;
    internal Dictionary<string, string> Completed;
    private PlaylistRecords(string path)
    {
        FilePath = path;
    }

    private static JsonCodec Json()
    {
        return new JsonCodec
        {
            MaxJsonLength = 32 * 1024 * 1024
        };
    }

    internal static string DefaultDirectory
    {
        get
        {
            return Path.Combine(AppPaths.UserData, "PlaylistState");
        }
    }

    internal static string RecordPath(string folder, string profile, string directory)
    {
        string key = Path.GetFullPath(folder).TrimEnd('\\', '/').ToUpperInvariant() + "\n" + profile;
        return Path.Combine(directory, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key))) + ".json");
    }

    internal static PlaylistRecords Open(string folder, string profile, string directory = null)
    {
        directory = directory ?? DefaultDirectory;
        Directory.CreateDirectory(directory);
        foreach (string legacy in Directory.GetFiles(folder, ".yd-completed-*.json"))
        {
            string previousProfile = Path.GetFileNameWithoutExtension(legacy).Substring(".yd-completed-".Length);
            if (Regex.IsMatch(previousProfile, @"^mp(?:4-\d{1,5}|3-(?:192|320))$"))
                Import(folder, previousProfile, directory);
        }

        return Load(folder, profile, directory);
    }

    private static PlaylistRecords Load(string folder, string profile, string directory)
    {
        var records = new PlaylistRecords(RecordPath(folder, profile, directory));
        records.Completed = Read(records.FilePath) ?? new Dictionary<string, string>();
        return records;
    }

    private static void Import(string folder, string profile, string directory)
    {
        var records = Load(folder, profile, directory);
        string legacy = Path.Combine(folder, ".yd-completed-" + profile + ".json");
        if (File.Exists(legacy))
        {
            var previous = Read(legacy);
            if (previous != null)
            {
                foreach (var entry in previous)
                    if (!records.Completed.ContainsKey(entry.Key))
                        records.Completed[entry.Key] = entry.Value;
                records.Save();
            }
            else
                File.Copy(legacy, records.FilePath + ".legacy", true);
            // Remove the old visible record only after its data is safely stored privately.
            File.Delete(legacy);
        }
    }

    private static Dictionary<string, string> Read(string file)
    {
        if (!File.Exists(file))
            return null;
        try
        {
            return Json().Deserialize<Dictionary<string, string>>(File.ReadAllText(file, Encoding.UTF8));
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    internal void Save()
    {
        string temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, Json().Serialize(Completed), Encoding.UTF8);
            if (File.Exists(FilePath))
                File.Replace(temporary, FilePath, null);
            else
                File.Move(temporary, FilePath);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }
}
// The flat playlist is fetched once. Each item has bounded retries and a single active process.
// Completed paths are recorded per output profile and revalidated before skipping on a later run.
