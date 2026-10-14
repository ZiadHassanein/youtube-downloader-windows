using System.Net;

namespace YouTubeDownloader;

public interface IMediaLinkResolver
{
    bool Supports(Uri uri);
    MediaTargets Resolve(string input);
}

public sealed class YouTubeLinkResolver : IMediaLinkResolver
{
    public bool Supports(Uri uri) => uri.Host is "youtu.be" or "www.youtu.be" or "youtube.com" or "www.youtube.com" or "m.youtube.com" or "music.youtube.com";
    public MediaTargets Resolve(string input)
    {
        var link = VideoLink.Parse(input);
        return new(link.VideoUrl, link.PlaylistUrl);
    }
}

public sealed class PublicMediaLinkResolver : IMediaLinkResolver
{
    public bool Supports(Uri uri) => true;
    public MediaTargets Resolve(string input) => new(MediaLink.PublicUrl(input), null);
}

public static class MediaLink
{
    internal static MediaLinkRouter DefaultRouter { get; } = new(new IMediaLinkResolver[] { new YouTubeLinkResolver(), new PublicMediaLinkResolver() });
    public static string PublicUrl(string input)
    {
        input = input?.Trim();
        if (string.IsNullOrEmpty(input) || input.Length > 8192 || input.Any(char.IsControl) ||
            !Uri.TryCreate(input, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") ||
            !string.IsNullOrEmpty(uri.UserInfo) || uri.IsLoopback || uri.HostNameType == UriHostNameType.Basic ||
            (uri.HostNameType == UriHostNameType.Dns && !uri.Host.TrimEnd('.').Contains('.')) ||
            uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            !uri.IsDefaultPort)
            throw new ArgumentException("Paste a public video or playlist link using HTTPS.");
        if (IPAddress.TryParse(uri.Host.Trim('[', ']'), out var address) && !IsPublicAddress(address))
            throw new ArgumentException("Local and private network links are not supported.");
        return uri.AbsoluteUri;
    }

    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)) return false;
        var bytes = address.GetAddressBytes();
        if (bytes.Length == 16) return !address.IsIPv6LinkLocal && !address.IsIPv6SiteLocal && (bytes[0] & 0xfe) != 0xfc && bytes[0] != 0xff;
        return bytes[0] is not (0 or 10 or 127) && !(bytes[0] == 169 && bytes[1] == 254) && !(bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) && !(bytes[0] == 192 && bytes[1] == 168) && !(bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) && bytes[0] < 224;
    }

    public static MediaTargets Resolve(string input)
    {
        return DefaultRouter.Resolve(input);
    }
    public static string Normalize(string input, bool playlist)
    {
        return DefaultRouter.Normalize(input, playlist);
    }
}

public sealed class MediaLinkRouter
{
    private readonly IMediaLinkResolver[] resolvers;
    public MediaLinkRouter(IEnumerable<IMediaLinkResolver> resolvers)
    {
        this.resolvers = resolvers?.ToArray() ?? throw new ArgumentNullException(nameof(resolvers));
        if (this.resolvers.Length == 0 || this.resolvers.Any(r => r == null)) throw new ArgumentException("Supply at least one link resolver.");
    }
    public MediaTargets Resolve(string input)
    {
        string valid = MediaLink.PublicUrl(input);
        var resolver = resolvers.FirstOrDefault(r => r.Supports(new Uri(valid))) ?? throw new ArgumentException("This media link is not supported.");
        return resolver.Resolve(valid);
    }
    public string Normalize(string input, bool playlist)
    {
        var targets = Resolve(input);
        return (playlist ? targets.PlaylistUrl ?? targets.VideoUrl : targets.VideoUrl) ?? throw new ArgumentException("Choose a video or the whole playlist.");
    }
}
