using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace YouTubeDownloader;
public sealed class AppRelease
{
    public Version Version;
    public string PackageUrl, ChecksumUrl, PackageName, PackageHash;
}

public sealed class PackageManifest
{
    public string Version;
    public string Architecture;
    public Dictionary<string, string> Files = new();
}

public static class AppUpdates
{
    // Canonical endpoint after the owner-approved rename. Installed versions through 4.2.1
    // still pin ViFree and need a manual upgrade; keep redirect validation strict.
    public const string Repository = "https://github.com/ZiadHassanein/Vydrop";
    private const string Api = "https://api.github.com/repos/ZiadHassanein/Vydrop/releases/latest";
    internal static readonly string[] PackageFiles =
    {
        "Vydrop.exe",
        "README.txt",
        "THIRD-PARTY-NOTICES.txt",
        "package.json"
    };
    private static readonly JsonCodec Json = new()
    {
        MaxJsonLength = 256 * 1024
    };
    public static Version Current => Assembly.GetExecutingAssembly().GetName().Version;

    public static async Task<AppRelease> CheckAsync(CancellationToken token)
    {
        using var client = new VerifiedDownloads();
        string text = await client.TextAsync(Api, Api, 2 * 1024 * 1024, token).ConfigureAwait(false);
        return Parse(text, Current);
    }

    internal static AppRelease Parse(string text, Version current)
    {
        using var document = JsonDocument.Parse(text);
        var root = document.RootElement;
        if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean())
            return null;
        if (!Version.TryParse(root.GetProperty("tag_name").GetString()?.TrimStart('v', 'V'), out var version) || version.Build < 0 || Compare(version) <= Compare(current))
            return null;
        string name = "Vydrop-v" + version.ToString(3) + "-win-x64.zip", package = null, checksum = null, packageHash = null;
        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            string url = asset.GetProperty("browser_download_url").GetString(), file = asset.GetProperty("name").GetString();
            if (!Trusted(url))
                continue;
            if (file == name)
            {
                package = url;
                if (asset.TryGetProperty("digest", out var digest) && digest.ValueKind != JsonValueKind.Null)
                {
                    string value = digest.ValueKind == JsonValueKind.String ? digest.GetString() : null;
                    if (!Regex.IsMatch(value ?? "", "^sha256:[A-Fa-f0-9]{64}$"))
                        throw new InvalidDataException("The release package digest is invalid.");
                    packageHash = value[7..];
                }
            }
            if (file == "checksums-desktop.json")
                checksum = url;
        }

        if (package == null || (packageHash == null && checksum == null))
            throw new InvalidDataException("This release has no verified desktop package yet.");
        return new AppRelease
        {
            Version = version,
            PackageName = name,
            PackageUrl = package,
            PackageHash = packageHash,
            ChecksumUrl = checksum
        };
    }

    private static Version Compare(Version value) => new(value.Major, value.Minor, Math.Max(0, value.Build), Math.Max(0, value.Revision));
    private static bool Trusted(string url)
    {
        try
        {
            VerifiedDownloads.ValidateOrigin(url, Repository + "/releases/download/");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<string> PrepareAsync(AppRelease release, CancellationToken token)
    {
        if (!Trusted(release.PackageUrl) || (release.ChecksumUrl != null && !Trusted(release.ChecksumUrl)))
            throw new InvalidDataException("The update address is invalid.");
        using var client = new VerifiedDownloads();
        string hash = release.PackageHash;
        if (release.ChecksumUrl != null)
        {
            var sum = Json.Deserialize<Dictionary<string, string>>(await client.TextAsync(release.ChecksumUrl, Repository + "/releases/download/", 4096, token).ConfigureAwait(false));
            if (!sum.TryGetValue("Hash", out var legacyHash) || !sum.TryGetValue("File", out var filename) || filename != release.PackageName || (hash != null && !string.Equals(hash, legacyHash, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("The update checksum is invalid.");
            hash = legacyHash;
        }
        if (!Regex.IsMatch(hash ?? "", "^[A-Fa-f0-9]{64}$"))
            throw new InvalidDataException("The update checksum is invalid.");
        string stage = Path.Combine(AppPaths.UserData, "Updates", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        string archive = Path.Combine(stage, "update.zip");
        await client.DownloadAsync(release.PackageUrl, Repository + "/releases/download/", archive, 256L * 1024 * 1024, hash, null, null, token).ConfigureAwait(false);
        SafeArchives.Extract(archive, stage, PackageFiles.ToDictionary(f => f, f => f), 400L * 1024 * 1024, token);
        ValidatePackage(stage, release.Version);
        return stage;
    }

    internal static PackageManifest ValidatePackage(string directory, Version version)
    {
        var manifest = Json.Deserialize<PackageManifest>(File.ReadAllText(Path.Combine(directory, "package.json")));
        if (manifest == null || manifest.Architecture != "win-x64" || !Version.TryParse(manifest.Version, out var declared) || Compare(declared) != Compare(version) || manifest.Files == null || !manifest.Files.Keys.Order().SequenceEqual(PackageFiles.Where(f => f != "package.json").Order()))
            throw new InvalidDataException("The update manifest is invalid.");
        foreach (var entry in manifest.Files)
        {
            if (!Regex.IsMatch(entry.Value ?? "", "^[A-Fa-f0-9]{64}$"))
                throw new InvalidDataException("The update file checksum is invalid.");
            using var file = File.OpenRead(Path.Combine(directory, entry.Key));
            if (!string.Equals(Convert.ToHexString(SHA256.HashData(file)), entry.Value, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The update files failed their integrity check.");
        }

        string executable = Path.Combine(directory, "Vydrop.exe");
        if (!ToolProbe.IsX64(executable) || !Version.TryParse(FileVersionInfo.GetVersionInfo(executable).FileVersion, out var actual) || Compare(actual) != Compare(version))
            throw new InvalidDataException("The update executable version is invalid.");
        return manifest;
    }

    public static void InstallAndRestart(string prepared) => UpdateTransaction.Launch(prepared, Environment.ProcessPath, Environment.ProcessId);
    public static bool RunHelper(string[] args)
    {
        if (args.Length == 2 && args[0] == "--apply-update")
        {
            UpdateTransaction.Run(args[1]);
            return true;
        }

        if (args.Length == 2 && args[0] == "--recover-update")
        {
            UpdateTransaction.Run(args[1], true);
            return true;
        }

        UpdateTransaction.SetAcknowledgement(args);
        return UpdateTransaction.RecoverFor(Environment.ProcessPath);
    }

    public static string RecoveryMessage => UpdateTransaction.RecoveryMessage;

    public static void AcknowledgeStartup() => UpdateTransaction.Acknowledge();
}
