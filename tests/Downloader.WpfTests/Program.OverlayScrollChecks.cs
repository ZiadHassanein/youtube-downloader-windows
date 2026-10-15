using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckOverlayScrolling(Window owner, MainViewModel model, string captures)
    {
        bool originalArabic = model.IsArabic, originalDark = model.IsDark;
        try
        {
            foreach (bool arabic in new[] { false, true })
                foreach (bool dark in new[] { false, true })
                    foreach (bool inset in new[] { false, true })
                    {
                        model.SelectLanguageCommand.Execute(arabic ? "ar" : "en");
                        if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                        string variant = (inset ? "inset" : "edge") + (arabic ? "-ar" : "-en") + (dark ? "-dark" : "-light");
                        var editor = new TextBox { Text = "https://example.com/local-scrollbar-fixture", FlowDirection = FlowDirection.LeftToRight };
                        var action = new Button { Content = arabic ? "تنزيل" : "Download", Margin = new Thickness(0, 16, 0, 0) };
                        var spacer = new Border { Height = 0 };
                        var body = new StackPanel { Margin = new Thickness(inset ? 0 : 30, 12, inset ? 0 : 30, 12) };
                        body.Children.Add(new TextBlock { Text = "Local scrollbar fixture", Margin = new Thickness(0, 0, 0, 16) });
                        body.Children.Add(editor);
                        body.Children.Add(action);
                        body.Children.Add(spacer);
                        body.Children.Add(new TextBlock { Text = arabic ? "نهاية المحتوى" : "End of content", Margin = new Thickness(0, 16, 0, 0) });
                        var viewport = new ScrollViewer
                        {
                            Style = (Style)Application.Current.FindResource(inset ? "InsetScrollFrame" : "VerticalScrollFrame"),
                            FlowDirection = arabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                            Content = body
                        };
                        var root = new Grid { Margin = new Thickness(28), FlowDirection = FlowDirection.LeftToRight };
                        root.Children.Add(viewport);
                        var dialog = new Window { Owner = owner, Width = 680, Height = 460, Content = root, ShowInTaskbar = false };
                        dialog.SetResourceReference(Control.BackgroundProperty, "Page");
                        dialog.SetResourceReference(Control.ForegroundProperty, "Ink");
                        try
                        {
                            dialog.Show();
                            await Task.Delay(80);
                            dialog.UpdateLayout();
                            var presenter = (FrameworkElement)viewport.Template.FindName("PART_ScrollContentPresenter", viewport);
                            var bar = (ScrollBar)viewport.Template.FindName("PART_VerticalScrollBar", viewport);
                            Rect initialEditor = RelativeBounds(editor, root), initialAction = RelativeBounds(action, root);
                            Check(bar.Visibility == Visibility.Collapsed && Math.Abs(presenter.ActualWidth - viewport.ActualWidth) < 1,
                                "Short content uses the full viewport width in " + variant);
                            Capture(dialog, System.IO.Path.Combine(captures, "overlay-" + variant + "-short.png"));
                            for (int cycle = 0; cycle < 3; cycle++)
                            {
                                spacer.Height = 700;
                                dialog.UpdateLayout();
                                Rect currentEditor = RelativeBounds(editor, root), currentAction = RelativeBounds(action, root);
                                Check(bar.Visibility == Visibility.Visible && SameHorizontalBounds(initialEditor, currentEditor) &&
                                    SameHorizontalBounds(initialAction, currentAction) && Math.Abs(presenter.ActualWidth - viewport.ActualWidth) < 1,
                                    $"Overflow does not shift or shrink fields/buttons in {variant}, cycle {cycle + 1}");
                                if (cycle == 0)
                                {
                                    Rect barBounds = RelativeBounds(bar, root);
                                    Check(!barBounds.IntersectsWith(currentEditor) && !barBounds.IntersectsWith(currentAction) &&
                                        (arabic ? barBounds.Right <= currentEditor.Left : barBounds.Left >= currentEditor.Right),
                                        "Overlay target stays clear of input borders and controls in " + variant);
                                    Rect clientBounds = RelativeBounds(bar, (FrameworkElement)dialog.Content);
                                    Check(clientBounds.Left >= -28 && clientBounds.Right <= root.ActualWidth + 28 &&
                                        clientBounds.Top >= -28 && clientBounds.Bottom <= root.ActualHeight + 28,
                                        "Overlay scrollbar remains within the window client in " + variant);
                                    var center = bar.TranslatePoint(new Point(bar.ActualWidth / 2, bar.ActualHeight / 2), dialog);
                                    var hit = dialog.InputHitTest(center) as DependencyObject;
                                    Check(hit != null && (ReferenceEquals(hit, bar) || bar.IsAncestorOf(hit)),
                                        "Overlay target can receive pointer input in " + variant);
                                    Capture(dialog, System.IO.Path.Combine(captures, "overlay-" + variant + "-overflow.png"));
                                    ScrollBar.PageDownCommand.Execute(null, bar);
                                    dialog.UpdateLayout();
                                    Check(viewport.VerticalOffset > 0 && Math.Abs(bar.Value - viewport.VerticalOffset) < 1,
                                        "Overlay paging updates content and thumb in " + variant);
                                    viewport.ScrollToEnd();
                                    dialog.UpdateLayout();
                                    Check(Math.Abs(viewport.VerticalOffset - viewport.ScrollableHeight) < 1,
                                        "Last content stays reachable in " + variant);
                                    viewport.ScrollToTop();
                                }
                                spacer.Height = 0;
                                dialog.UpdateLayout();
                                Check(bar.Visibility == Visibility.Collapsed && SameHorizontalBounds(initialEditor, RelativeBounds(editor, root)),
                                    $"Removing overflow preserves field position in {variant}, cycle {cycle + 1}");
                            }
                        }
                        finally { dialog.Close(); }
                    }
        }
        finally
        {
            model.SelectLanguageCommand.Execute(originalArabic ? "ar" : "en");
            if (model.IsDark != originalDark) model.ThemeCommand.Execute(null);
            owner.Activate();
        }
    }

    private static Rect RelativeBounds(FrameworkElement element, Visual ancestor) =>
        element.TransformToAncestor(ancestor).TransformBounds(new Rect(element.RenderSize));

    private static bool SameHorizontalBounds(Rect before, Rect after) =>
        Math.Abs(before.Left - after.Left) < 1 && Math.Abs(before.Width - after.Width) < 1;
}
