using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace YouTubeDownloader;

public partial class DownloadActionView : UserControl
{
    private MainViewModel model;
    private DownloadActionState previous;
    private double previousProgress;
    private ScaleTransform progressScale;

    public DownloadActionView()
    {
        InitializeComponent();
        Loaded += LoadedView;
        Unloaded += UnloadedView;
        DataContextChanged += (_, _) => Rebind();
        IsVisibleChanged += (_, _) => Refresh(false);
    }

    private void LoadedView(object sender, RoutedEventArgs args)
    {
        MotionPreferences.Changed += Settle;
        Rebind();
    }

    private void Rebind()
    {
        if (model != null) model.PropertyChanged -= ModelChanged;
        model = IsLoaded ? DataContext as MainViewModel : null;
        if (model != null) model.PropertyChanged += ModelChanged;
        DownloadButton.ApplyTemplate();
        progressScale = DownloadButton.Template.FindName("ProgressScale", DownloadButton) as ScaleTransform;
        Refresh(false);
    }

    private void ModelChanged(object sender, PropertyChangedEventArgs args)
    {
        if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName is nameof(MainViewModel.DownloadActionState) or nameof(MainViewModel.Progress) or nameof(MainViewModel.Indeterminate))
            Refresh(true);
    }

    private void Settle() => Refresh(false);

    private void Refresh(bool animate)
    {
        if (progressScale == null) return;
        var phase = model?.DownloadActionState ?? DownloadActionState.Ready;
        bool motion = animate && IsLoaded && IsVisible && MotionPreferences.Enabled;
        double value = phase == DownloadActionState.Downloading && model is { Indeterminate: false }
            ? Math.Clamp(model.Progress / 100, 0, 1) : phase == DownloadActionState.Complete ? 1 : 0;
        double from = progressScale.ScaleX;
        progressScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        progressScale.ScaleX = value;
        if (motion && phase == previous && value > previousProgress)
        {
            var progress = MotionPreferences.To(value, 180);
            progress.From = from;
            progressScale.BeginAnimation(ScaleTransform.ScaleXProperty, progress);
        }
        if (!motion || phase != previous)
        {
            ArrowOffset.BeginAnimation(TranslateTransform.YProperty, null);
            CompleteCheck.BeginAnimation(System.Windows.Shapes.Shape.StrokeDashOffsetProperty, null);
        }
        DownloadArrow.Visibility = phase == DownloadActionState.Complete ? Visibility.Hidden : Visibility.Visible;
        CompleteCheck.Visibility = phase == DownloadActionState.Complete ? Visibility.Visible : Visibility.Hidden;
        if (phase != previous && phase == DownloadActionState.Downloading && IsLoaded && IsVisible)
            _ = Dispatcher.InvokeAsync(() =>
            {
                if (IsVisible && model?.DownloadActionState == DownloadActionState.Downloading) BringIntoView();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        if (motion && phase != previous)
        {
            if (phase == DownloadActionState.Downloading)
            {
                var arrow = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
                arrow.KeyFrames.Add(new EasingDoubleKeyFrame(0, TimeSpan.Zero));
                arrow.KeyFrames.Add(new EasingDoubleKeyFrame(5, TimeSpan.FromMilliseconds(110), MotionPreferences.EaseOut));
                arrow.KeyFrames.Add(new EasingDoubleKeyFrame(0, TimeSpan.FromMilliseconds(250), MotionPreferences.EaseOut));
                ArrowOffset.BeginAnimation(TranslateTransform.YProperty, arrow);
            }
            else if (phase == DownloadActionState.Complete)
                CompleteCheck.BeginAnimation(System.Windows.Shapes.Shape.StrokeDashOffsetProperty,
                    new DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(240)) { EasingFunction = MotionPreferences.EaseOut, FillBehavior = FillBehavior.Stop });
        }
        previous = phase;
        previousProgress = value;
    }

    private void UnloadedView(object sender, RoutedEventArgs args)
    {
        MotionPreferences.Changed -= Settle;
        if (model != null) model.PropertyChanged -= ModelChanged;
        model = null;
        Refresh(false);
    }
}
