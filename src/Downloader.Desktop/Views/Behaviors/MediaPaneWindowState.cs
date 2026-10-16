using System.Windows;

namespace YouTubeDownloader;

internal sealed class MediaPaneWindowState
{
    internal const double MinimumPaneWidth = 320;
    private const double PreferredPaneWidth = 420;
    private readonly Window window;
    private Rect? restore;
    private bool shown, applying;
    private double expectedWidth, expectedLeft;

    internal MediaPaneWindowState(Window window) => this.window = window;
    internal double? FormWindowWidth => restore?.Width;

    internal void ObserveBounds()
    {
        if (applying || restore == null || window.WindowState != WindowState.Normal) return;
        if (Math.Abs(window.Width - expectedWidth) > 1)
            restore = null; // An explicit user resize takes precedence over automatic restoration.
        else if (Math.Abs(window.Left - expectedLeft) > 1)
        {
            var frame = restore.Value;
            frame.X = window.Left;
            restore = frame;
            expectedLeft = window.Left;
        }
    }

    internal void Update(bool visible, Rect work)
    {
        if (visible == shown) return;
        shown = visible;
        if (visible)
        {
            if (window.WindowState != WindowState.Normal || window.Width >= 1000 || work.Width < window.MinWidth + MinimumPaneWidth + 16) return;
            restore = new Rect(window.Left, window.Top, window.Width, window.Height);
            double width = Math.Min(window.Width + PreferredPaneWidth, work.Width - 16);
            expectedWidth = width;
            expectedLeft = Math.Clamp(window.Left, work.Left + 8, work.Right - width - 8);
            applying = true;
            try { window.Left = expectedLeft; window.Width = expectedWidth; }
            finally { applying = false; }
        }
        else if (restore is { } frame)
        {
            restore = null;
            if (window.WindowState != WindowState.Normal) return;
            window.Width = Math.Clamp(frame.Width, window.MinWidth, Math.Max(window.MinWidth, work.Width - 16));
            window.Left = Math.Clamp(frame.Left, work.Left + 8, Math.Max(work.Left + 8, work.Right - window.Width - 8));
        }
    }
}
