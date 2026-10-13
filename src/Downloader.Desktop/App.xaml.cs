using System.Windows;

namespace YouTubeDownloader;
public partial class App : Application
{
    private Mutex instance;
    protected override void OnStartup(StartupEventArgs args)
    {
        base.OnStartup(args);
        bool smoke = args.Args.Length == 2 && args.Args[0] == "--smoke-test";
        if (!smoke && AppUpdates.RunHelper(args.Args))
        {
            Shutdown();
            return;
        }

        instance = new Mutex(true, smoke ? "Local\\YouTubeDownloader.Smoke." + Environment.ProcessId : "Local\\YouTubeDownloader.Desktop", out bool first);
        if (!first)
        {
            Shutdown();
            return;
        }

        ThemeResources.Apply(true);
        if (smoke)
        {
            string root = Path.GetFullPath(args.Args[1]);
            var service = new DownloadService(new YtDlpProvider(new DependencyManager(Path.Combine(root, "Tools"), Array.Empty<string>())), new LocalPlaylistStorage(Path.Combine(root, "PlaylistState")));
            MainWindow = new MainWindow(service, new SettingsRepository(root));
            MainWindow.ContentRendered += (_, _) =>
            {
                Directory.CreateDirectory(root);
                File.WriteAllText(Path.Combine(root, "startup-ready.txt"), "ready");
            };
        }
        else
            MainWindow = new MainWindow();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs args)
    {
        instance?.Dispose();
        base.OnExit(args);
    }
}
