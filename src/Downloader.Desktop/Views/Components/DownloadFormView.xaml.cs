using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

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
        double top = AudioChoice.TranslatePoint(new Point(), client).Y;
        double y = top + target.Height + 6 + popup.Height <= client.ActualHeight - 8 ? target.Height + 6 : -popup.Height - 6;
        y = Math.Clamp(y, 8 - top, Math.Max(8 - top, client.ActualHeight - top - popup.Height - 8));
        return new[] { new CustomPopupPlacement(new Point((target.Width - popup.Width) / 2, y), PopupPrimaryAxis.None) };
    }
}
