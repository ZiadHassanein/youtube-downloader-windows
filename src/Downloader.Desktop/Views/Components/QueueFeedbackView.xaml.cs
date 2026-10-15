using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace YouTubeDownloader;

public partial class QueueFeedbackView : UserControl
{
    public static readonly DependencyProperty RevisionProperty = DependencyProperty.Register(nameof(Revision), typeof(int), typeof(QueueFeedbackView),
        new PropertyMetadata(0, (view, args) => ((QueueFeedbackView)view).Confirm()));
    public int Revision { get => (int)GetValue(RevisionProperty); set => SetValue(RevisionProperty, value); }

    public QueueFeedbackView()
    {
        InitializeComponent();
        Loaded += (_, _) => MotionPreferences.Changed += Reset;
        Unloaded += (_, _) => { MotionPreferences.Changed -= Reset; Reset(); };
        IsVisibleChanged += (_, _) => { if (!IsVisible) Reset(); };
    }

    private void Confirm()
    {
        Reset();
        if (!IsLoaded || !IsVisible || !MotionPreferences.Enabled) return;
        DoubleAnimationUsingKeyFrames Frames(double start, double shown)
        {
            var frames = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
            frames.KeyFrames.Add(new EasingDoubleKeyFrame(start, TimeSpan.Zero));
            frames.KeyFrames.Add(new EasingDoubleKeyFrame(shown, TimeSpan.FromMilliseconds(100), MotionPreferences.EaseOut));
            frames.KeyFrames.Add(new EasingDoubleKeyFrame(shown, TimeSpan.FromMilliseconds(650)));
            frames.KeyFrames.Add(new EasingDoubleKeyFrame(start, TimeSpan.FromMilliseconds(850), MotionPreferences.EaseOut));
            return frames;
        }
        PlusMark.BeginAnimation(OpacityProperty, Frames(1, 0));
        AddedMark.BeginAnimation(OpacityProperty, Frames(0, 1));
    }

    private void Reset()
    {
        PlusMark?.BeginAnimation(OpacityProperty, null);
        AddedMark?.BeginAnimation(OpacityProperty, null);
    }
}
