using System.IO.Pipes;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace YouTubeDownloader;
internal enum BrowserDelivery { NoListener, Rejected, Accepted }
public static class BrowserLinkTransport
{
    internal static string PipeNameOverride { get; set; }
    public static string PipeName => PipeNameOverride ?? (ReleaseChannel.IsPreview ? "ViFree.Preview." : "ViFree.") + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(WindowsIdentity.GetCurrent().User.Value)))[..24];
    internal static async Task<BrowserDelivery> TrySendAsync(string url, int milliseconds, CancellationToken token)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await pipe.ConnectAsync(milliseconds, token).ConfigureAwait(false);
            await BrowserProtocol.WriteAsync(pipe, new { version = 1, type = "open-link", url }, token).ConfigureAwait(false);
            using var reply = await BrowserProtocol.ReadAsync(pipe, token).ConfigureAwait(false);
            return reply.RootElement.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True ? BrowserDelivery.Accepted : BrowserDelivery.Rejected;
        }
        catch (OperationCanceledException) { throw; }
        catch (IOException) { return BrowserDelivery.NoListener; }
        catch (TimeoutException) { return BrowserDelivery.NoListener; }
    }
    public static async Task ListenAsync(Func<string, bool> received, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(token).ConfigureAwait(false);
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
                deadline.CancelAfter(TimeSpan.FromSeconds(3));
                using var message = await BrowserProtocol.ReadAsync(pipe, deadline.Token).ConfigureAwait(false);
                string url = BrowserProtocol.ReadUrl(message.RootElement);
                bool accepted = received(url);
                await BrowserProtocol.WriteAsync(pipe, new { ok = accepted }, deadline.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { if (token.IsCancellationRequested) return; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                try { await Task.Delay(1000, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }
            }
            catch (JsonException) { }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
        }
    }
}
