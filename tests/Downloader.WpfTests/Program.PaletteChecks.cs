using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckPalette(Window window, MainViewModel model, string captures)
    {
        static Color Role(string name) => ((SolidColorBrush)Application.Current.Resources[name]).Color;
        static double Luminance(Color color)
        {
            static double Linear(byte channel)
            {
                double value = channel / 255d;
                return value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4);
            }
            return .2126 * Linear(color.R) + .7152 * Linear(color.G) + .0722 * Linear(color.B);
        }
        static double Contrast(Color first, Color second)
        {
            double a = Luminance(first), b = Luminance(second);
            return (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
        }
        var video = (RadioButton)WpfTree.Find(window, "VideoChoice");
        var audio = (RadioButton)WpfTree.Find(window, "AudioChoice");
        model.SettingsVisible = false;
        model.Url = "https://youtu.be/jNQXAC9IVRw";
        await Task.Delay(850);
        var badgeColor = ((SolidColorBrush)((Border)WpfTree.Find(window, "DurationBadge")).Background).Color;
        double alpha = badgeColor.A / 255d;
        var lightestBadge = Color.FromRgb((byte)(badgeColor.R * alpha + 255 * (1 - alpha)),
            (byte)(badgeColor.G * alpha + 255 * (1 - alpha)), (byte)(badgeColor.B * alpha + 255 * (1 - alpha)));
        Check(Contrast(Colors.White, lightestBadge) >= 4.5,
            "The duration label stays readable even over the brightest possible thumbnail");
        foreach (bool dark in new[] { false, true })
        {
            if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            foreach (string foreground in new[] { "Ink", "Muted", "Accent", "Error", "Success", "InfoAccent" })
                foreach (string background in new[] { "Page", "Field", "Surface", "Hover", "Selected" })
                    Check(Contrast(Role(foreground), Role(background)) >= 4.5,
                        "Readable " + foreground + " on " + background + " in " + (dark ? "dark" : "light") + " theme");
            Check(Contrast(Role("OnAccent"), Role("Action")) >= 4.5 && Contrast(Role("Action"), Role("Page")) >= 3,
                "The primary action and its label remain distinct and readable");
            var action = Role("Action"); var label = Role("OnAccent");
            var hoveredAction = Color.FromRgb((byte)(action.R * .92 + label.R * .08),
                (byte)(action.G * .92 + label.G * .08), (byte)(action.B * .92 + label.B * .08));
            Check(Contrast(label, hoveredAction) >= 4.5,
                "The primary action label remains readable through its hover wash");
            foreach (string background in new[] { "Page", "Field", "Surface", "Hover" })
                Check(Contrast(Role("Border"), Role(background)) >= 3, "Control boundaries remain visible on " + background);
            foreach (bool arabic in new[] { false, true })
                foreach (double width in new[] { 1160d, 820d })
                {
                    model.SelectLanguageCommand.Execute(arabic ? "ar" : "en");
                    window.Width = width;
                    foreach (bool useAudio in new[] { false, true })
                    {
                        model.Audio = useAudio;
                        await Task.Delay(180);
                        window.UpdateLayout();
                        var selected = useAudio ? audio : video;
                        var face = (Border)selected.Template.FindName("Face", selected);
                        Check(selected.IsChecked == true && selected.FontWeight == FontWeights.SemiBold &&
                            Equals(face.Background, Application.Current.Resources["Selected"]) &&
                            Equals(selected.Foreground, Application.Current.Resources["Accent"]),
                            "Video and audio share the readable selection treatment without losing checked state");
                        string variant = (arabic ? "ar" : "en") + (dark ? "-dark" : "-light") +
                            (useAudio ? "-audio" : "-video") + (width < 1000 ? "-compact" : "-wide");
                        ((ScrollViewer)WpfTree.Find(window, "Viewport")).ScrollToTop();
                        window.UpdateLayout();
                        Capture(window, System.IO.Path.Combine(captures, "palette-" + variant + ".png"));
                    }
                }
        }
        model.Url = "";
        model.Audio = false;
        model.SelectLanguageCommand.Execute("en");
        window.Width = 1160;
    }
}
