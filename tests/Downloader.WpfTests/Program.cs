using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YouTubeDownloader;

internal static class Program
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
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/YouTubeDownloader;component/Themes/Controls.xaml") });
        string root = System.IO.Path.Combine(AppContext.BaseDirectory, "wpf-test-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(root);
        Exception failure = null;
        var service = new DownloadService(new FakeProvider(), new EmptyStorage());
        var window = new MainWindow(service, new SettingsRepository(root));
        window.ContentRendered += async (_, _) =>
        {
            try
            {
                var model = (MainViewModel)window.DataContext;
                await Task.Delay(100);
                window.UpdateLayout();
                Check(window.Title == "YouTube Downloader" && model.Url == "" && model.IsDark, "WPF starts with an English title, dark theme and empty URL");
                Check(window.Height <= SystemParameters.WorkArea.Height && window.ActualHeight < 900, "Native window fits content within the work area");
                var captures = args.Length > 0 ? args[0] : root;
                System.IO.Directory.CreateDirectory(captures);
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
                        var field = (TextBox)window.FindName("LinkInput");
                        Check(field.FlowDirection == FlowDirection.LeftToRight && window.FlowDirection == FlowDirection.LeftToRight, "URL editor and branding shell retain physical LTR in " + language);
                        Check(model.ContentDirection == (language == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight), "Task content direction follows " + language);
                        Capture(window, System.IO.Path.Combine(captures, language + (dark ? "-dark" : "-light") + ".png"));
                    }

                await MaximizeChecks(window, model, captures);
                window.Width = 820;
                await Task.Delay(50);
                window.UpdateLayout();
                var quality = (ComboBox)window.FindName("QualityInput");
                Check(quality.ActualWidth > 140 && quality.ActualHeight >= 44, "Quality control remains usable at minimum width");
                model.Url = "https://youtu.be/jNQXAC9IVRw";
                await Task.Delay(800);
                Check(model.CanDownload && model.Qualities.Count == 2 && model.SelectedQuality.Height == 720, "Automatic inspection offers only actual fake-provider qualities and recommends available720p");
                model.Audio = true;
                Check(model.Qualities.Any(q => q.Bitrate == 192) && model.Qualities.Any(q => q.Bitrate == 320), "MP3 encoding choices survive the WPF port");
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
                Check(window.ActualHeight <= SystemParameters.WorkArea.Height, "Expanded playlist/status content stays within the work area");
                model.Folder = System.IO.Path.Combine(root, "missing-folder");
                model.OpenFolderCommand.Execute(null);
                Check(model.NoticeVisible, "Folder failure shows an in-client notice");
                await Task.Delay(60);
                window.UpdateLayout();
                Capture(window, System.IO.Path.Combine(captures, "playlist-notice.png"));
                var notice = (FrameworkElement)window.FindName("Notice");
                var page = (FrameworkElement)window.FindName("Root");
                var position = notice.TranslatePoint(new Point(), page);
                var bounds = notice.TransformToAncestor(page).TransformBounds(new Rect(0, 0, notice.ActualWidth, notice.ActualHeight));
                Check(bounds.Left >= 0 && bounds.Top >= 0 && bounds.Right <= page.ActualWidth + 1 && bounds.Bottom <= page.ActualHeight + 1, "Transient notice remains inside client borders");
                var viewport = (ScrollViewer)window.FindName("Viewport");
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
                        var body = (FrameworkElement)window.FindName("PageContent");
                        var barBounds = bar.TransformToAncestor(page).TransformBounds(new Rect(0, 0, bar.ActualWidth, bar.ActualHeight));
                        var bodyBounds = body.TransformToAncestor(page).TransformBounds(new Rect(0, 0, body.ActualWidth, body.ActualHeight));
                        Capture(window, System.IO.Path.Combine(captures, "scroll-" + language + (dark ? "-dark" : "-light") + ".png"));
                        Check(bar.Visibility == Visibility.Visible && Math.Abs(bar.ActualWidth - 16) < 1 && track.Thumb.ActualHeight < track.ActualHeight &&
                            (language == "ar" ? barBounds.Right <= bodyBounds.Left + 1 : barBounds.Left >= bodyBounds.Right - 1),
                            "Scrollbar reserves a separate hit target with a proportional thumb in " + language + (dark ? " dark" : " light") +
                            $" (visibility={bar.Visibility}, width={bar.ActualWidth}, thumb={track.Thumb.ActualHeight}, track={track.ActualHeight}, bar={barBounds}, content={bodyBounds})");
                        viewport.ScrollToTop();
                        window.UpdateLayout();
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
                var root = (FrameworkElement)window.FindName("Root");
                var viewport = (ScrollViewer)window.FindName("Viewport");
                var footer = (FrameworkElement)window.FindName("FooterContent");
                var footerBounds = footer.TransformToAncestor(viewport).TransformBounds(new Rect(0, 0, footer.ActualWidth, footer.ActualHeight));
                double center = root.PointToScreen(new Point(root.ActualWidth / 2, 0)).X;
                Check(root.ActualWidth <= 1041 && Math.Abs(center - (info.Work.Left + info.Work.Right) / 2.0) <= 2 &&
                    Math.Abs(footerBounds.Bottom - viewport.ActualHeight) <= 2,
                    "The maximized form stays centered at a readable width with its footer at the bottom");
                Capture(window, System.IO.Path.Combine(captures, "maximized-" + language + (dark ? "-dark" : "-light") + ".png"));
            }
        window.WindowState = WindowState.Normal;
        await Task.Delay(100);
        window.UpdateLayout();
        var page = (FrameworkElement)window.FindName("PageContent");
        Check(Math.Abs(window.Width - normalWidth) <= 1 && page.MinHeight == 0 && window.ActualHeight < SystemParameters.WorkArea.Height,
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
        public bool IsCompleted(string folder, string file, bool audio) => true;
    }

    private sealed class FakeProvider : IMediaProvider
    {
        public string Id => "youtube";
        public bool IsReady => true;

        public Task EnsureReadyAsync(IProgress<DownloadProgress> progress, CancellationToken token) => Task.CompletedTask;
        public string Normalize(string url, bool playlist) => VideoLink.Normalize(url, playlist);
        public MediaTargets ResolveTargets(string input)
        {
            var link = VideoLink.Parse(input);
            return new(link.VideoUrl, link.PlaylistUrl);
        }

        public VideoInfo Inspect(string url, CancellationToken token, IProgress<DownloadProgress> progress = null) => new()
        {
            Title = "Local UI fixture",
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
            token.WaitHandle.WaitOne(10000);
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
