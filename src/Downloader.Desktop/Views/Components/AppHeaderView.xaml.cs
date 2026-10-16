using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace YouTubeDownloader;

public partial class AppHeaderView : UserControl
{
    public AppHeaderView()
    {
        InitializeComponent();
        var menu = LanguageButton.ContextMenu;
        menu.PlacementTarget = LanguageButton;
        menu.CustomPopupPlacementCallback = (popup, _, _) => ContextMenuPlacement.CenterWithinClient(LanguageButton, popup);
        Loaded += (_, _) => { MotionPreferences.Changed += ResetLogo; PlayLogo(); };
        Unloaded += (_, _) => { menu.IsOpen = false; MotionPreferences.Changed -= ResetLogo; ResetLogo(); };
        IsVisibleChanged += (_, _) => { if (!IsVisible) ResetLogo(); };
    }

    private void LogoClicked(object sender, RoutedEventArgs args) => PlayLogo(pulse: true);
    private void LogoHovered(object sender, MouseEventArgs args) => PlayLogo();
    private void LogoLeft(object sender, MouseEventArgs args) => ResetLogo();

    private void PlayLogo(bool pulse = false)
    {
        ResetLogo();
        if (!IsLoaded || !IsVisible || !BrandButton.IsEnabled || !MotionPreferences.Enabled) return;
        if (pulse)
        {
            LogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(.94, 1, TimeSpan.FromMilliseconds(360)) { EasingFunction = MotionPreferences.EaseOut, FillBehavior = FillBehavior.Stop });
            LogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(.94, 1, TimeSpan.FromMilliseconds(360)) { EasingFunction = MotionPreferences.EaseOut, FillBehavior = FillBehavior.Stop });
        }
        LightOffset.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(-1.2, 1.2, TimeSpan.FromMilliseconds(600)) { FillBehavior = FillBehavior.Stop });
        var light = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
        light.KeyFrames.Add(new LinearDoubleKeyFrame(0, TimeSpan.Zero));
        light.KeyFrames.Add(new LinearDoubleKeyFrame(.5, TimeSpan.FromMilliseconds(100)));
        light.KeyFrames.Add(new LinearDoubleKeyFrame(.5, TimeSpan.FromMilliseconds(420)));
        light.KeyFrames.Add(new LinearDoubleKeyFrame(0, TimeSpan.FromMilliseconds(600)));
        LogoLight.BeginAnimation(OpacityProperty, light);
    }

    private void ResetLogo()
    {
        LogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        LogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        LightOffset.BeginAnimation(TranslateTransform.XProperty, null);
        LogoLight.BeginAnimation(OpacityProperty, null);
    }

    private void LanguageClicked(object sender, RoutedEventArgs args) => OpenLanguageMenu();

    private void LanguageKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key != Key.Down) return;
        OpenLanguageMenu();
        args.Handled = true;
    }

    private void OpenLanguageMenu()
    {
        if (PrepareLanguageMenu()) LanguageButton.ContextMenu.IsOpen = true;
    }

    private void LanguageMenuOpening(object sender, ContextMenuEventArgs args) => args.Handled = !PrepareLanguageMenu();

    private bool PrepareLanguageMenu()
    {
        if (!LanguageButton.IsEnabled || DataContext is not MainViewModel model) return false;
        var menu = LanguageButton.ContextMenu;
        // Detached native menus can retain presentation and selection from their previous opening.
        menu.SetCurrentValue(FlowDirectionProperty, model.ContentDirection);
        menu.SetCurrentValue(FontFamilyProperty, model.ContentFont);
        EnglishChoice.SetCurrentValue(MenuItem.IsCheckedProperty, model.IsEnglish);
        ArabicChoice.SetCurrentValue(MenuItem.IsCheckedProperty, model.IsArabic);
        menu.Placement = PlacementMode.Custom;
        return true;
    }
}
