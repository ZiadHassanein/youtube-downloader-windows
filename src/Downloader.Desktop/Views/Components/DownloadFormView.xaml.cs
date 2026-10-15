using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace YouTubeDownloader;

public partial class DownloadFormView : UserControl
{
    public DownloadFormView()
    {
        InitializeComponent();
        AudioFormatMenu.PlacementTarget = AudioChoice;
        AudioFormatMenu.CustomPopupPlacementCallback = PlaceAudioMenu;
        Unloaded += (_, _) => AudioFormatMenu.IsOpen = false;
    }

    private void AudioClicked(object sender, RoutedEventArgs args) => OpenAudioMenu();

    private void AudioKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key is not (Key.Down or Key.Up)) return;
        OpenAudioMenu();
        args.Handled = true;
    }

    private void AudioMenuOpening(object sender, ContextMenuEventArgs args) => PrepareAudioMenu();

    private void OpenAudioMenu()
    {
        if (!PrepareAudioMenu()) return;
        AudioFormatMenu.Placement = PlacementMode.Custom;
        AudioFormatMenu.IsOpen = true;
    }

    private bool PrepareAudioMenu()
    {
        if (DataContext is not MainViewModel { IsIdle: true } model) return false;
        model.Audio = true;
        // A reused native popup can retain its previous language while it is detached.
        AudioFormatMenu.SetCurrentValue(FlowDirectionProperty, model.ContentDirection);
        AudioFormatMenu.SetCurrentValue(FontFamilyProperty, model.ContentFont);
        AudioFormatMenu.Items.Clear();
        foreach (var choice in model.Encodings)
        {
            var item = new MenuItem
            {
                Header = choice.Label, Tag = choice, IsCheckable = true, IsChecked = choice == model.SelectedEncoding,
                Style = (Style)FindResource("SelectionMenuItem")
            };
            item.Click += EncodingChosen;
            AudioFormatMenu.Items.Add(item);
        }
        return true;
    }

    private void EncodingChosen(object sender, RoutedEventArgs args)
    {
        if (DataContext is MainViewModel { IsIdle: true } model && sender is MenuItem { Tag: EncodingChoice choice })
            model.SelectedEncoding = choice;
        AudioFormatMenu.IsOpen = false;
    }

    private CustomPopupPlacement[] PlaceAudioMenu(Size popup, Size target, Point offset)
    {
        var client = (FrameworkElement)Window.GetWindow(this).Content;
        var origin = AudioChoice.PointToScreen(new Point());
        var anchor = new Rect(origin, AudioChoice.PointToScreen(new Point(AudioChoice.ActualWidth, AudioChoice.ActualHeight)));
        var frame = new Rect(client.PointToScreen(new Point()), client.PointToScreen(new Point(client.ActualWidth, client.ActualHeight)));
        var dpi = VisualTreeHelper.GetDpi(client);
        double insetX = 8 * dpi.DpiScaleX, insetY = 8 * dpi.DpiScaleY, gap = 6 * dpi.DpiScaleY;
        double x = Math.Clamp(anchor.Left + (anchor.Width - popup.Width) / 2, frame.Left + insetX,
            Math.Max(frame.Left + insetX, frame.Right - popup.Width - insetX));
        double y = anchor.Bottom + gap + popup.Height <= frame.Bottom - insetY ? anchor.Bottom + gap : anchor.Top - popup.Height - gap;
        y = Math.Clamp(y, frame.Top + insetY, Math.Max(frame.Top + insetY, frame.Bottom - popup.Height - insetY));
        // WPF custom placement uses screen pixels, and the target origin is its right edge in RTL.
        return new[] { new CustomPopupPlacement(new Point(x - origin.X, y - origin.Y), PopupPrimaryAxis.None) };
    }
}
