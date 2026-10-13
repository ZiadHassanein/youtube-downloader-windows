namespace YouTubeDownloader;
public static class MediaFiles
{
    public static bool IsCompleted(string folder, string file, bool audio)
    {
        if (string.IsNullOrWhiteSpace(file))
            return false;
        try
        {
            string root = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(file);
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(full) || !string.Equals(Path.GetExtension(full), audio ? ".mp3" : ".mp4", StringComparison.OrdinalIgnoreCase))
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
