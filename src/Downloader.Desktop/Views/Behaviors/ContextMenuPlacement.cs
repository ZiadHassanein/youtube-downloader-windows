using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace YouTubeDownloader;

internal static class ContextMenuPlacement
{
    public static CustomPopupPlacement[] CenterWithinClient(FrameworkElement target, Size popup)
    {
        var client = (FrameworkElement)Window.GetWindow(target).Content;
        var origin = target.PointToScreen(new Point());
        var anchor = new Rect(origin, target.PointToScreen(new Point(target.ActualWidth, target.ActualHeight)));
        var frame = new Rect(client.PointToScreen(new Point()), client.PointToScreen(new Point(client.ActualWidth, client.ActualHeight)));
        var dpi = VisualTreeHelper.GetDpi(client);
        double insetX = 8 * dpi.DpiScaleX, insetY = 8 * dpi.DpiScaleY, gap = 6 * dpi.DpiScaleY;
        double x = Math.Clamp(anchor.Left + (anchor.Width - popup.Width) / 2, frame.Left + insetX,
            Math.Max(frame.Left + insetX, frame.Right - popup.Width - insetX));
        double y = anchor.Bottom + gap + popup.Height <= frame.Bottom - insetY ? anchor.Bottom + gap : anchor.Top - popup.Height - gap;
        y = Math.Clamp(y, frame.Top + insetY, Math.Max(frame.Top + insetY, frame.Bottom - popup.Height - insetY));
        // WPF custom placement uses screen pixels; RTL targets have a physical-right origin.
        return new[] { new CustomPopupPlacement(new Point(x - origin.X, y - origin.Y), PopupPrimaryAxis.None) };
    }
}
