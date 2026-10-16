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
        PreviewSurface.Padding = compact ? new Thickness(0) : new Thickness(30, 24, 30, 24);
        ThumbnailFrame.Width = compact ? 114 : double.NaN;
        ThumbnailFrame.Margin = compact ? new Thickness(0, 0, 12, 0) : new Thickness(0, 0, 0, 18);
        Grid.SetColumnSpan(ThumbnailFrame, compact ? 1 : 2);
        Grid.SetColumn(MetadataText, compact ? 1 : 0);
        Grid.SetColumnSpan(MetadataText, compact ? 1 : 2);
        Grid.SetRow(MetadataText, compact ? 0 : 1);
        MediaIdentity.ColumnDefinitions[0].Width = compact ? GridLength.Auto : new GridLength(1, GridUnitType.Star);
        PreviewLabel.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        PreviewSubtitle.Margin = compact ? new Thickness(0, 2, 0, 0) : new Thickness(0, 9, 0, 0);
        OutputSummary.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        PreviewDivider.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        Activity.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        PreviewTitle.FontSize = compact ? 16 : 23;
        PreviewTitle.LineHeight = compact ? 22 : 32;
        PreviewTitle.MaxHeight = compact ? 44 : 96;
    }
}

public sealed class PreviewAspectRatioConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is double width && double.IsFinite(width) ? Math.Max(0, width * 9 / 16) : 0d;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
