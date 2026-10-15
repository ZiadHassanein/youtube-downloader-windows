using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckOutputPicker(Window window, MainViewModel model, string captures)
    {
        double originalWidth = window.Width;
        double originalHeight = window.Height;
        bool originalAudio = model.Audio, originalArabic = model.IsArabic, originalDark = model.IsDark;
        var originalEncoding = model.SelectedEncoding;
        string originalUrl = model.Url;
        var audio = (RadioButton)WpfTree.Find(window, "AudioChoice");
        var menu = audio.ContextMenu;
        var quality = (ComboBox)WpfTree.Find(window, "QualityInput");
        var options = (Button)WpfTree.Find(window, "MoreOptionsButton");
        var form = (FrameworkElement)WpfTree.Find(window, "DownloadForm");
        try
        {
            foreach (bool arabic in new[] { false, true })
                foreach (bool dark in new[] { false, true })
                    foreach (bool loaded in new[] { false, true })
                    {
                        window.Width = loaded ? 1160 : 820;
                        model.SelectLanguageCommand.Execute(arabic ? "ar" : "en");
                        if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                        model.Url = loaded ? "https://youtu.be/jNQXAC9IVRw" : "";
                        await Task.Delay(loaded ? 850 : 80);
                        model.SelectedEncoding = model.Encodings.First(choice => choice.Encoding == AudioEncoding.Mp3);
                        model.Audio = false;
                        audio.BringIntoView();
                        await Task.Delay(60);
                        window.UpdateLayout();
                        string variant = (arabic ? "ar" : "en") + (dark ? "-dark" : "-light") + (loaded ? "-split" : "-idle");
                        var qualityBounds = RelativeBounds(quality, form);
                        var optionsBounds = RelativeBounds(options, form);
                        Check(Math.Abs(qualityBounds.Bottom - optionsBounds.Bottom) < 1 &&
                            (arabic ? optionsBounds.Right < qualityBounds.Left : qualityBounds.Right < optionsBounds.Left) &&
                            quality.ActualWidth >= 140 && Descendants<ComboBox>(form).Count() == 1,
                            "Quality and More options share a usable row without a format dropdown in " + variant);
                        var sizeBefore = window.RenderSize;
                        audio.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        await Task.Delay(300);
                        var choices = menu.Items.OfType<MenuItem>().ToArray();
                        Check(model.Audio && menu.IsOpen && !menu.StaysOpen && choices.Length == 5 && choices.Count(item => item.IsChecked) == 1,
                            "Activating Audio opens its five-format light-dismiss picker in " + variant);
                        var source = (HwndSource)PresentationSource.FromVisual(menu);
                        GetWindowRect(source.Handle, out var popupBounds);
                        var client = (FrameworkElement)window.Content;
                        var clientStart = client.PointToScreen(new Point());
                        var clientEnd = client.PointToScreen(new Point(client.ActualWidth, client.ActualHeight));
                        Check(popupBounds.Left >= Math.Min(clientStart.X, clientEnd.X) - 1 && popupBounds.Right <= Math.Max(clientStart.X, clientEnd.X) + 1 &&
                            popupBounds.Top >= clientStart.Y - 1 && popupBounds.Bottom <= clientEnd.Y + 1 && choices.All(item => item.ActualHeight >= 40),
                            "The audio picker and all format targets stay inside the app client in " + variant);
                        CapturePopup(client, source, System.IO.Path.Combine(captures, "audio-placement-" + variant + ".png"));
                        var audioStart = audio.PointToScreen(new Point());
                        var audioEnd = audio.PointToScreen(new Point(audio.ActualWidth, audio.ActualHeight));
                        double audioCenter = (audioStart.X + audioEnd.X) / 2;
                        double popupCenter = (popupBounds.Left + popupBounds.Right) / 2d;
                        Check(Math.Abs(popupCenter - audioCenter) <= 3,
                            $"The format popup stays centered on Audio in {variant} (audio={audioCenter}, popup={popupCenter})");
                        Check(menu.FlowDirection == model.ContentDirection && choices.All(item => item.FlowDirection == model.ContentDirection),
                            "The menu and its choices use the current language direction in " + variant);
                        Capture(window, System.IO.Path.Combine(captures, "output-row-" + variant + ".png"));
                        GetClientRect(source.Handle, out var native);
                        CaptureVisual(source.RootVisual, System.IO.Path.Combine(captures, "audio-formats-" + variant + ".png"), native.Right, native.Bottom);
                        foreach (var encoding in model.Encodings)
                        {
                            if (!menu.IsOpen) { audio.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); await Task.Delay(50); }
                            var item = menu.Items.OfType<MenuItem>().Single(choice => ((EncodingChoice)choice.Tag).Encoding == encoding.Encoding);
                            ((IInvokeProvider)new MenuItemAutomationPeer(item).GetPattern(PatternInterface.Invoke)).Invoke();
                            await Task.Delay(65);
                            Check(!menu.IsOpen && model.SelectedEncoding == encoding && model.AudioLabel.Contains(encoding.Label) &&
                                (OutputProfiles.IsLossless(encoding.Encoding) ? model.Qualities.Count == 1 && !quality.IsEnabled :
                                    model.Qualities.Count == OutputProfiles.AudioBitrates(encoding.Encoding).Count + 1 && quality.IsEnabled),
                                $"Choosing {encoding.Label} closes the picker and supplies its valid quality choices in {variant}");
                        }
                        audio.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        await Task.Delay(60);
                        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(menu), Environment.TickCount, Key.Escape)
                        { RoutedEvent = Keyboard.KeyDownEvent, Source = menu });
                        await Task.Delay(60);
                        Check(!menu.IsOpen && model.SelectedEncoding.Encoding == AudioEncoding.Flac,
                            "Escape dismisses audio choices without changing the selected format in " + variant);
                        audio.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        await Task.Delay(60);
                        // A synthetic opening click has no physical release. Include the complete
                        // outside pointer cycle so native menu dismissal can consume that release.
                        foreach (var pointerEvent in new[] { Mouse.PreviewMouseDownOutsideCapturedElementEvent, Mouse.PreviewMouseUpOutsideCapturedElementEvent })
                            InputManager.Current.ProcessInput(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                            { RoutedEvent = pointerEvent, Source = menu });
                        await Task.Delay(60);
                        Check(!menu.IsOpen && model.SelectedEncoding.Encoding == AudioEncoding.Flac,
                            "Outside-click routing dismisses the picker and retains its format in " + variant);
                        Check(window.RenderSize == sizeBefore, "Picking audio formats does not resize the main window in " + variant);
                        model.SelectedEncoding = model.Encodings.First(choice => choice.Encoding == AudioEncoding.Mp3);
                        model.SelectedQuality = model.Qualities.First(choice => choice.Bitrate == 128);
                        model.LanguageCommand.Execute(null);
                        Check(model.SelectedQuality.Bitrate == 128, "Expanded quality selections survive language changes in " + variant);
                        model.LanguageCommand.Execute(null);
                        quality.IsDropDownOpen = true;
                        await Task.Delay(80);
                        var popup = (Popup)quality.Template.FindName("PART_Popup", quality);
                        var popupSource = (HwndSource)PresentationSource.FromVisual(popup.Child);
                        var selectedRow = (ComboBoxItem)quality.ItemContainerGenerator.ContainerFromItem(quality.SelectedItem);
                        var qualityViewport = Descendants<ScrollContentPresenter>(popup.Child).Single();
                        var selectedBounds = RelativeBounds(selectedRow, qualityViewport);
                        Check(selectedBounds.Top >= -1 && selectedBounds.Bottom <= qualityViewport.ActualHeight + 1,
                            $"The selected quality row is fully visible in the expanded scrolling menu in {variant} (selected={selectedBounds}, viewport={qualityViewport.RenderSize}, behavior={ComboBoxSelectionVisibility.GetKeepVisible(quality)}, scroll={Descendants<ScrollViewer>(popup.Child).Single().VerticalOffset}/{Descendants<ScrollViewer>(popup.Child).Single().ScrollableHeight})");
                        GetClientRect(popupSource.Handle, out native);
                        CaptureVisual(popupSource.RootVisual, System.IO.Path.Combine(captures, "audio-qualities-" + variant + ".png"), native.Right, native.Bottom);
                        quality.IsDropDownOpen = false;
                    }
            model.Url = "";
            window.Width = 960;
            model.SelectedEncoding = model.Encodings.First(choice => choice.Encoding == AudioEncoding.Mp3);
            model.Audio = true;
            model.SelectedQuality = model.Qualities.First(choice => choice.Bitrate == 128);
            foreach (bool dark in new[] { false, true })
            {
                menu.IsOpen = false;
                if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                int step = 0;
                foreach (string language in new[] { "en", "ar", "en", "ar" })
                {
                    bool wasOpen = menu.IsOpen;
                    model.SelectLanguageCommand.Execute(language);
                    await Task.Delay(80);
                    Check(!wasOpen || !menu.IsOpen, "Switching language dismisses the old format popup");
                    audio.BringIntoView();
                    audio.Focus();
                    audio.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(audio), Environment.TickCount, Key.Down)
                    { RoutedEvent = Keyboard.PreviewKeyDownEvent });
                    await Task.Delay(300);
                    var source = (HwndSource)PresentationSource.FromVisual(menu);
                    GetWindowRect(source.Handle, out var bounds);
                    var first = audio.PointToScreen(new Point());
                    var last = audio.PointToScreen(new Point(audio.ActualWidth, audio.ActualHeight));
                    Check(menu.IsOpen && Math.Abs((bounds.Left + bounds.Right - first.X - last.X) / 2) <= 3 &&
                        menu.FlowDirection == model.ContentDirection && menu.Items.OfType<MenuItem>().All(item => item.FlowDirection == model.ContentDirection) &&
                        model.SelectedQuality.Bitrate == 128,
                        $"The keyboard-opened popup follows Audio and retains quality after language switch {language} (open={menu.IsOpen}, centerDelta={(bounds.Left + bounds.Right - first.X - last.X) / 2}, direction={menu.FlowDirection}/{model.ContentDirection}, rows={string.Join(',', menu.Items.OfType<MenuItem>().Select(item => item.FlowDirection))}, bitrate={model.SelectedQuality.Bitrate})");
                    CapturePopup((FrameworkElement)window.Content, source, System.IO.Path.Combine(captures,
                        "audio-switch-" + language + (dark ? "-dark-" : "-light-") + step++ + ".png"));
                }
            }
            menu.IsOpen = false;
            window.Height = 500;
            foreach (string language in new[] { "en", "ar" })
            {
                model.SelectLanguageCommand.Execute(language);
                await Task.Delay(80);
                audio.BringIntoView();
                await Task.Delay(80);
                audio.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                await Task.Delay(300);
                var source = (HwndSource)PresentationSource.FromVisual(menu);
                GetWindowRect(source.Handle, out var bounds);
                var client = (FrameworkElement)window.Content;
                var top = client.PointToScreen(new Point());
                var bottom = client.PointToScreen(new Point(client.ActualWidth, client.ActualHeight));
                var first = audio.PointToScreen(new Point());
                var last = audio.PointToScreen(new Point(audio.ActualWidth, audio.ActualHeight));
                Check(Math.Abs((bounds.Left + bounds.Right - first.X - last.X) / 2) <= 3 &&
                    bounds.Left >= top.X && bounds.Right <= bottom.X && bounds.Top >= top.Y && bounds.Bottom <= bottom.Y,
                    "The audio popup stays attached and contained when space below is limited in " + language);
                CapturePopup(client, source, System.IO.Path.Combine(captures, "audio-short-" + language + ".png"));
                menu.IsOpen = false;
            }
        }
        finally
        {
            menu.IsOpen = false;
            quality.IsDropDownOpen = false;
            model.SelectedEncoding = originalEncoding;
            model.Audio = originalAudio;
            model.Url = originalUrl;
            model.SelectLanguageCommand.Execute(originalArabic ? "ar" : "en");
            if (model.IsDark != originalDark) model.ThemeCommand.Execute(null);
            window.Width = originalWidth;
            window.Height = originalHeight;
            window.Activate();
        }
    }
}
