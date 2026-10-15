using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Threading;
using YouTubeDownloader;

internal static class QualityPopupChecks
{
    internal static async Task Run(Window window, MainViewModel model, string captures, Action<bool, string> check)
    {
        var quality = (ComboBox)WpfTree.Find(window, "QualityInput");
        foreach (bool dark in new[] { true, false })
        {
            if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            foreach (int bitrate in new[] { 192, 320, 128, 32, 0 })
            {
                model.SelectedQuality = model.Qualities.First(q => q.Bitrate == bitrate);
                for (int toggle = 0; toggle < 6; toggle++)
                {
                    // Alternate ordinary language changes with changes while the popup is open.
                    quality.IsDropDownOpen = toggle % 2 == 0;
                    await Settle(window);
                    model.LanguageCommand.Execute(null);
                    if (toggle == 3)
                        for (int burst = 0; burst < 4; burst++) model.LanguageCommand.Execute(null);
                    await Settle(window);
                    quality.IsDropDownOpen = true;
                    await Settle(window);
                    var popup = (Popup)quality.Template.FindName("PART_Popup", quality);
                    var panel = (FrameworkElement)popup.Child;
                    var source = (HwndSource)PresentationSource.FromVisual(panel);
                    if (!GetClientRect(source.Handle, out var native))
                        throw new InvalidOperationException("Could not measure the native quality popup.");
                    var nativeSize = source.CompositionTarget.TransformFromDevice.Transform(new Vector(native.Right, native.Bottom));
                    string variant = (model.IsArabic ? "ar" : "en") + (dark ? "-dark" : "-light");
                    check(quality.SelectedItem is QualityChoice selected && selected.Bitrate == bitrate &&
                        ReferenceEquals(quality.SelectedItem, model.SelectedQuality) && quality.Items.Count == OutputProfiles.AudioBitrates(model.SelectedEncoding.Encoding).Count + 1,
                        $"Audio selection survives language switch {toggle + 1} in {variant} at {bitrate} kbps");
                    var viewport = FindDescendant<ScrollContentPresenter>(panel);
                    var row = (ComboBoxItem)quality.ItemContainerGenerator.ContainerFromItem(quality.SelectedItem);
                    var bounds = row.TransformToAncestor(viewport).TransformBounds(new Rect(row.RenderSize));
                    bool fits = row.ActualHeight > 20 && bounds.Top >= -1 && bounds.Left >= -1 &&
                        bounds.Bottom <= viewport.ActualHeight + 1 && bounds.Right <= viewport.ActualWidth + 1;
                    if (toggle >= 4)
                    {
                        var dpi = VisualTreeHelper.GetDpi(panel);
                        var image = new RenderTargetBitmap(native.Right, native.Bottom,
                            dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
                        // Include PopupRoot's RTL transform, as in the actual native window.
                        image.Render(source.RootVisual);
                        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
                        using var stream = File.Create(Path.Combine(captures, $"audio-popup-{variant}-{bitrate}.png"));
                        encoder.Save(stream);
                    }
                    check(popup.IsOpen && fits && panel.ActualHeight <= nativeSize.Y + 1 && panel.ActualWidth <= nativeSize.X + 1,
                        $"Selected audio quality stays fully visible in the scrolling list after switch {toggle + 1} in {variant} (row={bounds}, viewport={viewport.RenderSize}, panel={panel.RenderSize}, native={nativeSize})");
                    check(Math.Abs(panel.ActualHeight - panel.DesiredSize.Height) <= 1,
                        $"Audio popup discards the previous language's height in {variant} (actual={panel.ActualHeight}, desired={panel.DesiredSize.Height})");
                    quality.IsDropDownOpen = false;
                }
            }
        }
        quality.Focus();
        quality.IsDropDownOpen = true;
        await Settle(window);
        quality.SelectedItem = model.Qualities.First(q => q.Bitrate == 192);
        quality.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(quality), Environment.TickCount, Key.Escape)
            { RoutedEvent = Keyboard.KeyDownEvent });
        await Settle(window);
        check(!quality.IsDropDownOpen && model.SelectedQuality.Bitrate == 192,
            "The audio menu still changes selection and closes with Escape after repeated language switches");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeBounds { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr handle, out NativeBounds bounds);

    private static async Task Settle(Window window)
    {
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        await Task.Delay(30);
        window.UpdateLayout();
    }

    private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match) return match;
            if (FindDescendant<T>(child) is { } descendant) return descendant;
        }
        return null;
    }
}
