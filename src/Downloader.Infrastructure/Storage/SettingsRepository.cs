using System.Text;
using System.Text.Json;

namespace YouTubeDownloader;
public sealed class SettingsRepository
{
    private readonly string directory;
    private readonly JsonCodec json = new()
    {
        MaxJsonLength = 128 * 1024
    };
    public string FilePath => Path.Combine(directory, "settings.json");
    public string RecoveryMessage { get; private set; }
    internal Action BeforeReplace { get; set; }

    public SettingsRepository(string directory = null)
    {
        this.directory = directory ?? AppPaths.UserData;
    }

    public SavedState Load(string legacyDirectory = null)
    {
        RecoveryMessage = null;
        var primary = Read(FilePath);
        if (primary != null)
            return primary;
        var backup = Read(FilePath + ".backup");
        if (backup != null)
        {
            RecoveryMessage = "Your settings were recovered from a backup.";
            PreserveDamaged();
            return backup;
        }

        var legacy = Read(Path.Combine(legacyDirectory ?? AppContext.BaseDirectory, "settings.json"));
        if (legacy != null)
        {
            try
            {
                Save(legacy);
            }
            catch
            {
                RecoveryMessage = "Your previous settings were loaded but could not be saved. Choose a writable location or try again.";
            }

            return legacy;
        }

        if (File.Exists(FilePath))
        {
            RecoveryMessage = "Your settings could not be read. A recovery copy was kept.";
            PreserveDamaged();
        }

        return Validate(new SavedState());
    }

    private void PreserveDamaged()
    {
        try
        {
            if (File.Exists(FilePath))
                File.Copy(FilePath, FilePath + ".damaged-" + Guid.NewGuid().ToString("N"));
        }
        catch
        {
            RecoveryMessage = "Your settings could not be read. The original was retained for recovery.";
        }
    }

    private SavedState Read(string file)
    {
        if (!File.Exists(file))
            return null;
        try
        {
            if (new FileInfo(file).Length > 128 * 1024)
                return null;
            return Validate(json.Deserialize<SavedState>(File.ReadAllText(file, Encoding.UTF8)));
        }
        catch (InvalidDataException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Save(SavedState state)
    {
        Directory.CreateDirectory(directory);
        using var writer = AcquireWriter();
        string temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var bytes = Encoding.UTF8.GetBytes(json.Serialize(Validate(state)));
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(true);
            }

            if (Read(temporary) == null)
                throw new InvalidDataException("The settings could not be saved.");
            BeforeReplace?.Invoke();
            if (File.Exists(FilePath))
            {
                // A damaged primary must never replace a valid recovery backup.
                if (Read(FilePath) != null)
                    File.Replace(temporary, FilePath, FilePath + ".backup");
                else
                {
                    PreserveDamaged();
                    File.Move(temporary, FilePath, true);
                }
            }
            else
                File.Move(temporary, FilePath);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    private FileStream AcquireWriter()
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            try
            {
                return new FileStream(Path.Combine(directory, "settings.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)when (attempt < 49)
            {
                Thread.Sleep(20);
            }
        }

        throw new IOException("Settings are being saved by another instance. Try again.");
    }

    private static SavedState Validate(SavedState state)
    {
        if (state == null)
            throw new InvalidDataException("Empty settings.");
        state.Theme = state.Theme == "Light" ? "Light" : "Dark";
        state.Language = state.Language == "ar" ? "ar" : "en";
        state.Format = state.Format == 1 ? 1 : 0;
        state.Quality = Math.Clamp(state.Quality, 0, 100);
        state.AudioBitrate = state.AudioBitrate == 192 ? 192 : 320;
        if (!Enum.IsDefined(state.AudioEncoding)) state.AudioEncoding = AudioEncoding.Mp3;
        state.FolderVersion = state.FolderVersion == 1 ? 1 : 0;
        if (!ValidPath(state.Folder))
            state.Folder = null;
        state.Recent = (state.Recent ?? new()).Where(ValidPath).Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToList();
        return state;
    }

    private static bool ValidPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 32700 || value.Any(char.IsControl))
            return false;
        try
        {
            return Path.IsPathFullyQualified(value) && !value.StartsWith(@"\\") && Path.GetFullPath(value) == value;
        }
        catch
        {
            return false;
        }
    }
}
