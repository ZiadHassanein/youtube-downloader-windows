namespace YouTubeDownloader;

public static class OutputProfiles
{
    private static readonly IReadOnlyList<int> Mp3Bitrates = Array.AsReadOnly(new[] { 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320 });
    private static readonly IReadOnlyList<int> AacBitrates = Array.AsReadOnly(new[] { 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384, 448, 512 });
    private static readonly IReadOnlyList<int> OpusBitrates = Array.AsReadOnly(new[] { 6, 8, 12, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384, 448, 510 });
    public static IReadOnlyList<int> AudioBitrates(AudioEncoding encoding) => encoding switch
    {
        AudioEncoding.Mp3 => Mp3Bitrates,
        AudioEncoding.M4a => AacBitrates,
        AudioEncoding.Opus => OpusBitrates,
        _ => Array.Empty<int>()
    };
    public static bool SupportsAudioBitrate(AudioEncoding encoding, int bitrate) =>
        Enum.IsDefined(encoding) && (IsLossless(encoding) || bitrate == 0 || AudioBitrates(encoding).Contains(bitrate));
    public static int NormalizeAudioBitrate(AudioEncoding encoding, int bitrate) =>
        bitrate >= 0 && bitrate <= 512 && SupportsAudioBitrate(encoding, bitrate) ? bitrate : 320;
    public static string AudioQualityKey(int bitrate) => bitrate == 0 ? "best" : bitrate.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public static string AudioExtension(AudioEncoding encoding) => encoding.ToString().ToLowerInvariant();
    public static bool IsLossless(AudioEncoding encoding) => encoding is AudioEncoding.Wav or AudioEncoding.Flac;
    public static string Key(DownloadOptions options)
    {
        string profile = options.Audio ? AudioExtension(options.AudioEncoding) + "-" + (IsLossless(options.AudioEncoding) ? "source" : AudioQualityKey(options.AudioBitrate)) : "mp4-" + options.Height;
        if (options.Captions != null)
            profile += "-subs-" + options.Captions.Language + "-" + options.Captions.Automatic + "-" + options.Captions.Format;
        return profile;
    }
}
