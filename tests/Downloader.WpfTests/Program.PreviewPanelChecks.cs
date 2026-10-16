using System.Windows;
using System.Windows.Controls;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckPreviewPanel(Window window, MainViewModel model, FakeProvider provider, string captures)
    {
        model.Url = "";
        var pane = (FrameworkElement)WpfTree.Find(window, "ContextPane");
        var form = (FrameworkElement)WpfTree.Find(window, "DownloadForm");
        var inline = (FrameworkElement)WpfTree.Find(form, "InlineActivity");
        var surface = (Border)WpfTree.Find(window, "PreviewSurface");
        foreach (bool arabic in new[] { false, true })
        {
            if (model.IsArabic != arabic) model.LanguageCommand.Execute(null);
            foreach (bool dark in new[] { false, true })
            {
                if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                foreach (double width in new[] { 1160d, 820d })
                {
                    window.Width = width;
                    await Task.Delay(100);
                    window.UpdateLayout();
                    Check(!pane.IsVisible && !inline.IsVisible && Math.Abs(form.ActualWidth - form.MaxWidth) <= 1 && Grid.GetRow(form) == 0,
                        $"An empty link has one usable form and no preview or idle status: {arabic}/{dark}/{width} (pane={pane.IsVisible}, status={inline.IsVisible}, form={form.ActualWidth}, row={Grid.GetRow(form)})");
                    Check(Equals(surface.Background, window.Background), "Preview shares the window's theme background: " + arabic + "/" + dark);
                    await CaptureCompact(window, System.IO.Path.Combine(captures,
                        "preview-empty-" + (arabic ? "ar" : "en") + (dark ? "-dark" : "-light") + (width < 1000 ? "-compact" : "-wide") + ".png"));
                }
            }
        }
        foreach (double width in new[] { 1160d, 820d })
        {
            window.Width = width;
            model.SettingsVisible = true;
            await Task.Delay(100);
            Check(pane.IsVisible && ((FrameworkElement)WpfTree.Find(window, "ToolRefreshAnchor")).IsVisible,
                "Settings remains reachable without a media link at width " + width);
            model.SettingsVisible = false;
            Check(!pane.IsVisible, "Closing empty Settings restores the single form at width " + width);
        }
        model.Url = "unfinished text";
        await Task.Delay(550);
        Check(!pane.IsVisible, "Incomplete or unsupported text cannot reveal a placeholder preview");
        model.Url = "https://www.youtube.com/playlist?list=PLFixture";
        await Task.Delay(850);
        Check(pane.IsVisible && model.HasPlaylist && model.PreviewTitle == "Local playlist fixture",
            "An inspected playlist reveals its actual context");
        await CaptureCompact(window, System.IO.Path.Combine(captures, "preview-playlist-compact.png"));
        model.Url = "";
        Check(!pane.IsVisible, "Clearing a playlist removes its preview immediately");
        model.Url = "https://youtu.be/jNQXAC9IVRw";
        await Task.Delay(850);
        Check(pane.IsVisible && model.CanDownload, "An inspected video reveals its actual context");
        string folder = model.Folder;
        model.Folder = "relative-folder";
        await model.DownloadCommand.ExecuteAsync(null);
        model.Url = "";
        await Task.Delay(100);
        Check(!pane.IsVisible && inline.IsVisible && model.HasStatus &&
            Equals(((TextBlock)WpfTree.Find(inline, "StatusHeading")).Foreground, Application.Current.Resources["Error"]),
            "Errors stay readable in the form after the media link is cleared");
        inline.BringIntoView();
        await Task.Delay(250);
        window.UpdateLayout();
        Capture(window, System.IO.Path.Combine(captures, "preview-inline-error.png"));
        model.SettingsVisible = true;
        await Task.Delay(100);
        Check(!inline.IsVisible && WpfTree.Find(WpfTree.Find(window, "SettingsPane") as DependencyObject, "StatusHeading") == null,
            "Settings excludes media errors while preserving the failed download state");
        model.SettingsVisible = false;
        Check(inline.IsVisible && model.HasStatus,
            "Returning from Settings retains the media error and its recovery context");
        model.Folder = folder;
        model.Url = "https://youtu.be/jNQXAC9IVRw";
        await Task.Delay(850);
        provider.DownloadDelayMilliseconds = 30;
        try
        {
            await model.DownloadCommand.ExecuteAsync(null);
            await Task.Delay(100);
            Check(model.Url == "" && !pane.IsVisible && !inline.IsVisible && model.HasFile && model.HasRecentDownloadsBadge &&
                ((ProgressBar)WpfTree.Find(inline, "JobProgress")).Value == 100,
                "Successful downloads close the preview and signal completion through the recent badge");
            await Task.Delay(250);
            window.UpdateLayout();
            var fileAction = (Button)WpfTree.Find(inline, "RevealFile");
            var viewport = (FrameworkElement)WpfTree.Find(window, "Viewport");
            var fileBounds = fileAction.TransformToAncestor(viewport).TransformBounds(new Rect(fileAction.RenderSize));
            Check(!fileAction.IsVisible && ((Button)WpfTree.Find(window, "DownloadButton")).IsVisible,
                "Routine success removes the redundant completion block while retaining the main action");
            Capture(window, System.IO.Path.Combine(captures, "preview-inline-complete.png"));
            model.SettingsVisible = true;
            await Task.Delay(100);
            Check(WpfTree.Find(WpfTree.Find(window, "SettingsPane") as DependencyObject, "StatusHeading") == null && !inline.IsVisible && model.HasRecentDownloadsBadge,
                "Settings excludes completed media results and retains the recent-download badge");
            model.SettingsVisible = false;
        }
        finally
        {
            provider.DownloadDelayMilliseconds = 10000;
        }
        // Inspecting the next link clears the previous result and restores the normal form state.
        model.Url = "https://youtu.be/jNQXAC9IVRw";
        await Task.Delay(850);
        model.Url = "";
    }
}
