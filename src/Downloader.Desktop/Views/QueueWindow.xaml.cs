using System.Windows;
namespace YouTubeDownloader;
public partial class QueueWindow : Window
{
    public QueueWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ThemeResources.ApplyTitleBar(this, (DataContext as MainViewModel)?.IsDark == true);
        ContentRendered += (_, _) => QueueLinksInput.Focus();
    }
    private void CloseClicked(object sender, RoutedEventArgs args) => Close();
}
