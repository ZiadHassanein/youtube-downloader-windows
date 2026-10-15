using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace YouTubeDownloader;

internal static class ThemeResources
{
    internal static void ApplyTitleBar(Window window, bool dark)
    {
        int enabled = dark && !SystemParameters.HighContrast ? 1 : 0;
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;
        try
        {
            if (DwmSetWindowAttribute(handle, 20, ref enabled, 4) != 0)
                DwmSetWindowAttribute(handle, 19, ref enabled, 4);
        }
        catch (DllNotFoundException) { }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    private static readonly Dictionary<string, (string Light, string Dark)> Palette = new()
    {
        ["Page"] = ("#FFFFFF", "#111B2D"),
        ["Surface"] = ("#F1F5FC", "#192740"),
        ["Field"] = ("#FFFFFF", "#1C2B44"),
        ["Ink"] = ("#172B50", "#F1F5FF"),
        ["Muted"] = ("#526789", "#B2C3E1"),
        ["Accent"] = ("#2458E8", "#95B1FF"),
        ["Action"] = ("#2458E8", "#2458E8"),
        ["OnAccent"] = ("#FFFFFF", "#FFFFFF"),
        ["Border"] = ("#A5B3CA", "#6C7F9F"),
        ["Hover"] = ("#EDF2FC", "#263B5D"),
        ["Selected"] = ("#E7EEFF", "#29436D"),
        ["Error"] = ("#AE2639", "#FFADBA"),
        ["AudioAccent"] = ("#2458E8", "#95B1FF"),
        ["AudioSelected"] = ("#E7EEFF", "#29436D"),
        ["InfoAccent"] = ("#2458E8", "#95B1FF"),
        ["Divider"] = ("#DFE6F2", "#334766"),
        ["Success"] = ("#14763D", "#88E4AA")
    };

    public static void Apply(bool dark)
    {
        foreach (var (key, colors) in Palette)
        {
            Brush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? colors.Dark : colors.Light));
            if (SystemParameters.HighContrast)
                brush = key switch
                {
                    "Ink" or "Muted" or "Error" or "Success" => SystemColors.WindowTextBrush,
                    "Accent" or "Action" or "Border" or "AudioAccent" => SystemColors.HighlightBrush,
                    "OnAccent" => SystemColors.HighlightTextBrush,
                    _ => SystemColors.WindowBrush
                };
            if (brush.CanFreeze) brush.Freeze();
            Application.Current.Resources[key] = brush;
        }
    }
}
