using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckQueueWindow(Window owner, MainViewModel model, FakeProvider provider, string captures)
    {
        double originalHeight = owner.Height;
        double originalWidth = owner.Width;
        model.Url = "";
        model.Folder = System.IO.Path.GetTempPath();
        foreach (var entry in model.Queue.Entries) model.RemoveQueueCommand.Execute(entry.Id);
        var trigger = (Button)WpfTree.Find(owner, "RecentDownloadsButton");
        foreach (bool arabic in new[] { false, true })
            foreach (bool dark in new[] { false, true })
                foreach (bool shortWindow in new[] { false, true })
                {
                    model.SelectLanguageCommand.Execute(arabic ? "ar" : "en");
                    if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                    owner.Width = shortWindow ? 820 : 1160;
                    owner.Height = shortWindow ? 500 : originalHeight;
                    await Task.Delay(100);
                    trigger.Focus();
                    string variant = (arabic ? "ar" : "en") + (dark ? "-dark" : "-light") + (shortWindow ? "-short" : "-normal");
                    await InspectQueueModal(owner, model, async dialog =>
                    {
                        var input = (TextBox)WpfTree.Find(dialog, "QueueLinksInput");
                        var viewport = (ScrollViewer)WpfTree.Find(dialog, "QueueViewport");
                        var start = (Button)WpfTree.Find(dialog, "QueueStartButton");
                        var cancel = (Button)WpfTree.Find(dialog, "QueueCancelButton");
                        var close = (Button)WpfTree.Find(dialog, "QueueCloseButton");
                        var empty = (TextBlock)WpfTree.Find(dialog, "QueueEmptyState");
                        GetWindowRect(new WindowInteropHelper(dialog).Handle, out var bounds);
                        GetWindowRect(new WindowInteropHelper(owner).Handle, out var ownerBounds);
                        Check(dialog.Owner == owner && !IsWindowEnabled(new WindowInteropHelper(owner).Handle) &&
                            bounds.Left >= ownerBounds.Left && bounds.Right <= ownerBounds.Right && bounds.Top >= ownerBounds.Top && bounds.Bottom <= ownerBounds.Bottom,
                            "The queue is an owned modal contained within its main window");
                        Check(dialog.FlowDirection == FlowDirection.LeftToRight && ((Grid)WpfTree.Find(dialog, "QueueContent")).FlowDirection == model.ContentDirection &&
                            input.FlowDirection == FlowDirection.LeftToRight && Equals(dialog.Background, owner.Background),
                            "Queue content mirrors Arabic while links and native window chrome retain LTR");
                        Check(input.IsKeyboardFocusWithin && empty.IsVisible && !start.IsEnabled && cancel.Visibility == Visibility.Collapsed,
                            $"An empty queue focuses link entry, explains its state and hides idle cancellation ({variant}: focus={input.IsKeyboardFocusWithin}, active={dialog.IsActive}, empty={empty.IsVisible}, start={start.IsEnabled}, cancel={cancel.Visibility})");
                        Check(!((FrameworkElement)WpfTree.Find(dialog, "QueueNotice")).IsVisible,
                            "Reopening the queue clears stale feedback from its previous session");
                        Check(viewport.ScrollableWidth <= 1, "Queue content has no horizontal overflow");
                        Capture(dialog, System.IO.Path.Combine(captures, "queue-empty-" + variant + ".png"));
                        input.Text = "https://youtu.be/jNQXAC9IVRw\nhttps://youtu.be/aqz-KE-bpKQ\nhttps://youtu.be/jNQXAC9IVRw";
                        ((Button)WpfTree.Find(dialog, "AddQueueLinksButton")).Command.Execute(null);
                        await Task.Delay(100);
                        dialog.UpdateLayout();
                        Check(model.QueueItems.Count == 2 && input.Text == "" && !empty.IsVisible && start.IsEnabled,
                            "Adding multiple links deduplicates, clears the editor and enables Start");
                        Check(((FrameworkElement)WpfTree.Find(dialog, "QueueNotice")).IsVisible,
                            "Queue feedback appears next to its link-entry workflow");
                        double footerTop = ((FrameworkElement)WpfTree.Find(dialog, "QueueFooter")).TranslatePoint(new Point(), dialog).Y;
                        viewport.ScrollToEnd();
                        await Task.Delay(75);
                        dialog.UpdateLayout();
                        var footer = (FrameworkElement)WpfTree.Find(dialog, "QueueFooter");
                        var footerBounds = footer.TransformToAncestor(dialog).TransformBounds(new Rect(footer.RenderSize));
                        Check(Math.Abs(footerTop - footerBounds.Top) < 1 && footerBounds.Bottom <= dialog.ActualHeight &&
                            close.IsVisible && start.IsVisible && (!shortWindow || viewport.ScrollableHeight > 0),
                            "Short queue dialogs scroll their body while keeping footer actions fixed and reachable");
                        Capture(dialog, System.IO.Path.Combine(captures, "queue-filled-" + variant + ".png"));
                        var last = model.QueueItems.Last();
                        model.RemoveQueueCommand.Execute(last.Id);
                        Check(model.QueueItems.Count == 1, "The queue retains its actual remove command");
                        foreach (var entry in model.Queue.Entries) model.RemoveQueueCommand.Execute(entry.Id);
                        close.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    });
                    await Task.Delay(50);
                    Check(IsWindowEnabled(new WindowInteropHelper(owner).Handle) && trigger.IsKeyboardFocused,
                        "Closing the queue restores the original control's keyboard focus");
                }
        owner.Width = 1160;
        owner.Height = originalHeight;
        await InspectQueueModal(owner, model, async dialog =>
        {
            var input = (TextBox)WpfTree.Find(dialog, "QueueLinksInput");
            var add = (Button)WpfTree.Find(dialog, "AddQueueLinksButton");
            input.Text = "not a valid media link";
            add.Command.Execute(null);
            await Task.Delay(75);
            Check(model.QueueItems.Count == 0 && input.Text.Length > 0 && ((FrameworkElement)WpfTree.Find(dialog, "QueueNotice")).IsVisible,
                "An invalid queue link stays editable and its feedback stays in the dialog");
            Capture(dialog, System.IO.Path.Combine(captures, "queue-invalid-ar-dark.png"));
            input.Text = "https://youtu.be/jNQXAC9IVRw";
            add.Command.Execute(null);
            provider.DownloadDelayMilliseconds = 5000;
            var running = model.QueueStartCommand.ExecuteAsync(null);
            try
            {
                for (int attempt = 0; attempt < 30 && !model.QueueItems.Any(item => item.IsActive && item.Percent > 0); attempt++) await Task.Delay(50);
                var cancel = (Button)WpfTree.Find(dialog, "QueueCancelButton");
                var viewport = (ScrollViewer)WpfTree.Find(dialog, "QueueViewport");
                viewport.ScrollToEnd();
                dialog.UpdateLayout();
                Check(model.QueueRunning && cancel.IsVisible && cancel.IsEnabled && Descendants<ProgressBar>(dialog).Any(bar => bar.IsVisible),
                    "A running queue displays real item progress and an enabled cancellation action");
                Capture(dialog, System.IO.Path.Combine(captures, "queue-active-ar-dark.png"));
                cancel.Focus();
                cancel.Command.Execute(null);
                await running;
                await Task.Delay(100);
                viewport.ScrollToEnd();
                await Task.Delay(75);
                dialog.UpdateLayout();
                var cancelled = Descendants<TextBlock>(dialog).Single(label => label.Text == model.QueueItems.Single().Status);
                var statusBounds = cancelled.TransformToAncestor(viewport).TransformBounds(new Rect(cancelled.RenderSize));
                Check(!model.QueueRunning && cancel.Visibility == Visibility.Collapsed && model.QueueItems.Single().CanRetry &&
                    statusBounds.Top >= 0 && statusBounds.Bottom <= viewport.ActualHeight,
                    "Cancelling restores the idle footer and makes the full cancelled status and item retry reachable");
                Capture(dialog, System.IO.Path.Combine(captures, "queue-cancelled-ar-dark.png"));
                var retry = Descendants<Button>(dialog).Single(button => Equals(button.Content, model.RetryLabel));
                retry.Command.Execute(retry.CommandParameter);
                Check(model.QueueItems.Single().Entry.State == QueueState.Pending && ((Button)WpfTree.Find(dialog, "QueueStartButton")).IsEnabled,
                    "The visible Retry action resets the cancelled job and enables Start");
            }
            finally
            {
                model.Queue.Stop();
                await running;
                provider.DownloadDelayMilliseconds = 10000;
            }
            InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(dialog), Environment.TickCount, Key.Escape)
            { RoutedEvent = Keyboard.KeyDownEvent, Source = input });
            await Task.Delay(75);
            Check(!dialog.IsVisible, "Escape dismisses the queue modal");
        });
        foreach (var entry in model.Queue.Entries) model.RemoveQueueCommand.Execute(entry.Id);
        owner.Width = originalWidth;
        owner.Height = originalHeight;
        model.SelectLanguageCommand.Execute("en");
    }

    private static async Task InspectQueueModal(Window owner, MainViewModel model, Func<QueueWindow, Task> inspect)
    {
        var completion = new TaskCompletionSource<Exception>();
        _ = owner.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(async () =>
        {
            QueueWindow dialog = null;
            Exception failure = null;
            try
            {
                dialog = owner.OwnedWindows.OfType<QueueWindow>().Single();
                await Task.Delay(150);
                dialog.Activate();
                await Task.Delay(75);
                await inspect(dialog);
            }
            catch (Exception exception) { failure = exception; }
            finally
            {
                if (dialog?.IsVisible == true) dialog.Close();
                completion.TrySetResult(failure);
            }
        }));
        model.QueueCommand.Execute(null);
        if (await completion.Task is { } error) throw new InvalidOperationException("Queue modal check failed.", error);
    }
}
