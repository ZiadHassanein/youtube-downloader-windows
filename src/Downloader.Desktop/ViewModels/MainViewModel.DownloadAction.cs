using CommunityToolkit.Mvvm.Input;

namespace YouTubeDownloader;

public enum DownloadActionState { Ready, Downloading, Complete }

public sealed partial class MainViewModel
{
    private DownloadActionState downloadActionState;
    public DownloadActionState DownloadActionState => downloadActionState;
    public string DownloadActionLabel => downloadActionState switch
    {
        DownloadActionState.Downloading => T("Downloading"),
        DownloadActionState.Complete => RevealLabel,
        _ => DownloadLabel
    };
    public IRelayCommand DownloadActionCommand { get; private set; }
    public int QueueAdditionVersion { get; private set; }

    private void InitializeDownloadAction()
    {
        DownloadActionCommand = new RelayCommand(() =>
        {
            if (downloadActionState == DownloadActionState.Complete) RevealCommand.Execute(null);
            else DownloadCommand.Execute(null);
        }, () => downloadActionState == DownloadActionState.Complete ? RevealCommand.CanExecute(null) : DownloadCommand.CanExecute(null));
        DownloadCommand.CanExecuteChanged += (_, _) => DownloadActionCommand.NotifyCanExecuteChanged();
        RevealCommand.CanExecuteChanged += (_, _) => DownloadActionCommand.NotifyCanExecuteChanged();
    }

    private void SetDownloadAction(DownloadActionState state)
    {
        if (!SetProperty(ref downloadActionState, state, nameof(DownloadActionState))) return;
        OnPropertyChanged(nameof(DownloadActionLabel));
        DownloadActionCommand?.NotifyCanExecuteChanged();
    }
}
