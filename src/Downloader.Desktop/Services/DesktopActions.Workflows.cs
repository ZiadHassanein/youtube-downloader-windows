using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YouTubeDownloader;
public sealed partial class DesktopActions
{
    public void ShowOutputOptions(MainViewModel model)
    {
        var workArea = owner is MainWindow main ? main.CurrentWorkArea() : SystemParameters.WorkArea;
        var dialog = new OutputOptionsWindow
        {
            Owner = owner, DataContext = model, Icon = owner.Icon,
            Width = Math.Min(540, owner.ActualWidth - 48),
            MaxHeight = Math.Min(owner.ActualHeight - 48, workArea.Height - 48)
        };
        var previousFocus = System.Windows.Input.Keyboard.FocusedElement as UIElement;
        dialog.ShowDialog();
        if (previousFocus is { IsEnabled: true, IsVisible: true }) previousFocus.Focus();
    }

    public void ShowQueue(MainViewModel model)
    {
        model.ClearQueueMessage();
        var workArea = owner is MainWindow main ? main.CurrentWorkArea() : SystemParameters.WorkArea;
        var dialog = new QueueWindow
        {
            Owner = owner, DataContext = model, Icon = owner.Icon,
            MaxWidth = Math.Min(owner.ActualWidth - 48, workArea.Width - 48),
            MaxHeight = Math.Min(owner.ActualHeight - 48, workArea.Height - 48)
        };
        dialog.Width = Math.Min(dialog.Width, dialog.MaxWidth);
        dialog.Height = Math.Min(dialog.Height, dialog.MaxHeight);
        var previousFocus = System.Windows.Input.Keyboard.FocusedElement as UIElement;
        dialog.ShowDialog();
        if (previousFocus is { IsEnabled: true, IsVisible: true }) previousFocus.Focus();
    }
    public HashSet<string> SelectPlaylist(PlaylistInfo playlist, HashSet<string> selected, Func<string, string> t)
    {
        var dialog = Dialog(t("Choose videos"), 560);
        var grid = new Grid { Margin = new Thickness(24) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var header = new StackPanel();
        header.Children.Add(new TextBlock { Text = playlist.Title, FontSize = 18, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        var selectActions = new WrapPanel { Margin = new Thickness(0, 12, 0, 12) };
        var all = new Button { Content = t("Select all"), Style = (Style)Application.Current.Resources["QuietButton"] };
        var none = new Button { Content = t("Clear selection"), Style = all.Style, Margin = new Thickness(12, 0, 0, 0) };
        selectActions.Children.Add(all); selectActions.Children.Add(none); header.Children.Add(selectActions); grid.Children.Add(header);
        var rows = new StackPanel();
        var checks = new List<CheckBox>();
        int index = 0;
        foreach (var entry in playlist.Entries)
        {
            index++;
            var check = new CheckBox
            {
                Tag = entry.Id, Content = new TextBlock { Text = index + ". " + entry.Title, TextWrapping = TextWrapping.Wrap },
                IsEnabled = entry.Id != null && entry.MediaUrl != null,
                IsChecked = entry.Id != null && entry.MediaUrl != null && (selected == null || selected.Contains(entry.Id)),
                Margin = new Thickness(0, 0, 0, 12), Foreground = (Brush)Application.Current.Resources["Ink"]
            };
            checks.Add(check); rows.Children.Add(check);
        }
        all.Click += (_, _) => { foreach (var check in checks.Where(c => c.IsEnabled)) check.IsChecked = true; };
        none.Click += (_, _) => { foreach (var check in checks) check.IsChecked = false; };
        var scroll = new ScrollViewer { Content = rows, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Style = (Style)Application.Current.Resources["InsetScrollFrame"] };
        Grid.SetRow(scroll, 1); grid.Children.Add(scroll);
        var error = new TextBlock { Foreground = (Brush)Application.Current.Resources["Error"], TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) };
        Grid.SetRow(error, 2); grid.Children.Add(error);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
        var apply = new Button { Content = t("Use selection"), Style = (Style)Application.Current.Resources["PrimaryButton"] };
        var cancel = new Button { Content = t("Cancel"), IsCancel = true, Margin = new Thickness(12, 0, 0, 0) };
        apply.Click += (_, _) =>
        {
            if (!checks.Any(c => c.IsEnabled && c.IsChecked == true)) { error.Text = t("Choose at least one available video."); return; }
            dialog.DialogResult = true;
        };
        buttons.Children.Add(apply); buttons.Children.Add(cancel); Grid.SetRow(buttons, 3); grid.Children.Add(buttons);
        dialog.Content = grid;
        return dialog.ShowDialog() == true ? checks.Where(c => c.IsEnabled && c.IsChecked == true).Select(c => (string)c.Tag).ToHashSet() : null;
    }
    public void ShowBrowserIntegration(Func<string, string> t)
    {
        var dialog = Dialog(t("Browser integration"), 400);
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = t("Send a link from your browser"), FontSize = 20, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock { Text = t("Keep ViFree in a permanent folder. Enable the connection here, then load the companion extension from the Browser folder included with the app. Chrome, Edge and Firefox support local loading."), TextWrapping = TextWrapping.Wrap, Foreground = (Brush)Application.Current.Resources["Muted"], Margin = new Thickness(0, 12, 0, 0) });
        panel.Children.Add(new TextBlock { Text = t("Click the extension or right-click a link to open it in ViFree. Nothing downloads until you choose Download."), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 20) });
        var buttons = new WrapPanel();
        var enable = new Button { Content = t("Enable connection"), Style = (Style)Application.Current.Resources["PrimaryButton"] };
        var disable = new Button { Content = t("Disable connection"), Margin = new Thickness(12, 0, 0, 0) };
        var message = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 16, 0, 0), Foreground = (Brush)Application.Current.Resources["Muted"] };
        CancellationTokenSource notice = null;
        async void Run(Action action, string success)
        {
            notice?.Cancel();
            var lifetime = notice = new();
            try { action(); message.Text = t(success); }
            catch { message.Text = t("Could not change the browser connection. Keep ViFree.exe in a writable folder and try again."); }
            try { await Task.Delay(5000, lifetime.Token); message.Text = ""; } catch (OperationCanceledException) { }
            finally { lifetime.Dispose(); if (ReferenceEquals(notice, lifetime)) notice = null; }
        }
        enable.Click += (_, _) => Run(() => BrowserRegistration.Enable(Environment.ProcessPath), "Browser connection enabled. Load the companion extension to send links.");
        disable.Click += (_, _) => Run(BrowserRegistration.Disable, "Browser connection disabled.");
        buttons.Children.Add(enable); buttons.Children.Add(disable); panel.Children.Add(buttons); panel.Children.Add(message);
        var close = new Button { Content = t("Close"), IsCancel = true, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
        panel.Children.Add(close);
        dialog.Content = new ScrollViewer { Content = panel, Style = (Style)Application.Current.Resources["VerticalScrollFrame"], VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        dialog.Closed += (_, _) => notice?.Cancel();
        dialog.ShowDialog();
    }
}
