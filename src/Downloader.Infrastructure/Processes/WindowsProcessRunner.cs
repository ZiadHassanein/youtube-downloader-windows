using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace YouTubeDownloader;
// Processes cannot execute user code before their entire descendant tree is owned.
public sealed class WindowsProcessRunner : IDisposable
{
    private readonly object gate = new();
    private ContainedProcess active;
    internal bool RejectAssignment { get; set; }

    public bool IsBusy
    {
        get
        {
            lock (gate)
                return active != null;
        }
    }

    public int Run(string executable, IEnumerable<string> arguments, Action<string> stdout, Action<string> stderr, CancellationToken token, TimeSpan? timeout = null, int maxOutput = 32 * 1024 * 1024)
    {
        token.ThrowIfCancellationRequested();
        ContainedProcess process;
        lock (gate)
        {
            if (active != null)
                throw new InvalidOperationException("A media operation is already running.");
            process = ContainedProcess.Start(executable, arguments, RejectAssignment);
            active = process;
        }

        using (process)
        using (token.Register(Cancel))
        {
            var clock = Stopwatch.StartNew();
            var output = ReadLines(process.Output, stdout, maxOutput);
            var error = ReadLines(process.Error, stderr, Math.Min(maxOutput, 2 * 1024 * 1024));
            try
            {
                while (!process.Wait(50))
                {
                    if (output.IsFaulted || error.IsFaulted)
                    {
                        process.Stop();
                        break;
                    }

                    if (token.IsCancellationRequested)
                    {
                        process.Stop();
                        break;
                    }

                    if (timeout.HasValue && clock.Elapsed > timeout.Value)
                    {
                        process.Stop();
                        throw new TimeoutException("The media tool timed out. Try again.");
                    }
                }

                if (!process.Wait(5000))
                    throw new InvalidOperationException("Could not confirm that the media process stopped.");
                // Also reap descendants when the parent exits normally or unexpectedly.
                process.Stop();
                if (!Task.WhenAll(output, error).Wait(5000))
                    throw new InvalidOperationException("Could not confirm that the media output stopped.");
                token.ThrowIfCancellationRequested();
                output.GetAwaiter().GetResult();
                error.GetAwaiter().GetResult();
                return process.ExitCode;
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException ?? ex;
            }
            finally
            {
                process.Stop();
                lock (gate)
                    active = null;
            }
        }
    }

    private static async Task ReadLines(Stream stream, Action<string> receive, int maximum)
    {
        using var reader = new StreamReader(stream, new UTF8Encoding(false, false), false, 4096, leaveOpen: true);
        var line = new StringBuilder();
        var buffer = new char[4096];
        int total = 0, read;
        while ((read = await reader.ReadAsync(buffer).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > maximum)
                throw new InvalidDataException("The media tool returned too much data.");
            for (int i = 0; i < read; i++)
            {
                char c = buffer[i];
                if (c == '\n')
                {
                    receive?.Invoke(line.ToString().TrimEnd('\r'));
                    line.Clear();
                }
                else
                {
                    line.Append(c);
                    if (line.Length > maximum)
                        throw new InvalidDataException("The media tool returned too much data.");
                }
            }
        }

        if (line.Length > 0)
            receive?.Invoke(line.ToString().TrimEnd('\r'));
    }

