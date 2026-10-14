namespace YouTubeDownloader;
public static class ReleaseChannel
{
#if VIFREE_PREVIEW
    public static bool IsPreview => true;
#else
    public static bool IsPreview => false;
#endif
}
