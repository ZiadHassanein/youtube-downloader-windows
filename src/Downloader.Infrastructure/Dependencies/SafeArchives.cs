using System.IO.Compression;

namespace YouTubeDownloader;
internal static class SafeArchives
{
    internal static void Extract(string archive, string destination, IReadOnlyDictionary<string, string> required, long maximum, CancellationToken token)
    {
        Directory.CreateDirectory(destination);
        if ((File.GetAttributes(destination) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("The preparation folder is invalid.");
        using var zip = ZipFile.OpenRead(archive);
        long expanded = 0;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (zip.Entries.Count > 500)
            throw new InvalidDataException("The archive has too many files.");
        foreach (var entry in zip.Entries)
        {
            string name = entry.FullName;
            if (name.Length > 512 || name.StartsWith('/') || name.Contains('\\') || name.Contains(':') || name.Any(char.IsControl) || name.Split('/').Any(p => p is "." or "..") || !names.Add(name.TrimEnd('/')) || (((uint)entry.ExternalAttributes >> 16) & 0xF000) == 0xA000 || (entry.ExternalAttributes & 0x400) != 0)
                throw new InvalidDataException("The archive contains an unsafe path.");
            expanded = checked(expanded + entry.Length);
            if (expanded > maximum)
                throw new InvalidDataException("The extracted archive is too large.");
        }

        foreach (var pair in required)
        {
            token.ThrowIfCancellationRequested();
            var entry = zip.GetEntry(pair.Key);
            if (entry == null || entry.Length <= 0 || Path.GetFileName(pair.Value) != pair.Value)
                throw new InvalidDataException("The archive is incomplete.");
            string output = Path.Combine(destination, pair.Value);
            using var input = entry.Open();
            using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            byte[] buffer = new byte[65536];
            long copied = 0;
            int count;
            while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                token.ThrowIfCancellationRequested();
                copied += count;
                if (copied > entry.Length)
                    throw new InvalidDataException("The archive size is invalid.");
                stream.Write(buffer, 0, count);
            }

            if (copied != entry.Length)
                throw new InvalidDataException("The archive is incomplete.");
            stream.Flush(true);
        }
    }
}
