using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

internal static partial class Program
{
    private static void CapturePopup(FrameworkElement client, HwndSource popup, string path)
    {
        GetWindowRect(popup.Handle, out var bounds);
        var origin = client.PointToScreen(new Point());
        var dpi = VisualTreeHelper.GetDpi(client);
        var background = new RenderTargetBitmap((int)Math.Ceiling(client.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(client.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        background.Render(Window.GetWindow(client));
        var overlay = new RenderTargetBitmap(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top,
            dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        overlay.Render(popup.RootVisual);
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawImage(background, new Rect(client.RenderSize));
            drawing.DrawImage(overlay,
                new Rect((bounds.Left - origin.X) / dpi.DpiScaleX, (bounds.Top - origin.Y) / dpi.DpiScaleY,
                    (bounds.Right - bounds.Left) / dpi.DpiScaleX, (bounds.Bottom - bounds.Top) / dpi.DpiScaleY));
        }
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(client.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(client.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = System.IO.File.Create(path);
        encoder.Save(stream);
    }
}
