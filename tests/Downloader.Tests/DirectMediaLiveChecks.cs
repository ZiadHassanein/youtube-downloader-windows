using System.Text;
using System.Text.Json;
using YouTubeDownloader;

internal static class DirectMediaLiveChecks
{
    internal static int Run(string toolsRoot, string output)
    {
        const string url = "https://www.w3schools.com/html/mov_bbb.mp4";
        var tools = ToolSet.Validated(Path.Combine(toolsRoot, "yt-dlp.exe"), Path.Combine(toolsRoot, "ffmpeg.exe"),
            Path.Combine(toolsRoot, "ffprobe.exe"), Path.Combine(toolsRoot, "deno.exe"));
        using var provider = new YtDlpProvider(tools);
        var info = provider.Inspect(url);
        var quality = info.Qualities.Single();
        if (quality.Resolution != 176 || !info.HasAudio || info.Duration < 10 || info.AudioSource.BitrateKbps is not (> 160 and < 161))
            throw new Exception("The live sample did not retain its measured 176p/AAC source facts.");
        Directory.CreateDirectory(output);
        var options = new DownloadOptions { Url = url, Folder = Path.GetFullPath(output), Height = quality.Resolution, Selection = quality.Selection };
        var result = provider.DownloadItem(options);
        using var runner = new WindowsProcessRunner();
        var json = new StringBuilder();
        int code = runner.Run(tools.FFprobe, new[] { "-v", "error", "-show_entries", "stream=codec_type,codec_name,width,height:format=duration", "-of", "json", result.FilePath },
            line => json.AppendLine(line), Console.Error.WriteLine, CancellationToken.None, TimeSpan.FromSeconds(15));
        using var probe = JsonDocument.Parse(json.ToString());
        if (code != 0 || !probe.RootElement.GetProperty("streams").EnumerateArray().Any(s => s.GetProperty("codec_type").GetString() == "video" && s.GetProperty("height").GetInt32() == 176) ||
            !probe.RootElement.GetProperty("streams").EnumerateArray().Any(s => s.GetProperty("codec_type").GetString() == "audio" && s.GetProperty("codec_name").GetString() == "aac"))
            throw new Exception("The downloaded sample failed its real video/audio probe.");
        Console.WriteLine(JsonSerializer.Serialize(new { result = "PASS", url, file = result.FilePath, resolution = quality.Resolution, audioKbps = info.AudioSource.BitrateKbps, bytes = new FileInfo(result.FilePath).Length }));
        return 0;
    }
}
