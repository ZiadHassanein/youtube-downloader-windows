using System.Net;
using System.Security.Cryptography;

namespace YouTubeDownloader;
public sealed class VerifiedDownloads : IDisposable
{
    private static readonly string[] AssetHosts =
    {
        "release-assets.githubusercontent.com",
        "objects.githubusercontent.com",
        "github-releases.githubusercontent.com"
    };
    private readonly HttpClient client;
    public void Dispose() => client.Dispose();
    public VerifiedDownloads() : this(new HttpClientHandler { AllowAutoRedirect = false, AutomaticDecompression = DecompressionMethods.None })
    {
    }

    internal VerifiedDownloads(HttpMessageHandler handler)
    {
        client = new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("YouTubeDownloader/2.0");
    }

    internal static bool SafeRedirect(Uri uri) => uri.Scheme == "https" && uri.IsDefaultPort && string.IsNullOrEmpty(uri.UserInfo) && AssetHosts.Contains(uri.IdnHost, StringComparer.OrdinalIgnoreCase);
    private async Task<HttpResponseMessage> GetAsync(Uri uri, CancellationToken token)
    {
        for (int redirect = 0; redirect < 5; redirect++)
        {
            using var headers = CancellationTokenSource.CreateLinkedTokenSource(token);
            headers.CancelAfter(TimeSpan.FromSeconds(30));
            var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, headers.Token).ConfigureAwait(false);
            if ((int)response.StatusCode is >= 300 and < 400)
            {
                var target = response.Headers.Location;
                response.Dispose();
                if (target == null)
                    throw new InvalidDataException("The download address is invalid.");
                target = target.IsAbsoluteUri ? target : new Uri(uri, target);
                if (!SafeRedirect(target))
                    throw new InvalidDataException("The download redirected to an unapproved address.");
                uri = target;
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var status = response.StatusCode;
                response.Dispose();
                throw new HttpRequestException("Could not download the required file.", null, status);
            }

            return response;
        }

        throw new InvalidDataException("The download redirected too many times.");
    }

    internal static void ValidateOrigin(string url, string approvedPrefix)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo) || !uri.AbsoluteUri.StartsWith(approvedPrefix, StringComparison.Ordinal))
            throw new InvalidDataException("The download address is invalid.");
    }

    public async Task<string> TextAsync(string url, string prefix, int maximum, CancellationToken token)
    {
        ValidateOrigin(url, prefix);
        using var response = await GetAsync(new Uri(url), token).ConfigureAwait(false);
        using var input = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        using var output = new MemoryStream();
        await Transfer(input, output, maximum, null, token).ConfigureAwait(false);
        return System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    public async Task DownloadAsync(string url, string prefix, string destination, long maximum, string expectedHash, long? exactSize, Action<long> progress, CancellationToken token)
    {
        ValidateOrigin(url, prefix);
        using var response = await GetAsync(new Uri(url), token).ConfigureAwait(false);
        if (response.Content.Headers.ContentLength > maximum || (exactSize.HasValue && response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength != exactSize))
            throw new InvalidDataException("The download size is invalid.");
        using (var input = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false))
        using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, true))
        {
            await Transfer(input, output, maximum, progress, token).ConfigureAwait(false);
            await output.FlushAsync(token).ConfigureAwait(false);
        }

        if (exactSize.HasValue && new FileInfo(destination).Length != exactSize)
            throw new InvalidDataException("The download is incomplete.");
        using var file = File.OpenRead(destination);
        string actual = Convert.ToHexString(await SHA256.HashDataAsync(file, token).ConfigureAwait(false));
        if (!string.Equals(actual, expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The download did not pass its integrity check.");
    }

    private static async Task Transfer(Stream input, Stream output, long maximum, Action<long> progress, CancellationToken token)
    {
        var buffer = new byte[65536];
        long total = 0;
        while (true)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            int count = await input.ReadAsync(buffer, deadline.Token).ConfigureAwait(false);
            if (count == 0)
                break;
            total += count;
            if (total > maximum)
                throw new InvalidDataException("The download is too large.");
            await output.WriteAsync(buffer.AsMemory(0, count), token).ConfigureAwait(false);
            progress?.Invoke(total);
        }
    }
}
