using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace YouTubeDownloader;

public partial class AppHeaderView : UserControl
{
    public AppHeaderView() => InitializeComponent();

    private void LanguageClicked(object sender, RoutedEventArgs args) => OpenLanguageMenu();

    private void LanguageKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key != Key.Down) return;
        OpenLanguageMenu();
        args.Handled = true;
    }

    private void OpenLanguageMenu()
    {
        if (!LanguageButton.IsEnabled) return;
        LanguageButton.ContextMenu.PlacementTarget = LanguageButton;
        LanguageButton.ContextMenu.IsOpen = true;
    }
}
