using System.Diagnostics;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace YouTubeDownloader;
internal sealed class UpdateJournal
{
    public string Id, Target, Stage, Backup, Version, Phase;
    public int ParentPid, CandidatePid, HelperPid;
    public long ParentStart, CandidateStart, HelperStart;
    public Dictionary<string, bool> Existed = new();
    public Dictionary<string, string> BackupHashes = new();
}

internal static class UpdateTransaction
{
    private static readonly JsonCodec Json = new()
    {
        MaxJsonLength = 128 * 1024
    };
    private static string acknowledgement;
    internal static Action<string> Fault { get; set; }
    internal static TimeSpan StartupTimeout { get; set; } = TimeSpan.FromSeconds(30);
    internal static string RootOverride { get; set; }
    private static string Root => RootOverride ?? Path.Combine(AppPaths.UserData, "Updates");
    internal static string RecoveryMessage { get; private set; }

    internal static void Launch(string stage, string target, int parentPid)
    {
        var manifest = AppUpdates.ValidatePackage(stage, Version.Parse(FileVersionInfo.GetVersionInfo(Path.Combine(stage, "Vydrop.exe")).FileVersion));
        PreflightTarget(target);
        using var parent = Process.GetProcessById(parentPid);
        var journal = new UpdateJournal
        {
            Id = Guid.NewGuid().ToString("N"),
            Target = Path.GetFullPath(target),
            Stage = Path.GetFullPath(stage),
            Backup = Path.Combine(stage, "previous"),
            Version = manifest.Version,
            ParentPid = parentPid,
            ParentStart = parent.StartTime.ToUniversalTime().Ticks,
            Phase = "prepared"
        };
        string path = Path.Combine(stage, "transaction.json");
        Write(path, journal);
        string helper = Path.Combine(stage, "update-helper.exe");
        File.Copy(target, helper);
        StartHelper(helper, "--apply-update", path);
    }

    internal static void PreflightTarget(string target)
    {
        string directory = Path.GetDirectoryName(Path.GetFullPath(target));
        string probe = Path.Combine(directory, ".update-write-" + Guid.NewGuid().ToString("N"));
        try
        {
            foreach (string name in AppUpdates.PackageFiles)
            {
                string file = name == "Vydrop.exe" ? Path.GetFullPath(target) : Path.Combine(directory, name);
                if (!File.Exists(file))
                    continue;
                var attributes = File.GetAttributes(file);
                if ((attributes & (FileAttributes.ReadOnly | FileAttributes.ReparsePoint)) != 0)
                    throw new UnauthorizedAccessException("The app folder contains a protected package file.");
                // Test replacement access without modifying the running executable or companions.
                using var handle = CreateFile(file, 0x00010000, 7, IntPtr.Zero, 3, 0, IntPtr.Zero);
                if (handle.IsInvalid)
                    throw new UnauthorizedAccessException("A package file cannot be replaced.");
            }

            using (var file = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                file.Flush(true);
            File.Delete(probe);
        }
        catch (IOException error)
        {
            throw new UnauthorizedAccessException("The app folder cannot be updated. Extract the new ZIP into a writable folder.", error);
        }
        finally
        {
            try
            {
                if (File.Exists(probe))
                    File.Delete(probe);
            }
            catch
            {
            }
        }
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string name, uint access, uint sharing, IntPtr security, uint creation, uint flags, IntPtr template);
    private static void StartHelper(string helper, string command, string path)
    {
        var start = new ProcessStartInfo(helper)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(helper)
        };
        start.ArgumentList.Add(command);
        start.ArgumentList.Add(path);
        using var process = Process.Start(start) ?? throw new IOException("The update helper could not start.");
    }

