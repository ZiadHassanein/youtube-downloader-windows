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
    private bool closing, closed;
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
        SizeChanged += (_, _) => ApplyResponsiveLayout();
        StateChanged += (_, _) => ConstrainToWorkArea();
        DpiChanged += (_, _) => ConstrainToWorkArea();
        model.PropertyChanged += (_, args) =>
        {
            if (string.IsNullOrEmpty(args.PropertyName))
            {
                if (presentationDirection != model.ContentDirection)
                {
                    ResetDropDowns(Root);
                    SettingsPane.ResetHelp();
                    presentationDirection = model.ContentDirection;
                }
                ApplyTitleBar();
            }
            if (args.PropertyName is nameof(MainViewModel.SettingsVisible) or nameof(MainViewModel.HasMediaPreview))
            {
                ApplyResponsiveLayout();
                if (args.PropertyName == nameof(MainViewModel.SettingsVisible))
                {
                    Viewport.ScrollToTop();
                    if (model.SettingsVisible) ViewMotion.Reveal(SettingsPane);
                }
                else if (model.HasMediaPreview && !model.SettingsVisible)
                    ViewMotion.Reveal(MediaPreview.PreviewContent);
            }
            if (args.PropertyName == nameof(MainViewModel.HasFile) && model.HasFile && model.ShowInlineActivity)
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => DownloadForm.InlineActivity.BringIntoView());
            if (args.PropertyName is nameof(MainViewModel.StatusHeading) or nameof(MainViewModel.NoticeHeading)
                && !(args.PropertyName == nameof(MainViewModel.NoticeHeading) && model.OptionsNoticeVisible))
            {
                var element = args.PropertyName == nameof(MainViewModel.NoticeHeading) ? NoticeHeading :
                    model.SettingsVisible ? SettingsPane.ActiveDownload.StatusHeading :
                    model.HasMediaPreview ? MediaPreview.Activity.StatusHeading : DownloadForm.InlineActivity.StatusHeading;
                (UIElementAutomationPeer.FromElement(element) ?? UIElementAutomationPeer.CreatePeerForElement(element))?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
            }
        };
        SourceInitialized += (_, _) =>
        {
            ApplyTitleBar();
            ConstrainToWorkArea();
        };
        ContentRendered += (_, _) =>
        {
            ApplyResponsiveLayout();
            DownloadForm.LinkInput.Focus();
            AppUpdates.AcknowledgeStartup();
        };
        Closing += ClosingAsync;
    }

    private void ApplyResponsiveLayout()
    {
        bool compact = Root.ActualWidth < 1000;
        bool showContext = model.SettingsVisible || model.HasMediaPreview;
        bool split = showContext && !compact;
        PageContent.ColumnDefinitions[0].Width = new GridLength(split ? 55 : 1, GridUnitType.Star);
        PageContent.ColumnDefinitions[1].Width = split ? new GridLength(45, GridUnitType.Star) : new GridLength(0);
        Grid.SetColumn(ContextPane, compact ? 0 : 1);
        Grid.SetRow(ContextPane, 0);
        Grid.SetRow(DownloadForm, compact && showContext ? 1 : 0);
        ContextPane.Visibility = showContext ? Visibility.Visible : Visibility.Collapsed;
        DownloadForm.Visibility = compact && model.SettingsVisible ? Visibility.Collapsed : Visibility.Visible;
        ContextPane.MaxWidth = compact ? DownloadForm.MaxWidth : double.PositiveInfinity;
        MediaPreview.SetCompact(compact);
        MediaPreview.Visibility = model.HasMediaPreview && !model.SettingsVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void ResetDropDowns(DependencyObject parent)
    {
        if (parent is FrameworkElement { ContextMenu: { } menu }) menu.IsOpen = false;
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
        ThemeResources.ApplyTitleBar(this, model.IsDark);
    }

    private void ConstrainToWorkArea()
    {
        if (WindowState != WindowState.Normal) return;
        var area = CurrentWorkArea();
        if (Width > area.Width - 16) Width = Math.Max(MinWidth, area.Width - 16);
        if (Height > area.Height - 16) Height = Math.Max(MinHeight, area.Height - 16);
        if (Top + Height > area.Bottom) Top = area.Bottom - Height;
        if (Top < area.Top) Top = area.Top;
    }

    internal Rect CurrentWorkArea()
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
        if (kind == "options") return;
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
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
}
