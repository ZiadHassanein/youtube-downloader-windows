using System.ComponentModel;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Automation.Peers;
using System.Windows.Threading;

namespace YouTubeDownloader;
public partial class MainWindow : Window
{
    private readonly MainViewModel model;
    private bool closing, closed, fitQueued;
    public MainWindow() : this(null, null)
    {
    }

    internal MainWindow(DownloadService service, SettingsRepository repository)
    {
        InitializeComponent();
        Root.Children.Remove(Notice);
        FolderNoticeHost.Children.Add(Notice);
        AddHandler(RadioButton.CheckedEvent, new RoutedEventHandler((_, args) =>
        {
            if (args.OriginalSource is RadioButton choice && choice.IsLoaded && SystemParameters.ClientAreaAnimation && !SystemParameters.HighContrast && choice.Template.FindName("Dot", choice)is UIElement marker)
                marker.BeginAnimation(OpacityProperty, new DoubleAnimation(.55, 1, TimeSpan.FromMilliseconds(120)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.Stop });
        }));
        var actions = new DesktopActions(this);
        model = new MainViewModel(service ?? new DownloadService(new YtDlpProvider(new DependencyManager()), new LocalPlaylistStorage()), repository ?? new SettingsRepository(), actions);
        DataContext = model;
        model.NoticeRequested += PlaceNotice;
        PageContent.SizeChanged += (_, _) => QueueFit();
        SizeChanged += (_, _) => QueueFit();
        model.PropertyChanged += (_, args) =>
        {
            if (string.IsNullOrEmpty(args.PropertyName))
                ApplyTitleBar();
            if (args.PropertyName is nameof(MainViewModel.StatusHeading) or nameof(MainViewModel.NoticeHeading))
            {
                var element = args.PropertyName == nameof(MainViewModel.NoticeHeading) ? NoticeHeading : StatusHeading;
                (UIElementAutomationPeer.FromElement(element) ?? UIElementAutomationPeer.CreatePeerForElement(element))?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
            }
        };
        SourceInitialized += (_, _) => ApplyTitleBar();
        ContentRendered += (_, _) =>
        {
            QueueFit();
            LinkInput.Focus();
            AppUpdates.AcknowledgeStartup();
        };
        Closing += ClosingAsync;
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
        var area = CurrentWorkArea();
        double boundedHeight = Math.Max(MinHeight, Math.Min(area.Height, SystemParameters.WorkArea.Height) - 16);
        if (Math.Abs(MaxHeight - boundedHeight) > 1)
            MaxHeight = boundedHeight;
        double maximum = Math.Max(240, boundedHeight - Root.Margin.Top - Root.Margin.Bottom - SystemParameters.CaptionHeight - 20);
        if (Math.Abs(Viewport.MaxHeight - maximum) > 1)
            Viewport.MaxHeight = maximum;
        PageContent.Measure(new Size(Math.Max(1, ActualWidth - Root.Margin.Left - Root.Margin.Right - 20), double.PositiveInfinity));
        double chrome = Math.Max(SystemParameters.CaptionHeight + 8, ActualHeight - Root.ActualHeight - Root.Margin.Top - Root.Margin.Bottom);
        double desired = Math.Clamp(PageContent.DesiredSize.Height + Root.Margin.Top + Root.Margin.Bottom + chrome + 8, MinHeight, boundedHeight);
        SizeToContent = SizeToContent.Manual;
        if (Math.Abs(Height - desired) > 1)
            Height = desired;
        if (Top + ActualHeight > area.Bottom)
            Top = area.Bottom - ActualHeight;
        if (Top < area.Top)
            Top = area.Top;
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
            var host = kind == "folder" ? FolderNoticeHost : UpdateNoticeHost;
            if (Notice.Parent is Panel parent && parent != host)
            {
                parent.Children.Remove(Notice);
                host.Children.Add(Notice);
            }

            Notice.Margin = kind == "folder" ? new Thickness(0, 12, 0, 0) : new Thickness(0, 0, 0, 10);
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
