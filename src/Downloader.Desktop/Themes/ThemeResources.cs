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
        ["Page"] = ("#F5F7F6", "#171C20"),
        ["Surface"] = ("#E9F0ED", "#1E262B"),
        ["Field"] = ("#FFFFFF", "#252D33"),
        ["Ink"] = ("#1D2A28", "#F0F5F3"),
        ["Muted"] = ("#536660", "#AABAB4"),
        ["Accent"] = ("#096B5B", "#62D8BC"),
        ["Action"] = ("#0C7665", "#55D3B4"),
        ["OnAccent"] = ("#FFFFFF", "#092E25"),
        ["Border"] = ("#778C84", "#748D86"),
        ["Hover"] = ("#EDF4F1", "#2D3C3D"),
        ["Selected"] = ("#DDF2EB", "#203F39"),
        ["Error"] = ("#B33146", "#FF9EAD"),
        ["AudioAccent"] = ("#965A0E", "#F3BE6B"),
        ["AudioSelected"] = ("#FFF0D6", "#403322"),
        ["InfoAccent"] = ("#3B6593", "#9FC4ED"),
        ["Divider"] = ("#D4DFDA", "#3B4A48"),
        ["Success"] = ("#1C754E", "#7CD7A5")
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
