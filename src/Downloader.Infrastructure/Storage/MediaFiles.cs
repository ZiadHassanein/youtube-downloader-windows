namespace YouTubeDownloader;
public static class MediaFiles
{
    public static bool HasCaptions(string media, CaptionRequest captions)
    {
        if (captions == null) return true;
        string file = Path.ChangeExtension(media, null) + "." + captions.Language + "." + captions.Format.ToString().ToLowerInvariant();
        try { return File.Exists(file) && new FileInfo(file).Length > 0 && (File.GetAttributes(file) & FileAttributes.ReparsePoint) == 0; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }
    public static bool IsCompleted(string folder, string file, bool audio)
        => IsCompleted(folder, file, audio ? "mp3" : "mp4");

    public static bool IsCompleted(string folder, string file, DownloadOptions options)
        => IsCompleted(folder, file, OutputProfiles.Extension(options));

    public static bool IsCompleted(string folder, string file, string extension)
    {
        if (string.IsNullOrWhiteSpace(file))
            return false;
        try
        {
            string root = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(file);
            if (!OutputProfiles.IsMediaExtension(extension) || !full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(full) || new FileInfo(full).Length == 0 || !string.Equals(Path.GetExtension(full), "." + extension, StringComparison.OrdinalIgnoreCase))
                return false;
            for (string parent = full; parent != null && parent.Length >= root.TrimEnd('\\').Length; parent = Path.GetDirectoryName(parent))
                if ((File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0)
                    return false;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
