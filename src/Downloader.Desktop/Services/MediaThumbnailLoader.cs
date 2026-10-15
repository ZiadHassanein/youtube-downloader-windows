using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace YouTubeDownloader;

public interface IMediaThumbnailLoader
{
    Task<ImageSource> LoadAsync(string address, CancellationToken token);
}

/// <summary>Optional, bounded artwork loading; never uses credentials, files or private network hosts.</summary>
public sealed class MediaThumbnailLoader : IMediaThumbnailLoader
{
    private const int MaximumBytes = 2 * 1024 * 1024;
    private static readonly HttpClient Client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        UseProxy = false,
        ConnectTimeout = TimeSpan.FromSeconds(5),
        ConnectCallback = ConnectPublicHostAsync,
        PooledConnectionLifetime = TimeSpan.FromMinutes(2)
    }) { Timeout = TimeSpan.FromSeconds(8) };

    public async Task<ImageSource> LoadAsync(string address, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));
        token = timeout.Token;
        for (int redirect = 0; redirect < 4; redirect++)
        {
            if (!IsSafeAddress(address, out var uri)) return null;
            using var response = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            if ((int)response.StatusCode is >= 300 and <= 399 && response.Headers.Location is { } location)
            {
                address = new Uri(uri, location).AbsoluteUri;
                continue;
            }
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > MaximumBytes ||
                response.Content.Headers.ContentType?.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) != true)
                return null;
            await using var input = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            var bytes = new byte[16384];
            int read;
            while ((read = await input.ReadAsync(bytes, token).ConfigureAwait(false)) > 0)
            {
                if (buffer.Length + read > MaximumBytes) return null;
                buffer.Write(bytes, 0, read);
            }
            token.ThrowIfCancellationRequested();
            buffer.Position = 0;
            var header = BitmapFrame.Create(buffer, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            if (header.PixelWidth is < 1 or > 8192 || header.PixelHeight is < 1 or > 8192) return null;
            double scale = Math.Min(1, Math.Min(960d / header.PixelWidth, 540d / header.PixelHeight));
            buffer.Position = 0;
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = Math.Max(1, (int)(header.PixelWidth * scale));
            image.DecodePixelHeight = Math.Max(1, (int)(header.PixelHeight * scale));
            image.StreamSource = buffer;
            image.EndInit();
            image.Freeze();
            return image;
        }
        return null;
    }

    internal static bool IsSafeAddress(string address, out Uri uri) =>
        Uri.TryCreate(address, UriKind.Absolute, out uri) &&
        uri.Scheme == Uri.UriSchemeHttps && uri.Port == 443 &&
        string.IsNullOrEmpty(uri.UserInfo) && uri.HostNameType != UriHostNameType.Unknown &&
        !uri.IsLoopback && !uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) &&
        (!IPAddress.TryParse(uri.Host, out var ip) || IsPublicAddress(ip));

    internal static bool IsPublicAddress(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any) ||
            ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast) return false;
        var bytes = ip.GetAddressBytes();
        if (ip.AddressFamily == AddressFamily.InterNetworkV6) return (bytes[0] & 0xFE) != 0xFC;
        return bytes[0] is not (0 or 10 or 127) && bytes[0] < 224 &&
            !(bytes[0] == 169 && bytes[1] == 254) &&
            !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31) &&
            !(bytes[0] == 192 && bytes[1] == 168) &&
            !(bytes[0] == 100 && bytes[1] is >= 64 and <= 127);
    }

    private static async ValueTask<Stream> ConnectPublicHostAsync(SocketsHttpConnectionContext context, CancellationToken token)
    {
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, token).ConfigureAwait(false);
        foreach (var address in addresses.Where(IsPublicAddress))
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), token).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch { socket.Dispose(); if (token.IsCancellationRequested) throw; }
        }
        throw new HttpRequestException("No public thumbnail host was reachable.");
    }
}
