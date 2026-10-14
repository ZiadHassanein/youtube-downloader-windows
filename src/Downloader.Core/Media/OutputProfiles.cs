namespace YouTubeDownloader;

public static class OutputProfiles
{
    public static string AudioExtension(AudioEncoding encoding) => encoding.ToString().ToLowerInvariant();
    public static bool IsLossless(AudioEncoding encoding) => encoding is AudioEncoding.Wav or AudioEncoding.Flac;
    public static string Key(DownloadOptions options)
    {
        string profile = options.Audio ? AudioExtension(options.AudioEncoding) + "-" + (IsLossless(options.AudioEncoding) ? "source" : options.AudioBitrate) : "mp4-" + options.Height;
        if (options.Captions != null)
            profile += "-subs-" + options.Captions.Language + "-" + options.Captions.Automatic + "-" + options.Captions.Format;
        return profile;
    }
}
