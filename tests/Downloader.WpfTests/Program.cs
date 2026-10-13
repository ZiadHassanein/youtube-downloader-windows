using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        var root = (FrameworkElement)window.FindName("Root");
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling((root.ActualWidth + root.Margin.Left + root.Margin.Right) * dpi.DpiScaleX), (int)Math.Ceiling((root.ActualHeight + root.Margin.Top + root.Margin.Bottom) * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = System.IO.File.Create(file);
        png.Save(output);
    }

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
