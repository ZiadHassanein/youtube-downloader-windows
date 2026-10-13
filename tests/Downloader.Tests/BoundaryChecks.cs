using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using YouTubeDownloader;

internal static class BoundaryChecks
{
    private static int count;
    private static void Check(bool value, string name)
    {
        if (!value)
            throw new Exception(name);
        count++;
        Console.WriteLine("PASS: " + name);
    }

    internal static async Task Run(string fixtureDirectory)
    {
        string root = Path.Combine(AppContext.BaseDirectory, "boundary-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string Zip(string name, params string[] entries)
        {
            string path = Path.Combine(root, name);
            using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
            foreach (string entry in entries)
            {
                using var writer = new StreamWriter(zip.CreateEntry(entry).Open());
                writer.Write("test");
            }

            return path;
        }

        foreach (var entries in new[]
        {
            new[]
            {
                "../escape.exe"
            },
            new[]
            {
                "C:/escape.exe"
            },
            new[]
            {
                "file",
                "FILE"
            }
        }

        )
        {
            bool rejected = false;
            try
            {
                SafeArchives.Extract(Zip(Guid.NewGuid() + ".zip", entries), root, new Dictionary<string, string>() { ["file"] = "file" }, 100, CancellationToken.None);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }

            Check(rejected, "Unsafe or case-duplicate archive entries are rejected before extraction");
        }

        bool oversized = false;
        try
        {
            SafeArchives.Extract(Zip("large.zip", "file"), root, new Dictionary<string, string>() { ["file"] = "file" }, 1, CancellationToken.None);
        }
        catch (InvalidDataException)
        {
            oversized = true;
        }

        Check(oversized, "Expanded archive size is bounded");
        bool missing = false;
        try
        {
            SafeArchives.Extract(Zip("missing.zip", "other"), root, new Dictionary<string, string>() { ["file"] = "file" }, 100, CancellationToken.None);
        }
        catch (InvalidDataException)
        {
            missing = true;
        }

        Check(missing, "Missing required archive entry cannot become ready");
        var redirect = new Handler(_ => new HttpResponseMessage(HttpStatusCode.Redirect) { Headers = { Location = new Uri("https://unapproved.example/tool") } });
        bool unsafeRedirect = false;
        try
        {
            await new VerifiedDownloads(redirect).TextAsync("https://github.com/vendor/tool", "https://github.com/vendor/", 100, CancellationToken.None);
        }
        catch (InvalidDataException)
        {
            unsafeRedirect = true;
        }

        Check(unsafeRedirect && redirect.Calls == 1, "An unapproved redirect never receives a request");
        var bytes = new byte[]
        {
            1,
            2,
            3
        };
        var wrong = new Handler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) });
        bool corrupt = false;
        try
        {
            await new VerifiedDownloads(wrong).DownloadAsync("https://github.com/vendor/tool", "https://github.com/vendor/", Path.Combine(root, "bad.download"), 10, new string ('0', 64), 3, null, CancellationToken.None);
        }
        catch (InvalidDataException)
        {
            corrupt = true;
        }

        Check(corrupt, "A downloaded file with the wrong hash cannot activate");
        string partial = Path.Combine(root, "partial");
        Directory.CreateDirectory(partial);
        foreach (string file in Directory.GetFiles(fixtureDirectory))
            File.Copy(file, Path.Combine(partial, Path.GetFileName(file)));
        foreach (string name in new[]
        {
            "yt-dlp.exe",
            "ffmpeg.exe",
            "ffprobe.exe"
        }

        )
            File.Copy(Path.Combine(fixtureDirectory, "FakeDownloader.exe"), Path.Combine(partial, name));
        var acquired = new List<string>();
        var dependencies = new DependencyManager(Path.Combine(root, "cache"), new[] { partial });
        dependencies.AcquireArtifact = (artifact, path, token) =>
        {
            acquired.Add(artifact.Name);
            File.WriteAllText(path, "corrupt fixture");
            return Task.CompletedTask;
        };
        bool incomplete = false;
        try
        {
            await dependencies.EnsureAsync(null, CancellationToken.None);
        }
        catch (InvalidDataException)
        {
            incomplete = true;
        }

        Check(incomplete && acquired.SequenceEqual(new[] { "Deno" }) && !Directory.Exists(Path.Combine(root, "cache", ToolManifest.Identity)), "Partial installed tools download only Deno and a bad download never promotes");
        Check(!Directory.GetDirectories(Path.Combine(root, "cache"), "stage-*").Any(), "Failed bootstrap cleans its owned staging directory");
        {
            var operations = new OperationCoordinator();
            using var owner = operations.Begin(OperationState.Inspecting, CancellationToken.None);
            bool overlap = false;
            try
            {
                using var other = operations.Begin(OperationState.Updating, CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                overlap = true;
            }

            operations.Cancel();
            Check(overlap && owner.Token.IsCancellationRequested && operations.IsBusy, "One core owner excludes updates during inspection and holds ownership until teardown");
        }

        UpdateChecks(root, fixtureDirectory);
        Console.WriteLine(count + " boundary checks passed.");
    }

    private static void UpdateChecks(string root, string fixtureDirectory)
    {
        string stage = Path.Combine(root, "package");
        Directory.CreateDirectory(stage);
        File.Copy(Path.Combine(fixtureDirectory, "FakeDownloader.exe"), Path.Combine(stage, "ViFree.exe"));
        File.WriteAllText(Path.Combine(stage, "README.txt"), "new readme");
        File.WriteAllText(Path.Combine(stage, "THIRD-PARTY-NOTICES.txt"), "new notices");
        var manifest = new PackageManifest
        {
            Version = "2.0.0",
            Architecture = "win-x64"
        };
        foreach (string file in AppUpdates.PackageFiles.Where(f => f != "package.json"))
        {
            using var input = File.OpenRead(Path.Combine(stage, file));
            manifest.Files[file] = Convert.ToHexString(SHA256.HashData(input));
        }

        File.WriteAllText(Path.Combine(stage, "package.json"), new JsonCodec().Serialize(manifest));
        Check(AppUpdates.ValidatePackage(stage, new Version(2, 0, 0)) != null, "Exact desktop package hashes, architecture and executable version validate");
        foreach (string protectedName in new[]
        {
            "ViFree.exe",
            "README.txt",
            "Renamed.exe"
        }

        )
        {
            string target = Path.Combine(root, "protected-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(target);
            foreach (string name in AppUpdates.PackageFiles)
                File.WriteAllText(Path.Combine(target, name), "old bytes");
            if (protectedName == "Renamed.exe")
                File.Move(Path.Combine(target, "ViFree.exe"), Path.Combine(target, protectedName));
            string protectedFile = Path.Combine(target, protectedName);
            File.SetAttributes(protectedFile, FileAttributes.ReadOnly);
            bool rejected = false;
            try
            {
                UpdateTransaction.Launch(stage, Path.Combine(target, protectedName == "Renamed.exe" ? protectedName : "ViFree.exe"), Environment.ProcessId);
            }
            catch (UnauthorizedAccessException)
            {
                rejected = true;
            }
            finally
            {
                File.SetAttributes(protectedFile, FileAttributes.Normal);
            }

            Check(rejected && !File.Exists(Path.Combine(stage, "transaction.json")) && !File.Exists(Path.Combine(stage, "update-helper.exe")) && File.ReadAllText(protectedFile) == "old bytes", "Protected " + protectedName + " fails before helper launch or app shutdown");
        }

        foreach (string failure in new[]
        {
            "backup-README.txt",
            "after-backup",
            "replace-README.txt",
            "before-start",
            "startup"
        }

        )
        {
            string target = Path.Combine(root, "target-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(target);
            File.WriteAllText(Path.Combine(target, "ViFree.exe"), "old exe");
            File.WriteAllText(Path.Combine(target, "README.txt"), "old readme");
            var journal = new UpdateJournal
            {
                Id = Guid.NewGuid().ToString("N"),
                Target = Path.Combine(target, "ViFree.exe"),
                Stage = stage,
                Backup = Path.Combine(root, "backup-" + Guid.NewGuid().ToString("N")),
                Version = "2.0.0",
                Phase = "prepared"
            };
            UpdateTransaction.Fault = step =>
            {
                if (step == failure)
                    throw new IOException("fixture");
            };
            bool failed = false, stopped = false;
            try
            {
                UpdateTransaction.Apply(Path.Combine(target, "journal.json"), journal, () =>
                {
                    if (failure == "startup")
                        throw new IOException("fixture");
                }, () => stopped = true);
            }
            catch (IOException)
            {
                failed = true;
            }

            Check(failed && stopped && journal.Phase == "rolled-back" && File.ReadAllText(journal.Target) == "old exe" && File.ReadAllText(Path.Combine(target, "README.txt")) == "old readme" && !File.Exists(Path.Combine(target, "package.json")), "Update failure at " + failure + " restores all old bytes and removes newly created companions");
        }

        string committed = Path.Combine(root, "committed");
        Directory.CreateDirectory(committed);
        var success = new UpdateJournal
        {
            Target = Path.Combine(committed, "ViFree.exe"),
            Stage = stage,
            Backup = Path.Combine(root, "backup-success"),
            Version = "2.0.0"
        };
        UpdateTransaction.Fault = step =>
        {
            if (step == "cleanup")
                throw new IOException("fixture");
        };
        UpdateTransaction.Apply(Path.Combine(committed, "journal.json"), success, () =>
        {
        });
        Check(success.Phase == "committed" && File.ReadAllText(Path.Combine(committed, "README.txt")) == "new readme", "Cleanup failure cannot undo a confirmed update");
        UpdateTransaction.Fault = null;
        bool oldRelease = false;
        string oldJson = "{\"draft\":false,\"prerelease\":false,\"tag_name\":\"v2.0.1\",\"assets\":[{\"name\":\"YouTubeDownloader-v2.0.1-windows-x64.zip\",\"browser_download_url\":\"https://github.com/ZiadHassanein/youtube-downloader-windows/releases/download/v2.0.1/YouTubeDownloader-v2.0.1-windows-x64.zip\"}]}";
        try
        {
            AppUpdates.Parse(oldJson, new Version(2, 0, 0));
        }
        catch (InvalidDataException)
        {
            oldRelease = true;
        }

        Check(oldRelease, "Legacy release names cannot be installed as native desktop payloads");
        Check(AppUpdates.Repository == "https://github.com/ZiadHassanein/ViFree-Downloads",
            "Updates use the public download repository independently of private source access");
        string releaseJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            draft = false,
            prerelease = false,
            tag_name = "v3.0.1",
            assets = new[]
            {
                new { name = "ViFree-v3.0.1-win-x64.zip", browser_download_url = AppUpdates.Repository + "/releases/download/v3.0.1/ViFree-v3.0.1-win-x64.zip" },
                new { name = "checksums-desktop.json", browser_download_url = AppUpdates.Repository + "/releases/download/v3.0.1/checksums-desktop.json" }
            }
        });
        var release = AppUpdates.Parse(releaseJson, new Version(3, 0, 0));
        Check(release.Version == new Version(3, 0, 1) && release.PackageName == "ViFree-v3.0.1-win-x64.zip",
            "ViFree discovers a newer package with its required checksum asset");
        Check(AppUpdates.Parse(releaseJson, new Version(3, 0, 1)) == null,
            "An installed ViFree release is recognized as current");
        foreach (string invalid in new[]
        {
            releaseJson.Replace(AppUpdates.Repository, "https://github.com/other/ViFree"),
            releaseJson.Replace(AppUpdates.Repository, "https://github.com/ZiadHassanein/ViFree"),
            releaseJson.Replace("ViFree-v3.0.1-win-x64.zip", "YouTubeDownloader-v3.0.1-win-x64.zip")
        })
        {
            bool rejected = false;
            try { AppUpdates.Parse(invalid, new Version(3, 0, 0)); }
            catch (InvalidDataException) { rejected = true; }
            Check(rejected, "A foreign repository or obsolete package name cannot supply a ViFree update");
        }
        NativeStartupChecks(root, fixtureDirectory);
    }

    private static void NativeStartupChecks(string root, string fixtureDirectory)
    {
        string jobs = Path.Combine(root, "jobs");
        Directory.CreateDirectory(jobs);
        UpdateTransaction.RootOverride = jobs;
        UpdateTransaction.StartupTimeout = TimeSpan.FromSeconds(2);
        try
        {
            foreach (bool acknowledge in new[]
            {
                true,
                false
            }

            )
            {
                string target = Path.Combine(root, "native-target-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(target);
                foreach (string file in Directory.GetFiles(fixtureDirectory))
                    File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
                File.Copy(Path.Combine(fixtureDirectory, "FakeDownloader.exe"), Path.Combine(target, "ViFree.exe"));
                File.WriteAllText(Path.Combine(target, "README.txt"), "old readme");
                if (!acknowledge)
                    File.WriteAllText(Path.Combine(target, "no-ack.flag"), "fixture");
                string stage = Path.Combine(jobs, Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(stage);
                File.Copy(Path.Combine(fixtureDirectory, "FakeDownloader.exe"), Path.Combine(stage, "ViFree.exe"));
                File.WriteAllText(Path.Combine(stage, "README.txt"), "new readme");
                File.WriteAllText(Path.Combine(stage, "THIRD-PARTY-NOTICES.txt"), "new notices");
                var manifest = new PackageManifest
                {
                    Version = "2.0.0",
                    Architecture = "win-x64"
                };
                foreach (string file in AppUpdates.PackageFiles.Where(f => f != "package.json"))
                {
                    using var input = File.OpenRead(Path.Combine(stage, file));
                    manifest.Files[file] = Convert.ToHexString(SHA256.HashData(input));
                }

                File.WriteAllText(Path.Combine(stage, "package.json"), new JsonCodec().Serialize(manifest));
                var journal = new UpdateJournal
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Target = Path.Combine(target, "ViFree.exe"),
                    Stage = stage,
                    Backup = Path.Combine(stage, "previous"),
                    Version = "2.0.0",
                    Phase = "prepared"
                };
                string path = Path.Combine(stage, "transaction.json");
                File.WriteAllText(path, new JsonCodec().Serialize(journal));
                UpdateTransaction.Run(path);
                var result = new JsonCodec().Deserialize<UpdateJournal>(File.ReadAllText(path));
                if (acknowledge)
                {
                    UpdateTransaction.PreflightTarget(result.Target);
                    Check(true, "Replacement preflight permits a writable running executable without changing it");
                    Check(result.Phase == "committed" && File.ReadAllText(Path.Combine(target, "README.txt")) == "new readme", "A real contained candidate must confirm startup before commit");
                    using var process = System.Diagnostics.Process.GetProcessById(result.CandidatePid);
                    process.Kill(true);
                    process.WaitForExit(5000);
                }
                else
                {
                    bool alive = false;
                    string child = Path.Combine(target, "candidate-child.pid");
                    Check(File.Exists(child), "Unacknowledged startup fixture spawned an independent descendant");
                    try
                    {
                        using var process = System.Diagnostics.Process.GetProcessById(int.Parse(File.ReadAllText(child)));
                        alive = !process.HasExited;
                    }
                    catch (ArgumentException)
                    {
                    }

                    string writer = Path.Combine(target, "candidate-writer.txt");
                    long before = File.Exists(writer) ? new FileInfo(writer).Length : 0;
                    Thread.Sleep(150);
                    Check(result.Phase == "rolled-back" && !alive && (!File.Exists(writer) || new FileInfo(writer).Length == before) && File.ReadAllText(Path.Combine(target, "README.txt")) == "old readme", "Startup timeout stops the candidate tree before restoring old companions");
                }
            }
        }
        finally
        {
            UpdateTransaction.RootOverride = null;
            UpdateTransaction.StartupTimeout = TimeSpan.FromSeconds(30);
        }
    }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        internal int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            return Task.FromResult(respond(request));
        }
    }
}
