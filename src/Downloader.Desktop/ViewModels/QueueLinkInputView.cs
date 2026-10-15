using CommunityToolkit.Mvvm.ComponentModel;

namespace YouTubeDownloader;

public sealed class QueueLinkInputView : ObservableObject
{
    private readonly Func<string, string> translate;
    private readonly Action changed;
    private string url = "";
    private string label = "";
    private string error = "";

    internal QueueLinkInputView(Func<string, string> translate, Action changed)
        => (this.translate, this.changed) = (translate, changed);

    public string Label => label;
    public string Error => translate(error);
    public bool HasError => error.Length > 0;
    public string Url
    {
        get => url;
        set
        {
            if (!SetProperty(ref url, value ?? "")) return;
            SetError("");
            changed();
        }
    }

    internal void SetError(string message)
    {
        error = message;
        OnPropertyChanged(nameof(Error));
        OnPropertyChanged(nameof(HasError));
    }

    internal void RefreshLabel(string value)
    {
        SetProperty(ref label, value, nameof(Label));
        OnPropertyChanged(nameof(Error));
    }
}
