using System.Globalization;
using System.Text.RegularExpressions;

namespace YouTubeDownloader;

// Runs inside the provider's existing operation lease. No background task can replace tools mid-download.
internal sealed class AutomaticToolUpdates(string cache, IToolUpdateClient client = null)
{
    private readonly IToolUpdateClient updates = client ?? new ToolUpdateClient(cache);
    private ToolSet previous;
    internal Func<DateTimeOffset> UtcNow { get; set; } = () => DateTimeOffset.UtcNow;
    internal TimeSpan UpdateTimeout { get; set; } = TimeSpan.FromMinutes(3);
    internal Func<CancellationToken, ToolSet> FindBaseline { get; set; }

    internal async Task<ToolSet> CheckDailyAsync(ToolSet current, IProgress<DownloadProgress> progress, CancellationToken token)
    {
        try
        {
            Directory.CreateDirectory(cache);
            using var lease = new FileStream(Path.Combine(cache, "daily-check.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            string marker = Path.Combine(cache, "last-update-check.txt");
            if (File.Exists(marker) && new FileInfo(marker).Length < 128 &&
                DateTimeOffset.TryParse(File.ReadAllText(marker), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var last) &&
                last <= UtcNow() && UtcNow() - last < TimeSpan.FromDays(1)) return current;
            token.ThrowIfCancellationRequested();
            // Persist attempts as well as successes, so offline/rate-limited launches do not poll repeatedly.
            File.WriteAllText(marker, UtcNow().ToString("O", CultureInfo.InvariantCulture));
            var result = await RefreshAsync(current, progress, token).ConfigureAwait(false);
            return result.Tools;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex) when (IsMaintenanceFailure(ex)) { return current; }
    }

    internal async Task<(ToolSet Tools, MediaToolUpdateResult Result)> RefreshAsync(ToolSet current, IProgress<DownloadProgress> progress, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(UpdateTimeout);
        (ToolSet Tools, MediaToolUpdateResult Result) result;
        try { result = await updates.RefreshAsync(current, progress, deadline.Token).ConfigureAwait(false); }
        catch (OperationCanceledException ex) when (!token.IsCancellationRequested) { throw new TimeoutException("The media-tool update timed out.", ex); }
        if (result.Result.Updated) previous = current;
        return result;
    }

    internal async Task<T> RunAsync<T>(Func<T> action, Func<ToolSet> current, Action<ToolSet> activate, IProgress<DownloadProgress> progress, CancellationToken token)
    {
        try { return action(); }
        catch (MediaToolExecutionException original) when (IsRecoverable(original.Message))
        {
            token.ThrowIfCancellationRequested();
            progress?.Report(new() { Stage = "Preparing media tools", Detail = "Checking for a downloader fix…", Indeterminate = true });
            bool changed;
            try
            {
                var refreshed = await RefreshAsync(current(), progress, token).ConfigureAwait(false);
                activate(refreshed.Tools);
                changed = refreshed.Result.Updated;
                // A daily/manual update may have introduced a regression. Try the retained working set once.
                var fallback = previous ?? FindBaseline?.Invoke(token);
                if (!changed && fallback != null)
                {
                    var before = current();
                    var restored = await updates.RollbackAsync(before, fallback, token).ConfigureAwait(false);
                    changed = restored.YtDlp != before.YtDlp || restored.Deno != before.Deno;
                    activate(restored);
                    previous = null;
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception ex) when (IsMaintenanceFailure(ex))
            {
                throw new MediaToolRecoveryException(false, original);
            }
            if (!changed) throw new MediaToolRecoveryException(true, original);
            token.ThrowIfCancellationRequested();
            try { return action(); }
            catch (MediaToolExecutionException retry) when (IsRecoverable(retry.Message))
            {
                if (previous != null)
                {
                    try { activate(await updates.RollbackAsync(current(), previous, token).ConfigureAwait(false)); }
                    catch (Exception ex) when (IsMaintenanceFailure(ex) && !token.IsCancellationRequested) { activate(previous); }
                    previous = null;
                }
                throw new MediaToolRecoveryException(true, retry);
            }
        }
    }

    internal static bool IsRecoverable(string message)
    {
        if (DownloadErrors.IsBlocked(message) || DownloadErrors.IsTransient(message)) return false;
        string text = (message ?? "").ToLowerInvariant();
        if (new[] { "private", "removed", "unavailable", "unsupported url", "requested format", "drm", "age-restricted", "geo-restricted" }.Any(text.Contains)) return false;
        return new[] { "signature extraction failed", "nsig extraction failed", "signature solving failed", "challenge solving failed", "no supported javascript runtime", "javascript runtime is too old" }.Any(text.Contains) ||
            Regex.IsMatch(text, @"unable to extract .*(?:signature|player|initial data|n function)");
    }

    private static bool IsMaintenanceFailure(Exception error) => error is IOException or UnauthorizedAccessException or HttpRequestException or
        OperationCanceledException or TimeoutException or System.ComponentModel.Win32Exception or System.Text.Json.JsonException or InvalidOperationException or FormatException or KeyNotFoundException;
}

internal sealed class MediaToolExecutionException(string message) : InvalidOperationException(message);
