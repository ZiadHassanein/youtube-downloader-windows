namespace YouTubeDownloader;

// A reported source bitrate is an estimate, not a measurement of perceived quality.
public sealed record AudioSourceInfo(string Codec, double? BitrateKbps, bool Lossless = false)
{
    public bool HasBitrate => BitrateKbps is > 0 and <= 100000 && double.IsFinite(BitrateKbps.Value);
    public bool Matches(AudioEncoding encoding) => encoding switch
    {
        AudioEncoding.Mp3 => Codec == "mp3",
        AudioEncoding.M4a => Codec is "aac" or "mp4a" || Codec?.StartsWith("mp4a.", StringComparison.OrdinalIgnoreCase) == true,
        AudioEncoding.Opus => Codec == "opus",
        AudioEncoding.Flac => Codec == "flac",
        AudioEncoding.Wav => Codec?.StartsWith("pcm_", StringComparison.OrdinalIgnoreCase) == true,
        _ => false
    };
}
