using System.Windows;
using System.Windows.Controls;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckRedesign(Window window, MainViewModel model, FakeProvider provider, FixtureThumbnailLoader thumbnailLoader, string captures)
    {
        if (model.IsArabic) model.LanguageCommand.Execute(null);
        model.Audio = false;
        window.Width = 1160;
        model.Url = "https://youtu.be/jNQXAC9IVRw";
        await Task.Delay(850);
        window.UpdateLayout();
        Check(model.PreviewTitle == "Local UI fixture" && model.PreviewDuration == "8:24" && model.PreviewImage?.IsFrozen == true,
            "Preview uses inspected media metadata and an immutable thumbnail");
        Check(model.CanDownload && model.PreviewStatus == "Ready to download", "Media preview follows actual download readiness");
        var form = (FrameworkElement)WpfTree.Find(window, "DownloadForm");
        var pane = (FrameworkElement)WpfTree.Find(window, "ContextPane");
        var root = (FrameworkElement)WpfTree.Find(window, "Root");
        var brand = (FrameworkElement)WpfTree.Find(window, "BrandWordmark");
        foreach (bool arabic in new[] { false, true })
        {
            if (model.IsArabic != arabic) model.LanguageCommand.Execute(null);
            foreach (bool dark in new[] { false, true })
            {
                if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                await Task.Delay(200);
                window.UpdateLayout();
                var formBounds = form.TransformToAncestor(root).TransformBounds(new Rect(form.RenderSize));
                var paneBounds = pane.TransformToAncestor(root).TransformBounds(new Rect(pane.RenderSize));
                var brandBounds = brand.TransformToAncestor(root).TransformBounds(new Rect(brand.RenderSize));
                Check((arabic ? paneBounds.Right <= formBounds.Left + 1 : formBounds.Right <= paneBounds.Left + 1) &&
                    brandBounds.Left < 100, "Task panes mirror while ViFree branding remains physically left");
                Check(form.ActualWidth > 500 && pane.ActualWidth > 400, "Split panes retain usable proportions");
                var link = (FrameworkElement)WpfTree.Find(window, "LinkInput");
                var paste = (FrameworkElement)WpfTree.Find(window, "PasteButton");
                var linkBounds = link.TransformToAncestor(root).TransformBounds(new Rect(link.RenderSize));
                var pasteBounds = paste.TransformToAncestor(root).TransformBounds(new Rect(paste.RenderSize));
                Check(arabic ? pasteBounds.Right <= linkBounds.Left + 1 : linkBounds.Right <= pasteBounds.Left + 1,
                    $"The link row mirrors its controls correctly (Arabic={arabic}, link={linkBounds}, paste={pasteBounds})");
                var heading = (TextBlock)WpfTree.Find(window, "NewDownloadHeading");
                var headingBounds = heading.TransformToAncestor(root).TransformBounds(new Rect(heading.RenderSize));
                var characterBounds = heading.TransformToAncestor(root).TransformBounds(heading.ContentStart.GetCharacterRect(System.Windows.Documents.LogicalDirection.Forward));
                Check(arabic ? Math.Abs(characterBounds.Right - headingBounds.Right) < 4 : Math.Abs(characterBounds.Left - headingBounds.Left) < 4,
                    $"The localized heading aligns to its physical reading edge (Arabic={arabic}, text={characterBounds}, heading={headingBounds})");
                Capture(window, System.IO.Path.Combine(captures, "redesign-" + (arabic ? "ar" : "en") + (dark ? "-dark" : "-light") + ".png"));
            }
        }
        model.SettingsCommand.Execute(null);
        await Task.Delay(220);
        Check(model.SettingsVisible && ((FrameworkElement)WpfTree.Find(window, "ToolRefreshAnchor")).IsVisible,
            "Settings exposes browser integration, app updates and downloader maintenance");
        model.NewDownloadCommand.Execute(null);
        Check(!model.SettingsVisible, "New download returns to the media context");
        foreach (double width in new[] { 1160d, 820d })
        {
            window.Width = width;
            await Task.Delay(100);
            var download = model.DownloadCommand.ExecuteAsync(null);
            await Task.Delay(120);
            model.SettingsVisible = true;
            await Task.Delay(220);
            window.UpdateLayout();
            var settings = (FrameworkElement)WpfTree.Find(window, "SettingsPane");
            var active = (FrameworkElement)WpfTree.Find(settings, "ActiveDownload");
            var cancel = (Button)WpfTree.Find(active, "CancelDownload");
            var progress = (ProgressBar)WpfTree.Find(active, "JobProgress");
            var cancelBounds = cancel.TransformToAncestor(root).TransformBounds(new Rect(cancel.RenderSize));
            Check(model.IsBusy && active.IsVisible && cancel.IsVisible && cancel.IsEnabled && progress.IsVisible && progress.Value == 20 &&
                cancelBounds.Top >= 0 && cancelBounds.Bottom <= root.ActualHeight,
                "Settings keeps active progress and cancellation visible at width " + width);
            Check(width >= 1000 || !form.IsVisible, "Compact Settings replaces the form rather than appending it");
            Capture(window, System.IO.Path.Combine(captures, width >= 1000 ? "settings-active-wide.png" : "settings-active-compact.png"));
            cancel.Command.Execute(null);
            await download;
            Check(model.IsIdle, "The Cancel action inside Settings actually stops the active download");
            model.NewDownloadCommand.Execute(null);
        }
        string originalFolder = model.Folder;
        model.SettingsVisible = true;
        model.Folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vifree-missing-" + Guid.NewGuid().ToString("N"));
        model.OpenFolderCommand.Execute(null);
        await Task.Delay(180);
        window.UpdateLayout();
        Check(!model.SettingsVisible && form.IsVisible && model.NoticeVisible, "Folder recovery reveals the compact form and its notice anchor");
        Capture(window, System.IO.Path.Combine(captures, "settings-folder-recovery.png"));
        model.Folder = originalFolder;
        model.DismissNoticeCommand.Execute(null);
        foreach (string address in new[] { "file:///C:/image.png", "http://example.com/i.png", "https://localhost/i", "https://127.0.0.1/i", "https://[::1]/i", "https://192.168.1.5/i", "https://10.1.2.3/i", "https://[fd00::1]/i", "https://user:password@example.com/i", "https://example.com:444/i" })
            Check(!MediaThumbnailLoader.IsSafeAddress(address, out _), "Artwork rejects unsafe addresses: " + address.Split('/')[0]);
        Check(MediaThumbnailLoader.IsSafeAddress("https://i.ytimg.com/vi/sample/hqdefault.jpg", out _), "Artwork accepts a public HTTPS image address");
        window.Width = 820;
        await Task.Delay(150);
        window.UpdateLayout();
        Check(Grid.GetRow(form) == 1 && Grid.GetColumn(pane) == 0, "Narrow windows use a compact preview above one form column");
        await CaptureCompact(window, System.IO.Path.Combine(captures, "redesign-compact-ar.png"));
        model.LanguageCommand.Execute(null);
        await Task.Delay(150);
        window.UpdateLayout();
        await CaptureCompact(window, System.IO.Path.Combine(captures, "redesign-compact-en.png"));
        model.Url = "";
        await Task.Delay(30);
        Check(model.PreviewImage == null && model.PreviewTitle != "Local UI fixture", "Editing the link clears stale media and artwork");
        var delayed = new TaskCompletionSource<System.Windows.Media.ImageSource>(TaskCreationOptions.RunContinuationsAsynchronously);
        thumbnailLoader.Handler = (_, _) => delayed.Task;
        provider.ThumbnailAddress = "https://example.test/slow.png";
        model.Url = "https://youtu.be/jNQXAC9IVRx";
        await Task.Delay(850);
        Check(model.CanDownload && model.PreviewImage == null, "Slow artwork never blocks a ready download");
        var staleImage = await new FixtureThumbnailLoader().LoadAsync("", CancellationToken.None);
        model.Url = "";
        thumbnailLoader.Handler = (_, _) => Task.FromException<System.Windows.Media.ImageSource>(new System.Net.Http.HttpRequestException("Fixture image failure"));
        provider.ThumbnailAddress = "https://example.test/missing.png";
        model.Url = "https://youtu.be/jNQXAC9IVRy";
        await Task.Delay(850);
        delayed.SetResult(staleImage);
        await Task.Delay(50);
        Check(model.CanDownload && model.PreviewImage == null && !model.HasStatus, "Failed and stale thumbnails cannot replace current media or disable downloads");
        thumbnailLoader.Handler = null;
        provider.ThumbnailAddress = "https://example.test/thumbnail.png";
        model.Url = "";
    }

    private static async Task CaptureCompact(Window window, string path)
    {
        var viewport = (ScrollViewer)WpfTree.Find(window, "Viewport");
        await window.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        viewport.ScrollToTop();
        window.UpdateLayout();
        await Task.Delay(250);
        await window.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
        Capture(window, path);
    }
}
