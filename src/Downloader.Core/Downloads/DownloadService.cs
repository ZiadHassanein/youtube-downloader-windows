namespace YouTubeDownloader;
// All front ends use the same bounded application operations, including later extensions.
public sealed partial class DownloadService : IDisposable
{
    private readonly IMediaProvider provider;
    private readonly IPlaylistStorage playlists;
    public OperationCoordinator Operations { get; } = new();

    public DownloadService(IMediaProvider provider, IPlaylistStorage playlists)
    {
        this.provider = provider;
        this.playlists = playlists;
    }

    private IProgress<DownloadProgress> BoundProgress(long operationId, Action<DownloadProgress> report)
    {
        return new InlineProgress(p =>
        {
            if (Operations.CurrentId != operationId || !Operations.IsBusy)
                return;
            report(new DownloadProgress { OperationId = operationId, Stage = p.Stage, Detail = p.Detail, Percent = p.Percent, Indeterminate = p.Indeterminate, Item = p.Item, Total = p.Total, ItemTitle = p.ItemTitle });
        });
    }

    private sealed class InlineProgress(Action<DownloadProgress> report) : IProgress<DownloadProgress>
    {
        public void Report(DownloadProgress value) => report(value);
    }

    public event Action<DownloadProgress> Progress;
    public bool IsReady => provider.IsReady;

    public string Normalize(string url, bool playlist) => provider.Normalize(url, playlist);
    public MediaTargets ResolveTargets(string input) => provider.ResolveTargets(input);
    public async Task<string> RefreshToolsAsync(IProgress<DownloadProgress> progress, CancellationToken token)
    {
        using var operation = Operations.Begin(OperationState.Updating, token);
        if (provider is not IMediaMaintenance maintenance) throw new NotSupportedException("This provider does not support tool maintenance.");
        return await maintenance.RefreshToolsAsync(BoundProgress(operation.Id, p => progress?.Report(p)), operation.Token).ConfigureAwait(false);
    }
    public async Task<VideoInfo> InspectAsync(string url, IProgress<DownloadProgress> preparation, CancellationToken token)
    {
        var canonical = provider.Normalize(url, false);
        using var operation = Operations.Begin(OperationState.Preparing, token);
        await provider.EnsureReadyAsync(BoundProgress(operation.Id, p => preparation?.Report(p)), operation.Token).ConfigureAwait(false);
        Operations.SetState(operation.Id, OperationState.Inspecting);
        return await Task.Run(() => provider.Inspect(canonical, operation.Token, BoundProgress(operation.Id, p => Progress?.Invoke(p))), operation.Token).ConfigureAwait(false);
    }

    public async Task<PlaylistInfo> InspectPlaylistAsync(string url, IProgress<DownloadProgress> preparation, CancellationToken token)
    {
        var canonical = provider.Normalize(url, true);
        using var operation = Operations.Begin(OperationState.Preparing, token);
        await provider.EnsureReadyAsync(BoundProgress(operation.Id, p => preparation?.Report(p)), operation.Token).ConfigureAwait(false);
        Operations.SetState(operation.Id, OperationState.Inspecting);
        return await Task.Run(() => provider.InspectPlaylist(canonical, operation.Token, BoundProgress(operation.Id, p => Progress?.Invoke(p))), operation.Token).ConfigureAwait(false);
    }

    public async Task<DownloadResult> DownloadAsync(DownloadOptions options, IProgress<DownloadProgress> preparation, CancellationToken token)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        options = options.Snapshot();
        if (options.Playlist != null && options.Playlist.Entries.Count is < 1 or > 1000)
            throw new ArgumentException("Choose between one and 1,000 playlist videos.");
        if (options.Playlist != null)
            foreach (var entry in options.Playlist.Entries)
                if (entry.MediaUrl != null) entry.MediaUrl = provider.Normalize(entry.MediaUrl, false);
        options.Url = provider.Normalize(options.Url, options.Playlist != null);
        using var operation = Operations.Begin(OperationState.Preparing, token);
        await provider.EnsureReadyAsync(BoundProgress(operation.Id, p => preparation?.Report(p)), operation.Token).ConfigureAwait(false);
        Operations.SetState(operation.Id, OperationState.Downloading);
        return await Task.Run(() =>
        {
            if (options.Playlist == null)
                return provider.DownloadItem(options, operation.Token, BoundProgress(operation.Id, p => Progress?.Invoke(p)));
            IProgress<DownloadProgress> itemProgress = null;
            return PlaylistScheduler.Run(options, playlists, (request, cancel) => provider.DownloadItem(request, cancel, itemProgress), (item, total, title) =>
            {
                double fraction = 0;
                itemProgress = BoundProgress(operation.Id, p =>
                {
                    fraction = Math.Max(fraction, p.Stage == "Download complete" ? 1 : Math.Min(.95, p.Percent / 100));
                    p.Item = item;
                    p.Total = total;
                    p.ItemTitle = title;
                    p.Percent = 100.0 * (item - 1 + fraction) / total;
                    p.Indeterminate = false;
                    Progress?.Invoke(p);
                });
                itemProgress.Report(new DownloadProgress { Stage = "Downloading playlist", Detail = title });
            }, operation.Token);
        }, operation.Token).ConfigureAwait(false);
    }

    public void Cancel()
    {
        Operations.Cancel();
        provider.Cancel();
    }

    public void Dispose()
    {
        if (Operations.IsBusy)
            throw new InvalidOperationException("Wait for the active operation before disposing.");
        provider.Dispose();
    }
}
