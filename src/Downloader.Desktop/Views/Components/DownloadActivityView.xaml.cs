using System.Windows;
using System.Windows.Controls;

namespace YouTubeDownloader;

public partial class DownloadActivityView : UserControl
{
    public static readonly DependencyProperty DarkSurfaceProperty = DependencyProperty.Register(
        nameof(DarkSurface), typeof(bool), typeof(DownloadActivityView), new PropertyMetadata(false));
    public bool DarkSurface { get => (bool)GetValue(DarkSurfaceProperty); set => SetValue(DarkSurfaceProperty, value); }
    public DownloadActivityView() => InitializeComponent();
}
