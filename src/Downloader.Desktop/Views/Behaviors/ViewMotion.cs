using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace YouTubeDownloader;

internal static class ViewMotion
{
    internal static void Reveal(FrameworkElement view)
    {
        if (!MotionPreferences.Enabled) return;
        var offset = new TranslateTransform();
        view.RenderTransform = offset;
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var duration = TimeSpan.FromMilliseconds(180);
        view.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(.82, 1, duration)
            { EasingFunction = ease, FillBehavior = FillBehavior.Stop });
        offset.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(5, 0, duration)
            { EasingFunction = ease, FillBehavior = FillBehavior.Stop });
    }
}
