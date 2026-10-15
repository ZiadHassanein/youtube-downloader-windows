using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace YouTubeDownloader;

public sealed class ThemeTransitionView : Canvas
{
    private readonly Image snapshot = new() { Stretch = Stretch.Fill, IsHitTestVisible = false };
    private EllipseGeometry reveal;
    private int generation;
    internal bool IsTransitioning => snapshot.Source != null;

    public ThemeTransitionView()
    {
        IsHitTestVisible = false;
        Visibility = Visibility.Collapsed;
        Children.Add(snapshot);
        Loaded += (_, _) => MotionPreferences.Changed += Reset;
        Unloaded += (_, _) => { MotionPreferences.Changed -= Reset; Reset(); };
        SizeChanged += (_, args) =>
        {
            if (snapshot.Source != null && (Math.Abs(args.NewSize.Width - snapshot.Width) > 1 || Math.Abs(args.NewSize.Height - snapshot.Height) > 1))
                Reset();
        };
        IsVisibleChanged += (_, _) => { if (!IsVisible) Reset(); };
    }

    internal void Run(FrameworkElement surface, FrameworkElement origin, Action change)
    {
        Reset();
        if (!MotionPreferences.Enabled || !surface.IsVisible || surface.ActualWidth < 1 || surface.ActualHeight < 1)
        {
            change();
            return;
        }
        var dpi = VisualTreeHelper.GetDpi(surface);
        int width = (int)Math.Ceiling(surface.ActualWidth * dpi.DpiScaleX);
        int height = (int)Math.Ceiling(surface.ActualHeight * dpi.DpiScaleY);
        if ((long)width * height > 16_000_000)
        {
            change();
            return;
        }
        RenderTargetBitmap frame;
        try
        {
            frame = new RenderTargetBitmap(width, height, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            frame.Render(surface);
            frame.Freeze();
        }
        catch (InvalidOperationException)
        {
            change();
            return;
        }
        change();
        snapshot.Width = surface.ActualWidth;
        snapshot.Height = surface.ActualHeight;
        snapshot.Source = frame;
        var center = origin.TranslatePoint(new Point(origin.ActualWidth / 2, origin.ActualHeight / 2), surface);
        double radius = Math.Sqrt(Math.Pow(Math.Max(center.X, surface.ActualWidth - center.X), 2) +
                                  Math.Pow(Math.Max(center.Y, surface.ActualHeight - center.Y), 2));
        reveal = new EllipseGeometry(center, radius, radius);
        snapshot.Clip = new CombinedGeometry(GeometryCombineMode.Exclude,
            new RectangleGeometry(new Rect(0, 0, surface.ActualWidth, surface.ActualHeight)), reveal);
        Visibility = Visibility.Visible;
        int current = generation;
        var motion = new DoubleAnimation(0, radius, TimeSpan.FromMilliseconds(260))
            { EasingFunction = MotionPreferences.EaseOut, FillBehavior = FillBehavior.Stop };
        motion.Completed += (_, _) => { if (current == generation) Reset(); };
        reveal.BeginAnimation(EllipseGeometry.RadiusXProperty, motion);
        reveal.BeginAnimation(EllipseGeometry.RadiusYProperty, motion);
    }

    internal void Reset()
    {
        generation++;
        reveal?.BeginAnimation(EllipseGeometry.RadiusXProperty, null);
        reveal?.BeginAnimation(EllipseGeometry.RadiusYProperty, null);
        reveal = null;
        snapshot.Source = null;
        snapshot.Clip = null;
        Visibility = Visibility.Collapsed;
    }
}
