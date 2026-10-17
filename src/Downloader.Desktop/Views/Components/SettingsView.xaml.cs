using System.Windows.Controls;

namespace YouTubeDownloader;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    internal void ResetHelp()
    {
        if (ToolRefreshAnchor.ToolTip is ToolTip help) help.IsOpen = false;
        ToolRefreshAnchor.SetCurrentValue(ToolTipProperty, FindResource("DownloaderHelp"));
    }
}
