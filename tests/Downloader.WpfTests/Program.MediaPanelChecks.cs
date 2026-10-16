using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckMediaPanel(MainWindow window, MainViewModel model, FakeProvider provider, string captures)
    {
        var original = (window.Width, window.Height, window.Left, window.Top, model.Folder, model.Audio, model.IsArabic, model.IsDark);
        var viewport = (ScrollViewer)WpfTree.Find(window, "Viewport");
        var context = (ScrollViewer)WpfTree.Find(window, "ContextViewport");
        var form = (FrameworkElement)WpfTree.Find(window, "DownloadForm");
        var preview = (FrameworkElement)WpfTree.Find(window, "MediaPreview");
        var heading = (FrameworkElement)WpfTree.Find(form, "NewDownloadHeading");
        var inline = (FrameworkElement)WpfTree.Find(form, "InlineActivity");
        string folder = Path.GetFullPath(Path.Combine(captures, "media-fixtures"));
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, "Owned UI fixture.mp4");
        File.WriteAllText(file, "Synthetic UI fixture; not playable media.");
        Rect Bounds(FrameworkElement view) => view.TransformToAncestor(window).TransformBounds(new Rect(view.RenderSize));
        async Task Settle() { await Task.Delay(250); window.UpdateLayout(); }
        async Task Input(string url) { model.Url = url; await Task.Delay(900); await Settle(); Check(model.CanDownload, "Media inspection completes: " + url); }
        void HeadingFirst(string state)
        {
            viewport.ScrollToTop(); window.UpdateLayout();
            Check(((StackPanel)((UserControl)form).Content).Children[0] == heading && Bounds(heading).Top < Bounds((FrameworkElement)WpfTree.Find(form, "LinkInput")).Top,
                "New download is the first form content with no title or status banner above it: " + state);
        }
        try
        {
            model.SettingsVisible = false; model.Url = ""; model.Audio = true; model.Folder = folder;
            foreach (var entry in model.Queue.Entries) model.RemoveQueueCommand.Execute(entry.Id);
            window.Width = 740;
            var work = window.CurrentWorkArea();
            window.Left = work.Left + 20;
            double left = window.Left, top = window.Top, height = window.Height;
            foreach (string locale in new[] { "en", "ar" })
                foreach (bool dark in new[] { true, false })
                {
                    model.SelectLanguageCommand.Execute(locale);
                    if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                    await Input("https://www.youtube.com/shorts/jNQXAC9IVRw?si=fixture");
                    Check(window.Width > 740 && Math.Abs(window.Top - top) <= 1 && Math.Abs(window.Height - height) <= 1 &&
                        Bounds(preview).Left >= Bounds(form).Right - 1 && context.IsVisible && !inline.IsVisible,
                        "Shorts expand to a physical-right context pane without changing height: " + locale + "/" + dark);
                    var image = (Image)WpfTree.Find(preview, "ThumbnailImage");
                    Check(image.IsVisible && image.Source != null && image.Stretch == Stretch.Uniform && model.PreviewDuration == "8:24",
                        "Shorts retain the same real thumbnail, duration and uncropped artwork treatment as video");
                    HeadingFirst("Short " + locale);
                    Capture(window, Path.Combine(captures, "media-expanded-" + locale + (dark ? "-dark" : "-light") + ".png"));
                    model.Url = ""; await Settle();
                    Check(Math.Abs(window.Width - 740) <= 1 && Math.Abs(window.Left - left) <= 1 && !context.IsVisible,
                        "Clearing media restores the original narrow frame and removes the context pane");
                }
            await Input("https://www.youtube.com/watch?v=YKMJXdNBvNQ&list=RDYKMJXdNBvNQ&start_radio=1");
            model.WholePlaylist = true; await Task.Delay(900); await Settle();
            Check(model.HasPlaylist && model.CanDownload && model.PlaylistLabel.Contains("50") && model.PreviewImage != null,
                "A Mix has a visible bounded playlist choice, actual entries and artwork");
            HeadingFirst("playlist");
            Capture(window, Path.Combine(captures, "media-mix-ar-light.png"));
            // The context scroll must not shift the form's heading or its separate scroll offset.
            double headingTop = Bounds(heading).Top;
            context.ScrollToEnd(); await Settle();
            Check(Math.Abs(Bounds(heading).Top - headingTop) <= 1 && viewport.VerticalOffset == 0,
                "Scrolling media details cannot scroll the download form");
            model.Url = "";
            model.SelectLanguageCommand.Execute("en");
            if (!model.IsDark) model.ThemeCommand.Execute(null);
            await Input("https://youtu.be/jNQXAC9IVRw");
            context.ScrollToTop(); await Settle();
            provider.DownloadDelayMilliseconds = 5000;
            var running = model.DownloadCommand.ExecuteAsync(null);
            await Task.Delay(150); await Settle();
            Check(model.IsBusy && ((ProgressBar)WpfTree.Find(preview, "JobProgress")).IsVisible && !inline.IsVisible,
                "Active progress appears only in the right context pane");
            HeadingFirst("downloading");
            Capture(window, Path.Combine(captures, "media-progress-en-dark.png"));
            model.CancelCommand.Execute(null); await running; await Settle();
            Check(model.Url.Length > 0 && context.IsVisible && window.Width > 740 && model.IsIdle,
                "Cancellation keeps the link and context available for retry");
            provider.DownloadHandler = (_, _, _) => new DownloadResult { FilePath = file, Files = new() { file }, Total = 1, Completed = 1 };
            await model.DownloadCommand.ExecuteAsync(null); await Settle();
            Check(model.Url == "" && Math.Abs(window.Width - 740) <= 1 && !context.IsVisible && inline.IsVisible && model.HasFile,
                "Successful download collapses the pane and retains completion actions below Download");
            HeadingFirst("completed");
            Check(Bounds(inline).Top > Bounds((FrameworkElement)WpfTree.Find(form, "DownloadButton")).Bottom,
                "Completion feedback stays below the main action");
            Capture(window, Path.Combine(captures, "media-completed-en-dark.png"));
            model.Url = "invalid next link"; await Task.Delay(700);
            Check(!model.HasFile, "A new invalid link cannot expose the previous download's file actions");
            await Input("https://youtu.be/jNQXAC9IVRw");
            window.Width = 820; await Settle();
            Check(model.InlineMediaPreview && preview.Parent == WpfTree.Find(form, "InlinePreviewHost") &&
                Bounds(preview).Top > Bounds((FrameworkElement)WpfTree.Find(form, "LinkInput")).Bottom &&
                ((Image)WpfTree.Find(preview, "ThumbnailImage")).IsVisible,
                "Manual narrowing retains thumbnail and details directly below the link");
            HeadingFirst("manual narrow fallback");
            Capture(window, Path.Combine(captures, "media-manual-narrow.png"));
            model.Url = ""; await Settle();
            Check(Math.Abs(window.Width - 820) <= 1, "Automatic collapse respects an explicit manual resize");
            window.Width = 740;
            window.Left = work.Right - 760;
            double edgeLeft = window.Left;
            await Input("https://youtu.be/jNQXAC9IVRw");
            Check(window.Left >= work.Left && window.Left + window.Width <= work.Right && window.Left < edgeLeft,
                "Expansion near the screen edge moves only enough to keep the right pane inside the work area");
            model.Url = ""; await Settle();
            Check(Math.Abs(window.Left - edgeLeft) <= 1 && window.Width == 740,
                "Collapse restores the original position after screen-edge containment");
            window.Left = left;
            await Input("https://www.youtube.com/shorts/jNQXAC9IVRw");
            model.AddQueueCommand.Execute(null); model.DismissNoticeCommand.Execute(null);
            await model.QueueStartCommand.ExecuteAsync(null); await Settle();
            Check(model.QueueItems.Single().HasCompletedOutput && model.Url == "" && window.Width == 740 && !context.IsVisible,
                "Queue completion matches the canonical Short URL and restores the narrow frame");
            foreach (string locale in new[] { "en", "ar" })
            {
                model.SelectLanguageCommand.Execute(locale);
                await InspectQueueModal(window, model, async dialog =>
                {
                    var open = Descendants<Button>(dialog).Single(button => button.Name == "QueueOpenFolder");
                    Check(open.IsVisible && open.IsEnabled && Equals(open.Content, model.OpenFolderLabel) && Equals(open.ToolTip, folder) &&
                        open.Command.CanExecute(open.CommandParameter), "Completed queue items expose their real output folder in " + locale);
                    var status = Descendants<TextBlock>(dialog).Single(label => label.Text == model.QueueItems.Single().Status);
                    var queueViewport = (ScrollViewer)WpfTree.Find(dialog, "QueueViewport");
                    var statusBounds = status.TransformToAncestor(queueViewport).TransformBounds(new Rect(status.RenderSize));
                    Check(statusBounds.Top >= 0 && statusBounds.Bottom <= queueViewport.ActualHeight,
                        "A completed queue opens with its result and folder action in view");
                    Capture(dialog, Path.Combine(captures, "media-queue-completed-" + locale + ".png"));
                    await Task.CompletedTask;
                });
            }
            File.Delete(file); Directory.Delete(folder);
            Check(!model.OpenQueueFolderCommand.CanExecute(model.QueueItems.Single().Id), "A deleted output folder cannot be opened by a stale queue action");
            HeadingFirst("queue finished");
        }
        finally
        {
            model.CancelCommand.Execute(null);
            provider.DownloadHandler = null; provider.DownloadDelayMilliseconds = 10000;
            foreach (var entry in model.Queue.Entries) model.RemoveQueueCommand.Execute(entry.Id);
            model.Url = ""; model.SettingsVisible = false; model.Folder = original.Folder; model.Audio = original.Audio;
            model.SelectLanguageCommand.Execute(original.IsArabic ? "ar" : "en");
            if (model.IsDark != original.IsDark) model.ThemeCommand.Execute(null);
            window.Width = original.Width; window.Height = original.Height; window.Left = original.Left; window.Top = original.Top;
        }
    }
}
