using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckDownloaderMaintenance(Window window, MainViewModel model, FakeProvider provider, string captures)
    {
        string url = model.Url, originalStatus = model.StatusHeading;
        var quality = model.SelectedQuality;
        bool checkedLifetime = false;
        foreach (bool arabic in new[] { false, true })
            foreach (bool dark in new[] { true, false })
                foreach (bool updated in new[] { false, true })
                {
                    if (model.IsArabic != arabic) model.LanguageCommand.Execute(null);
                    if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                    if (!updated)
                    {
                        var anchor = (Button)window.FindName("ToolRefreshAnchor");
                        var tip = (ToolTip)anchor.ToolTip;
                        tip.PlacementTarget = anchor;
                        tip.IsOpen = true;
                        await SettleMaintenance(window);
                        Check(((TextBlock)tip.Content).Text == model.ToolRefreshHint && tip.FlowDirection == model.ContentDirection &&
                            System.Windows.Automation.AutomationProperties.GetHelpText(anchor) == model.ToolRefreshHint,
                            "The downloader action explains when to use it, visually and accessibly, in " + (arabic ? "Arabic" : "English"));
                        var tipSource = (HwndSource)PresentationSource.FromVisual(tip);
                        GetClientRect(tipSource.Handle, out var tipBounds);
                        var dpi = VisualTreeHelper.GetDpi(tip);
                        var nativeSize = tipSource.CompositionTarget.TransformFromDevice.Transform(new Vector(tipBounds.Right, tipBounds.Bottom));
                        Check(tip.ActualWidth <= nativeSize.X + 1 && tip.ActualHeight <= nativeSize.Y + 1,
                            $"The explanation tooltip fits its native window after a language change (tip={tip.RenderSize}, native={nativeSize})");
                        var bitmap = new RenderTargetBitmap(tipBounds.Right, tipBounds.Bottom, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
                        bitmap.Render(tipSource.RootVisual);
                        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                        using (var output = System.IO.File.Create(System.IO.Path.Combine(captures, "maintenance-help-" + (arabic ? "ar" : "en") + (dark ? "-dark" : "-light") + ".png"))) encoder.Save(output);
                        tip.IsOpen = false;
                    }
                    // Presentation changes intentionally rebuild the localized quality choices.
                    quality = model.SelectedQuality;
                    var completion = new TaskCompletionSource<MediaToolUpdateResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                    IProgress<DownloadProgress> progress = null;
                    provider.MaintenanceHandler = (sink, token) => { progress = sink; return completion.Task.WaitAsync(token); };
                    var operation = model.ToolRefreshCommand.ExecuteAsync(null);
                    await SettleMaintenance(window);
                    string variant = (arabic ? "ar" : "en") + (dark ? "-dark" : "-light");
                    Check(model.IsRefreshingTools && model.IsBusy && model.NoticeVisible && !model.ToolRefreshCommand.CanExecute(null),
                        "Maintenance immediately explains its busy state and blocks duplicate requests in " + variant);
                    var notice = (FrameworkElement)window.FindName("Notice");
                    Check(ReferenceEquals(notice.Parent, window.FindName("ToolNoticeHost")), "Maintenance feedback stays beside its own action in " + variant);
                    if (!checkedLifetime)
                    {
                        await Task.Delay(5200);
                        Check(model.NoticeVisible, "Active maintenance progress stays visible until the operation finishes");
                        checkedLifetime = true;
                    }
                    progress.Report(new DownloadProgress { Detail = "Downloading the downloader update…", Percent = 50, Indeterminate = false });
                    await SettleMaintenance(window);
                    var bar = (ProgressBar)window.FindName("ToolRefreshProgressBar");
                    Check(bar.Visibility == Visibility.Visible && bar.Value == 50 && !bar.IsIndeterminate,
                        "Maintenance shows actual download progress in " + variant);
                    Capture(window, System.IO.Path.Combine(captures, "maintenance-" + variant + "-progress.png"));
                    completion.SetResult(new(updated, "2026.08.19"));
                    await operation;
                    await SettleMaintenance(window);
                    string expected = Texts.For(arabic ? "ar" : "en", updated ? "Downloader updated" : "Downloader is up to date");
                    Check(!model.IsRefreshingTools && model.IsIdle && model.NoticeVisible && model.NoticeHeading == expected && bar.Visibility == Visibility.Collapsed,
                        "Maintenance clearly distinguishes " + (updated ? "an installed update" : "no available update") + " in " + variant);
                    string result = model.NoticeDetail;
                    progress.Report(new DownloadProgress { Detail = "stale progress", Percent = 1 });
                    await SettleMaintenance(window);
                    Check(model.NoticeDetail == result && model.Url == url && ReferenceEquals(model.SelectedQuality, quality) && model.StatusHeading == originalStatus,
                        "Maintenance ignores late progress and preserves the link, quality and main status in " + variant);
                    var page = (FrameworkElement)window.FindName("Root");
                    var bounds = notice.TransformToAncestor(page).TransformBounds(new Rect(notice.RenderSize));
                    Check(bounds.Left >= -1 && bounds.Top >= -1 && bounds.Right <= page.ActualWidth + 1 && bounds.Bottom <= page.ActualHeight + 1,
                        "Maintenance result stays inside app borders in " + variant);
                    Capture(window, System.IO.Path.Combine(captures, "maintenance-" + variant + (updated ? "-updated" : "-current") + ".png"));
                }
        provider.MaintenanceHandler = (_, _) => Task.FromException<MediaToolUpdateResult>(new HttpRequestException("Fixture network failure"));
        await model.ToolRefreshCommand.ExecuteAsync(null);
        await SettleMaintenance(window);
        Check(model.IsIdle && !model.IsRefreshingTools && model.NoticeHeading == Texts.For(model.IsArabic ? "ar" : "en", "Could not update the downloader"),
            "Maintenance failure shows recovery feedback and restores the action");
        Capture(window, System.IO.Path.Combine(captures, "maintenance-failure.png"));
        provider.MaintenanceHandler = async (_, token) => { await Task.Delay(Timeout.Infinite, token); return new(false, ""); };
        var cancelled = model.ToolRefreshCommand.ExecuteAsync(null);
        model.CancelCommand.Execute(null);
        await cancelled;
        await SettleMaintenance(window);
        Check(model.IsIdle && !model.IsRefreshingTools && model.NoticeHeading == Texts.For(model.IsArabic ? "ar" : "en", "Downloader update cancelled"),
            "Cancelling maintenance gives a clear outcome and releases the operation");
        await Task.Delay(5200);
        Check(!model.NoticeVisible, "Finished maintenance feedback disappears after five seconds");
    }

    private static async Task SettleMaintenance(Window window)
    {
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        await Task.Delay(60);
        window.UpdateLayout();
    }
}
