using System.ComponentModel;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Automation.Peers;
using System.Windows.Threading;

namespace YouTubeDownloader;
public partial class MainWindow : Window
{
    private readonly MainViewModel model;
    private bool closing, closed, fitQueued;
    private FlowDirection presentationDirection;
    public MainWindow() : this(null, null)
    {
    }

    internal MainWindow(DownloadService service, SettingsRepository repository, IMediaThumbnailLoader thumbnailLoader = null)
    {
        InitializeComponent();
        Root.Children.Remove(Notice);
        DownloadForm.FolderNoticeHost.Children.Add(Notice);
        var actions = new DesktopActions(this);
        model = new MainViewModel(service ?? new DownloadService(new YtDlpProvider(new DependencyManager()), new LocalPlaylistStorage()), repository ?? new SettingsRepository(), actions, thumbnailLoader);
        presentationDirection = model.ContentDirection;
        DataContext = model;
        model.NoticeRequested += PlaceNotice;
        PageContent.SizeChanged += (_, _) => QueueFit();
        SizeChanged += (_, args) =>
        {
            ApplyResponsiveLayout();
            if (args.WidthChanged) QueueFit();
        };
        StateChanged += (_, _) => QueueFit();
        model.PropertyChanged += (_, args) =>
        {
            if (string.IsNullOrEmpty(args.PropertyName))
            {
                if (presentationDirection != model.ContentDirection)
                {
                    ResetDropDowns(PageContent);
                    SettingsPane.ResetHelp();
                    presentationDirection = model.ContentDirection;
                }
                ApplyTitleBar();
            }
            if (args.PropertyName == nameof(MainViewModel.SettingsVisible))
            {
                ApplyResponsiveLayout();
                Viewport.ScrollToTop();
                if (model.SettingsVisible) ViewMotion.Reveal(SettingsPane);
            }
            if (args.PropertyName == nameof(MainViewModel.PreviewImage) && model.PreviewImage != null)
                ViewMotion.Reveal(MediaPreview.PreviewContent);
            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName is nameof(MainViewModel.OptionsVisible)
                or nameof(MainViewModel.HasPlaylist) or nameof(MainViewModel.HasStatus) or nameof(MainViewModel.NoticeVisible)
                or nameof(MainViewModel.SettingsVisible))
                QueueFit();
            if (args.PropertyName is nameof(MainViewModel.StatusHeading) or nameof(MainViewModel.NoticeHeading))
            {
                var element = args.PropertyName == nameof(MainViewModel.NoticeHeading) ? NoticeHeading :
                    model.SettingsVisible ? SettingsPane.ActiveDownload.StatusHeading : MediaPreview.Activity.StatusHeading;
                (UIElementAutomationPeer.FromElement(element) ?? UIElementAutomationPeer.CreatePeerForElement(element))?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
            }
        };
        SourceInitialized += (_, _) => ApplyTitleBar();
        ContentRendered += (_, _) =>
        {
            ApplyResponsiveLayout();
            QueueFit();
            DownloadForm.LinkInput.Focus();
            AppUpdates.AcknowledgeStartup();
        };
        Closing += ClosingAsync;
    }

    private void ApplyResponsiveLayout()
    {
        bool compact = Root.ActualWidth < 1000;
        PageContent.ColumnDefinitions[0].Width = new GridLength(compact ? 1 : 55, GridUnitType.Star);
        PageContent.ColumnDefinitions[1].Width = compact ? new GridLength(0) : new GridLength(45, GridUnitType.Star);
        Grid.SetColumn(ContextPane, compact ? 0 : 1);
        Grid.SetRow(ContextPane, 0);
        Grid.SetRow(DownloadForm, compact ? 1 : 0);
        DownloadForm.Visibility = compact && model.SettingsVisible ? Visibility.Collapsed : Visibility.Visible;
        MediaPreview.SetCompact(compact);
        MediaPreview.Visibility = model.SettingsVisible ? Visibility.Collapsed : Visibility.Visible;
    }

    private static void ResetDropDowns(DependencyObject parent)
    {
        if (parent is ComboBox combo)
        {
            // WPF can retain the old popup HWND's height after inherited font/direction changes.
            // Recreate the template's popup; keep the ComboBox, selection and bindings intact.
            combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty, false);
            var template = combo.Template;
            combo.SetCurrentValue(Control.TemplateProperty, null);
            combo.SetCurrentValue(Control.TemplateProperty, template);
            combo.ApplyTemplate();
            return;
        }
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            ResetDropDowns(VisualTreeHelper.GetChild(parent, index));
    }

    private void ApplyTitleBar()
    {
        int enabled = model.IsDark && !SystemParameters.HighContrast ? 1 : 0;
        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
            try
            {
                if (DwmSetWindowAttribute(handle, 20, ref enabled, 4) != 0)
                    DwmSetWindowAttribute(handle, 19, ref enabled, 4);
            }
            catch (DllNotFoundException)
            {
            }
    }

    private void QueueFit()
    {
        if (fitQueued || closed)
            return;
        fitQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            fitQueued = false;
            FitContent();
        });
    }

    private void FitContent()
    {
        if (WindowState == WindowState.Minimized)
            return;
        SizeToContent = SizeToContent.Manual;
        if (WindowState == WindowState.Maximized)
        {
            PageContent.MinHeight = Viewport.ActualHeight;
            return;
        }
        PageContent.MinHeight = 0;
        var area = CurrentWorkArea();
        if (Width > area.Width - 16) Width = Math.Max(MinWidth, area.Width - 16);
        double contentWidth = Math.Max(1, Viewport.ActualWidth -
            (Viewport.ComputedVerticalScrollBarVisibility == Visibility.Visible ? 26 : 0));
        PageContent.Measure(new Size(contentWidth, double.PositiveInfinity));
        double chrome = Math.Max(SystemParameters.CaptionHeight + 8, ActualHeight - Root.ActualHeight);
        double desired = Math.Clamp(PageContent.DesiredSize.Height + HeaderContent.ActualHeight + FooterContent.ActualHeight + chrome,
            MinHeight, Math.Max(MinHeight, area.Height - 16));
        if (Math.Abs(Height - desired) > 1) Height = desired;
        if (Top + ActualHeight > area.Bottom) Top = area.Bottom - ActualHeight;
        if (Top < area.Top) Top = area.Top;
    }

    private Rect CurrentWorkArea()
    {
        var source = PresentationSource.FromVisual(this);
        var handle = new WindowInteropHelper(this).Handle;
        var info = new MonitorInfo
        {
            Size = Marshal.SizeOf<MonitorInfo>()
        };
        if (handle != IntPtr.Zero && GetMonitorInfo(MonitorFromWindow(handle, 2), ref info))
        {
            var matrix = source?.CompositionTarget?.TransformFromDevice ?? System.Windows.Media.Matrix.Identity;
            return new Rect(matrix.Transform(new Point(info.Work.Left, info.Work.Top)), matrix.Transform(new Point(info.Work.Right, info.Work.Bottom)));
        }

        return SystemParameters.WorkArea;
    }

    private void PlaceNotice(string kind)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            model.SettingsVisible = kind != "folder";
            var host = kind switch
            {
                "folder" => DownloadForm.FolderNoticeHost,
                "tools" => SettingsPane.ToolNoticeHost,
                _ => SettingsPane.UpdateNoticeHost
            };
            if (Notice.Parent is Panel parent && parent != host)
            {
                parent.Children.Remove(Notice);
                host.Children.Add(Notice);
            }

            Notice.Margin = kind is "folder" or "tools" ? new Thickness(0, 12, 0, 0) : new Thickness(0, 0, 0, 10);
            Notice.HorizontalAlignment = HorizontalAlignment.Stretch;
            QueueFit();
            Notice.BringIntoView();
        });
    }

    private async void ClosingAsync(object sender, CancelEventArgs args)
    {
        if (closed)
            return;
        args.Cancel = true;
        if (closing)
            return;
        closing = true;
        IsEnabled = false;
        try
        {
            await model.CloseAsync();
            closed = true;
            _ = Dispatcher.BeginInvoke(DispatcherPriority.Normal, () => Close());
        }
        catch
        {
            closing = false;
            IsEnabled = true;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor, Work;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
}
