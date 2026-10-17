using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckHeader(Window window, MainViewModel model, string captures)
    {
        var header = (FrameworkElement)WpfTree.Find(window, "HeaderContent");
        var brand = (FrameworkElement)WpfTree.Find(window, "BrandWordmark");
        var actions = (FrameworkElement)WpfTree.Find(window, "HeaderActions");
        var language = (Button)WpfTree.Find(window, "LanguageButton");
        var settings = (Button)WpfTree.Find(window, "SettingsButton");
        double startupWidth = window.Width;
        model.Url = "https://youtu.be/jNQXAC9IVRw";
        await Task.Delay(850);
        model.Audio = true;
        model.SelectedQuality = model.Qualities.First(q => q.Bitrate == 320);
        foreach (bool arabic in new[] { false, true })
            foreach (bool dark in new[] { false, true })
                foreach (double width in new[] { window.MinWidth, startupWidth, 1160d })
                {
                    model.SelectLanguageCommand.Execute(arabic ? "ar" : "en");
                    if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                    window.Width = width;
                    await Task.Delay(150);
                    window.UpdateLayout();
                    Rect Bounds(FrameworkElement element) => element.TransformToAncestor(window).TransformBounds(new Rect(element.RenderSize));
                    var heading = (FrameworkElement)WpfTree.Find(window, "NewDownloadHeading");
                    double headerHeight = header.ActualHeight, headingTop = Bounds(heading).Top;
                    ((Button)WpfTree.Find(window, "BrandButton")).Focus();
                    await Task.Delay(60);
                    window.UpdateLayout();
                    Check(Math.Abs(header.ActualHeight - headerHeight) < .1 && Math.Abs(Bounds(heading).Top - headingTop) < .1,
                        $"Focusing the logo preserves header height and form position (header={headerHeight}->{header.ActualHeight}, heading={headingTop}->{Bounds(heading).Top})");
                    var theme = (Button)WpfTree.Find(window, "ThemeButton");
                    theme.Focus();
                    window.UpdateLayout();
                    var themeBounds = Bounds(theme);
                    var glyphBounds = Bounds((FrameworkElement)WpfTree.Find(theme, "ThemeGlyph"));
                    var themeDpi = VisualTreeHelper.GetDpi(theme);
                    Check(Math.Abs(theme.ActualWidth - 42) <= 1 / themeDpi.DpiScaleX + .01 && Math.Abs(theme.ActualHeight - 42) <= 1 / themeDpi.DpiScaleY + .01 &&
                        Math.Abs(header.ActualHeight - headerHeight) < .1 && Math.Abs(Bounds(heading).Top - headingTop) < .1 &&
                        glyphBounds.Left >= themeBounds.Left + 8 && glyphBounds.Right <= themeBounds.Right - 8 &&
                        glyphBounds.Top >= themeBounds.Top + 8 && glyphBounds.Bottom <= themeBounds.Bottom - 8,
                        "The focused theme button keeps its icon clear and does not move the header or form");
                    if (width == startupWidth)
                    {
                        theme.Command.Execute(null);
                        await Task.Delay(320);
                        window.UpdateLayout();
                        Check(Math.Abs(header.ActualHeight - headerHeight) < .1 && Math.Abs(Bounds(heading).Top - headingTop) < .1,
                            "Switching the theme while its button is focused preserves content position");
                        theme.Command.Execute(null);
                        await Task.Delay(320);
                    }
                    var brandBounds = Bounds(brand);
                    var actionBounds = Bounds(actions);
                    Check(brandBounds.Left < 100 && brandBounds.Right < actionBounds.Left && actionBounds.Right <= window.ActualWidth &&
                        Bounds(settings).Top > Bounds(header).Bottom,
                        "Header remains unclipped with fixed-left branding, right actions and reachable footer Settings");
                    Check(brand is Viewbox && Equals(((System.Windows.Shapes.Path)WpfTree.Find(window, "BrandLetters")).Fill, Application.Current.Resources["Ink"]) && Equals(((System.Windows.Shapes.Path)WpfTree.Find(window, "BrandInitial")).Fill, Application.Current.Resources["BrandFill"]) && brand.FlowDirection == FlowDirection.LeftToRight,
                        "The compact vector wordmark keeps a red V, themed remaining letters and fixed direction across themes");
                    string variant = (arabic ? "ar" : "en") + (dark ? "-dark" : "-light") + (width == startupWidth ? "-default" : width < 1000 ? "-compact" : "-wide");
                    CaptureVisual(header, System.IO.Path.Combine(captures, "header-" + variant + ".png"));
                    language.Focus();
                    await SettleMaintenance(window);
                    language.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    await Task.Delay(300);
                    var menu = language.ContextMenu;
                    var choices = menu.Items.OfType<MenuItem>().ToArray();
                    Check(menu.IsOpen && ReferenceEquals(menu.DataContext, model) && choices.Count(c => c.IsChecked) == 1 &&
                        (string)choices.Single(c => c.IsChecked).CommandParameter == (arabic ? "ar" : "en"),
                        $"The language menu opens and identifies the current language in {variant} (open={menu.IsOpen}, context={ReferenceEquals(menu.DataContext, model)}, checked={string.Join(',', choices.Where(choice => choice.IsChecked).Select(choice => choice.CommandParameter))})");
                    var popup = (HwndSource)PresentationSource.FromVisual(menu);
                    GetClientRect(popup.Handle, out var client);
                    var nativeSize = popup.CompositionTarget.TransformFromDevice.Transform(new Vector(client.Right, client.Bottom));
                    Check(choices.All(c => c.IsVisible && c.ActualHeight >= 40) && menu.ActualWidth <= nativeSize.X + 1 && menu.ActualHeight <= nativeSize.Y + 1,
                        "Both language choices fit their actual native popup");
                    GetWindowRect(popup.Handle, out var screen);
                    var start = language.PointToScreen(new Point());
                    var end = language.PointToScreen(new Point(language.ActualWidth, language.ActualHeight));
                    var page = (FrameworkElement)window.Content;
                    var pageStart = page.PointToScreen(new Point());
                    var pageEnd = page.PointToScreen(new Point(page.ActualWidth, page.ActualHeight));
                    double gap = 6 * VisualTreeHelper.GetDpi(language).DpiScaleY;
                    Check(Math.Abs((screen.Left + screen.Right - start.X - end.X) / 2) <= 3 &&
                        Math.Abs(screen.Top - end.Y - gap) <= 2 && screen.Left >= pageStart.X && screen.Right <= pageEnd.X && screen.Bottom <= pageEnd.Y,
                        "The language menu is centered under its control with a consistent gap inside the app in " + variant);
                    Rect FaceBounds(MenuItem choice) => ((FrameworkElement)choice.Template.FindName("Face", choice))
                        .TransformToAncestor(menu).TransformBounds(new Rect(choice.RenderSize));
                    var firstFace = FaceBounds(choices[0]);
                    var lastFace = FaceBounds(choices[^1]);
                    Check(Math.Abs(menu.ActualWidth - 160) < 1 && Math.Abs(menu.ActualHeight - 94) < 1 &&
                        Math.Abs(firstFace.Top - 5) < 1 && Math.Abs(menu.ActualHeight - lastFace.Bottom - 5) < 1 &&
                        Math.Abs(lastFace.Top - firstFace.Bottom - 4) < 1 &&
                        choices.All(choice => Math.Abs(choice.ActualHeight - 40) < 1),
                        "The language menu uses equal compact rows, four-DIP edge spacing and a four-DIP row gap in " + variant);
                    Check(menu.FlowDirection == model.ContentDirection && choices.All(choice => choice.FlowDirection == model.ContentDirection),
                        "The language menu uses the current direction after reuse in " + variant);
                    CapturePopup(page, popup, System.IO.Path.Combine(captures, "language-placement-" + variant + ".png"));
                    if (width < 1000) CaptureVisual(popup.RootVisual, System.IO.Path.Combine(captures, "language-" + variant + ".png"), client.Right, client.Bottom);
                    var next = choices.Single(c => (string)c.CommandParameter == (arabic ? "en" : "ar"));
                    if (width == startupWidth)
                    {
                        next.Focus();
                        await Task.Delay(80);
                        CapturePopup(page, popup, System.IO.Path.Combine(captures, "language-focus-" + variant + ".png"));
                    }
                    ((IInvokeProvider)new MenuItemAutomationPeer(next).GetPattern(PatternInterface.Invoke)).Invoke();
                    await Task.Delay(150);
                    Check(!menu.IsOpen && model.IsArabic != arabic && model.SelectedQuality.Bitrate == 320 &&
                        model.CurrentLanguageLabel == (model.IsArabic ? "العربية" : "English"),
                        "Selecting a language closes the menu, updates its label and preserves audio quality");
                }
        foreach (bool dark in new[] { false, true })
            foreach (string locale in new[] { "en", "ar" })
            {
                model.SelectLanguageCommand.Execute(locale);
                if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                await Task.Delay(80);
                language.Focus();
                language.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(language), Environment.TickCount, Key.Down)
                { RoutedEvent = Keyboard.PreviewKeyDownEvent });
                await Task.Delay(80);
                var menu = language.ContextMenu;
                Check(menu.IsOpen && menu.FlowDirection == model.ContentDirection && model.SelectedQuality.Bitrate == 320,
                    "Keyboard opening retains the current language and quality in " + locale);
                InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(menu), Environment.TickCount, Key.Escape)
                { RoutedEvent = Keyboard.KeyDownEvent, Source = menu });
                await Task.Delay(80);
                Check(!menu.IsOpen && model.IsArabic == (locale == "ar"), "Escape dismisses the language picker without selecting another language");
                language.Focus();
                InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(language), Environment.TickCount, Key.Apps)
                { RoutedEvent = Keyboard.KeyUpEvent, Source = language });
                await Task.Delay(80);
                var nativeMenu = (HwndSource)PresentationSource.FromVisual(menu);
                GetWindowRect(nativeMenu.Handle, out var nativeBounds);
                var first = language.PointToScreen(new Point());
                var last = language.PointToScreen(new Point(language.ActualWidth, language.ActualHeight));
                Check(menu.IsOpen && Math.Abs((nativeBounds.Left + nativeBounds.Right - first.X - last.X) / 2) <= 3 &&
                    menu.FlowDirection == model.ContentDirection && menu.Items.OfType<MenuItem>().Single(choice => choice.IsChecked).CommandParameter.Equals(locale),
                    "The native context-menu key opens the same anchored language picker in " + locale);
                menu.IsOpen = false;
                language.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(80);
                foreach (var pointerEvent in new[] { Mouse.PreviewMouseDownOutsideCapturedElementEvent, Mouse.PreviewMouseUpOutsideCapturedElementEvent })
                    InputManager.Current.ProcessInput(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                    { RoutedEvent = pointerEvent, Source = menu });
                await Task.Delay(80);
                Check(!menu.IsOpen && model.IsArabic == (locale == "ar"), "Outside interaction dismisses the language picker without changing selection");
                language.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(80);
                model.LanguageCommand.Execute(null);
                await Task.Delay(80);
                Check(!menu.IsOpen, "External language changes close the old header popup before reuse");
            }
        bool previousLanguage = model.IsArabic;
        model.SelectLanguageCommand.Execute("unsupported");
        Check(model.IsArabic == previousLanguage && !model.SelectLanguageCommand.CanExecute("unsupported"),
            "Unsupported language values cannot alter the application state");
        settings.Command.Execute(null);
        await Task.Delay(100);
        Check(model.SettingsVisible && ((FrameworkElement)WpfTree.Find(window, "ToolRefreshAnchor")).IsVisible,
            "Footer Settings keeps app updates, browser integration and maintenance accessible");
        ((Button)WpfTree.Find(window, "BrandButton")).Command.Execute(null);
        Check(!model.SettingsVisible, "The brand returns from Settings to the download form");
        Exception dialogFailure = null;
        _ = window.Dispatcher.BeginInvoke(new Action(() =>
        {
            Window history = null;
            try
            {
                history = window.OwnedWindows.Cast<Window>().Single();
                Check(history.Title == model.HistoryLabel && history.IsVisible && history.Owner == window,
                    "The new header action opens the existing recent-downloads dialog");
            }
            catch (Exception ex) { dialogFailure = ex; }
            finally { history?.Close(); }
        }));
        ((Button)WpfTree.Find(window, "RecentDownloadsButton")).Command.Execute(null);
        if (dialogFailure != null) throw dialogFailure;
        using var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Vydrop;component/Assets/app.ico")).Stream;
        var frames = new IconBitmapDecoder(iconStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames;
        Check(frames.Select(frame => frame.PixelWidth).SequenceEqual(new[] { 16, 24, 32, 48, 64, 128, 256 }),
            "The app icon contains seven native sizes instead of a stretched small bitmap");
        using var logoStream = Application.GetResourceStream(new Uri("pack://application:,,,/Vydrop;component/Assets/header-logo.png")).Stream;
        var logo = new FormatConvertedBitmap(BitmapFrame.Create(logoStream), PixelFormats.Bgra32, null, 0);
        byte[] pixels = new byte[logo.PixelWidth * logo.PixelHeight * 4];
        logo.CopyPixels(pixels, logo.PixelWidth * 4, 0);
        int armPixel = (128 * 512 + 128) * 4;
        int arrowPixel = (256 * 512 + 256) * 4;
        int pointPixel = (384 * 512 + 256) * 4;
        Check(logo.PixelWidth == 512 && pixels[3] == 0 &&
            pixels[arrowPixel] == 255 && pixels[arrowPixel + 1] == 255 && pixels[arrowPixel + 2] == 255 && pixels[arrowPixel + 3] == 255 &&
            pixels[pointPixel + 3] == 255 && pixels[pointPixel] == 0x4D && pixels[pointPixel + 1] == 0x3D && pixels[pointPixel + 2] == 0xE6 &&
            pixels[armPixel + 3] == 255 && pixels[armPixel] == 0x4D && pixels[armPixel + 1] == 0x3D && pixels[armPixel + 2] == 0xE6,
            "The V-only icon retains transparent edges, a solid white download arrow and its red lower point");
        model.Audio = false;
        model.Url = "";
        await Task.Delay(200);
        window.UpdateLayout();
        Capture(window, System.IO.Path.Combine(captures, "header-workspace.png"));
    }

    private static void CaptureVisual(Visual visual, string path, int width = 0, int height = 0)
    {
        var dpi = VisualTreeHelper.GetDpi(visual);
        if (visual is FrameworkElement element)
        {
            width = (int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX);
            height = (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY);
        }
        var bitmap = new RenderTargetBitmap(width, height, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = System.IO.File.Create(path);
        encoder.Save(stream);
    }
}
