using System.Diagnostics;
using static YouTubeDownloader.BrowserIdentity;

namespace YouTubeDownloader;
public static class BrowserHost
{
    public static bool IsInvocation(string[] args) => args.Length > 0 && (args[0].StartsWith("chrome-extension://", StringComparison.Ordinal) || args[0].EndsWith(HostName + ".firefox.json", StringComparison.OrdinalIgnoreCase));
    public static bool IsAllowedCaller(string[] args)
    {
        if (args.Length is < 1 or > 3) return false;
        if (args[0] == "chrome-extension://" + ChromiumId + "/")
            return args.Skip(1).All(a => a.StartsWith("--parent-window=", StringComparison.Ordinal) && ulong.TryParse(a[16..], out _));
        return args.Length == 2 && args[0].EndsWith(HostName + ".firefox.json", StringComparison.OrdinalIgnoreCase) && args[1] == FirefoxId;
    }
    public static async Task RunAsync(string[] args)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        object response;
        try
        {
            if (!IsAllowedCaller(args)) throw new InvalidDataException("This browser extension is not authorized.");
            using var message = await BrowserProtocol.ReadAsync(Console.OpenStandardInput(), timeout.Token).ConfigureAwait(false);
            string url = BrowserProtocol.ReadUrl(message.RootElement);
            var delivery = await DeliverAsync(url, () =>
            {
                using var process = Process.Start(new ProcessStartInfo(Environment.ProcessPath) { UseShellExecute = false, CreateNoWindow = true, ArgumentList = { "--browser-launch" } });
            }, timeout.Token).ConfigureAwait(false);
            response = delivery == BrowserDelivery.Accepted
                ? new { ok = true, code = "opened", message = "Link opened in Vydrop. Review it before downloading." }
                : new { ok = false, code = "busy", message = "Vydrop is busy. Finish or cancel the current download, then try again." };
        }
        catch { response = new { ok = false, message = "Could not send this link. Open Vydrop and enable browser integration, then try again." }; }
        try { await BrowserProtocol.WriteAsync(Console.OpenStandardOutput(), response, CancellationToken.None).ConfigureAwait(false); } catch (IOException) { }
    }

    internal static async Task<BrowserDelivery> DeliverAsync(string url, Action launch, CancellationToken token)
    {
        var delivery = await BrowserLinkTransport.TrySendAsync(url, 250, token).ConfigureAwait(false);
        if (delivery != BrowserDelivery.NoListener) return delivery;
        launch();
        for (int attempt = 0; attempt < 20; attempt++)
        {
            await Task.Delay(200, token).ConfigureAwait(false);
            delivery = await BrowserLinkTransport.TrySendAsync(url, 250, token).ConfigureAwait(false);
            if (delivery != BrowserDelivery.NoListener) return delivery;
        }
        throw new IOException("Open Vydrop and try sending the link again.");
    }
}
