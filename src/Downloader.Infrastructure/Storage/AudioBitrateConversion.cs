namespace YouTubeDownloader;

internal static class AudioBitrateConversion
{
    internal static void Apply(string file, DownloadOptions options, ToolSet tools, CancellationToken token)
    {
        // yt-dlp preserves an already matching codec. Honor an explicit lower bitrate locally.
        if (!options.Audio || options.AudioBitrate == 0 || OutputProfiles.IsLossless(options.AudioEncoding) || options.AudioSource?.Matches(options.AudioEncoding) != true) return;
        if (!MediaFiles.IsCompleted(options.Folder, file, options)) throw new InvalidDataException("The downloaded audio could not be verified.");
        string temporary = Path.Combine(Path.GetDirectoryName(file), ".vifree-audio-" + Guid.NewGuid().ToString("N") + "." + OutputProfiles.AudioExtension(options.AudioEncoding));
        string encoder = options.AudioEncoding switch { AudioEncoding.Mp3 => "libmp3lame", AudioEncoding.M4a => "aac", _ => "libopus" };
        try
        {
            using var runner = new WindowsProcessRunner();
            int code = runner.Run(tools.FFmpeg, new[] { "-nostdin", "-hide_banner", "-loglevel", "error", "-i", "file:" + Path.GetFullPath(file), "-map", "0:a:0", "-vn", "-map_metadata", "0", "-c:a", encoder, "-b:a", options.AudioBitrate + "k", "file:" + temporary }, null, null, token);
            if (code != 0) throw new InvalidOperationException("The audio could not be converted to the selected bitrate. The original file was kept.");
            MediaValidator.Validate(temporary, options, tools, token);
            token.ThrowIfCancellationRequested();
            File.Move(temporary, file, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
