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
        var author = (TextBlock)WpfTree.Find(window, "AuthorCredit");
        var footer = (FrameworkElement)WpfTree.Find(window, "FooterContent");
        var root = (FrameworkElement)WpfTree.Find(window, "Root");
        foreach (bool arabic in new[] { false, true })
            foreach (bool dark in new[] { false, true })
                foreach (double width in new[] { 1160d, 820d })
                {
                    model.SelectLanguageCommand.Execute(arabic ? "ar" : "en");
                    if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                    window.Width = width;
                    await Task.Delay(150);
                    window.UpdateLayout();
                    var authorBounds = author.TransformToAncestor(root).TransformBounds(new Rect(author.RenderSize));
                    var contactBounds = contact.TransformToAncestor(root).TransformBounds(new Rect(contact.RenderSize));
                    Check(authorBounds.Left < 40 && contactBounds.Right > root.ActualWidth - 40 && authorBounds.Right < contactBounds.Left &&
                        model.ContactLabel == (arabic ? "تواصل معي" : "Contact"),
                        "Contact replaces the social-link row while keeping the author left and contact action right");
                    double height = window.ActualHeight, footerHeight = footer.ActualHeight;
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
                        "The keyboard-opened contact menu includes the existing GitHub, LinkedIn and email actions");
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
                    await Task.Delay(100);
                    Check(!menu.IsOpen && contact.IsKeyboardFocused && Math.Abs(window.ActualHeight - height) <= 1 &&
                        Math.Abs(footer.ActualHeight - footerHeight) <= 1,
                        "Escape closes Contact, restores focus and preserves the footer and window height");
                }
    }
}
