using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace YouTubeDownloader;

public partial class AppFooterView : UserControl
{
    public AppFooterView()
    {
        InitializeComponent();
        var menu = ContactButton.ContextMenu;
        menu.PlacementTarget = ContactButton;
        menu.CustomPopupPlacementCallback = (popup, target, _) =>
            new[] { new CustomPopupPlacement(new Point(target.Width - popup.Width, -popup.Height - 6), PopupPrimaryAxis.Vertical) };
    }

    private void ContactClicked(object sender, RoutedEventArgs args) => OpenContactMenu();

    private void ContactKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key is not (Key.Up or Key.Down)) return;
        OpenContactMenu();
        args.Handled = true;
    }

    private void OpenContactMenu()
    {
        // ContextMenuService changes the menu's base placement after native activation.
        ContactButton.ContextMenu.Placement = PlacementMode.Custom;
        ContactButton.ContextMenu.IsOpen = true;
    }
}
