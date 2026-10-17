using System.Diagnostics;
using System.IO.Pipes;
using YouTubeDownloader;

internal static class NativeHostChecks
{
    internal static async Task<int> Run(string executable)
    {
        // Never forward a fixture URL into a user's already-running stable app.
        using (var probe = new NamedPipeServerStream(BrowserLinkTransport.PipeName, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly)) { }
        using var lifetime = new CancellationTokenSource();
        int links = 0;
        bool accepting = true;
        string responseCode = null;
        var listener = BrowserLinkTransport.ListenAsync(_ => { Interlocked.Increment(ref links); return accepting; }, lifetime.Token);
        async Task<bool> Request(string origin)
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var start = new ProcessStartInfo(Path.GetFullPath(executable)) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(origin);
            start.ArgumentList.Add("--parent-window=0");
            using var process = Process.Start(start);
            try
            {
                await BrowserProtocol.WriteAsync(process.StandardInput.BaseStream, new { version = 1, type = "open-link", url = "https://example.com/public-native-fixture" }, deadline.Token);
                process.StandardInput.Close();
                using var response = await BrowserProtocol.ReadAsync(process.StandardOutput.BaseStream, deadline.Token);
                responseCode = response.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
                await process.WaitForExitAsync(deadline.Token);
                return response.RootElement.GetProperty("ok").GetBoolean();
            }
            finally { if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); } }
        }
        try
        {
            if (!await Request("chrome-extension://" + BrowserIdentity.ChromiumId + "/") || links != 1) throw new Exception("Packaged native host did not deliver its framed link through the current-user pipe.");
            accepting = false;
            var clock = Stopwatch.StartNew();
            if (await Request("chrome-extension://" + BrowserIdentity.ChromiumId + "/") || links != 2 || responseCode != "busy" || clock.Elapsed > TimeSpan.FromSeconds(3))
                throw new Exception("Packaged native host did not report an immediate busy rejection.");
            if (await Request("chrome-extension://aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/") || links != 2) throw new Exception("Packaged native host accepted an unapproved origin.");
            Console.WriteLine("PASS: Packaged GUI EXE handles native stdin/stdout framing, current-user delivery and origin rejection without a visible window or download.");
            return 0;
        }
        finally { lifetime.Cancel(); await listener; }
    }
}
