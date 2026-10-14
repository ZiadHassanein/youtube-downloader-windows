using System.Windows;
namespace YouTubeDownloader;
public partial class QueueWindow : Window
{
    public QueueWindow() { InitializeComponent(); }
    private void CloseClicked(object sender, RoutedEventArgs args) => Close();
}
