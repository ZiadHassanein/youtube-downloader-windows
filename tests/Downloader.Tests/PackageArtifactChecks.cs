using System.IO.Compression;
using System.Text.Json;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using YouTubeDownloader;

internal static class PackageArtifactChecks
{
    internal static int Run(string path)
    {
        string stage = Path.Combine(AppContext.BaseDirectory, "package-check-" + Guid.NewGuid().ToString("N"));
        SafeArchives.Extract(path, stage, AppUpdates.PackageFiles.ToDictionary(f => f, f => f), 400L * 1024 * 1024, CancellationToken.None);
        using (var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(stage, "package.json"))))
        {
            string version = document.RootElement.GetProperty("Version").GetString();
            if (version.Contains("-preview.")) ValidatePreview(stage, document.RootElement, version);
            else AppUpdates.ValidatePackage(stage, Version.Parse(version));
        }
        using var zip = ZipFile.OpenRead(path);
        if (zip.Entries.Count(e => e.FullName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) != 1 || zip.Entries.Any(e => e.FullName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("The app ZIP must contain one EXE and no nested downloads.");
        foreach (string browser in new[] { "chromium", "firefox" })
        {
            using var input = zip.GetEntry($"Browser/{browser}/manifest.json")?.Open() ?? throw new InvalidDataException("The browser companion is missing.");
            using var manifest = JsonDocument.Parse(input);
            string expectedName = ReleaseChannel.IsPreview ? "ViFree Preview" : "ViFree";
            if (manifest.RootElement.GetProperty("name").GetString() != expectedName || zip.GetEntry($"Browser/{browser}/connection.js") == null)
                throw new InvalidDataException("The browser companion uses the wrong release channel.");
            if (browser == "firefox" && manifest.RootElement.GetProperty("browser_specific_settings").GetProperty("gecko").GetProperty("id").GetString() != BrowserIdentity.FirefoxId)
                throw new InvalidDataException("The Firefox companion identity does not match the app.");
        }
        if (zip.GetEntry("Browser/README.md") == null || Directory.Exists(Path.Combine(stage, "Browser")))
            throw new InvalidDataException("Optional companions must remain separate from automatic app replacement.");
        Console.WriteLine("PASS: Single app ZIP validates its EXE/manifest and contains matching optional browser companions; automatic updates extract only app files.");
        return 0;
    }

    // Preview ZIPs are manual test artifacts; the production updater must continue rejecting prereleases.
    private static void ValidatePreview(string directory, JsonElement manifest, string version)
    {
        if (!ReleaseChannel.IsPreview || !Regex.IsMatch(version, @"^\d+\.\d+\.\d+-preview\.\d+$") ||
            manifest.GetProperty("Architecture").GetString() != "win-x64") throw new InvalidDataException("Invalid preview package/channel.");
        var files = manifest.GetProperty("Files").Deserialize<Dictionary<string, string>>();
        if (!files.Keys.Order().SequenceEqual(AppUpdates.PackageFiles.Where(file => file != "package.json").Order()))
            throw new InvalidDataException("Invalid preview payload names.");
        foreach (var file in files)
        {
            using var input = File.OpenRead(Path.Combine(directory, file.Key));
            if (!string.Equals(Convert.ToHexString(SHA256.HashData(input)), file.Value, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Invalid preview payload hash.");
        }
        string executable = Path.Combine(directory, "ViFree.exe");
        var info = FileVersionInfo.GetVersionInfo(executable);
        if (!ToolProbe.IsX64(executable) || info.FileVersion != version.Split('-')[0] + ".0" || !info.ProductVersion.StartsWith(version + "+", StringComparison.Ordinal))
            throw new InvalidDataException("Invalid preview executable identity.");
    }
}
