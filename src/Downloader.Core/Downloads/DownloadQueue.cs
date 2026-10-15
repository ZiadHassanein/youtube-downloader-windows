namespace YouTubeDownloader;

public enum QueueState { Pending, Preparing, Downloading, Completed, Failed, Cancelled }

public sealed class QueueEntry
{
    private readonly DownloadOptions request;
    public Guid Id { get; } = Guid.NewGuid();
    public string Url => request.Url;
    public string Title { get; internal set; }
    public QueueState State { get; internal set; }
    public double Percent { get; internal set; }
    public int Attempts { get; internal set; }
    public string Error { get; internal set; }
    public DownloadResult Result { get; internal set; }
    internal long Generation { get; set; }
    public DownloadOptions Options => request.Snapshot();
    internal QueueEntry(DownloadOptions options, string title)
        => (request, Title) = (options.Snapshot(), title ?? options.Url);
}

// Session-only state. A single dispatcher owns each job, including all retries.
public sealed class DownloadQueue
{
    public const int Capacity = 100;
    private readonly object gate = new();
    private readonly List<QueueEntry> entries = new();
    private CancellationTokenSource lifetime;
    public event Action Changed;
    public bool IsRunning { get { lock (gate) return lifetime != null; } }
    public IReadOnlyList<QueueEntry> Entries { get { lock (gate) return entries.ToArray(); } }

    public QueueEntry Add(DownloadOptions options, string title = null)
    {
        if (options == null || string.IsNullOrWhiteSpace(options.Url)) throw new ArgumentException("Choose a media link.");
        QueueEntry entry;
        lock (gate)
        {
            if (entries.Count >= Capacity) throw new InvalidOperationException("The queue can hold at most 100 downloads. Remove finished items first.");
            if (entries.Any(e => e.State is QueueState.Pending or QueueState.Preparing or QueueState.Downloading && e.Url == options.Url && e.Options.Folder == options.Folder && OutputProfiles.Key(e.Options) == OutputProfiles.Key(options)))
                throw new InvalidOperationException("This download is already in the queue.");
            entry = new(options, title);
            entries.Add(entry);
        }
        Changed?.Invoke();
        return entry;
    }
    public bool Remove(Guid id)
    {
        bool removed;
        lock (gate) removed = entries.RemoveAll(e => e.Id == id && e.State is not (QueueState.Preparing or QueueState.Downloading)) > 0;
        if (removed) Changed?.Invoke();
        return removed;
    }
    public bool Retry(Guid id)
    {
        lock (gate)
        {
            var entry = entries.FirstOrDefault(e => e.Id == id);
            if (entry == null || entry.State is not (QueueState.Failed or QueueState.Cancelled)) return false;
            entry.State = QueueState.Pending;
            entry.Error = null;
            entry.Percent = 0;
            entry.Attempts = 0;
            entry.Result = null;
            entry.Generation++;
        }
        Changed?.Invoke();
        return true;
    }
    public void Stop() { lock (gate) lifetime?.Cancel(); }

    public async Task RunAsync(Func<DownloadOptions, IProgress<DownloadProgress>, CancellationToken, Task<DownloadResult>> execute,
        CancellationToken token = default, Func<TimeSpan, CancellationToken, Task> wait = null)
    {
        CancellationTokenSource run;
        lock (gate)
        {
            if (lifetime != null) throw new InvalidOperationException("The queue is already running.");
            run = lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
        }
        wait ??= Task.Delay;
        Changed?.Invoke();
        try
        {
            while (!run.IsCancellationRequested)
            {
                QueueEntry entry;
                lock (gate)
                {
                    entry = entries.FirstOrDefault(e => e.State == QueueState.Pending);
                    if (entry == null) break;
                    entry.State = QueueState.Preparing;
                }
                Changed?.Invoke();
                bool halt = false;
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    try
                    {
                        long generation;
                        lock (gate) generation = ++entry.Generation;
                        entry.Attempts++;
                        var progress = new QueueProgress(p =>
                        {
                            lock (gate)
                            {
                                if (!ReferenceEquals(lifetime, run) || entry.Generation != generation || run.IsCancellationRequested || entry.State is QueueState.Cancelled or QueueState.Completed or QueueState.Failed) return;
                                entry.State = p.Stage is "Preparing media tools" or "Checking video" or "Checking playlist" ? QueueState.Preparing : QueueState.Downloading;
                                entry.Percent = Math.Clamp(p.Percent, 0, 100);
                                if (!string.IsNullOrWhiteSpace(p.ItemTitle)) entry.Title = p.ItemTitle;
                            }
                            Changed?.Invoke();
                        });
                        var result = await execute(entry.Options, progress, run.Token).ConfigureAwait(false);
                        entry.Result = result;
                        if (result.Cancelled) throw new OperationCanceledException();
                        halt = result.StopQueue;
                        if (result.Failed > 0 || result.Remaining > 0 || result.StopReason != null)
                            throw new InvalidOperationException(result.StopReason ?? "Some playlist items failed. Retry to resume completed files.");
                        entry.State = QueueState.Completed;
                        entry.Percent = 100;
                        entry.Error = null;
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        entry.State = QueueState.Cancelled;
                        entry.Error = null;
                        halt = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        string error = ex.Message ?? "Download failed.";
                        if (attempt == 0 && !halt && ex is not MediaToolRecoveryException && !DownloadErrors.IsBlocked(error) && DownloadErrors.IsTransient(error))
                        {
                            lock (gate) entry.Generation++;
                            entry.Error = "Connection interrupted. Retrying…";
                            Changed?.Invoke();
                            try { await wait(TimeSpan.FromSeconds(3), run.Token).ConfigureAwait(false); }
                            catch (OperationCanceledException) { entry.State = QueueState.Cancelled; entry.Error = null; halt = true; break; }
                            continue;
                        }
                        entry.Error = error.Length > 512 ? error[..512] : error;
                        entry.State = QueueState.Failed;
                        halt = halt || ex is UnauthorizedAccessException or MediaToolRecoveryException || DownloadErrors.IsBlocked(error) || DownloadErrors.IsTransient(error);
                        break;
                    }
                }
                Changed?.Invoke();
                if (halt) break;
                // Pace separate jobs as well as items within a playlist.
                if (Entries.Any(e => e.State == QueueState.Pending))
                    await wait(TimeSpan.FromSeconds(2), run.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            lock (gate) { if (ReferenceEquals(lifetime, run)) lifetime = null; }
            run.Dispose();
            Changed?.Invoke();
        }
    }
    private sealed class QueueProgress(Action<DownloadProgress> report) : IProgress<DownloadProgress>
    { public void Report(DownloadProgress value) => report(value); }
}
