using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckOutputOptionsModal(Window owner, MainViewModel model, FakeProvider provider, string captures)
    {
        provider.CaptionTracks = new() { new("en", "English"), new("ar", "العربية") };
        model.Url = "https://youtu.be/jNQXAC9IVRw";
        await Task.Delay(850);
        var more = (Button)WpfTree.Find(owner, "MoreOptionsButton");
        foreach (bool arabic in new[] { false, true })
        {
            if (model.IsArabic != arabic) model.LanguageCommand.Execute(null);
            foreach (bool dark in new[] { false, true })
            {
                if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                await Task.Delay(150);
                double height = owner.ActualHeight;
                more.Focus();
                await InspectOptionsModal(owner, model, async dialog =>
                {
                    Check(dialog.Owner == owner && !IsWindowEnabled(new WindowInteropHelper(owner).Handle),
                        "More options opens an owned modal that blocks the main window");
                    Check(ReferenceEquals(dialog.DataContext, model) && dialog.FlowDirection == model.ContentDirection &&
                        Equals(dialog.Background, owner.Background) && dialog.Title == model.OptionsLabel,
                        "The modal follows the current language, theme and settings");
                    var caption = (ComboBox)WpfTree.Find(dialog, "CaptionInput");
                    var vtt = (RadioButton)WpfTree.Find(dialog, "VttOption");
                    Check(caption.IsKeyboardFocusWithin, "Initial modal focus starts on the subtitle selector");
                    caption.SelectedItem = model.CaptionChoices.First(c => c.Language == "en");
                    vtt.IsChecked = true;
                    Check(model.SelectedCaption.Language == "en" && model.VttCaptions,
                        "Modal controls change the actual subtitle language and file format");
                    var done = (Button)WpfTree.Find(dialog, "DoneButton");
                    done.Focus();
                    done.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    Check(caption.IsKeyboardFocusWithin, "Tab cycles inside the modal");
                    GetWindowRect(new WindowInteropHelper(dialog).Handle, out var bounds);
                    GetWindowRect(new WindowInteropHelper(owner).Handle, out var ownerBounds);
                    Check(bounds.Left >= ownerBounds.Left && bounds.Right <= ownerBounds.Right &&
                        bounds.Top >= ownerBounds.Top && bounds.Bottom <= ownerBounds.Bottom,
                        "The options dialog stays inside its owner bounds");
                    await Task.Delay(100);
                    Capture(dialog, System.IO.Path.Combine(captures, "options-" + (arabic ? "ar" : "en") + (dark ? "-dark" : "-light") + ".png"));
                    done.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Check(!dialog.IsVisible, "Done closes the modal");
                });
                await Task.Delay(100);
                Check(IsWindowEnabled(new WindowInteropHelper(owner).Handle) && more.IsKeyboardFocused &&
                    Math.Abs(owner.ActualHeight - height) <= 2 && model.VttCaptions && model.SelectedCaption.Language == "en",
                    "Closing restores focus, retains choices and leaves the main layout unchanged");
            }
        }
        double previousMaxHeight = owner.MaxHeight;
        double previousHeight = owner.Height;
        owner.MaxHeight = 500;
        owner.Width = 820;
        owner.Height = 500;
        await Task.Delay(150);
        await InspectOptionsModal(owner, model, dialog =>
        {
            var done = (Button)WpfTree.Find(dialog, "DoneButton");
            var doneBounds = done.TransformToAncestor(dialog).TransformBounds(new Rect(done.RenderSize));
            Check(dialog.ActualHeight <= owner.ActualHeight - 46 && doneBounds.Bottom <= dialog.ActualHeight &&
                ((ScrollViewer)WpfTree.Find(dialog, "OptionsViewport")).ScrollableHeight > 0,
                $"A short window scrolls the options body while keeping Done reachable (owner={owner.ActualHeight}, dialog={dialog.ActualHeight}, buttonBottom={doneBounds.Bottom}, scroll={((ScrollViewer)WpfTree.Find(dialog, "OptionsViewport")).ScrollableHeight})");
            Capture(dialog, System.IO.Path.Combine(captures, "options-ar-dark-short.png"));
            return Task.CompletedTask;
        });
        owner.MaxHeight = previousMaxHeight;
        owner.Height = previousHeight;
        owner.Width = 1160;
        await Task.Delay(150);
        await InspectOptionsModal(owner, model, async dialog =>
        {
            Check(model.VttCaptions && Equals(((ComboBox)WpfTree.Find(dialog, "CaptionInput")).SelectedItem, model.SelectedCaption),
                "Reopening options preserves the current choices");
            InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(dialog), Environment.TickCount, Key.Escape)
            {
                RoutedEvent = Keyboard.KeyDownEvent,
                Source = WpfTree.Find(dialog, "CaptionInput")
            });
            await Task.Delay(100);
            Check(!dialog.IsVisible, "Escape dismisses the options modal");
        });
        model.AddQueueCommand.Execute(null);
        var entry = model.Queue.Entries.Single();
        Check(entry.Options.Captions is { Language: "en", Format: CaptionFormat.Vtt },
            "Queued downloads receive the subtitle settings selected in the modal");
        model.RemoveQueueCommand.Execute(entry.Id);
        model.Url = "https://www.youtube.com/playlist?list=PLFixture";
        await Task.Delay(850);
        await InspectOptionsModal(owner, model, async dialog =>
        {
            Check(((Button)WpfTree.Find(dialog, "GetLanguages")).IsVisible, "Playlist subtitle lookup remains available in the modal");
            await model.CaptionLanguagesCommand.ExecuteAsync(null);
            await Task.Delay(100);
            Check(model.OptionsNoticeVisible && !model.MainNoticeVisible && ((FrameworkElement)WpfTree.Find(dialog, "OptionsNotice")).IsVisible &&
                ((ComboBox)WpfTree.Find(dialog, "CaptionInput")).Items.Count == 3,
                "Playlist language results and their explanation appear inside the modal");
            Capture(dialog, System.IO.Path.Combine(captures, "options-playlist-result.png"));
            provider.InspectionHook = _ => throw new System.Net.Http.HttpRequestException("Fixture network failure");
            await model.CaptionLanguagesCommand.ExecuteAsync(null);
            await Task.Delay(100);
            Check(model.OptionsNoticeVisible && !model.MainNoticeVisible && Equals(model.NoticeBrush, Application.Current.Resources["Error"]) &&
                model.NoticeDetail.Contains(model.CaptionLanguagesLabel),
                "Language lookup errors stay inside the modal and name its available retry action");
            Capture(dialog, System.IO.Path.Combine(captures, "options-playlist-error.png"));
        });
        Check(!model.OptionsNoticeVisible, "Closing clears the modal's transient message");
        provider.InspectionHook = token => { token.WaitHandle.WaitOne(3000); token.ThrowIfCancellationRequested(); };
        await InspectOptionsModal(owner, model, async dialog =>
        {
            var loading = model.CaptionLanguagesCommand.ExecuteAsync(null);
            await Task.Delay(100);
            Check(model.IsBusy && model.CaptionLanguagesCommand.IsRunning, "Language loading has a real busy state");
            dialog.Close();
            await loading;
            Check(model.IsIdle && !model.OptionsNoticeVisible, "Closing cancels an active lookup without stale feedback");
        });
        provider.InspectionHook = null;
        provider.CaptionTracks = new();
        model.Url = "https://youtu.be/jNQXAC9IVRw";
        await Task.Delay(850);
        model.Url = "";
    }

    private static async Task InspectOptionsModal(Window owner, MainViewModel model, Func<OutputOptionsWindow, Task> inspect)
    {
        var completion = new TaskCompletionSource<Exception>();
        _ = owner.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(async () =>
        {
            OutputOptionsWindow dialog = null;
            Exception failure = null;
            try
            {
                dialog = owner.OwnedWindows.OfType<OutputOptionsWindow>().Single();
                await Task.Delay(250);
                await inspect(dialog);
            }
            catch (Exception ex) { failure = ex; }
            finally
            {
                if (dialog?.IsVisible == true) dialog.Close();
                completion.TrySetResult(failure);
            }
        }));
        model.OptionsCommand.Execute(null);
        if (await completion.Task is { } error) throw new InvalidOperationException("Options modal check failed.", error);
    }

    [DllImport("user32.dll")]
    private static extern bool IsWindowEnabled(IntPtr handle);
}
