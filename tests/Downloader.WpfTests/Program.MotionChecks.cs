using System.Runtime.InteropServices;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckMotion(MainWindow window, MainViewModel model, FakeProvider provider, string captures)
    {
        string oldFolder = model.Folder;
        bool oldAudio = model.Audio, oldDark = model.IsDark, oldArabic = model.IsArabic;
        var oldEncoding = model.SelectedEncoding;
        var oldQuality = model.SelectedQuality;
        double oldWidth = window.Width, oldHeight = window.Height;
        string fixtureFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(captures, "motion-fixtures"));
        System.IO.Directory.CreateDirectory(fixtureFolder);
        string fixtureFile = System.IO.Path.Combine(fixtureFolder, "Local UI fixture.mp4");
        System.IO.File.WriteAllText(fixtureFile, "Owned UI fixture; not playable media.");
        var action = (FrameworkElement)WpfTree.Find(window, "DownloadAction");
        var button = (Button)WpfTree.Find(window, "DownloadButton");
        var arrow = (System.Windows.Shapes.Path)WpfTree.Find(action, "DownloadArrow");
        var check = (System.Windows.Shapes.Path)WpfTree.Find(action, "CompleteCheck");
        var arrowOffset = (TranslateTransform)arrow.RenderTransform;
        var scale = (ScaleTransform)button.Template.FindName("ProgressScale", button);
        var header = (FrameworkElement)WpfTree.Find(window, "HeaderContent");
        var brand = (Button)WpfTree.Find(header, "BrandButton");
        var light = (System.Windows.Shapes.Path)WpfTree.Find(header, "LogoLight");
        var artwork = (FrameworkElement)WpfTree.Find(header, "BrandArtwork");
        var viewport = (ScrollViewer)WpfTree.Find(window, "Viewport");
        var transition = (ThemeTransitionView)WpfTree.Find(window, "ThemeTransition");
        GetMotionCursor(out var originalCursor);
        async Task Until(Func<bool> predicate, string label)
        {
            for (int attempt = 0; attempt < 150 && !predicate(); attempt++) await Task.Delay(20);
            Check(predicate(), label + $" (busy={model.IsBusy}, progress={model.Progress}, phase={model.DownloadActionState}, status={model.StatusHeading}: {model.StatusDetail})");
        }
        Rect Bounds(FrameworkElement view) => view.TransformToAncestor(window).TransformBounds(new Rect(view.RenderSize));
        void Shot(string name) { window.UpdateLayout(); Capture(window, System.IO.Path.Combine(captures, "motion-" + name + ".png")); }
        async Task Input()
        {
            model.Url = "";
            model.Url = "https://youtu.be/jNQXAC9IVRw";
            await Until(() => model.CanDownload, "Fixture inspection finishes without starting download motion");
            Check(model.DownloadActionState == DownloadActionState.Ready && scale.ScaleX == 0 && check.Visibility == Visibility.Hidden,
                "Metadata inspection cannot imitate a download or a completed file");
        }
        try
        {
            MotionPreferences.SetTestOverride(true);
            window.Activate();
            window.Width = 740;
            window.Height = Math.Min(800, window.CurrentWorkArea().Height - 16);
            model.SettingsVisible = false;
            model.Folder = fixtureFolder;
            model.SelectedEncoding = model.Encodings.First(choice => choice.Encoding == AudioEncoding.Mp3);
            model.Audio = true;
            await Input();
            model.SelectedQuality = model.Qualities.First(q => q.Bitrate == 320);
            model.Url = "";
            foreach (string locale in new[] { "en", "ar" })
                foreach (bool dark in new[] { true, false })
                {
                    model.SelectLanguageCommand.Execute(locale);
                    if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                    await Task.Delay(320);
                    viewport.ScrollToTop();
                    await Task.Delay(30);
                    string variant = locale + (dark ? "-dark" : "-light");
                    var frame = new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight);
                    var logoBounds = Bounds(artwork);
                    brand.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    // Wait for a rendered animation tick; elapsed wall time alone is not a WPF frame.
                    await Until(() => light.Opacity > 0, "The logo begins its finite light pass in " + variant);
                    Check(Bounds(artwork).Width <= logoBounds.Width + .5,
                        "Logo light pass stays inside its fixed silhouette in " + variant + $" (before={logoBounds}, after={Bounds(artwork)}, opacity={light.Opacity})");
                    Shot("logo-" + variant);
                    await Task.Delay(580);
                    Check(light.Opacity == 0 && ((ScaleTransform)artwork.RenderTransform).ScaleX == 1,
                        "The logo settles without a decorative loop in " + variant);
                    await Input();
                    action.BringIntoView();
                    await Task.Delay(50);
                    var before = Bounds(button);
                    using var finish = new ManualResetEventSlim();
                    provider.DownloadHandler = (_, token, progress) =>
                    {
                        progress.Report(new DownloadProgress { Stage = "Downloading", Detail = "Local animation fixture", Percent = 20 });
                        finish.Wait(token);
                        return new DownloadResult { FilePath = fixtureFile, Files = new() { fixtureFile }, Completed = 1, Total = 1 };
                    };
                    int calls = provider.DownloadCalls;
                    var download = model.DownloadCommand.ExecuteAsync(null);
                    await Until(() => model.Progress == 20 && model.IsBusy, "Download motion receives actual reported progress in " + variant);
                    action.BringIntoView();
                    await Task.Delay(70);
                    Check(model.DownloadActionState == DownloadActionState.Downloading && !button.IsEnabled && arrow.Visibility == Visibility.Visible && check.Visibility == Visibility.Hidden,
                        "Only an active download moves the arrow and prevents duplicate activation in " + variant);
                    Check(scale.ScaleX is > 0 and <= .2 && model.Progress == 20,
                        "The animated fill approaches the real target without changing semantic progress in " + variant);
                    Shot("downloading-" + variant);
                    await Task.Delay(240);
                    Check(Math.Abs(scale.ScaleX - .2) < .001 && arrowOffset.Y == 0 && provider.DownloadCalls == calls + 1,
                        "Progress settles at twenty percent and the arrow plays once in " + variant);
                    Check(Math.Abs(Bounds(button).Width - before.Width) < 1 && Math.Abs(Bounds(button).Height - before.Height) < 1,
                        "Download content keeps the fixed action dimensions in " + variant);
                    var cancel = (Button)WpfTree.Find(action, "ActionCancel");
                    var cancelBounds = cancel.TransformToAncestor(viewport).TransformBounds(new Rect(cancel.RenderSize));
                    Check(cancel.IsEnabled && cancel.IsVisible && cancelBounds.Top >= 0 && cancelBounds.Bottom <= viewport.ActualHeight + 1,
                        "Cancel stays visible beside the active action in the compact viewport in " + variant);
                    finish.Set();
                    await download;
                    await Task.Delay(70);
                    Check(model.DownloadActionState == DownloadActionState.Complete && model.DownloadActionLabel == model.RevealLabel &&
                        check.Visibility == Visibility.Visible && check.StrokeDashOffset > 0 && button.IsEnabled && model.Progress == 100,
                        "Only full success draws the check and exposes the localized folder action in " + variant);
                    Shot("complete-draw-" + variant);
                    await Task.Delay(240);
                    Check(check.StrokeDashOffset == 0 && scale.ScaleX == 1 &&
                        AutomationProperties.GetName(button) == model.RevealLabel && button.Command == model.DownloadActionCommand,
                        "Completion settles into a named native folder action in " + variant);
                    Shot("complete-" + variant);
                    Check(new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight) == frame,
                        "Motion, metadata and completion preserve the outer frame in " + variant);
                }

            await Input();
            provider.DownloadHandler = (_, token, progress) =>
            {
                progress.Report(new DownloadProgress { Stage = "Downloading", Percent = 35 });
                token.WaitHandle.WaitOne();
                token.ThrowIfCancellationRequested();
                throw new InvalidOperationException("Unreachable fixture");
            };
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var task = model.DownloadCommand.ExecuteAsync(null);
                await Until(() => model.IsBusy && model.Progress == 35, "Repeat/cancel fixture reaches its actual progress");
                model.CancelCommand.Execute(null);
                await task;
                await Task.Delay(50);
                Check(model.DownloadActionState == DownloadActionState.Ready && scale.ScaleX == 0 && arrowOffset.Y == 0 &&
                    check.Visibility == Visibility.Hidden && button.IsEnabled, "Cancellation resets motion and permits immediate retry without stale success");
            }
            provider.DownloadHandler = (_, _, _) => throw new IOException("Fixture download failed");
            await model.DownloadCommand.ExecuteAsync(null);
            Check(model.DownloadActionState == DownloadActionState.Ready && scale.ScaleX == 0 && check.Visibility == Visibility.Hidden,
                "A failed download cannot display the success check or fill");

            var feedback = (QueueFeedbackView)WpfTree.Find(window, "QueueFeedback");
            var added = (System.Windows.Shapes.Path)WpfTree.Find(feedback, "AddedMark");
            int revision = model.QueueAdditionVersion;
            model.AddQueueCommand.Execute(null);
            await Task.Delay(140);
            Check(model.QueueAdditionVersion == revision + 1 && added.Opacity > .9, "A successful queue addition receives one brief check confirmation");
            Shot("queue-confirmation");
            model.DismissNoticeCommand.Execute(null);
            await Task.Delay(800);
            Check(added.Opacity == 0, "The queue confirmation returns to the normal add icon");

            model.Url = "";
            await Task.Delay(100);
            viewport.ScrollToTop();
            window.UpdateLayout();
            var paste = (Button)WpfTree.Find(window, "PasteButton");
            paste.ApplyTemplate();
            window.Topmost = true;
            window.Activate();
            await Task.Delay(80);
            window.Topmost = false;
            var hover = (Border)paste.Template.FindName("HoverLayer", paste);
            var point = paste.PointToScreen(new Point(paste.ActualWidth / 2, paste.ActualHeight / 2));
            SetMotionCursor((int)window.Left, (int)window.Top);
            await Task.Delay(160);
            var pasteBounds = Bounds(paste);
            bool moved = SetMotionCursor((int)point.X, (int)point.Y);
            await Task.Delay(45);
            Shot("hover-enter");
            Check(paste.IsMouseOver && hover.Opacity is > 0 and < 1,
                $"Native pointer hover interpolates the button wash (moved={moved}, over={paste.IsMouseOver}, opacity={hover.Opacity}, enabled={paste.IsEnabled}, loaded={hover.IsLoaded}, parent={hover.TemplatedParent}, active={window.IsActive}, target={point})");
            await Task.Delay(160);
            Check(hover.Opacity == 1 && Bounds(paste) == pasteBounds, "Hover settles without moving or resizing the button");
            SetMotionCursor((int)window.Left, (int)window.Top);
            await Task.Delay(160);
            Check(hover.Opacity == 0, "Pointer exit clears the hover wash");

            model.ThemeCommand.Execute(null);
            await Task.Delay(55);
            Check(transition.IsTransitioning && !transition.IsHitTestVisible, "Theme changes reveal coherent palette regions without blocking input");
            Shot("theme-midpoint");
            model.ThemeCommand.Execute(null);
            await Task.Delay(320);
            Check(!transition.IsTransitioning, "Rapid theme changes replace and release the previous snapshot");
            model.ThemeCommand.Execute(null);
            window.Width += 10;
            await Task.Delay(60);
            Check(!transition.IsTransitioning, "Resizing interrupts the theme snapshot cleanly");
            window.Width -= 10;

            MotionPreferences.SetTestOverride(false);
            brand.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            model.ThemeCommand.Execute(null);
            Check(light.Opacity == 0 && !transition.IsTransitioning, "Reduced motion applies theme and branding immediately");
            await Input();
            provider.DownloadHandler = (_, token, progress) =>
            {
                progress.Report(new DownloadProgress { Stage = "Downloading", Percent = 20 });
                token.WaitHandle.WaitOne();
                token.ThrowIfCancellationRequested();
                throw new InvalidOperationException("Unreachable fixture");
            };
            var reduced = model.DownloadCommand.ExecuteAsync(null);
            await Until(() => model.IsBusy && model.Progress == 20, "Reduced-motion download receives semantic progress");
            Check(scale.ScaleX == .2 && arrowOffset.Y == 0, "Reduced motion presents the real progress immediately without spatial movement");
            model.CancelCommand.Execute(null);
            await reduced;
            Shot("reduced-motion");

            provider.DownloadHandler = (_, _, _) => new DownloadResult { FilePath = fixtureFile, Files = new() { fixtureFile }, Completed = 1, Total = 1 };
            await model.DownloadCommand.ExecuteAsync(null);
            Check(model.DownloadActionState == DownloadActionState.Complete && check.StrokeDashOffset == 0 && check.IsVisible && scale.ScaleX == 1,
                "Reduced motion preserves immediate success and folder action feedback");
            model.Url = "";

            MotionPreferences.SetTestOverride(true);
            brand.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(80);
            MotionPreferences.SetTestOverride(false);
            Check(light.Opacity == 0 && ((ScaleTransform)artwork.RenderTransform).ScaleX == 1,
                "A preference change interrupts an in-flight logo animation");
            MotionPreferences.SetTestOverride(true);
            model.ThemeCommand.Execute(null);
            MotionPreferences.SetTestOverride(false);
            Check(!transition.IsTransitioning, "A preference change immediately releases an in-flight theme snapshot");
        }
        finally
        {
            provider.DownloadHandler = null;
            model.CancelCommand.Execute(null);
            MotionPreferences.SetTestOverride(null);
            model.Folder = oldFolder;
            model.Url = "";
            model.SelectedEncoding = oldEncoding;
            model.Audio = oldAudio;
            model.SelectedQuality = model.Qualities.FirstOrDefault(choice => choice.Key == oldQuality?.Key && choice.Bitrate == oldQuality?.Bitrate && choice.Height == oldQuality?.Height) ?? model.Qualities.FirstOrDefault();
            model.SelectLanguageCommand.Execute(oldArabic ? "ar" : "en");
            if (model.IsDark != oldDark) model.ThemeCommand.Execute(null);
            window.Width = oldWidth;
            window.Height = oldHeight;
            window.Topmost = false;
            SetMotionCursor(originalCursor.X, originalCursor.Y);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MotionPoint { internal int X, Y; }
    [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
    private static extern bool SetMotionCursor(int x, int y);
    [DllImport("user32.dll", EntryPoint = "GetCursorPos")]
    private static extern bool GetMotionCursor(out MotionPoint point);
}
