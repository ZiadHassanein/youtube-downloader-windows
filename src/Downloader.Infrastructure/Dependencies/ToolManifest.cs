namespace YouTubeDownloader;
internal sealed record ToolArtifact(string Name, string Url, string Hash, long Bytes, long ExpandedBytes, Dictionary<string, string> Entries);
internal static class ToolManifest
{
    internal const string Identity = "yt2026.08.19-ff9.0.2-deno2.9.7-x64";
    internal static readonly ToolArtifact[] Artifacts =
    {
        new("yt-dlp", "https://github.com/yt-dlp/yt-dlp/releases/download/2026.08.19/yt-dlp.exe", "66674953fe251b89f4d08c5f0e35e0728679bd67ab3d7d05c0562af101dd3e7a", 17840399, 17840399, new() { ["yt-dlp.exe"] = "yt-dlp.exe" }),
        new("FFmpeg", "https://github.com/GyanD/codexffmpeg/releases/download/9.0.2/ffmpeg-9.0.2-essentials_build.zip", "60f467265b1e312373dbcd92200c2618a74850f98d3d078e94296bb3fa2047ba", 114768076, 340000000, new() { ["ffmpeg-9.0.2-essentials_build/bin/ffmpeg.exe"] = "ffmpeg.exe", ["ffmpeg-9.0.2-essentials_build/bin/ffprobe.exe"] = "ffprobe.exe", ["ffmpeg-9.0.2-essentials_build/LICENSE"] = "FFmpeg-LICENSE.txt", ["ffmpeg-9.0.2-essentials_build/README.txt"] = "FFmpeg-README.txt" }),
        new("Deno", "https://github.com/denoland/deno/releases/download/v2.9.7/deno-x86_64-pc-windows-msvc.zip", "a0c3101b4158d1dfb7d6a78a7bf0f3de80c96bb423c152beec8beb22786f2238", 42630221, 100000000, new() { ["deno.exe"] = "deno.exe" })
    };
}
