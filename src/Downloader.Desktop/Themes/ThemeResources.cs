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
                _ = DwmSetWindowAttribute(handle, 19, ref enabled, 4);
        }
        catch (DllNotFoundException) { }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    private static readonly Dictionary<string, (string Light, string Dark)> Palette = new()
    {
        ["Page"] = ("#F8F7F8", "#181B20"),
        ["Surface"] = ("#F1EDF0", "#20242A"),
        ["Field"] = ("#FFFFFF", "#282B32"),
        ["Ink"] = ("#272329", "#F6F2F4"),
        ["Muted"] = ("#655963", "#BEB2BB"),
        ["Accent"] = ("#A91F33", "#FF8393"),
        ["Action"] = ("#BA263A", "#CB2D42"),
        ["OnAccent"] = ("#FFFFFF", "#FFFFFF"),
        ["Border"] = ("#8E7D87", "#94828D"),
        ["Hover"] = ("#F3E9ED", "#353039"),
        ["Selected"] = ("#F8E3E9", "#3E2933"),
        ["Error"] = ("#A12232", "#FFB0B7"),
        ["InfoAccent"] = ("#355F8D", "#AACAF1"),
        ["Divider"] = ("#DFD7DC", "#48414A"),
        ["Success"] = ("#24643E", "#85D1A2")
    };

    public static void Apply(bool dark, bool animate = false)
    {
        if (animate && Application.Current.MainWindow is MainWindow { IsVisible: true } window)
        {
            window.ThemeTransition.Run((FrameworkElement)window.Content, window.HeaderContent.ThemeButton, () => Apply(dark));
            return;
        }
        foreach (var (key, colors) in Palette)
        {
            Brush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? colors.Dark : colors.Light));
            if (SystemParameters.HighContrast)
                brush = key switch
                {
                    "Ink" or "Muted" or "Error" or "Success" => SystemColors.WindowTextBrush,
                    "Accent" or "Action" or "Border" => SystemColors.HighlightBrush,
                    "OnAccent" => SystemColors.HighlightTextBrush,
                    _ => SystemColors.WindowBrush
                };
            if (brush.CanFreeze) brush.Freeze();
            Application.Current.Resources[key] = brush;
        }
    }
}
