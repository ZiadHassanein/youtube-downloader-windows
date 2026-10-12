using YouTubeDownloader;
internal static class UpdateHarness {
    public static void Main(string[] args) { AppUpdates.InstallAndRestart(args[0]); }
}
