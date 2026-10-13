using System.Windows;
using System.Windows.Media;

namespace YouTubeDownloader;
internal static class ThemeResources
{
    private static readonly string[] Keys =
    {
        "Page",
        "Surface",
        "Field",
        "Ink",
        "Muted",
        "Accent",
        "OnAccent",
        "Border",
        "Hover",
        "Selected",
        "Error",
        "AudioAccent",
        "AudioSelected",
        "InfoAccent",
        "Divider"
    };
    public static void Apply(bool dark)
    {
        string[] values = dark ? new[]
        {
            "#17191E",
            "#20242A",
            "#22262F",
            "#F3F1ED",
            "#B8B7B3",
            "#7499FF",
            "#0E1F46",
            "#67717E",
            "#2C313B",
            "#263653",
            "#FFABA7",
            "#F6B585",
            "#362C27",
            "#AFC4FF",
            "#373E4B"
        }

        : new[]
        {
            "#F6F5F1",
            "#FFFFFF",
            "#FFFFFF",
            "#1D2329",
            "#56616C",
            "#275BD8",
            "#FFFFFF",
            "#89909B",
            "#EBEBE7",
            "#EAF0FF",
            "#AF2527",
            "#7B3F12",
            "#FFF0E4",
            "#275BD8",
            "#CED1D7"
        };
        for (int i = 0; i < Keys.Length; i++)
        {
            Brush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(values[i]));
            if (SystemParameters.HighContrast)
                brush = Keys[i] switch
                {
                    "Ink" or "Muted" or "Error" => SystemColors.ControlTextBrush,
                    "Accent" or "Border" or "AudioAccent" => SystemColors.HighlightBrush,
                    "OnAccent" => SystemColors.HighlightTextBrush,
                    _ => SystemColors.WindowBrush
                };
            Application.Current.Resources[Keys[i]] = brush;
        }
    }
}
