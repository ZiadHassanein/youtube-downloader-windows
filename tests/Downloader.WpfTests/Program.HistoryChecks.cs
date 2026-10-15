using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckRecentDownloads(Window owner, MainViewModel model, string captures)
    {
        var actions = new DesktopActions(owner);
        string longTitle = string.Concat(Enumerable.Repeat("Local UI fixture — عنوان عربي طويل — ", 3));
        string fixture = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ViFree-history-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(fixture);
        string[] files = Enumerable.Range(1, 7).Select(index => System.IO.Path.Combine(fixture, $"{index} — {longTitle}.mp3")).ToArray();
        // Synthetic bytes exercise file presence/size only; never launch them as real media.
        foreach (string file in files.Take(4)) System.IO.File.WriteAllBytes(file, new byte[2048]);
        double originalHeight = owner.Height;
        foreach (bool arabic in new[] { false, true })
            foreach (bool dark in new[] { false, true })
                foreach (int entries in new[] { 1, 7, 0 })
                {
                    model.SelectLanguageCommand.Execute(arabic ? "ar" : "en");
                    if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                    owner.Height = entries == 7 ? 500 : originalHeight;
                    await Task.Delay(100);
                    string variant = (arabic ? "ar" : "en") + (dark ? "-dark" : "-light") + "-" + entries;
                    var completed = new TaskCompletionSource<Exception>();
                    _ = owner.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(async () =>
                    {
                        Window dialog = null;
                        Exception failure = null;
                        try
                        {
                            dialog = owner.OwnedWindows.Cast<Window>().Single();
                            await Task.Delay(150);
                            dialog.UpdateLayout();
                            Capture(dialog, System.IO.Path.Combine(captures, "history-" + variant + ".png"));
                            Check(dialog.FlowDirection == model.ContentDirection && Equals(dialog.Background, owner.Background),
                                "Recent downloads follows the current language and theme");
                            var historyModel = (RecentDownloadsViewModel)dialog.DataContext;
                            var reveals = Descendants<Button>(dialog).Where(button => Equals(button.Content, model.RevealLabel)).ToArray();
                            Check(reveals.Length == Math.Min(entries, 5) && reveals.All(button => button.IsEnabled == ((RecentDownloadView)button.DataContext).Available),
                                "Every history row has its own reveal action; moved/deleted files stay visibly unavailable");
                            Check(dialog.ActualWidth <= owner.ActualWidth - 47 && dialog.ActualHeight <= owner.ActualHeight - 47 &&
                                (entries != 1 || dialog.ActualHeight <= 370), "History adapts to its owner and a single file does not leave a tall empty panel");
                            Check(historyModel.Heading == model.HistoryLabel && (entries != 0 || historyModel.IsEmpty), "History heading and empty state use the current language");
                            if (entries > 0)
                            {
                                var list = Descendants<ListBox>(dialog).Single();
                                var viewport = Descendants<ScrollViewer>(list).Single();
                                Check(viewport.ComputedHorizontalScrollBarVisibility != Visibility.Visible && viewport.ScrollableWidth <= 1,
                                    $"Long mixed-language history names do not create a horizontal scrollbar (scroll={viewport.ScrollableWidth}, visible={viewport.ComputedHorizontalScrollBarVisibility})");
                                Check(list.Items.Count == Math.Min(entries, 5), "Recent downloads keeps its five-file limit");
                                var item = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
                                var labels = Descendants<TextBlock>(item).ToArray();
                                var fileView = (RecentDownloadView)list.Items[0];
                                Check(Equals(list.Foreground, Application.Current.Resources["Ink"]) && Equals(item.Foreground, Application.Current.Resources["Ink"]), "History titles retain readable themed text instead of system ListBox colors");
                                Check(fileView.FilePath == files[0] && fileView.Title == System.IO.Path.GetFileNameWithoutExtension(files[0]) &&
                                    Descendants<Grid>(item).Any(grid => Equals(grid.ToolTip, files[0])) && labels.Any(label => label.Text == fileView.Title && label.MaxHeight == 22),
                                    "History keeps a compact title, distinct file details, full-path tooltip and exact reveal target");
                                Check(labels.All(label => label.ActualWidth <= viewport.ViewportWidth + 1),
                                    "Both history labels remain within the list viewport");
                                if (entries > 1)
                                {
                                    list.SelectedIndex = list.Items.Count - 1;
                                    list.ScrollIntoView(list.SelectedItem);
                                    await Task.Delay(75);
                                    dialog.UpdateLayout();
                                    var last = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(list.SelectedIndex);
                                    var bounds = last.TransformToAncestor(viewport).TransformBounds(new Rect(last.RenderSize));
                                    Check(viewport.ScrollableHeight > 0 && bounds.Bottom <= viewport.ActualHeight + 1,
                                        $"Short history dialogs can scroll vertically to the last file and its action (scroll={viewport.ScrollableHeight}, offset={viewport.VerticalOffset}, viewport={viewport.ActualHeight}, last={bounds})");
                                    Capture(dialog, System.IO.Path.Combine(captures, "history-" + variant + "-last.png"));
                                }
                                if (entries == 1)
                                {
                                    System.IO.File.Delete(files[0]);
                                    try
                                    {
                                        var peer = new System.Windows.Automation.Peers.ButtonAutomationPeer(reveals[0]);
                                        ((System.Windows.Automation.Provider.IInvokeProvider)peer.GetPattern(System.Windows.Automation.Peers.PatternInterface.Invoke)).Invoke();
                                        await Task.Delay(75); dialog.UpdateLayout();
                                        Check(historyModel.HasError && historyModel.Error == Texts.For(arabic ? "ar" : "en", "This file is no longer in its saved location."),
                                            arabic ? "Arabic reveal failures give translated recovery feedback" : "Reveal failures give clear recovery feedback");
                                        Check(!arabic || historyModel.Error != "This file is no longer in its saved location.", "Arabic history recovery does not fall back to English");
                                        Capture(dialog, System.IO.Path.Combine(captures, "history-" + variant + "-missing.png"));
                                    }
                                    finally { System.IO.File.WriteAllBytes(files[0], new byte[2048]); }
                                }
                            }
                        }
                        catch (Exception exception) { failure = exception; }
                        finally
                        {
                            dialog?.Close();
                            completed.SetResult(failure);
                        }
                    }));
                    actions.ShowHistory(files.Take(entries), text => Texts.For(arabic ? "ar" : "en", text).Replace("&", ""));
                    if (await completed.Task is { } error) throw error;
                }
        owner.Height = originalHeight;
        model.SelectLanguageCommand.Execute("en");
    }

    private static async Task CheckHorizontalScrolling(Window owner, string captures)
    {
        var viewport = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new Border { Width = 1200, Height = 1000, Background = (Brush)Application.Current.Resources["Surface"] }
        };
        var dialog = new Window { Owner = owner, Width = 440, Height = 320, Content = viewport, ShowInTaskbar = false };
        try
        {
            dialog.Show();
            await Task.Delay(150);
            dialog.UpdateLayout();
            var horizontal = Descendants<ScrollBar>(viewport).Single(bar => bar.Orientation == Orientation.Horizontal);
            var track = (Track)horizontal.Template.FindName("PART_Track", horizontal);
            Check(track.Orientation == Orientation.Horizontal && horizontal.ActualWidth > 300 && horizontal.ActualHeight <= 16 &&
                track.Thumb.ActualWidth >= 28 && track.Thumb.ActualHeight <= 16,
                "An actual horizontal scrollbar renders a horizontal thumb instead of a vertical dot");
            double before = viewport.HorizontalOffset;
            ScrollBar.PageRightCommand.Execute(null, horizontal);
            await Task.Delay(75);
            Check(viewport.HorizontalOffset > before, "Horizontal page-right moves content along the horizontal axis");
            ScrollBar.PageLeftCommand.Execute(null, horizontal);
            await Task.Delay(75);
            Check(viewport.HorizontalOffset < viewport.ScrollableWidth && viewport.VerticalOffset == 0,
                "Horizontal page-left does not move content vertically");
            Capture(dialog, System.IO.Path.Combine(captures, "horizontal-scrollbar.png"));
        }
        finally { dialog.Close(); }
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
}
