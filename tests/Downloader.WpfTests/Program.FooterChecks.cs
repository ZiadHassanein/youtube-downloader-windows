using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckFooter(Window window, MainViewModel model, string captures)
    {
        model.SettingsVisible = false;
        model.Url = "";
        var contact = (Button)WpfTree.Find(window, "ContactButton");
        var settings = (Button)WpfTree.Find(window, "SettingsButton");
        var footer = (FrameworkElement)WpfTree.Find(window, "FooterContent");
        var root = (FrameworkElement)WpfTree.Find(window, "Root");
        window.Activate();
        contact.Focus();
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(contact), Environment.TickCount, Key.Apps)
        { RoutedEvent = Keyboard.KeyDownEvent, Source = contact });
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(contact), Environment.TickCount, Key.Apps)
        { RoutedEvent = Keyboard.KeyUpEvent, Source = contact });
        await Task.Delay(150);
        var firstMenu = contact.ContextMenu;
        Check(firstMenu.IsOpen && ReferenceEquals(firstMenu.PlacementTarget, contact),
            "The first native ContextMenuService activation opens Contact without needing a prior button click");
        var firstTop = firstMenu.PointToScreen(new Point());
        var firstBottom = firstMenu.PointToScreen(new Point(firstMenu.ActualWidth, firstMenu.ActualHeight));
        var clientTop = root.PointToScreen(new Point());
        var clientBottom = root.PointToScreen(new Point(root.ActualWidth, root.ActualHeight));
        Check(firstTop.X >= clientTop.X - 1 && firstTop.Y >= clientTop.Y - 1 && firstBottom.X <= clientBottom.X + 1 &&
            firstBottom.Y <= contact.PointToScreen(new Point()).Y,
            "First-open native keyboard placement is above Contact and inside the app borders" +
            $" (top={firstTop}, bottom={firstBottom}, clientTop={clientTop}, clientBottom={clientBottom}, placement={firstMenu.Placement})");
        var firstPopup = (HwndSource)PresentationSource.FromVisual(firstMenu);
        GetClientRect(firstPopup.Handle, out var firstClient);
        CaptureVisual(firstPopup.RootVisual, System.IO.Path.Combine(captures, "contact-native-first-open.png"), firstClient.Right, firstClient.Bottom);
        firstMenu.IsOpen = false;
        await Task.Delay(100);
        foreach (bool arabic in new[] { false, true })
            foreach (bool dark in new[] { false, true })
                foreach (double width in new[] { 1160d, 820d })
                {
                    model.SelectLanguageCommand.Execute(arabic ? "ar" : "en");
                    if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                    window.Width = width;
                    await Task.Delay(150);
                    window.UpdateLayout();
                    var settingsBounds = settings.TransformToAncestor(root).TransformBounds(new Rect(settings.RenderSize));
                    var contactBounds = contact.TransformToAncestor(root).TransformBounds(new Rect(contact.RenderSize));
                    Check(WpfTree.Find(window, "AuthorCredit") == null && settingsBounds.Left < 40 && contactBounds.Right > root.ActualWidth - 40 && settingsBounds.Right < contactBounds.Left &&
                        model.ContactLabel == (arabic ? "تواصل معي" : "Contact"),
                        "The footer has no author credit and keeps Settings left and Contact right");
                    double height = window.ActualHeight, footerHeight = footer.ActualHeight;
                    window.Activate();
                    contact.Focus();
                    contact.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(contact), Environment.TickCount, Key.Up)
                    { RoutedEvent = Keyboard.PreviewKeyDownEvent });
                    await Task.Delay(150);
                    var menu = contact.ContextMenu;
                    var choices = menu.Items.OfType<MenuItem>().ToArray();
                    Check(menu.IsOpen && ReferenceEquals(menu.DataContext, model) && choices.Length == 3 &&
                        choices.Select(c => (string)c.CommandParameter).SequenceEqual(new[] { "github", "linkedin", "email" }) &&
                        choices.All(c => ReferenceEquals(c.Command, model.SocialCommand) && c.IsVisible && c.IsEnabled) &&
                        (string)choices[2].Header == model.EmailLabel,
                        $"The keyboard-opened contact menu includes the existing GitHub, LinkedIn and email actions ({arabic}/{dark}/{width}, active={window.IsActive}, open={menu.IsOpen}, context={ReferenceEquals(menu.DataContext, model)}, labels={string.Join(';', choices.Select(c => c.Header + ":" + c.IsVisible + ":" + c.IsEnabled + ":" + c.CommandParameter))}, email={model.EmailLabel})");
                    var popup = (HwndSource)PresentationSource.FromVisual(menu);
                    GetClientRect(popup.Handle, out var client);
                    var popupTop = menu.PointToScreen(new Point());
                    var popupBottom = menu.PointToScreen(new Point(menu.ActualWidth, menu.ActualHeight));
                    var rootTop = root.PointToScreen(new Point());
                    var rootBottom = root.PointToScreen(new Point(root.ActualWidth, root.ActualHeight));
                    Check(popupTop.X >= rootTop.X - 1 && popupTop.Y >= rootTop.Y - 1 &&
                        popupBottom.X <= rootBottom.X + 1 && popupBottom.Y <= contact.PointToScreen(new Point()).Y &&
                        choices.All(c => c.ActualHeight >= 40),
                        "The complete contact popup opens above the footer within the app borders");
                    string variant = (arabic ? "ar" : "en") + (dark ? "-dark" : "-light") + (width < 1000 ? "-compact" : "-wide");
                    Capture(window, System.IO.Path.Combine(captures, "footer-" + variant + ".png"));
                    if (width < 1000)
                        CaptureVisual(popup.RootVisual, System.IO.Path.Combine(captures, "contact-" + variant + ".png"), client.Right, client.Bottom);
                    choices[0].Focus();
                    InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(menu), Environment.TickCount, Key.Escape)
                    { RoutedEvent = Keyboard.KeyDownEvent, Source = choices[0] });
                    InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(menu), Environment.TickCount, Key.Escape)
                    { RoutedEvent = Keyboard.KeyUpEvent, Source = choices[0] });
                    await Task.Delay(100);
                    Check(!menu.IsOpen && contact.IsKeyboardFocused && Math.Abs(window.ActualHeight - height) <= 1 &&
                        Math.Abs(footer.ActualHeight - footerHeight) <= 1,
                        "Escape closes Contact, restores focus and preserves the footer and window height");
                }
    }
}
