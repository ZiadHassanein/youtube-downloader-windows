using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YouTubeDownloader;

internal static partial class Program
{
    private static int count;
    private static void Check(bool value, string name)
    {
        if (!value)
            throw new Exception(name);
        count++;
        Console.WriteLine("PASS: " + name);
    }

    [STAThread]
    public static int Main(string[] args)
    {
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/ViFree;component/Themes/Controls.xaml") });
        string root = System.IO.Path.Combine(AppContext.BaseDirectory, "wpf-test-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(root);
        Exception failure = null;
        var provider = new FakeProvider();
        var service = new DownloadService(provider, new EmptyStorage());
        var thumbnailLoader = new FixtureThumbnailLoader();
        var window = new MainWindow(service, new SettingsRepository(root), thumbnailLoader);
        window.ContentRendered += async (_, _) =>
        {
            try
            {
                var model = (MainViewModel)window.DataContext;
                await Task.Delay(100);
                window.UpdateLayout();
                Check(window.Title == "ViFree" && model.Url == "" && model.IsDark, "WPF starts with an English title, dark theme and empty URL");
                Check(window.Height <= SystemParameters.WorkArea.Height && window.ActualHeight < 900, "Native window fits content within the work area");
                var captures = args.FirstOrDefault(arg => !arg.StartsWith("--")) ?? root;
                System.IO.Directory.CreateDirectory(captures);
                if (args.Contains("--header"))
                {
                    await CheckHeader(window, model, captures);
                    Console.WriteLine(count + " WPF header checks passed.");
                    return;
                }
                if (args.Contains("--options-modal"))
                {
                    await CheckOutputOptionsModal(window, model, provider, captures);
                    Console.WriteLine(count + " WPF options-modal checks passed.");
                    return;
                }
                if (args.Contains("--maintenance"))
                {
                    await CheckDownloaderMaintenance(window, model, provider, captures);
                    Console.WriteLine(count + " WPF maintenance checks passed.");
                    return;
                }
                if (args.Contains("--redesign"))
                {
                    await CheckRedesign(window, model, provider, thumbnailLoader, captures);
                    Console.WriteLine(count + " WPF redesign checks passed.");
                    return;
                }
                if (args.Contains("--audio-popup"))
                {
                    window.Width = 820;
                    model.Audio = true;
                    await QualityPopupChecks.Run(window, model, captures, Check);
                    Console.WriteLine(count + " WPF audio-popup checks passed.");
                    return;
                }
                foreach (string language in new[]
                {
                    "en",
                    "ar"
                }

                )
                    foreach (bool dark in new[]
                    {
                        true,
                        false
                    }

                    )
                    {
                        if (model.IsArabic != (language == "ar"))
                            model.LanguageCommand.Execute(null);
                        if (model.IsDark != dark)
                            model.ThemeCommand.Execute(null);
                        await window.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                        await Task.Delay(30);
                        window.UpdateLayout();
                        var field = (TextBox)WpfTree.Find(window, "LinkInput");
                        Check(field.FlowDirection == FlowDirection.LeftToRight && window.FlowDirection == FlowDirection.LeftToRight, "URL editor and branding shell retain physical LTR in " + language);
                        Check(model.ContentDirection == (language == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight), "Task content direction follows " + language);
                        var placeholder = (TextBlock)WpfTree.Find(window, "LinkPlaceholder");
                        field.Focus();
                        field.CaretIndex = 0;
                        window.UpdateLayout();
                        var caret = field.GetRectFromCharacterIndex(0);
                        var hintBounds = placeholder.TransformToVisual(field).TransformBounds(new Rect(placeholder.RenderSize));
                        Check(!caret.IsEmpty && Math.Abs(caret.Left - hintBounds.Left) <= 1 &&
                            placeholder.Visibility == Visibility.Visible && placeholder.FlowDirection == model.ContentDirection,
                            "Empty-link caret and placeholder use the same text inset in " + language + (dark ? " dark" : " light") +
                            $" (caret={caret}, hint={hintBounds}, visibility={placeholder.Visibility}, direction={placeholder.FlowDirection})");
                        Capture(window, System.IO.Path.Combine(captures, "placeholder-" + language + (dark ? "-dark" : "-light") + ".png"));
                        field.Text = "h";
                        field.CaretIndex = 1;
                        window.UpdateLayout();
                        await window.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                        var firstCharacter = field.GetRectFromCharacterIndex(0);
                        Check(placeholder.Visibility == Visibility.Collapsed && model.Url == "h" && field.CaretIndex == 1 &&
                            Math.Abs(firstCharacter.Left - caret.Left) <= 1,
                            "Typing replaces the hint without moving the text origin in " + language + (dark ? " dark" : " light"));
                        Capture(window, System.IO.Path.Combine(captures, "typed-" + language + (dark ? "-dark" : "-light") + ".png"));
                        field.Clear();
                        window.UpdateLayout();
                        Check(placeholder.Visibility == Visibility.Visible && model.Url == "",
                            "Clearing restores the localized hint in " + language + (dark ? " dark" : " light"));
                        Capture(window, System.IO.Path.Combine(captures, language + (dark ? "-dark" : "-light") + ".png"));
                    }

                await CheckHeader(window, model, captures);
                await CheckOutputOptionsModal(window, model, provider, captures);
                await CheckRedesign(window, model, provider, thumbnailLoader, captures);
                await MaximizeChecks(window, model, captures);
                window.Width = 820;
                await Task.Delay(50);
                window.UpdateLayout();
                var quality = (ComboBox)WpfTree.Find(window, "QualityInput");
                Check(quality.ActualWidth > 140 && quality.ActualHeight >= 44, "Quality control remains usable at minimum width");
                model.Url = "https://youtu.be/jNQXAC9IVRw";
                await Task.Delay(800);
                for (int wait = 0; wait < 20 && !model.CanDownload; wait++) await Task.Delay(100);
                Check(model.CanDownload && model.Qualities.Count == 2 && model.SelectedQuality.Height == 720,
                    $"Automatic inspection offers actual qualities and recommends 720p (ready={model.CanDownload}, audio={model.Audio}, count={model.Qualities.Count}, height={model.SelectedQuality?.Height}, status={model.StatusHeading})");
                model.Audio = true;
                Check(model.Qualities.Any(q => q.Bitrate == 192) && model.Qualities.Any(q => q.Bitrate == 320), "MP3 encoding choices survive the WPF port");
                await QualityPopupChecks.Run(window, model, captures, Check);
                await CheckDownloaderMaintenance(window, model, provider, captures);
                await Task.Delay(180);
                window.UpdateLayout();
                Capture(window, System.IO.Path.Combine(captures, "minimum-audio.png"));
                model.WholePlaylist = false;
                model.Audio = false;
                var task = model.DownloadCommand.ExecuteAsync(null);
                await Task.Delay(80);
                model.CancelCommand.Execute(null);
                await task;
                Check(model.IsIdle && model.Url.Length > 0, "Confirmed cancellation restores command availability and preserves the link");
                model.Url = "https://www.youtube.com/playlist?list=PLFixture";
                await Task.Delay(800);
                Check(model.HasPlaylist && model.WholePlaylist && model.Qualities.Any(q => q.Height == 1080), "Playlist-only input expands scope and offers maximum-quality choices");
                await Task.Delay(50);
                window.UpdateLayout();
                Capture(window, System.IO.Path.Combine(captures, "expanded-work-area.png"));
                var monitor = new MonitorInformation { Size = Marshal.SizeOf<MonitorInformation>() };
                var nativeHandle = new WindowInteropHelper(window).Handle;
                bool hasMonitor = GetMonitorInfo(MonitorFromWindow(nativeHandle, 2), ref monitor);
                bool hasBounds = GetWindowRect(nativeHandle, out var nativeBounds);
                Check(hasMonitor && hasBounds && nativeBounds.Top >= monitor.Work.Top - 8 &&
                    nativeBounds.Bottom <= monitor.Work.Bottom + 8 &&
                    nativeBounds.Bottom - nativeBounds.Top <= monitor.Work.Bottom - monitor.Work.Top + 16,
                    "Expanded playlist/status content stays within its native monitor work area" +
                    $" (actual={window.ActualHeight}, requested={window.Height}, primaryArea={SystemParameters.WorkArea.Height}, " +
                    $"nativeTop={nativeBounds.Top}, nativeBottom={nativeBounds.Bottom}, workTop={monitor.Work.Top}, workBottom={monitor.Work.Bottom})");
                model.Folder = System.IO.Path.Combine(root, "missing-folder");
                model.OpenFolderCommand.Execute(null);
                Check(model.NoticeVisible, "Folder failure shows an in-client notice");
                await Task.Delay(60);
                window.UpdateLayout();
                Capture(window, System.IO.Path.Combine(captures, "playlist-notice.png"));
                var notice = (FrameworkElement)WpfTree.Find(window, "Notice");
                var page = (FrameworkElement)WpfTree.Find(window, "Root");
                var position = notice.TranslatePoint(new Point(), page);
                var bounds = notice.TransformToAncestor(page).TransformBounds(new Rect(0, 0, notice.ActualWidth, notice.ActualHeight));
                Check(bounds.Left >= 0 && bounds.Top >= 0 && bounds.Right <= page.ActualWidth + 1 && bounds.Bottom <= page.ActualHeight + 1, "Transient notice remains inside client borders");
                var viewport = (ScrollViewer)WpfTree.Find(window, "Viewport");
                foreach (string language in new[] { "en", "ar" })
                {
                    if (model.IsArabic != (language == "ar")) model.LanguageCommand.Execute(null);
                    foreach (bool dark in new[] { true, false })
                    {
                        if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                        await window.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                        // Keep the expanded fixture scrolled, independently of language-specific line heights.
                        window.Height = 540;
                        viewport.MaxHeight = double.PositiveInfinity;
                        window.UpdateLayout();
                        var bar = (ScrollBar)viewport.Template.FindName("PART_VerticalScrollBar", viewport);
                        var track = (Track)bar.Template.FindName("PART_Track", bar);
                        var body = (FrameworkElement)WpfTree.Find(window, "PageContent");
                        var barBounds = bar.TransformToAncestor(page).TransformBounds(new Rect(0, 0, bar.ActualWidth, bar.ActualHeight));
                        var bodyBounds = body.TransformToAncestor(page).TransformBounds(new Rect(0, 0, body.ActualWidth, body.ActualHeight));
                        Capture(window, System.IO.Path.Combine(captures, "scroll-" + language + (dark ? "-dark" : "-light") + ".png"));
                        Check(bar.Visibility == Visibility.Visible && Math.Abs(bar.ActualWidth - 16) < 1 && track.Thumb.ActualHeight < track.ActualHeight &&
                            (language == "ar" ? bodyBounds.Left - barBounds.Right >= 9 : barBounds.Left - bodyBounds.Right >= 9),
                            "Scrollbar reserves a separate hit target with a proportional thumb in " + language + (dark ? " dark" : " light") +
                            $" (visibility={bar.Visibility}, width={bar.ActualWidth}, thumb={track.Thumb.ActualHeight}, track={track.ActualHeight}, bar={barBounds}, content={bodyBounds})");
                        viewport.ScrollToTop();
                        window.UpdateLayout();
                        var presenter = (FrameworkElement)viewport.Template.FindName("PART_ScrollContentPresenter", viewport);
                        var input = (FrameworkElement)WpfTree.Find(window, "LinkInput");
                        var inputBounds = input.TransformToAncestor(presenter).TransformBounds(new Rect(input.RenderSize));
                        Check(inputBounds.Left >= -0.5 && inputBounds.Right <= presenter.ActualWidth + .5,
                            "The full input border remains inside the scroll viewport in " + language + (dark ? " dark" : " light"));
                        Capture(window, System.IO.Path.Combine(captures, "scroll-" + language + (dark ? "-dark" : "-light") + ".png"));
                    }
                }
                viewport.PageDown();
                window.UpdateLayout();
                var activeBar = (ScrollBar)viewport.Template.FindName("PART_VerticalScrollBar", viewport);
                Check(viewport.VerticalOffset > 0 && Math.Abs(activeBar.Value - viewport.VerticalOffset) < 1, "Paging scrolls content and keeps the scrollbar value synchronized");
                viewport.ScrollToEnd();
                window.UpdateLayout();
                Check(Math.Abs(viewport.VerticalOffset - viewport.ScrollableHeight) < 1, "The bottom footer is reachable through the refined scrollbar");
                Capture(window, System.IO.Path.Combine(captures, "scroll-footer.png"));
                await Task.Delay(5200);
                Check(!model.NoticeVisible, "Transient notices disappear after five seconds");
                Console.WriteLine(count + " WPF checks passed.");
            }
            catch (Exception ex)
            {
                failure = ex;
                Console.Error.WriteLine("UI test failure: " + ex);
            }
            finally
            {
                window.Close();
            }
        };
        app.Run(window);
        if (failure != null)
        {
            Console.Error.WriteLine(failure);
            return 1;
        }

        return 0;
    }

    private static void Capture(Window window, string file)
    {
        var dpi = VisualTreeHelper.GetDpi(window);
        GetClientRect(new WindowInteropHelper(window).Handle, out var client);
        var bitmap = new RenderTargetBitmap(client.Right - client.Left, client.Bottom - client.Top, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = System.IO.File.Create(file);
        png.Save(output);
    }

    private static async Task MaximizeChecks(Window window, MainViewModel model, string captures)
    {
        double normalWidth = window.Width;
        window.WindowState = WindowState.Maximized;
        await Task.Delay(120);
        var handle = new WindowInteropHelper(window).Handle;
        var info = new MonitorInformation { Size = Marshal.SizeOf<MonitorInformation>() };
        Check(GetMonitorInfo(MonitorFromWindow(handle, 2), ref info), "The native monitor work area is available for maximization checks");
        foreach (string language in new[] { "en", "ar" })
            foreach (bool dark in new[] { true, false })
            {
                if (model.IsArabic != (language == "ar")) model.LanguageCommand.Execute(null);
                if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                await window.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                window.UpdateLayout();
                GetWindowRect(handle, out var bounds);
                Check(window.WindowState == WindowState.Maximized && double.IsPositiveInfinity(window.MaxHeight) &&
                    bounds.Left <= info.Work.Left + 1 && bounds.Top <= info.Work.Top + 1 && bounds.Right >= info.Work.Right - 1 && bounds.Bottom >= info.Work.Bottom - 1,
                    "Maximization fills the native work area in " + language + (dark ? " dark" : " light"));
                var root = (FrameworkElement)WpfTree.Find(window, "Root");
                var viewport = (ScrollViewer)WpfTree.Find(window, "Viewport");
                var footer = (FrameworkElement)WpfTree.Find(window, "FooterContent");
                // Small desktops can require scrolling even when the window is maximized.
                viewport.ScrollToEnd();
                window.UpdateLayout();
                var footerBounds = footer.TransformToAncestor(root).TransformBounds(new Rect(0, 0, footer.ActualWidth, footer.ActualHeight));
                double center = root.PointToScreen(new Point(root.ActualWidth / 2, 0)).X;
                Check(root.ActualWidth >= window.MinWidth - 1 && Math.Abs(center - (info.Work.Left + info.Work.Right) / 2.0) <= 2 &&
                    Math.Abs(footerBounds.Bottom - root.ActualHeight) <= 2,
                    "The split workspace fills the window and keeps its footer anchored outside the scrolling task" +
                    $" (footerBottom={footerBounds.Bottom}, viewportHeight={viewport.ActualHeight}, scrollableHeight={viewport.ScrollableHeight})");
                viewport.ScrollToTop();
                window.UpdateLayout();
                Capture(window, System.IO.Path.Combine(captures, "maximized-" + language + (dark ? "-dark" : "-light") + ".png"));
            }
        window.WindowState = WindowState.Normal;
        await Task.Delay(100);
        window.UpdateLayout();
        var page = (FrameworkElement)WpfTree.Find(window, "PageContent");
        GetWindowRect(handle, out var restoredBounds);
        Check(Math.Abs(window.Width - normalWidth) <= 1 && page.MinHeight == 0 &&
            restoredBounds.Top >= info.Work.Top - 8 && restoredBounds.Bottom <= info.Work.Bottom + 8,
            $"Restoring returns to content-fit sizing without inheriting the maximized height (state={window.WindowState}, width={window.Width}, height={window.ActualHeight}, pageMin={page.MinHeight})");
        Capture(window, System.IO.Path.Combine(captures, "restored.png"));
        window.WindowState = WindowState.Minimized;
        await Task.Delay(60);
        Check(Math.Abs(window.RestoreBounds.Width - normalWidth) <= 1, "Minimizing preserves the normal restore width");
        window.WindowState = WindowState.Normal;
        await Task.Delay(100);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeBounds { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInformation { public int Size; public NativeBounds Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr window, out NativeBounds bounds);
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr window, out NativeBounds bounds);
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInformation info);

    private sealed class EmptyStorage : IPlaylistStorage
    {
        public PlaylistSession Open(DownloadOptions options) => new()
        {
            Folder = options.Folder,
            Completed = new(),
            Save = () =>
            {
            }
        };
        public bool IsCompleted(string folder, string file, DownloadOptions options, bool requireCaptions = false) => true;
    }

    private sealed class FakeProvider : IMediaProvider, IMediaMaintenance
    {
        public string Id => "youtube";
        public bool IsReady => true;
        public string ThumbnailAddress = "https://example.test/thumbnail.png";
        public int DownloadDelayMilliseconds = 10000;
        public List<CaptionChoice> CaptionTracks = new();
        public Action<CancellationToken> InspectionHook;
        public Func<IProgress<DownloadProgress>, CancellationToken, Task<MediaToolUpdateResult>> MaintenanceHandler;
        public Task<MediaToolUpdateResult> RefreshToolsAsync(IProgress<DownloadProgress> progress, CancellationToken token) => MaintenanceHandler(progress, token);

        public Task EnsureReadyAsync(IProgress<DownloadProgress> progress, CancellationToken token) => Task.CompletedTask;
        public string Normalize(string url, bool playlist) => VideoLink.Normalize(url, playlist);
        public MediaTargets ResolveTargets(string input)
        {
            var link = VideoLink.Parse(input);
            return new(link.VideoUrl, link.PlaylistUrl);
        }

        public VideoInfo Inspect(string url, CancellationToken token, IProgress<DownloadProgress> progress = null)
        {
            InspectionHook?.Invoke(token);
            return new()
            {
                Title = "Local UI fixture",
                Uploader = "Local test fixture",
                Duration = 504,
                ThumbnailUrl = ThumbnailAddress,
                Captions = CaptionTracks,
                HasAudio = true,
                Qualities = new()
                {
                    new()
                    {
                        Resolution = 720,
                        SelectionKey = "hd"
                    },
                    new()
                    {
                        Resolution = 480,
                        SelectionKey = "sd"
                    }
                }
            };
        }
        public PlaylistInfo InspectPlaylist(string url, CancellationToken token, IProgress<DownloadProgress> progress = null) => new()
        {
            Id = "PLFixture",
            Url = url,
            Title = "Local playlist fixture",
            Entries = new()
            {
                new()
                {
                    Id = "jNQXAC9IVRw",
                    MediaUrl = "https://www.youtube.com/watch?v=jNQXAC9IVRw",
                    Title = "Local fixture"
                }
            }
        };
        public DownloadResult DownloadItem(DownloadOptions options, CancellationToken token, IProgress<DownloadProgress> progress = null)
        {
            progress?.Report(new DownloadProgress { Stage = "Downloading", Detail = "Local fixture only", Percent = 20 });
            token.WaitHandle.WaitOne(DownloadDelayMilliseconds);
            token.ThrowIfCancellationRequested();
            return new()
            {
                FilePath = System.IO.Path.Combine(options.Folder, "fixture.mp4")
            };
        }

        public void Cancel()
        {
        }

        public void Dispose()
        {
        }
    }
}
