using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace YouTubeDownloader;

public partial class MediaPreviewView : UserControl
{
    public MediaPreviewView()
    {
        InitializeComponent();
        ThumbnailFrame.SizeChanged += (_, _) => ThumbnailFrame.Clip =
            new System.Windows.Media.RectangleGeometry(new Rect(ThumbnailFrame.RenderSize), 8, 8);
    }

    internal void SetCompact(bool compact)
    {
        ThumbnailFrame.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        PreviewLabel.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        PreviewSubtitle.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        OutputSummary.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        PreviewDivider.Margin = compact ? new Thickness(0, 12, 0, 12) : new Thickness(0, 22, 0, 20);
        PreviewTitle.FontSize = compact ? 17 : 23;
        PreviewTitle.MaxHeight = compact ? 60 : 104;
    }
}

public sealed class PreviewAspectRatioConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is double width && double.IsFinite(width) ? Math.Max(0, width * 9 / 16) : 0d;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
