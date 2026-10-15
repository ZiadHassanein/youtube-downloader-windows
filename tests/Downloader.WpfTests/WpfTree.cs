using System.Windows;
using System.Windows.Media;
using YouTubeDownloader;

internal static class WpfTree
{
    internal static object Find(DependencyObject root, string name)
    {
        if (root is FrameworkElement element)
        {
            if (element.Name == name) return element;
            if (element.FindName(name) is { } found) return found;
        }
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            if (Find(child, name) is { } found) return found;
        return null;
    }
}

internal sealed class FixtureThumbnailLoader : IMediaThumbnailLoader
{
    internal Func<string, CancellationToken, Task<ImageSource>> Handler;
    public Task<ImageSource> LoadAsync(string address, CancellationToken token)
    {
        if (Handler != null) return Handler(address, token);
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(145, 184, 220)), null, new RectangleGeometry(new Rect(0, 0, 640, 360))));
        group.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(47, 104, 157)), null, new RectangleGeometry(new Rect(0, 155, 640, 205))));
        group.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(62, 90, 83)), null, Geometry.Parse("M0,360 L0,230 180,155 320,250 430,360 Z")));
        var image = new DrawingImage(group);
        image.Freeze();
        return Task.FromResult<ImageSource>(image);
    }
}
