using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
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
        if (!File.Exists(file) || !new[]
        {
            ".mp4",
            ".mp3", ".m4a", ".opus", ".wav", ".flac"
        }.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
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
        var dialog = Dialog(t("Recent downloads"), 460);
        var grid = new Grid
        {
            Margin = new Thickness(24)
        };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var list = new ListBox
        {
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = (Brush)Application.Current.Resources["Ink"],
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(list, ScrollBarVisibility.Auto);
        list.Resources.Add(typeof(ScrollViewer), Application.Current.Resources["VerticalScrollFrame"]);
        foreach (string file in history.Take(5))
            list.Items.Add(new ListBoxItem
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = Path.GetFileName(file), FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, TextWrapping = TextWrapping.NoWrap },
                        new TextBlock { Text = file, Foreground = (Brush)Application.Current.Resources["Muted"], TextTrimming = TextTrimming.CharacterEllipsis, TextWrapping = TextWrapping.NoWrap, Margin = new Thickness(0, 5, 0, 0), FlowDirection = FlowDirection.LeftToRight }
                    }
                },
                Tag = file,
                Padding = new Thickness(10),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                ToolTip = file
            });
        if (list.Items.Count == 0)
            grid.Children.Add(new TextBlock { Text = t("Your completed downloads will appear here."), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
        else
        {
            list.SelectedIndex = 0;
            grid.Children.Add(list);
        }

        var message = new TextBlock
        {
            Foreground = (Brush)Application.Current.Resources["Error"],
            Margin = new Thickness(0, 12, 0, 0)
        };
        Grid.SetRow(message, 1);
        grid.Children.Add(message);
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0)
        };
        Grid.SetRow(row, 2);
        var reveal = new Button
        {
            Content = t("Show in folder"),
            IsEnabled = list.SelectedItem != null
        };
        var close = new Button
        {
            Content = t("Close"),
            IsCancel = true,
            Margin = new Thickness(12, 0, 0, 0),
            MinWidth = 100
        };
        CancellationTokenSource notice = null;
        reveal.Click += async (_, _) =>
        {
            try
            {
                OpenMedia((string)((ListBoxItem)list.SelectedItem).Tag, true);
            }
            catch
            {
                notice?.Cancel();
                notice = new();
                message.Text = t("This file is no longer in its saved location.");
                try
                {
                    await Task.Delay(5000, notice.Token);
                    message.Text = "";
                }
                catch (OperationCanceledException)
                {
                }
            }
        };
        list.SelectionChanged += (_, _) =>
        {
            reveal.IsEnabled = list.SelectedItem != null;
            notice?.Cancel();
            message.Text = "";
        };
        row.Children.Add(reveal);
        row.Children.Add(close);
        grid.Children.Add(row);
        dialog.Content = grid;
        dialog.Closed += (_, _) => notice?.Cancel();
        dialog.ShowDialog();
        notice?.Dispose();
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHParseDisplayName(string path, IntPtr bindContext, out IntPtr item, uint attributes, out uint returnedAttributes);
    [DllImport("shell32.dll")]
    private static extern int SHOpenFolderAndSelectItems(IntPtr item, uint count, IntPtr children, uint flags);
}
