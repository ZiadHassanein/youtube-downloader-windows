using Microsoft.Win32;
using System.Text.Json;
using static YouTubeDownloader.BrowserIdentity;

namespace YouTubeDownloader;
public static class BrowserRegistration
{
    private static string DirectoryPath => Path.Combine(AppPaths.UserData, "Browser");

    private static readonly string[] RegistryPaths = { @"Software\Google\Chrome\NativeMessagingHosts\" + HostName, @"Software\Microsoft\Edge\NativeMessagingHosts\" + HostName, @"Software\Mozilla\NativeMessagingHosts\" + HostName };

    public static void Enable(string executable)
    {
        if (!Path.IsPathFullyQualified(executable) || !File.Exists(executable) || Path.GetExtension(executable) != ".exe") throw new IOException("Keep ViFree.exe in a permanent folder before connecting your browser.");
        Directory.CreateDirectory(DirectoryPath);
        var chromium = new { name = HostName, description = "Send a media link to ViFree Preview", path = executable, type = "stdio", allowed_origins = new[] { "chrome-extension://" + ChromiumId + "/" } };
        var firefox = new { name = HostName, description = "Send a media link to ViFree Preview", path = executable, type = "stdio", allowed_extensions = new[] { FirefoxId } };
        string chromePath = Path.Combine(DirectoryPath, HostName + ".chromium.json"), firefoxPath = Path.Combine(DirectoryPath, HostName + ".firefox.json");
        File.WriteAllText(chromePath, JsonSerializer.Serialize(chromium));
        File.WriteAllText(firefoxPath, JsonSerializer.Serialize(firefox));
        foreach (RegistryView view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            using (var user = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view))
                for (int i = 0; i < RegistryPaths.Length; i++)
                {
                    using var key = user.CreateSubKey(RegistryPaths[i]);
                    key.SetValue("", i == 2 ? firefoxPath : chromePath, RegistryValueKind.String);
                }
    }
    public static void Disable()
    {
        foreach (RegistryView view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            using (var user = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view))
                foreach (string path in RegistryPaths)
                {
                    using var key = user.OpenSubKey(path, true);
                    if (key?.GetValue("") is string value && Path.GetDirectoryName(value) == DirectoryPath) key.DeleteValue("", false);
                }
        foreach (string name in new[] { HostName + ".chromium.json", HostName + ".firefox.json" })
        {
            string file = Path.Combine(DirectoryPath, name);
            if (File.Exists(file)) File.Delete(file);
        }
    }
}
