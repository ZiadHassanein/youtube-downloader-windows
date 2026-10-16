using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckDropdownScrolling(Window window, MainViewModel model, string captures)
    {
        var quality = (ComboBox)WpfTree.Find(window, "QualityInput");
        model.Audio = true;
        model.Url = "https://youtu.be/jNQXAC9IVRw";
        await Task.Delay(850);
        model.SelectedEncoding = model.Encodings.First(choice => choice.Encoding == AudioEncoding.Mp3);
        foreach (bool arabic in new[] { false, true })
            foreach (bool dark in new[] { false, true })
            {
                model.SelectLanguageCommand.Execute(arabic ? "ar" : "en");
                if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                string variant = (arabic ? "ar" : "en") + (dark ? "-dark" : "-light");
                foreach (double width in new[] { 820d, 1160d })
                {
                    window.Width = width;
                    model.SelectedQuality = model.Qualities.First(choice => choice.Bitrate == 256);
                    quality.BringIntoView();
                    await Task.Delay(80);
                    quality.IsDropDownOpen = true;
                    await Task.Delay(150);
                    var popup = (Popup)quality.Template.FindName("PART_Popup", quality);
                    var source = (HwndSource)PresentationSource.FromVisual(popup.Child);
                    var viewer = Descendants<ScrollViewer>(popup.Child).Single();
                    viewer.ScrollToTop();
                    viewer.UpdateLayout();
                    CapturePopup((FrameworkElement)window.Content, source, System.IO.Path.Combine(captures,
                        "dropdown-" + variant + (width < 1000 ? "-compact.png" : "-wide.png")));
                    CheckDropdownLane(viewer, arabic, variant);
                    ScrollBar.PageDownCommand.Execute(null, (ScrollBar)viewer.Template.FindName("PART_VerticalScrollBar", viewer));
                    viewer.UpdateLayout();
                    Check(viewer.VerticalOffset > 0, "Dropdown paging remains usable in " + variant);
                    viewer.ScrollToEnd();
                    viewer.UpdateLayout();
                    var last = (ComboBoxItem)quality.ItemContainerGenerator.ContainerFromIndex(quality.Items.Count - 1);
                    var viewport = (ScrollContentPresenter)viewer.Template.FindName("PART_ScrollContentPresenter", viewer);
                    var lastBounds = RelativeBounds(last, viewport);
                    Check(lastBounds.Top >= -1 && lastBounds.Bottom <= viewport.ActualHeight + 1 && model.SelectedQuality.Bitrate == 256,
                        "The last quality stays reachable without changing selection in " + variant);
                    if (width < 1000) CapturePopup((FrameworkElement)window.Content, source,
                        System.IO.Path.Combine(captures, "dropdown-" + variant + "-bottom.png"));
                    quality.IsDropDownOpen = false;
                }

                var items = new ObservableCollection<string> { "Local dropdown fixture" };
                var fixture = new ComboBox { ItemsSource = items, SelectedIndex = 0, FlowDirection = model.ContentDirection,
                    FontFamily = model.ContentFont, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(24) };
                var root = new Grid { FlowDirection = FlowDirection.LeftToRight };
                root.Children.Add(fixture);
                var dialog = new Window { Owner = window, Width = 560, Height = 400, Content = root, ShowInTaskbar = false };
                dialog.SetResourceReference(Control.BackgroundProperty, "Page");
                try
                {
                    dialog.Show();
                    fixture.IsDropDownOpen = true;
                    await Task.Delay(120);
                    var popup = (Popup)fixture.Template.FindName("PART_Popup", fixture);
                    var viewer = Descendants<ScrollViewer>(popup.Child).Single();
                    var bar = (ScrollBar)viewer.Template.FindName("PART_VerticalScrollBar", viewer);
                    var row = (ComboBoxItem)fixture.ItemContainerGenerator.ContainerFromIndex(0);
                    var original = RelativeBounds(row, (FrameworkElement)popup.Child);
                    var originalControl = fixture.RenderSize;
                    Check(bar.Visibility == Visibility.Collapsed, "Short dropdowns hide the scrollbar in " + variant);
                    CapturePopup(root, (HwndSource)PresentationSource.FromVisual(popup.Child),
                        System.IO.Path.Combine(captures, "dropdown-fixture-" + variant + "-short.png"));
                    for (int cycle = 0; cycle < 3; cycle++)
                    {
                        for (int index = 1; index <= 16; index++) items.Add("Local option " + index);
                        await Task.Delay(80);
                        Check(bar.Visibility == Visibility.Visible && SameHorizontalBounds(original, RelativeBounds(row, (FrameworkElement)popup.Child)) &&
                            fixture.RenderSize == originalControl,
                            "Overflow keeps dropdown row/control width and position stable in " + variant);
                        if (cycle == 0)
                        {
                            CheckDropdownLane(viewer, arabic, "fixture-" + variant);
                            CapturePopup(root, (HwndSource)PresentationSource.FromVisual(popup.Child),
                                System.IO.Path.Combine(captures, "dropdown-fixture-" + variant + "-long.png"));
                        }
                        while (items.Count > 1) items.RemoveAt(items.Count - 1);
                        await Task.Delay(80);
                        Check(bar.Visibility == Visibility.Collapsed && SameHorizontalBounds(original, RelativeBounds(row, (FrameworkElement)popup.Child)) &&
                            fixture.RenderSize == originalControl,
                            "Removing overflow keeps dropdown row/control bounds stable in " + variant);
                    }
                }
                finally { dialog.Close(); }
            }
        window.Activate();
        model.Url = "";
    }

    private static void CheckDropdownLane(ScrollViewer viewer, bool arabic, string variant)
    {
        var bar = (ScrollBar)viewer.Template.FindName("PART_VerticalScrollBar", viewer);
        var viewport = (ScrollContentPresenter)viewer.Template.FindName("PART_ScrollContentPresenter", viewer);
        Rect ScreenBounds(FrameworkElement element) => new(element.PointToScreen(new Point()),
            element.PointToScreen(new Point(element.ActualWidth, element.ActualHeight)));
        var barBounds = ScreenBounds(bar);
        double clearance = 3 * VisualTreeHelper.GetDpi(viewer).DpiScaleX;
        var rows = Descendants<ComboBoxItem>(viewport).ToArray();
        Check(bar.Visibility == Visibility.Visible && rows.Length > 0 && rows.All(row =>
        {
            var face = (FrameworkElement)row.Template.FindName("Face", row);
            var bounds = ScreenBounds(face);
            return arabic ? bounds.Left - barBounds.Right >= clearance : barBounds.Left - bounds.Right >= clearance;
        }), $"The complete scrollbar target stays beside every option face with clearance in {variant} (bar={barBounds}, padding={viewer.Padding})");
        var point = bar.TranslatePoint(new Point(bar.ActualWidth / 2, bar.ActualHeight / 2), viewer);
        var hit = viewer.InputHitTest(point) as DependencyObject;
        Check(hit != null && (ReferenceEquals(hit, bar) || bar.IsAncestorOf(hit)),
            "The dropdown scrollbar receives pointer input in its own gutter in " + variant);
    }
}
