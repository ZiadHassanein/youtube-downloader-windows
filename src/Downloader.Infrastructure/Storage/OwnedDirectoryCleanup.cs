namespace YouTubeDownloader;

// Only direct, app-owned children are eligible. Never follow junctions or symbolic links.
internal static class OwnedDirectoryCleanup
{
    internal static void TryDelete(string root, string directory)
    {
        try
        {
            root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
            if (!string.Equals(Path.GetDirectoryName(directory), root, StringComparison.OrdinalIgnoreCase) ||
                !Directory.Exists(directory) || ToolUpdateStore.IsLink(root) || !IsPlainTree(directory)) return;
            DeleteTree(directory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    private static bool IsPlainTree(string directory) => !ToolUpdateStore.IsLink(directory) &&
        Directory.GetFiles(directory).All(file => !ToolUpdateStore.IsLink(file)) &&
        Directory.GetDirectories(directory).All(IsPlainTree);

    private static void DeleteTree(string directory)
    {
        if (ToolUpdateStore.IsLink(directory)) return;
        foreach (string file in Directory.GetFiles(directory))
        {
            if (ToolUpdateStore.IsLink(file)) return;
            File.Delete(file);
        }
        foreach (string child in Directory.GetDirectories(directory)) DeleteTree(child);
        Directory.Delete(directory);
    }
}
