using System.Windows;
using System.Windows.Controls;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckWindowLayout(MainWindow window, MainViewModel model, FakeProvider provider,
        FixtureThumbnailLoader thumbnailLoader, string captures)
    {
        model.SettingsVisible = false;
        model.Audio = false;
        model.Url = "";
        // Other full-suite scenarios deliberately resize the same host. Start this matrix from its own normal frame.
        window.Width = 740;
        double startupWidth = window.Width;
        double normalHeight = Math.Min(800, window.CurrentWorkArea().Height - 16);
        window.Height = normalHeight;
        var viewport = (ScrollViewer)WpfTree.Find(window, "Viewport");
        var form = (FrameworkElement)WpfTree.Find(window, "DownloadForm");
        var footer = (FrameworkElement)WpfTree.Find(window, "FooterContent");
        var download = (Button)WpfTree.Find(window, "DownloadButton");
        string[] anchors = { "LinkInput", "QualityInput", "MoreOptionsButton", "DestinationPicker", "DownloadButton" };
        Rect Frame() => new(window.Left, window.Top, window.ActualWidth, window.ActualHeight);
        double[] Rows() => anchors.Select(name => ((FrameworkElement)WpfTree.Find(window, name)).TranslatePoint(new Point(), form).Y).ToArray();
        async Task Settle()
        {
            await Task.Delay(220);
            window.UpdateLayout();
        }
        async Task Stable(Func<Task> action, string label, bool mediaTransition = false)
        {
            var before = Frame();
            int heightChanges = 0;
            void Changed(object sender, SizeChangedEventArgs args) { if (args.HeightChanged) heightChanges++; }
            window.SizeChanged += Changed;
            try { await action(); await Settle(); }
            finally { window.SizeChanged -= Changed; }
            var after = Frame();
            Check(heightChanges == 0 && Math.Abs(after.Height - before.Height) <= 1 &&
                Math.Abs(after.Top - before.Top) <= 1 && (mediaTransition || Math.Abs(after.Width - before.Width) <= 1),
                "Window height and top stay stable through " + label);
        }
        async Task Language(string language, string state)
        {
            await Stable(() => { model.SelectLanguageCommand.Execute(language); return Task.CompletedTask; }, state + " language " + language);
        }
        void FooterAnchored()
        {
            var root = (FrameworkElement)WpfTree.Find(window, "Root");
            var bounds = footer.TransformToAncestor(root).TransformBounds(new Rect(footer.RenderSize));
            Check(Math.Abs(bounds.Bottom - root.ActualHeight) <= 1 &&
                Math.Abs(((FrameworkElement)WpfTree.Find(window, "FormContent")).MinHeight - viewport.ViewportHeight) <= 1,
                "The footer stays outside the scrolling content and the workspace fills its viewport");
        }
        void CompactFormFits(string state)
        {
            var root = (FrameworkElement)WpfTree.Find(window, "Root");
            Rect Bounds(FrameworkElement element) => element.TransformToAncestor(root).TransformBounds(new Rect(element.RenderSize));
            var formBounds = Bounds(form);
            var fields = Bounds((FrameworkElement)WpfTree.Find(window, "FolderAnchor"));
            var action = Bounds(download);
            var brand = Bounds((FrameworkElement)WpfTree.Find(window, "BrandWordmark"));
            var utilities = Bounds((FrameworkElement)WpfTree.Find(window, "HeaderActions"));
            Check(form.ActualWidth <= 621 && Math.Abs(download.ActualWidth - 280) <= 1 &&
                Math.Abs((action.Left + action.Right) / 2 - (fields.Left + fields.Right) / 2) <= 1 &&
                anchors.All(name => { var bounds = Bounds((FrameworkElement)WpfTree.Find(window, name));
                    return bounds.Left >= fields.Left - 1 && bounds.Right <= fields.Right + 1; }) &&
                ((FrameworkElement)WpfTree.Find(window, "LinkInput")).ActualWidth >= 200 &&
                ((FrameworkElement)WpfTree.Find(window, "QualityInput")).ActualWidth >= 140 &&
                brand.Right < utilities.Left && utilities.Right <= root.ActualWidth - 23,
                "The bounded form, shorter Download action and full header fit in " + state +
                $" (form={formBounds}, action={action}, arabic={model.IsArabic}, brand={brand}, utilities={utilities}, root={root.RenderSize}, anchors={string.Join(';', anchors.Select(name => name + '=' + Bounds((FrameworkElement)WpfTree.Find(window, name))))})");
        }

        foreach (double width in new[] { startupWidth, 1160d, window.MinWidth }.Distinct())
            foreach (bool dark in new[] { false, true })
            {
                window.Width = width;
                if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                model.Url = "";
                model.SelectLanguageCommand.Execute("en");
                await Settle();
                viewport.ScrollToTop();
                var emptyRows = Rows();
                string variant = (dark ? "-dark" : "-light") + (width == startupWidth ? "-default" : width < 1000 ? "-compact" : "-wide");
                foreach (string language in new[] { "en", "ar", "en", "ar" })
                {
                    await Language(language, "empty" + variant);
                    Check(Rows().Zip(emptyRows).All(pair => Math.Abs(pair.First - pair.Second) <= 1),
                        "Form rows keep their vertical positions after repeated language changes: " + language + variant);
                    CompactFormFits("empty " + language + variant);
                    Capture(window, System.IO.Path.Combine(captures, "layout-empty-" + language + variant + ".png"));
                    if (normalHeight >= 799)
                        Check(viewport.ScrollableHeight <= 1 && download.TranslatePoint(new Point(0, download.ActualHeight), viewport).Y <= viewport.ActualHeight + 1,
                            "The complete idle form and Download action fit at the default height: " + language + variant +
                            $" (height={window.ActualHeight}, viewport={viewport.ActualHeight}, extent={viewport.ExtentHeight}, form={form.ActualHeight}, downloadBottom={download.TranslatePoint(new Point(0, download.ActualHeight), viewport).Y})");
                    else
                    {
                        viewport.ScrollToEnd();
                        window.UpdateLayout();
                        var actionBounds = download.TransformToAncestor(viewport).TransformBounds(new Rect(download.RenderSize));
                        Check(actionBounds.Top >= 0 && actionBounds.Bottom <= viewport.ActualHeight + 1 &&
                            window.ActualHeight <= window.CurrentWorkArea().Height,
                            "A work-area-constrained idle window keeps Download reachable by scrolling: " + language + variant);
                        viewport.ScrollToTop();
                        window.UpdateLayout();
                    }
                    FooterAnchored();
                }

                await Stable(async () => { model.Url = "https://youtu.be/jNQXAC9IVRw"; await Task.Delay(850); }, "video inspection" + variant, true);
                Check(model.HasMediaPreview && model.CanDownload, "The real media context appears beside the form" + variant);
                model.SelectLanguageCommand.Execute("en");
                await Settle();
                var loadedRows = Rows();
                foreach (string language in new[] { "en", "ar" })
                {
                    await Language(language, "video" + variant);
                    viewport.ScrollToTop();
                    window.UpdateLayout();
                    Check(Rows().Zip(loadedRows).All(pair => Math.Abs(pair.First - pair.Second) <= 1),
                        "Loaded form rows stay aligned in both languages: " + language + variant);
                    CompactFormFits("video " + language + variant);
                    Capture(window, System.IO.Path.Combine(captures, "layout-video-" + language + variant + ".png"));
                }

                await Stable(async () => { model.Url = "https://www.youtube.com/playlist?list=PLFixture"; await Task.Delay(850); }, "playlist inspection" + variant);
                foreach (string language in new[] { "en", "ar" })
                {
                    await Language(language, "playlist" + variant);
                    viewport.ScrollToEnd();
                    window.UpdateLayout();
                    var bounds = download.TransformToAncestor(viewport).TransformBounds(new Rect(download.RenderSize));
                    Check(model.HasPlaylist && bounds.Top >= 0 && bounds.Bottom <= viewport.ActualHeight + 1,
                        "Playlist overflow scrolls internally and keeps Download reachable: " + language + variant);
                    CompactFormFits("playlist " + language + variant);
                    viewport.ScrollToTop();
                    window.UpdateLayout();
                    Capture(window, System.IO.Path.Combine(captures, "layout-playlist-" + language + variant + ".png"));
                    FooterAnchored();
                }
                await Stable(() => { model.Url = ""; return Task.CompletedTask; }, "clearing media" + variant, true);
                await Stable(() => { model.SettingsVisible = true; return Task.CompletedTask; }, "opening Settings" + variant);
                await Stable(() => { model.SettingsVisible = false; return Task.CompletedTask; }, "closing Settings" + variant);
            }

        window.Width = window.MinWidth;
        window.Height = 540;
        await Settle();
        foreach (string language in new[] { "en", "ar" })
        {
            await Language(language, "manually resized window");
            Check(Math.Abs(window.Height - 540) <= 1 && viewport.ScrollableHeight > 0, "The app respects a user's shorter window and scrolls its body");
            download.BringIntoView();
            await Settle();
            var bounds = download.TransformToAncestor(viewport).TransformBounds(new Rect(download.RenderSize));
            Check(bounds.Top >= 0 && bounds.Bottom <= viewport.ActualHeight + 1, "Keyboard navigation can reveal the main action in a short " + language + " window");
            FooterAnchored();
            Capture(window, System.IO.Path.Combine(captures, "layout-short-" + language + ".png"));
        }

        window.Width = 1160;
        window.Height = normalHeight;
        await Settle();
        var artwork = new TaskCompletionSource<System.Windows.Media.ImageSource>(TaskCreationOptions.RunContinuationsAsynchronously);
        thumbnailLoader.Handler = (_, _) => artwork.Task;
        try
        {
            await Stable(async () => { model.Url = "https://youtu.be/jNQXAC9IVRx"; await Task.Delay(850); }, "inspection with delayed artwork");
            Check(model.CanDownload && model.PreviewImage == null, "Ready media does not wait for optional artwork");
            var image = await new FixtureThumbnailLoader().LoadAsync("", CancellationToken.None);
            await Stable(async () => { artwork.SetResult(image); await Task.Delay(250); }, "late artwork arrival");
            Check(model.PreviewImage != null && ((FrameworkElement)WpfTree.Find(window, "PreviewContent")).Opacity == 1,
                "Artwork fills its reserved frame without replaying the panel entrance");
        }
        finally
        {
            thumbnailLoader.Handler = null;
            artwork.TrySetCanceled();
            model.Url = "";
        }
        try
        {
            foreach (double width in new[] { 1160d, window.MinWidth })
                foreach (string language in new[] { "en", "ar" })
                {
                    window.Width = width;
                    model.Url = "";
                    model.SelectLanguageCommand.Execute(language);
                    await Settle();
                    provider.VideoTitle = string.Join(" ", Enumerable.Repeat(language == "ar" ?
                        "عنوان تجريبي طويل لاختبار عرض الفيديو" : "Local long video title fixture for layout testing", 8));
                    await Stable(async () => { model.Url = "https://youtu.be/jNQXAC9IVRz"; await Task.Delay(850); },
                        "long " + language + " media title at width " + width, true);
                    var title = (TextBlock)WpfTree.Find(window, "PreviewTitle");
                    Check(title.ActualHeight <= title.LineHeight * (model.InlineMediaPreview ? 2 : 3) + 1 &&
                        Equals(title.ToolTip, provider.VideoTitle) && viewport.ScrollableWidth == 0,
                        "Long titles stay bounded with the complete title available: " + language + "/" + width);
                    viewport.ScrollToTop();
                    window.UpdateLayout();
                    Capture(window, System.IO.Path.Combine(captures, "layout-long-title-" + language + (width < 1000 ? "-compact" : "-wide") + ".png"));
                }
        }
        finally
        {
            provider.VideoTitle = "Local UI fixture";
            model.Url = "";
        }
        model.SelectLanguageCommand.Execute("en");
        window.Width = 1160;
        await Settle();
    }
}
