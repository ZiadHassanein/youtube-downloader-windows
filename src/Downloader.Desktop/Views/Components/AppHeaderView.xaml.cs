using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace YouTubeDownloader;

public partial class AppHeaderView : UserControl
{
    public AppHeaderView()
    {
        InitializeComponent();
        var menu = LanguageButton.ContextMenu;
        menu.PlacementTarget = LanguageButton;
        menu.CustomPopupPlacementCallback = (popup, _, _) => ContextMenuPlacement.CenterWithinClient(LanguageButton, popup);
        Unloaded += (_, _) => menu.IsOpen = false;
    }

    private void LanguageClicked(object sender, RoutedEventArgs args) => OpenLanguageMenu();

    private void LanguageKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key != Key.Down) return;
        OpenLanguageMenu();
        args.Handled = true;
    }

    private void OpenLanguageMenu()
    {
        if (PrepareLanguageMenu()) LanguageButton.ContextMenu.IsOpen = true;
    }

    private void LanguageMenuOpening(object sender, ContextMenuEventArgs args) => args.Handled = !PrepareLanguageMenu();

    private bool PrepareLanguageMenu()
    {
        if (!LanguageButton.IsEnabled || DataContext is not MainViewModel model) return false;
        var menu = LanguageButton.ContextMenu;
        // Detached native menus can retain presentation and selection from their previous opening.
        menu.SetCurrentValue(FlowDirectionProperty, model.ContentDirection);
        menu.SetCurrentValue(FontFamilyProperty, model.ContentFont);
        EnglishChoice.SetCurrentValue(MenuItem.IsCheckedProperty, model.IsEnglish);
        ArabicChoice.SetCurrentValue(MenuItem.IsCheckedProperty, model.IsArabic);
        menu.Placement = PlacementMode.Custom;
        return true;
    }
}
