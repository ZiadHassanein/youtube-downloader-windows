using YouTubeDownloader;

internal static class ServiceChecks
{
    internal static async Task Run()
    {
        var provider = new FixtureProvider();
        using var service = new DownloadService(provider, new FixtureStorage());
        var delivered = new List<DownloadProgress>();
        var preparation = new Capture(delivered.Add);
        service.Progress += delivered.Add;
        await service.InspectAsync("video", preparation, CancellationToken.None);
        var oldPreparation = provider.Preparation;
        var oldInspection = provider.Inspection;
        var queued = delivered.First();
        delivered.Clear();
        provider.HoldDownload = true;
        var download = service.DownloadAsync(new DownloadOptions { Url = "video", Folder = "fixture" }, preparation, CancellationToken.None);
        await provider.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        oldPreparation.Report(new DownloadProgress { Stage = "late preparation" });
        oldInspection.Report(new DownloadProgress { Stage = "late inspection" });
        if (delivered.Any(p => p.Stage.StartsWith("late")) || queued.OperationId == service.Operations.CurrentId)
            throw new Exception("Progress escaped its source operation identity.");
        provider.Release.SetResult();
        await download;
        Console.WriteLine("PASS: Late provider callbacks are rejected and queued preparation retains its old operation identity");
        provider.HoldDownload = false;
        provider.Downloads = 0;
        var result = await service.DownloadAsync(new DownloadOptions { Url = "playlist", Folder = "fixture", Playlist = new PlaylistInfo { Id = "playlist", Entries = new() { new() { Id = "unavailable", Title = "Unavailable", MediaUrl = null }, new() { Id = "available", Title = "Available", MediaUrl = "video" } } } }, preparation, CancellationToken.None);
        if (result.Failed != 1 || result.Completed != 1 || result.Remaining != 0 || provider.Downloads != 1)
            throw new Exception("An unavailable playlist entry prevented the next item from downloading.");
        Console.WriteLine("PASS: A mixed playlist records unavailable items and continues to available videos");
    }

    private sealed class Capture(Action<DownloadProgress> callback) : IProgress<DownloadProgress>
    {
        public void Report(DownloadProgress value) => callback(value);
    }

    private sealed class FixtureStorage : IPlaylistStorage
    {
        public PlaylistSession Open(DownloadOptions options) => new()
        {
            Folder = options.Folder,
            Completed = new(),
            Save = () =>
            {
            }
        };
        public bool IsCompleted(string folder, string file, DownloadOptions options, bool requireCaptions = false) => true;
    }

    private sealed class FixtureProvider : IMediaProvider
    {
        public string Id => "fixture";
        public bool IsReady => true;

        public bool HoldDownload;
        public int Downloads;
        public IProgress<DownloadProgress> Preparation, Inspection;
        public TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string Normalize(string input, bool playlist) => input ?? throw new ArgumentException("Unavailable");
        public MediaTargets ResolveTargets(string input) => new(input, null);
        public Task EnsureReadyAsync(IProgress<DownloadProgress> progress, CancellationToken token)
        {
            Preparation = progress;
            progress.Report(new DownloadProgress { Stage = "preparing" });
            return Task.CompletedTask;
        }

        public VideoInfo Inspect(string input, CancellationToken token, IProgress<DownloadProgress> progress = null)
        {
            Inspection = progress;
            return new();
        }

        public PlaylistInfo InspectPlaylist(string input, CancellationToken token, IProgress<DownloadProgress> progress = null) => new();
        public DownloadResult DownloadItem(DownloadOptions options, CancellationToken token, IProgress<DownloadProgress> progress = null)
        {
            Downloads++;
            Started.TrySetResult();
            if (HoldDownload)
                Release.Task.Wait(token);
            progress?.Report(new DownloadProgress { Stage = "Download complete", Percent = 100 });
            return new()
            {
                FilePath = "fixture.mp4"
            };
        }

        public void Cancel()
        {
        }

        public void Dispose()
        {
        }
    }
}
