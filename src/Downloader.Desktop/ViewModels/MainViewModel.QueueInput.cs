using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace YouTubeDownloader;

public sealed partial class MainViewModel
{
    public ObservableCollection<QueueLinkInputView> QueueLinkInputs { get; } = new();
    public IRelayCommand AddLinkFieldCommand { get; private set; }
    public IRelayCommand<QueueLinkInputView> RemoveLinkFieldCommand { get; private set; }
    public IRelayCommand<QueueLinkInputView> PasteQueueLinkCommand { get; private set; }
    public IAsyncRelayCommand QueueDownloadCommand { get; private set; }
    public string AddAnotherLinkLabel => T("Add another link");
    public string RemoveLinkLabel => T("Remove link");

    private bool HasDraftLinks => QueueLinkInputs.Any(draft => !string.IsNullOrWhiteSpace(draft.Url));
    private QueueLinkInputView NewQueueLinkInput() => new(T, RefreshQueueInputAvailability);

    private void InitializeQueueInputs()
    {
        AddLinkFieldCommand = new RelayCommand(() =>
        {
            QueueLinkInputs.Add(NewQueueLinkInput());
            RefreshQueueLinkLabels();
        }, () => QueueLinkInputs.Count < DownloadQueue.Capacity);
        RemoveLinkFieldCommand = new RelayCommand<QueueLinkInputView>(field =>
        {
            if (!QueueLinkInputs.Contains(field)) return;
            if (QueueLinkInputs.Count == 1) { field.Url = ""; field.SetError(""); }
            else QueueLinkInputs.Remove(field);
            RefreshQueueLinkLabels();
        });
        PasteQueueLinkCommand = new RelayCommand<QueueLinkInputView>(field => PasteQueueLinks(field, actions.ClipboardText()));
        AddLinksCommand = new RelayCommand(() => { AddLinks(); }, () => HasDraftLinks);
        QueueDownloadCommand = new AsyncRelayCommand(async () =>
        {
            if (HasDraftLinks && !AddLinks()) return;
            if (QueueStartCommand.CanExecute(null)) await QueueStartCommand.ExecuteAsync(null);
        }, () => IsIdle && (HasDraftLinks || QueueStartCommand.CanExecute(null)));
        QueueLinkInputs.Add(NewQueueLinkInput());
        RefreshQueueLinkLabels();
    }

    internal void PasteQueueLinks(QueueLinkInputView field, string text)
    {
        if (!QueueLinkInputs.Contains(field) || string.IsNullOrWhiteSpace(text)) return;
        var links = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (QueueLinkInputs.Count + links.Length - 1 > DownloadQueue.Capacity)
        {
            field.SetError("You can enter up to 100 links. Remove a link before adding another.");
            return;
        }
        int index = QueueLinkInputs.IndexOf(field);
        field.Url = links[0];
        for (int offset = 1; offset < links.Length; offset++)
        {
            var added = NewQueueLinkInput();
            added.Url = links[offset];
            QueueLinkInputs.Insert(index + offset, added);
        }
        RefreshQueueLinkLabels();
    }

    private void RefreshQueueLinkLabels()
    {
        for (int index = 0; index < QueueLinkInputs.Count; index++)
            QueueLinkInputs[index].RefreshLabel(F("Link {0}", index + 1));
        RefreshQueueInputAvailability();
    }

    private void RefreshQueueInputAvailability()
    {
        AddLinksCommand?.NotifyCanExecuteChanged();
        AddLinkFieldCommand?.NotifyCanExecuteChanged();
        QueueDownloadCommand?.NotifyCanExecuteChanged();
    }

    private bool AddLinks()
    {
        int added = 0;
        foreach (var field in QueueLinkInputs.Where(field => !string.IsNullOrWhiteSpace(field.Url)).ToArray())
        {
            try
            {
                var targets = downloads.ResolveTargets(field.Url.Trim());
                var request = OutputRequest();
                request.Url = targets.VideoUrl ?? targets.PlaylistUrl;
                request.Selection = null;
                request.QualityKey = null;
                Queue.Add(request, new Uri(request.Url).Host);
                QueueLinkInputs.Remove(field);
                added++;
            }
            catch (ArgumentException) { field.SetError("Enter a valid video or playlist link."); }
            catch (UriFormatException) { field.SetError("Enter a valid video or playlist link."); }
            catch (InvalidOperationException exception) { field.SetError(exception.Message); }
        }
        if (QueueLinkInputs.Count == 0) QueueLinkInputs.Add(NewQueueLinkInput());
        QueueMessage = added > 0 ? F("{0} links added to the queue.", added) : "";
        OnPropertyChanged(nameof(QueueMessage));
        RefreshQueueLinkLabels();
        return !QueueLinkInputs.Any(field => field.HasError);
    }
}
