using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace YouTubeDownloader;
public sealed partial class DesktopActions
{
    private readonly Window owner;
    public DesktopActions(Window owner)
    {
        this.owner = owner;
    }

    public void Dispatch(Action action)
    {
        if (owner.Dispatcher.HasShutdownStarted)
            return;
        if (owner.Dispatcher.CheckAccess())
            action();
        else
            owner.Dispatcher.BeginInvoke(action);
    }

    public string ClipboardText()
    {
        try
        {
            return Clipboard.ContainsText() ? Clipboard.GetText() : null;
        }
        catch (ExternalException)
        {
            return null;
        }
    }

    public string BrowseFolder(string current)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose a save folder",
            InitialDirectory = Directory.Exists(current) ? current : AppPaths.Downloads
        };
        return dialog.ShowDialog(owner) == true ? dialog.FolderName : null;
    }

    public void OpenFolder(string folder) => Process.Start(new ProcessStartInfo(Path.GetFullPath(folder)) { UseShellExecute = true });
    public void OpenMedia(string file, bool reveal)
    {
        if (!File.Exists(file) || !OutputProfiles.IsMediaExtension(Path.GetExtension(file)))
            throw new IOException("File is not available.");
        if (!reveal)
        {
            Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
            return;
        }

        uint flags;
        IntPtr item;
        int result = SHParseDisplayName(Path.GetFullPath(file), IntPtr.Zero, out item, 0, out flags);
        if (result < 0)
            Marshal.ThrowExceptionForHR(result);
        try
        {
            result = SHOpenFolderAndSelectItems(item, 0, IntPtr.Zero, 0);
            if (result < 0)
                Marshal.ThrowExceptionForHR(result);
        }
        finally
        {
            Marshal.FreeCoTaskMem(item);
        }
    }

    public void OpenSocial(string kind)
    {
        string url = kind switch
        {
            "github" => "https://github.com/ZiadHassanein",
            "linkedin" => "https://www.linkedin.com/in/ziad-hassanein/",
            "email" => "mailto:ziadmuhammed501@gmail.com",
            _ => null
        };
        if (url != null)
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    public void OpenReleaseDownload() => Process.Start(new ProcessStartInfo(AppUpdates.Repository + "/releases") { UseShellExecute = true });
    private Window Dialog(string title, int height = 300) => new()
    {
        Owner = owner,
        Title = title,
        Width = Math.Min(620, Math.Max(480, owner.ActualWidth - 64)),
        Height = Math.Min(height, Math.Max(260, owner.ActualHeight - 60)),
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ResizeMode = ResizeMode.NoResize,
        ShowInTaskbar = false,
        Background = (Brush)Application.Current.Resources["Page"],
        Foreground = (Brush)Application.Current.Resources["Ink"],
        FontFamily = (owner.DataContext as MainViewModel)?.ContentFont ?? (FontFamily)Application.Current.Resources["LatinFont"],
        FlowDirection = (owner.DataContext as MainViewModel)?.ContentDirection ?? FlowDirection.LeftToRight,
        FontSize = 15,
        Icon = owner.Icon
    };
    public void ShowHistory(IEnumerable<string> history, Func<string, string> t)
    {
        var model = new RecentDownloadsViewModel(history, t, path => OpenMedia(path, true));
        var workArea = owner is MainWindow main ? main.CurrentWorkArea() : SystemParameters.WorkArea;
        var dialog = new RecentDownloadsWindow
        {
            Owner = owner, DataContext = model, Icon = owner.Icon,
            FontFamily = (owner.DataContext as MainViewModel)?.ContentFont ?? (FontFamily)Application.Current.Resources["LatinFont"],
            FlowDirection = (owner.DataContext as MainViewModel)?.ContentDirection ?? FlowDirection.LeftToRight,
            Width = Math.Min(600, Math.Min(owner.ActualWidth - 48, workArea.Width - 48)),
            SizeToContent = SizeToContent.Height,
            MaxHeight = Math.Min(owner.ActualHeight - 48, workArea.Height - 48)
        };
        var previousFocus = System.Windows.Input.Keyboard.FocusedElement as UIElement;
        dialog.ShowDialog();
        if (previousFocus is { IsEnabled: true, IsVisible: true }) previousFocus.Focus();
    }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHParseDisplayName(string path, IntPtr bindContext, out IntPtr item, uint attributes, out uint returnedAttributes);
    [DllImport("shell32.dll")]
    private static extern int SHOpenFolderAndSelectItems(IntPtr item, uint count, IntPtr children, uint flags);
}
