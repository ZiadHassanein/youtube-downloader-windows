namespace YouTubeDownloader;
public static class BrowserIdentity
{
    // Retain installed companion IDs and native host names through the Vydrop rebrand.
    public static string HostName => ReleaseChannel.IsPreview ? "com.vifree.preview" : "com.vifree";
    // Public development-extension identity; no signing secret is shipped.
    public const string ChromiumId = "cpoiefoeogidkjcgbacikpcdjkjeekcf";
    public static string FirefoxId => ReleaseChannel.IsPreview ? "vifree-preview@ziadhassanein.github.io" : "vifree@ziadhassanein.github.io";
}
