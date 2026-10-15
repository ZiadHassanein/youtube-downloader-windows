using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
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
                    owner.Activate();
                    trigger.Focus();
                    await Task.Delay(75);
                    string variant = (arabic ? "ar" : "en") + (dark ? "-dark" : "-light") + (shortWindow ? "-short" : "-normal");
                    await InspectQueueModal(owner, model, async dialog =>
                    {
                        var input = QueueFields(dialog).Single();
                        var viewport = (ScrollViewer)WpfTree.Find(dialog, "QueueViewport");
                        var start = (Button)WpfTree.Find(dialog, "QueueStartButton");
                        var cancel = (Button)WpfTree.Find(dialog, "QueueCancelButton");
                        var close = (Button)WpfTree.Find(dialog, "QueueCloseButton");
                        var jobs = (FrameworkElement)WpfTree.Find(dialog, "QueuedDownloadsSection");
                        GetWindowRect(new WindowInteropHelper(dialog).Handle, out var bounds);
                        GetWindowRect(new WindowInteropHelper(owner).Handle, out var ownerBounds);
                        Check(dialog.Owner == owner && !IsWindowEnabled(new WindowInteropHelper(owner).Handle) &&
                            bounds.Left >= ownerBounds.Left && bounds.Right <= ownerBounds.Right && bounds.Top >= ownerBounds.Top && bounds.Bottom <= ownerBounds.Bottom,
                            "The queue is an owned modal contained within its main window");
                        Check(dialog.FlowDirection == FlowDirection.LeftToRight && ((Grid)WpfTree.Find(dialog, "QueueContent")).FlowDirection == model.ContentDirection &&
                            input.FlowDirection == FlowDirection.LeftToRight && Equals(dialog.Background, owner.Background),
                            "Queue content mirrors Arabic while links and native window chrome retain LTR");
                        Check(input.IsKeyboardFocusWithin && !jobs.IsVisible && !start.IsEnabled && cancel.Visibility == Visibility.Collapsed,
                            $"An empty queue focuses its link field without redundant empty-list controls ({variant}: focus={input.IsKeyboardFocusWithin}, active={dialog.IsActive}, jobs={jobs.IsVisible}, start={start.IsEnabled}, cancel={cancel.Visibility})");
                        Check(!((FrameworkElement)WpfTree.Find(dialog, "QueueNotice")).IsVisible,
                            "Reopening the queue clears stale feedback from its previous session");
                        Check(viewport.ScrollableWidth <= 1, "Queue content has no horizontal overflow");
                        Capture(dialog, System.IO.Path.Combine(captures, "queue-empty-" + variant + ".png"));
                        InvokeQueueButton((Button)WpfTree.Find(dialog, "AddLinkFieldButton"));
                        await Task.Delay(100);
                        Check(QueueFields(dialog).Length == 2 && QueueFields(dialog).Last().IsKeyboardFocusWithin,
                            "Add another link creates and focuses a separate single-line editor");
                        var removeField = Descendants<Button>(dialog).Last(button => AutomationProperties.GetName(button) == model.RemoveLinkLabel);
                        InvokeQueueButton(removeField);
                        await Task.Delay(100);
                        Check(QueueFields(dialog).Length == 1 && QueueFields(dialog).Single().IsKeyboardFocusWithin,
                            "Removing an input keeps one editable field and restores link-entry focus");
                        input = QueueFields(dialog).Single();
                        input.RaiseEvent(new DataObjectPastingEventArgs(new DataObject(DataFormats.UnicodeText,
                            "https://youtu.be/jNQXAC9IVRw\nhttps://youtu.be/aqz-KE-bpKQ"), false, DataFormats.UnicodeText));
                        await Task.Delay(75);
                        Check(QueueFields(dialog).Length == 2 && QueueFields(dialog).All(field => !field.AcceptsReturn && !field.Text.Contains('\n')) && start.IsEnabled,
                            "A synthetic multi-link paste event creates one field per URL and enables direct Start");
                        Capture(dialog, System.IO.Path.Combine(captures, "queue-drafts-" + variant + ".png"));
                        ((Button)WpfTree.Find(dialog, "AddQueueLinksButton")).Command.Execute(null);
                        await Task.Delay(100);
                        dialog.UpdateLayout();
                        Check(model.QueueItems.Count == 2 && QueueFields(dialog).Single().Text == "" && jobs.IsVisible && start.IsEnabled,
                            "Adding link fields queues their URLs and leaves a fresh input for another link");
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
                    for (int settle = 0; settle < 10 && !IsWindowEnabled(new WindowInteropHelper(owner).Handle); settle++)
                        await Task.Delay(50);
                    Check(IsWindowEnabled(new WindowInteropHelper(owner).Handle) &&
                        ReferenceEquals(FocusManager.GetFocusedElement(owner), trigger) && (!owner.IsActive || trigger.IsKeyboardFocused),
                        $"Closing the queue restores its origin's logical focus and active-window keyboard focus (enabled={IsWindowEnabled(new WindowInteropHelper(owner).Handle)}, managedEnabled={owner.IsEnabled}, owned={owner.OwnedWindows.Count}, active={owner.IsActive}, logical={ReferenceEquals(FocusManager.GetFocusedElement(owner), trigger)}, keyboard={trigger.IsKeyboardFocused})");
                }
        owner.Width = 1160;
        owner.Height = originalHeight;
        await InspectQueueModal(owner, model, async dialog =>
        {
            var input = QueueFields(dialog).Single();
            var add = (Button)WpfTree.Find(dialog, "AddQueueLinksButton");
            input.Text = "not a valid media link";
            add.Command.Execute(null);
            await Task.Delay(75);
            Check(model.QueueItems.Count == 0 && input.Text.Length > 0 && model.QueueLinkInputs.Single().HasError &&
                Descendants<TextBlock>(dialog).Any(label => label.IsVisible && label.Text == model.QueueLinkInputs.Single().Error),
                "An invalid link retains its value and shows an error beside its own field");
            Capture(dialog, System.IO.Path.Combine(captures, "queue-invalid-ar-dark.png"));
            model.AddLinkFieldCommand.Execute(null);
            model.QueueLinkInputs.Last().Url = "https://youtu.be/jNQXAC9IVRw";
            add.Command.Execute(null);
            await Task.Delay(75);
            Check(model.QueueItems.Count == 1 && model.QueueLinkInputs.Single().HasError && input.Text == "not a valid media link",
                "A valid field can be queued without discarding another field's invalid value");
            input.Text = "https://youtu.be/jNQXAC9IVRw";
            add.Command.Execute(null);
            Check(model.QueueItems.Count == 1 && model.QueueLinkInputs.Single().HasError,
                "Duplicate links are identified on their field instead of being silently dropped");
            foreach (var entry in model.Queue.Entries) model.RemoveQueueCommand.Execute(entry.Id);
            input.Text = "https://youtu.be/jNQXAC9IVRw";
            provider.DownloadDelayMilliseconds = 5000;
            InvokeQueueButton((Button)WpfTree.Find(dialog, "QueueStartButton"));
            await Task.Delay(75);
            var running = model.QueueDownloadCommand.ExecutionTask ?? throw new InvalidOperationException("Start did not execute.");
            try
            {
                for (int attempt = 0; attempt < 30 && !model.QueueItems.Any(item => item.IsActive && item.Percent > 0); attempt++) await Task.Delay(50);
                var cancel = (Button)WpfTree.Find(dialog, "QueueCancelButton");
                var viewport = (ScrollViewer)WpfTree.Find(dialog, "QueueViewport");
                dialog.UpdateLayout();
                var progress = Descendants<ProgressBar>(dialog).Single(bar => bar.IsVisible);
                var progressBounds = progress.TransformToAncestor(viewport).TransformBounds(new Rect(progress.RenderSize));
                Check(model.QueueRunning && model.QueueItems.Count == 1 && QueueFields(dialog).Single().Text == "" &&
                    cancel.IsVisible && cancel.IsEnabled && progressBounds.Top >= 0 && progressBounds.Bottom <= viewport.ActualHeight,
                    "Start directly queues filled fields, reveals real progress and enables cancellation");
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
        await InspectQueueModal(owner, model, async dialog =>
        {
            dialog.Width = 580;
            dialog.Height = 360;
            while (model.AddLinkFieldCommand.CanExecute(null)) model.AddLinkFieldCommand.Execute(null);
            model.PasteQueueLinks(model.QueueLinkInputs[0], "https://youtu.be/jNQXAC9IVRw\nhttps://youtu.be/aqz-KE-bpKQ");
            await Task.Delay(100);
            var viewport = (ScrollViewer)WpfTree.Find(dialog, "QueueViewport");
            var footer = (FrameworkElement)WpfTree.Find(dialog, "QueueFooter");
            var bounds = footer.TransformToAncestor(dialog).TransformBounds(new Rect(footer.RenderSize));
            Check(model.QueueLinkInputs.Count == 100 && model.QueueLinkInputs[0].HasError &&
                !((Button)WpfTree.Find(dialog, "AddLinkFieldButton")).IsEnabled,
                "Draft fields enforce their 100-link limit without overwriting existing input");
            Check(viewport.ScrollableWidth <= 1 && viewport.ScrollableHeight > 0 && bounds.Bottom <= dialog.ActualHeight,
                "Many link fields scroll inside the smallest queue window while actions remain reachable");
            Capture(dialog, System.IO.Path.Combine(captures, "queue-many-fields-ar-dark.png"));
            foreach (var draft in model.QueueLinkInputs.Skip(1).ToArray()) model.RemoveLinkFieldCommand.Execute(draft);
            model.RemoveLinkFieldCommand.Execute(model.QueueLinkInputs[0]);
        });
        foreach (var entry in model.Queue.Entries) model.RemoveQueueCommand.Execute(entry.Id);
        owner.Width = originalWidth;
        owner.Height = originalHeight;
        model.SelectLanguageCommand.Execute("en");
    }

    private static TextBox[] QueueFields(Window dialog) => Descendants<TextBox>((ItemsControl)WpfTree.Find(dialog, "QueueInputFields")).ToArray();

    private static void InvokeQueueButton(Button button)
        => ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();

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
