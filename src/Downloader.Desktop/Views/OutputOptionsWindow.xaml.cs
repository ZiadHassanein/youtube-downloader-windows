using System.Windows;
using System.Windows.Automation.Peers;
using System.ComponentModel;

namespace YouTubeDownloader;

public partial class OutputOptionsWindow : Window
{
    public OutputOptionsWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ThemeResources.ApplyTitleBar(this, (DataContext as MainViewModel)?.IsDark == true);
        Loaded += (_, _) => { if (DataContext is MainViewModel model) model.PropertyChanged += NoticeChanged; };
        Closed += (_, _) => { if (DataContext is MainViewModel model) model.PropertyChanged -= NoticeChanged; };
        ContentRendered += (_, _) =>
        {
            CaptionInput.Focus();
            ViewMotion.Reveal(OptionsContent);
        };
        Closing += (_, _) =>
        {
            if (DataContext is not MainViewModel model) return;
            model.CaptionLanguagesCommand.Cancel();
            if (model.OptionsNoticeVisible) model.DismissNoticeCommand.Execute(null);
        };
    }

    private void DoneClicked(object sender, RoutedEventArgs args) => Close();

    private void NoticeChanged(object sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(MainViewModel.OptionsNoticeVisible) ||
            DataContext is not MainViewModel { OptionsNoticeVisible: true }) return;
        (UIElementAutomationPeer.FromElement(OptionsNoticeHeading) ?? UIElementAutomationPeer.CreatePeerForElement(OptionsNoticeHeading))?
            .RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }
}
