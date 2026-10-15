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

    private CustomPopupPlacement[] PlaceAudioMenu(Size popup, Size target, Point offset) =>
        ContextMenuPlacement.CenterWithinClient(AudioChoice, popup);
}
