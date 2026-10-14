using System.IO.Compression;
using System.Text.Json;
using YouTubeDownloader;

internal static class PackageArtifactChecks
{
    internal static int Run(string path)
    {
        string stage = Path.Combine(AppContext.BaseDirectory, "package-check-" + Guid.NewGuid().ToString("N"));
        SafeArchives.Extract(path, stage, AppUpdates.PackageFiles.ToDictionary(f => f, f => f), 400L * 1024 * 1024, CancellationToken.None);
        AppUpdates.ValidatePackage(stage, new Version(4, 0, 0));
        using var zip = ZipFile.OpenRead(path);
        if (zip.Entries.Count(e => e.FullName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) != 1 || zip.Entries.Any(e => e.FullName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("The app ZIP must contain one EXE and no nested downloads.");
        foreach (string browser in new[] { "chromium", "firefox" })
        {
            using var input = zip.GetEntry($"Browser/{browser}/manifest.json")?.Open() ?? throw new InvalidDataException("The browser companion is missing.");
            using var manifest = JsonDocument.Parse(input);
            if (manifest.RootElement.GetProperty("name").GetString() != "ViFree" || zip.GetEntry($"Browser/{browser}/connection.js") == null)
                throw new InvalidDataException("The browser companion uses the wrong release channel.");
            if (browser == "firefox" && manifest.RootElement.GetProperty("browser_specific_settings").GetProperty("gecko").GetProperty("id").GetString() != BrowserIdentity.FirefoxId)
                throw new InvalidDataException("The Firefox companion identity does not match the app.");
        }
        if (zip.GetEntry("Browser/README.md") == null || Directory.Exists(Path.Combine(stage, "Browser")))
            throw new InvalidDataException("Optional companions must remain separate from automatic app replacement.");
        Console.WriteLine("PASS: Single app ZIP validates its EXE/manifest and contains matching optional browser companions; automatic updates extract only app files.");
        return 0;
    }
}