    internal static void Run(string path, bool recovery = false)
    {
        var journal = Read(path);
        ValidateJournal(path, journal);
        try
        {
            using (var helper = Process.GetCurrentProcess())
            {
                journal.HelperPid = helper.Id;
                journal.HelperStart = helper.StartTime.ToUniversalTime().Ticks;
                Write(path, journal);
            }

            using (var parent = FindProcess(journal.ParentPid, journal.ParentStart))
                if (parent != null && !parent.WaitForExit(60000))
                    throw new IOException("The current app did not close.");
            if (recovery)
            {
                StopCandidate(journal);
                Rollback(path, journal);
                Restart(journal.Target);
                return;
            }

            using var owner = new CandidateOwner();
            Apply(path, journal, () =>
            {
                owner.Process = WindowsProcessRunner.ContainedProcess.Start(journal.Target, new[] { "--update-ack", path }, false);
                var candidate = owner.Process;
                journal.CandidatePid = candidate.Id;
                journal.CandidateStart = candidate.StartTicks;
                Write(path, journal);
                _ = Drain(candidate.Output);
                _ = Drain(candidate.Error);
                var clock = Stopwatch.StartNew();
                while (clock.Elapsed < StartupTimeout)
                {
                    string ack = path + ".ack";
                    if (File.Exists(ack) && File.ReadAllText(ack) == journal.Id + "|" + journal.CandidatePid + "|" + journal.CandidateStart)
                        return;
                    if (candidate.Wait(0))
                        throw new IOException("The updated app closed before startup completed.");
                    Thread.Sleep(100);
                }

                throw new IOException("The updated app did not confirm startup.");
            }, owner.Stop, () => owner.Process.ReleaseAfterStartup());
        }
        catch
        {
            if (journal.Phase == "rolled-back" || journal.Phase == "prepared")
            {
                try
                {
                    Restart(journal.Target);
                }
                catch
                {
                }
            }
            else
            {
                File.WriteAllText(Path.Combine(journal.Stage, "RECOVERY.txt"), "The update could not be restored automatically. Previous files: " + journal.Backup + ". Close app windows and extract a fresh app ZIP into a new writable folder. Settings and downloads are retained separately.");
                try
                {
                    StartHelper(Path.Combine(journal.Stage, "update-helper.exe"), "--recovery-failed", path);
                }
                catch
                {
                }
            }
        }
    }

    private sealed class CandidateOwner : IDisposable
    {
        internal WindowsProcessRunner.ContainedProcess Process;
        internal void Stop() => Process?.Stop();
        public void Dispose() => Process?.Dispose();
    }

