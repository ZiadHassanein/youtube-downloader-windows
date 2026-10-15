using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;

namespace YouTubeDownloader;

internal static class MotionPreferences
{
    private static bool? testOverride;
    internal static bool Enabled => testOverride ?? (SystemParameters.ClientAreaAnimation && !SystemParameters.HighContrast);
    internal static event Action Changed;

    static MotionPreferences() => SystemParameters.StaticPropertyChanged += SystemChanged;

    private static void SystemChanged(object sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(SystemParameters.ClientAreaAnimation) or nameof(SystemParameters.HighContrast))
            Changed?.Invoke();
    }

    internal static IEasingFunction EaseOut => new CubicEase { EasingMode = EasingMode.EaseOut };

    internal static DoubleAnimation To(double target, double milliseconds) => new(target, TimeSpan.FromMilliseconds(milliseconds))
    {
        EasingFunction = EaseOut, FillBehavior = FillBehavior.Stop
    };

    internal static void SetTestOverride(bool? enabled)
    {
        testOverride = enabled;
        Changed?.Invoke();
    }
}
