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
        VideoFormatMenu.PlacementTarget = VideoChoice;
        AudioFormatMenu.CustomPopupPlacementCallback = (popup, _, _) => ContextMenuPlacement.CenterWithinClient(AudioChoice, popup);
        VideoFormatMenu.CustomPopupPlacementCallback = (popup, _, _) => ContextMenuPlacement.CenterWithinClient(VideoChoice, popup);
        Unloaded += (_, _) => { AudioFormatMenu.IsOpen = false; VideoFormatMenu.IsOpen = false; };
    }

    private void AudioClicked(object sender, RoutedEventArgs args) => OpenAudioMenu();

    private void AudioKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key is not (Key.Down or Key.Up)) return;
        OpenAudioMenu();
        args.Handled = true;
    }

    private void AudioMenuOpening(object sender, ContextMenuEventArgs args) => args.Handled = !PrepareAudioMenu();
    private void VideoMenuOpening(object sender, ContextMenuEventArgs args) => args.Handled = !PrepareVideoMenu();
    private void VideoClicked(object sender, RoutedEventArgs args) => OpenVideoMenu();
    private void VideoKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key is not (Key.Down or Key.Up)) return;
        OpenVideoMenu();
        args.Handled = true;
    }
    private void OpenVideoMenu()
    {
        if (PrepareVideoMenu()) VideoFormatMenu.IsOpen = true;
    }

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
        PrepareMenu(AudioFormatMenu, model, model.Encodings, model.SelectedEncoding, choice => model.SelectedEncoding = choice);
        return true;
    }

    private bool PrepareVideoMenu()
    {
        if (DataContext is not MainViewModel { IsIdle: true } model) return false;
        model.Audio = false;
        PrepareMenu(VideoFormatMenu, model, model.VideoFormats, model.SelectedVideoFormat, choice => model.SelectedVideoFormat = choice);
        return VideoFormatMenu.Items.Count > 0;
    }

    private void PrepareMenu<T>(ContextMenu menu, MainViewModel model, IEnumerable<T> choices, T selected, Action<T> choose)
    {
        // Native popups are detached and must refresh direction/font before reuse.
        menu.SetCurrentValue(FlowDirectionProperty, model.ContentDirection);
        menu.SetCurrentValue(FontFamilyProperty, model.ContentFont);
        menu.Items.Clear();
        foreach (var choice in choices)
        {
            var item = new MenuItem
            {
                Header = choice.ToString(), Tag = choice, IsCheckable = true, IsChecked = Equals(choice, selected),
                Style = (Style)FindResource("SelectionMenuItem")
            };
            item.Click += (_, _) => { if (model.IsIdle) choose(choice); menu.IsOpen = false; };
            menu.Items.Add(item);
        }
    }
}