    private static async Task Drain(Stream stream)
    {
        var bytes = new byte[4096];
        try
        {
            while (await stream.ReadAsync(bytes) > 0)
            {
            }
        }
        catch (IOException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static void Restart(string target)
    {
        using var process = Process.Start(new ProcessStartInfo(target) { UseShellExecute = false });
    }

    internal static void Apply(string path, UpdateJournal journal, Action startAndConfirm, Action stopAndConfirm = null, Action releaseAfterCommit = null)
    {
        AppUpdates.ValidatePackage(journal.Stage, Version.Parse(journal.Version));
        Directory.CreateDirectory(journal.Backup);
        bool replacementsStarted = false;
        try
        {
            // All backups are durable before the first target changes.
            foreach (string name in AppUpdates.PackageFiles)
            {
                Fault?.Invoke("backup-" + name);
                string target = Target(journal, name);
                journal.Existed[name] = File.Exists(target);
                if (journal.Existed[name])
                {
                    AtomicCopy(target, Path.Combine(journal.Backup, name));
                    journal.BackupHashes[name] = Hash(Path.Combine(journal.Backup, name));
                }
            }

            journal.Phase = "backed-up";
            Write(path, journal);
            Fault?.Invoke("after-backup");
            journal.Phase = "replacing";
            Write(path, journal);
            replacementsStarted = true;
            foreach (string name in AppUpdates.PackageFiles)
            {
                Fault?.Invoke("replace-" + name);
                AtomicCopy(Path.Combine(journal.Stage, name), Target(journal, name));
            }

            journal.Phase = "launching";
            Write(path, journal);
            Fault?.Invoke("before-start");
            startAndConfirm();
            journal.Phase = "committed";
            Write(path, journal);
            releaseAfterCommit?.Invoke();
            try
            {
                Fault?.Invoke("cleanup");
                CleanupBackup(journal);
            }
            catch
            {
            }
        }
        catch
        {
            try
            {
                stopAndConfirm?.Invoke();
                StopCandidate(journal);
                if (replacementsStarted)
                    Rollback(path, journal);
                else
                {
                    journal.Phase = "rolled-back";
                    Write(path, journal);
                }
            }
            catch
            {
                journal.Phase = "recovery-required";
                Write(path, journal);
            }

            throw;
        }
    }

    private static string Hash(string file)
    {
        using var stream = File.OpenRead(file);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static void AtomicCopy(string source, string target)
    {
        string temp = target + ".update-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var input = File.OpenRead(source))
            using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                input.CopyTo(output);
                output.Flush(true);
            }

            if (File.Exists(target))
                File.Replace(temp, target, null);
            else
                File.Move(temp, target);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    private static string Target(UpdateJournal journal, string name) => name == "Vydrop.exe" ? journal.Target : Path.Combine(Path.GetDirectoryName(journal.Target), name);
    private static void Rollback(string path, UpdateJournal journal)
    {
        // Validate every backup before restoring any target.
        foreach (string name in AppUpdates.PackageFiles)
        {
            if (!journal.Existed.TryGetValue(name, out bool existed))
                throw new InvalidDataException("Incomplete update recovery record.");
            if (existed && (!journal.BackupHashes.TryGetValue(name, out var hash) || Hash(Path.Combine(journal.Backup, name)) != hash))
                throw new InvalidDataException("An update backup is damaged.");
        }

        foreach (string name in AppUpdates.PackageFiles)
        {
            Fault?.Invoke("restore-" + name);
            if (journal.Existed[name])
                AtomicCopy(Path.Combine(journal.Backup, name), Target(journal, name));
            else if (File.Exists(Target(journal, name)))
                File.Delete(Target(journal, name));
        }

        journal.Phase = "rolled-back";
        Write(path, journal);
    }

    private static void StopCandidate(UpdateJournal journal)
    {
        using var process = FindProcess(journal.CandidatePid, journal.CandidateStart);
        if (process == null)
            return;
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            if (!process.WaitForExit(5000))
                throw new IOException("The updated app did not stop for recovery.");
        }
    }

    private static Process FindProcess(int pid, long start)
    {
        if (pid <= 0 || start <= 0)
            return null;
        try
        {
            var process = Process.GetProcessById(pid);
            if (process.StartTime.ToUniversalTime().Ticks == start)
                return process;
            process.Dispose();
        }
        catch (ArgumentException)
        {
        }
        catch (InvalidOperationException)
        {
        }

        return null;
    }

    internal static void SetAcknowledgement(string[] args)
    {
        if (args.Length == 2 && args[0] == "--update-ack")
        {
            var journal = Read(args[1]);
            ValidateJournal(args[1], journal);
            if (!string.Equals(Path.GetFullPath(Environment.ProcessPath), journal.Target, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The startup confirmation target is invalid.");
            AppUpdates.ValidatePackage(Path.GetDirectoryName(journal.Target), Version.Parse(journal.Version));
            acknowledgement = args[1];
        }

        if (args.Length == 2 && args[0] == "--recovery-failed")
        {
            var journal = Read(args[1]);
            ValidateJournal(args[1], journal);
            RecoveryMessage = "The update could not be restored automatically. Close this window and extract a fresh app ZIP into a new writable folder. Your settings and downloads are retained. Previous app files: " + journal.Backup;
        }
    }

    internal static void Acknowledge()
    {
        if (acknowledgement == null)
            return;
        var journal = Read(acknowledgement);
        using var process = Process.GetCurrentProcess();
        File.WriteAllText(acknowledgement + ".ack", journal.Id + "|" + process.Id + "|" + process.StartTime.ToUniversalTime().Ticks);
        acknowledgement = null;
    }

    internal static bool RecoverFor(string target)
    {
        if (!Directory.Exists(Root))
            return false;
        foreach (string directory in Directory.GetDirectories(Root))
        {
            string path = Path.Combine(directory, "transaction.json");
            if (!File.Exists(path) || path == acknowledgement)
                continue;
            try
            {
                var journal = Read(path);
                ValidateJournal(path, journal);
                if (!string.Equals(journal.Target, target, StringComparison.OrdinalIgnoreCase) || journal.Phase is "committed" or "rolled-back" or "prepared")
                    continue;
                using var parent = FindProcess(journal.ParentPid, journal.ParentStart);
                if (parent != null && !parent.HasExited)
                    continue;
                using var helper = FindProcess(journal.HelperPid, journal.HelperStart);
                if (helper != null && !helper.HasExited)
                    continue;
                using var current = Process.GetCurrentProcess();
                journal.ParentPid = current.Id;
                journal.ParentStart = current.StartTime.ToUniversalTime().Ticks;
                Write(path, journal);
                StartHelper(Path.Combine(journal.Stage, "update-helper.exe"), "--recover-update", path);
                return true;
            }
            catch
            {
                RecoveryMessage = "A previous update needs recovery. Extract a fresh app ZIP into a new writable folder. Your settings and downloads are retained.";
            }
        }

        PruneStages();
        return false;
    }

    private static void PruneStages()
    {
        try
        {
            if (ToolUpdateStore.IsLink(Root)) return;
            foreach (string directory in Directory.GetDirectories(Root))
            {
                try
                {
                    string path = Path.Combine(directory, "transaction.json");
                    if (string.Equals(path, acknowledgement, StringComparison.OrdinalIgnoreCase) || ToolUpdateStore.IsLink(directory)) continue;
                    if (File.Exists(path))
                    {
                        if (ToolUpdateStore.IsLink(path)) continue;
                        var journal = Read(path);
                        ValidateJournal(path, journal);
                        if (journal.Phase == "prepared")
                        {
                            // A preparation whose install never started is abandoned once it is a week old.
                            if (Directory.GetLastWriteTimeUtc(directory) >= DateTime.UtcNow.AddDays(-7)) continue;
                            using var parent = FindProcess(journal.ParentPid, journal.ParentStart);
                            if (parent != null && !parent.HasExited) continue;
                        }
                        else if (journal.Phase is not ("committed" or "rolled-back")) continue;
                        using var helper = FindProcess(journal.HelperPid, journal.HelperStart);
                        if (helper != null && !helper.HasExited) continue;
                    }
                    else if (!Guid.TryParseExact(Path.GetFileName(directory), "N", out _) ||
                        Directory.GetLastWriteTimeUtc(directory) >= DateTime.UtcNow.AddDays(-7)) continue;
                    OwnedDirectoryCleanup.TryDelete(Root, directory);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or System.Text.Json.JsonException) { }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    private static void ValidateJournal(string path, UpdateJournal journal)
    {
        string root = Path.GetFullPath(Root).TrimEnd('\\') + "\\";
        if (journal == null || !Guid.TryParseExact(journal.Id, "N", out _) || !Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(path) != "transaction.json" || Path.GetDirectoryName(Path.GetFullPath(path)) != Path.GetFullPath(journal.Stage) || journal.Backup != Path.Combine(journal.Stage, "previous") || !Path.IsPathFullyQualified(journal.Target) || !journal.Target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The update transaction is invalid.");
    }

    private static UpdateJournal Read(string path) => Json.Deserialize<UpdateJournal>(File.ReadAllText(path));
    private static void Write(string path, UpdateJournal journal)
    {
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(Json.Serialize(journal));
            output.Write(bytes);
            output.Flush(true);
        }

        File.Move(temp, path, true);
    }

    private static void CleanupBackup(UpdateJournal journal)
    {
        foreach (string name in AppUpdates.PackageFiles)
        {
            string backup = Path.Combine(journal.Backup, name);
            if (File.Exists(backup))
                File.Delete(backup);
        }

        if (Directory.Exists(journal.Backup))
            Directory.Delete(journal.Backup);
    }
}