    public void Cancel()
    {
        try
        {
            lock (gate)
                active?.Stop();
        }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception) { }
    }

    public void Dispose() => Cancel();
    internal sealed class ContainedProcess : IDisposable
    {
        private readonly object sync = new();
        private IntPtr job, process;
        public Stream Output { get; private set; }
        public Stream Error { get; private set; }

        public int ExitCode
        {
            get
            {
                if (!GetExitCodeProcess(process, out uint code))
                    throw NativeError();
                return unchecked((int)code);
            }
        }

        public bool Wait(uint milliseconds) => WaitForSingleObject(process, milliseconds) == 0;
        public int Id
        {
            get
            {
                using var value = Process.GetProcessById((int)GetProcessId(process));
                return value.Id;
            }
        }

        public long StartTicks
        {
            get
            {
                using var value = Process.GetProcessById(Id);
                return value.StartTime.ToUniversalTime().Ticks;
            }
        }

        public void Stop()
        {
            lock (sync)
            {
                if (job == IntPtr.Zero)
                    return;
                if (!TerminateJobObject(job, 1))
                    throw NativeError();
                var clock = Stopwatch.StartNew();
                while (true)
                {
                    if (!QueryInformationJobObject(job, 1, out JobAccounting accounting, (uint)Marshal.SizeOf<JobAccounting>(), IntPtr.Zero))
                        throw NativeError();
                    if (accounting.ActiveProcesses == 0)
                        break;
                    if (clock.Elapsed > TimeSpan.FromSeconds(5))
                        throw new IOException("Could not confirm that every media process stopped.");
                    Thread.Sleep(20);
                }

                CloseHandle(job);
                job = IntPtr.Zero;
            }
        }

        internal void ReleaseAfterStartup()
        {
            lock (sync)
            {
                var limits = new ExtendedLimits();
                int size = Marshal.SizeOf<ExtendedLimits>();
                IntPtr memory = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.StructureToPtr(limits, memory, false);
                    if (!SetInformationJobObject(job, 9, memory, (uint)size))
                        throw NativeError();
                }
                finally
                {
                    Marshal.FreeHGlobal(memory);
                }

                CloseHandle(job);
                job = IntPtr.Zero;
            }
        }

        public static ContainedProcess Start(string executable, IEnumerable<string> arguments, bool rejectAssignment)
        {
            if (!Path.IsPathFullyQualified(executable) || !File.Exists(executable))
                throw new FileNotFoundException("The media tool is not available.");
            var owner = new ContainedProcess();
            IntPtr stdoutRead = IntPtr.Zero, stdoutWrite = IntPtr.Zero, stderrRead = IntPtr.Zero, stderrWrite = IntPtr.Zero, stdinRead = IntPtr.Zero, stdinWrite = IntPtr.Zero;
            IntPtr attributes = IntPtr.Zero, handles = IntPtr.Zero;
            ProcessInformation info = default;
            try
            {
                owner.job = CreateJobObjectW(IntPtr.Zero, null);
                if (owner.job == IntPtr.Zero)
                    throw NativeError();
                var limits = new ExtendedLimits();
                limits.Basic.LimitFlags = 0x2000;
                int size = Marshal.SizeOf<ExtendedLimits>();
                IntPtr memory = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.StructureToPtr(limits, memory, false);
                    if (!SetInformationJobObject(owner.job, 9, memory, (uint)size))
                        throw NativeError();
                }
                finally
                {
                    Marshal.FreeHGlobal(memory);
                }

                var security = new SecurityAttributes
                {
                    Length = Marshal.SizeOf<SecurityAttributes>(),
                    Inherit = true
                };
                if (!CreatePipe(out stdoutRead, out stdoutWrite, ref security, 0) || !CreatePipe(out stderrRead, out stderrWrite, ref security, 0) || !CreatePipe(out stdinRead, out stdinWrite, ref security, 0))
                    throw NativeError();
                if (!SetHandleInformation(stdoutRead, 1, 0) || !SetHandleInformation(stderrRead, 1, 0) || !SetHandleInformation(stdinWrite, 1, 0))
                    throw NativeError();
                // Inherit only these three handles, never unrelated application handles.
                IntPtr attributeBytes = IntPtr.Zero;
                InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref attributeBytes);
                attributes = Marshal.AllocHGlobal(attributeBytes);
                if (!InitializeProcThreadAttributeList(attributes, 1, 0, ref attributeBytes))
                    throw NativeError();
                handles = Marshal.AllocHGlobal(IntPtr.Size * 3);
                Marshal.WriteIntPtr(handles, 0, stdoutWrite);
                Marshal.WriteIntPtr(handles, IntPtr.Size, stderrWrite);
                Marshal.WriteIntPtr(handles, IntPtr.Size * 2, stdinRead);
                if (!UpdateProcThreadAttribute(attributes, 0, (IntPtr)0x20002, handles, (IntPtr)(IntPtr.Size * 3), IntPtr.Zero, IntPtr.Zero))
                    throw NativeError();
                var startup = new StartupInfoEx
                {
                    Attributes = attributes
                };
                startup.Info.Size = Marshal.SizeOf<StartupInfoEx>();
                startup.Info.Flags = 0x100 | 1;
                startup.Info.StdOutput = stdoutWrite;
                startup.Info.StdError = stderrWrite;
                startup.Info.StdInput = stdinRead;
                var command = new StringBuilder(Arguments.Quote(executable) + " " + string.Join(" ", arguments.Select(Arguments.Quote)));
                if (!CreateProcessW(executable, command, IntPtr.Zero, IntPtr.Zero, true, 0x4 | 0x08000000 | 0x00080000, IntPtr.Zero, Path.GetDirectoryName(executable), ref startup, out info))
                    throw NativeError();
                owner.process = info.Process;
                if (rejectAssignment || !AssignProcessToJobObject(owner.job, info.Process))
                    throw new InvalidOperationException("Could not safely start the media tool.");
                // Readers take ownership only after the process is contained.
                owner.Output = new FileStream(new SafeFileHandle(stdoutRead, true), FileAccess.Read, 4096, false);
                stdoutRead = IntPtr.Zero;
                owner.Error = new FileStream(new SafeFileHandle(stderrRead, true), FileAccess.Read, 4096, false);
                stderrRead = IntPtr.Zero;
                if (ResumeThread(info.Thread) == uint.MaxValue)
                    throw NativeError();
                return owner;
            }
            catch
            {
                if (info.Process != IntPtr.Zero)
                {
                    TerminateProcess(info.Process, 1);
                    _ = WaitForSingleObject(info.Process, 5000);
                }

                owner.Dispose();
                throw;
            }
            finally
            {
                if (info.Thread != IntPtr.Zero)
                    CloseHandle(info.Thread);
                foreach (var handle in new[]
                {
                    stdoutRead,
                    stdoutWrite,
                    stderrRead,
                    stderrWrite,
                    stdinRead,
                    stdinWrite
                }

                )
                    if (handle != IntPtr.Zero)
                        CloseHandle(handle);
                if (attributes != IntPtr.Zero)
                {
                    DeleteProcThreadAttributeList(attributes);
                    Marshal.FreeHGlobal(attributes);
                }

                if (handles != IntPtr.Zero)
                    Marshal.FreeHGlobal(handles);
            }
        }

        public void Dispose()
        {
            Stop();
            Output?.Dispose();
            Error?.Dispose();
            if (process != IntPtr.Zero)
            {
                CloseHandle(process);
                process = IntPtr.Zero;
            }
        }
    }

    private static Win32Exception NativeError() => new(Marshal.GetLastWin32Error(), "Could not start the media tool on this PC.");
    [StructLayout(LayoutKind.Sequential)]
    private struct SecurityAttributes
    {
        public int Length;
        public IntPtr Descriptor;
        [MarshalAs(UnmanagedType.Bool)]
        public bool Inherit;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int Size;
        public string Reserved, Desktop, Title;
        public uint X, Y, XSize, YSize, XCountChars, YCountChars, FillAttribute, Flags;
        public ushort ShowWindow, ReservedSize;
        public IntPtr ReservedData, StdInput, StdOutput, StdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInfoEx
    {
        public StartupInfo Info;
        public IntPtr Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public IntPtr Process, Thread;
        public uint ProcessId, ThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BasicLimits
    {
        public long PerProcessUserTimeLimit, PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize, MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass, SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount, ReadTransferCount, WriteTransferCount, OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ExtendedLimits
    {
        public BasicLimits Basic;
        public IoCounters IO;
        public UIntPtr ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemoryUsed, PeakJobMemoryUsed;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobAccounting
    {
        public long TotalUser, TotalKernel, PeriodUser, PeriodKernel;
        public uint PageFaults, TotalProcesses, ActiveProcesses, TerminatedProcesses;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateJobObject(IntPtr job, uint code);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool QueryInformationJobObject(IntPtr job, int infoClass, out JobAccounting information, uint length, IntPtr returned);
    [DllImport("kernel32.dll")]
    private static extern uint GetProcessId(IntPtr process);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreatePipe(out IntPtr read, out IntPtr write, ref SecurityAttributes attributes, uint size);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetHandleInformation(IntPtr handle, uint mask, uint flags);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateJobObjectW(IntPtr attributes, string name);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(IntPtr job, int infoClass, IntPtr info, uint length);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcessW(string application, StringBuilder command, IntPtr processAttributes, IntPtr threadAttributes, bool inherit, uint flags, IntPtr environment, string directory, ref StartupInfoEx startup, out ProcessInformation process);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint ResumeThread(IntPtr thread);
    [DllImport("kernel32.dll")]
    private static extern bool TerminateProcess(IntPtr process, uint code);
    [DllImport("kernel32.dll")]
    private static extern uint WaitForSingleObject(IntPtr process, uint milliseconds);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeProcess(IntPtr process, out uint code);
    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool InitializeProcThreadAttributeList(IntPtr list, int count, uint flags, ref IntPtr size);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateProcThreadAttribute(IntPtr list, uint flags, IntPtr attribute, IntPtr value, IntPtr size, IntPtr previous, IntPtr returnSize);
    [DllImport("kernel32.dll")]
    private static extern void DeleteProcThreadAttributeList(IntPtr list);
}
