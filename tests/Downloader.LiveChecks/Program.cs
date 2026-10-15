using YouTubeDownloader;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--workflows") return await WorkflowLiveChecks.Run(args[1]);
        if (args.Length == 2 && args[0] == "--bootstrap") return await BootstrapLiveChecks.Run(args[1]);
        if (args.Length == 2 && args[0] == "--cached-tools") return await BootstrapLiveChecks.Run(args[1], false);
        if (args.Length == 2 && args[0] == "--audio-profiles") return await AudioProfileLiveChecks.Run(args[1]);
        try
        {
            string root = Path.GetFullPath(args[0]);
            Directory.CreateDirectory(root);
            var dependencies = new DependencyManager(Path.Combine(root, "Tools"), Array.Empty<string>());
            if (args.Length > 1)
                dependencies.AcquireArtifact = (artifact, path, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    File.Copy(Path.Combine(args[1], artifact.Name == "yt-dlp" ? "yt-dlp.exe" : artifact.Name == "Deno" ? "deno.zip" : "ffmpeg.zip"), path);
                    return Task.CompletedTask;
                };
            using var service = new DownloadService(new YtDlpProvider(dependencies), new LocalPlaylistStorage(Path.Combine(root, "PlaylistState")));
            var info = await service.InspectAsync("https://youtu.be/jNQXAC9IVRw", new Progress<DownloadProgress>(p => Console.WriteLine(p.Detail)), CancellationToken.None);
            var quality = info.Qualities.Where(q => q.Resolution <= 240).OrderBy(q => q.Resolution).FirstOrDefault() ?? info.Qualities.Last();
            Console.WriteLine("LIVE: inspection offers " + info.Qualities.Count + " qualities; selected " + quality.Resolution + "p");
            foreach (bool audio in new[]
            {
                false,
                true
            }

            )
                foreach (int bitrate in audio ? new[]
                {
                    192,
                    320
                }

                : new[]
                {
                    320
                }

                )
                {
                    var result = await service.DownloadAsync(new DownloadOptions { Url = "https://youtu.be/jNQXAC9IVRw", Folder = Path.Combine(root, "Media"), Audio = audio, AudioBitrate = bitrate, Height = quality.Resolution, QualityKey = quality.SelectionKey }, null, CancellationToken.None);
                    if (result.Cancelled || !File.Exists(result.FilePath))
                        throw new Exception("No completed live output");
                    Console.WriteLine("LIVE: verified " + (audio ? bitrate + " kbps MP3" : quality.Resolution + "p MP4"));
                }

            var playlist = await service.InspectPlaylistAsync("https://www.youtube.com/playlist?list=PLDoPjvoNmBAy532K9M_fjiAmrJ0gkCyLJ", null, CancellationToken.None);
            Console.WriteLine("LIVE: provided playlist has " + playlist.Entries.Count + " entries; full queue not downloaded again");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("LIVE: " + ex.Message);
            return 1;
        }
    }
}
