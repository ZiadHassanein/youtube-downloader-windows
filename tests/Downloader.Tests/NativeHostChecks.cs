using System.Diagnostics;
using YouTubeDownloader;

internal static class NativeHostChecks
{
    internal static async Task<int> Run(string executable)
    {
        using var lifetime = new CancellationTokenSource();
        int links = 0;
        var listener = BrowserLinkTransport.ListenAsync(_ => { Interlocked.Increment(ref links); return true; }, lifetime.Token);
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
                await process.WaitForExitAsync(deadline.Token);
                return response.RootElement.GetProperty("ok").GetBoolean();
            }
            finally { if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); } }
        }
        try
        {
            if (!await Request("chrome-extension://" + BrowserIdentity.ChromiumId + "/") || links != 1) throw new Exception("Packaged native host did not deliver its framed link through the current-user pipe.");
            if (await Request("chrome-extension://aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/") || links != 1) throw new Exception("Packaged native host accepted an unapproved origin.");
            Console.WriteLine("PASS: Packaged GUI EXE handles native stdin/stdout framing, current-user delivery and origin rejection without a visible window or download.");
            return 0;
        }
        finally { lifetime.Cancel(); await listener; }
    }
}
