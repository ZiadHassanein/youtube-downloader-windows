using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Collections.Generic;

namespace YouTubeDownloader {
    internal sealed class AppRelease {
        public Version Version;
        public string PackageUrl, ChecksumUrl, PackageName;
    }
    internal static class AppUpdates {
        private sealed class UpdateClient : WebClient {
            protected override WebRequest GetWebRequest(Uri address) { var request=base.GetWebRequest(address); request.Timeout=15000; return request; }
        }
        internal const string Repository="https://github.com/ZiadHassanein/youtube-downloader-windows";
        private const string Api="https://api.github.com/repos/ZiadHassanein/youtube-downloader-windows/releases/latest";
        public static Version Current { get { return Assembly.GetExecutingAssembly().GetName().Version; } }
        private static WebClient Client() {
            ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
            var client=new UpdateClient { Encoding=Encoding.UTF8 };
            client.Headers[HttpRequestHeader.UserAgent]="YouTubeDownloader-Windows/"+Current;
            client.Headers[HttpRequestHeader.Accept]="application/vnd.github+json";
            return client;
        }
        public static AppRelease Check() {
            using(var client=Client()) {
                try { return Parse(client.DownloadString(Api),Current); }
                catch(WebException ex) { var response=ex.Response as HttpWebResponse; if(response!=null && response.StatusCode==HttpStatusCode.NotFound) return null; throw; }
            }
        }
        internal static AppRelease Parse(string json,Version current) {
            var release=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(json);
            object flag; if(release.TryGetValue("draft",out flag) && Convert.ToBoolean(flag)) return null;
            if(release.TryGetValue("prerelease",out flag) && Convert.ToBoolean(flag)) return null;
            string tag=Convert.ToString(release["tag_name"]); Version version;
            if(!Version.TryParse(tag.TrimStart('v','V'),out version) || version.Build<0 || Comparable(version)<=Comparable(current)) return null;
            string name="YouTubeDownloader-v"+version.ToString(3)+"-windows-x64.zip",package=null,checksum=null;
            foreach(var item in (System.Collections.IEnumerable)release["assets"]) {
                var asset=item as Dictionary<string,object>; if(asset==null) continue;
                string filename=Convert.ToString(asset["name"]),link=Convert.ToString(asset["browser_download_url"]);
                if(!Trusted(link)) continue;
                if(filename==name) package=link; if(filename=="checksums.json") checksum=link;
            }
            if(package==null || checksum==null) throw new InvalidOperationException("This release has no verified Windows package yet. Try again later.");
            return new AppRelease { Version=version,PackageName=name,PackageUrl=package,ChecksumUrl=checksum };
        }
        private static Version Comparable(Version v) { return new Version(v.Major,v.Minor,Math.Max(0,v.Build),Math.Max(0,v.Revision)); }
        private static bool Trusted(string value) { Uri uri; return Uri.TryCreate(value,UriKind.Absolute,out uri) && uri.AbsoluteUri.StartsWith(Repository+"/releases/download/",StringComparison.OrdinalIgnoreCase); }
        public static string Prepare(AppRelease release) {
            if(!Trusted(release.PackageUrl) || !Trusted(release.ChecksumUrl)) throw new InvalidOperationException("The update address is invalid.");
            string temp=Path.Combine(Path.GetTempPath(),"YouTubeDownloader-update-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp);
            string archive=Path.Combine(temp,"update.zip");
            using(var client=Client()) {
                var checksum=new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(client.DownloadString(release.ChecksumUrl));
                string hash;
                if(!checksum.TryGetValue("Hash",out hash) || !Regex.IsMatch(hash,"^[A-Fa-f0-9]{64}$") || checksum["File"]!=release.PackageName) throw new InvalidOperationException("The release checksum is invalid.");
                client.DownloadFile(release.PackageUrl,archive);
                using(var sha=SHA256.Create()) using(var input=File.OpenRead(archive)) {
                    string actual=BitConverter.ToString(sha.ComputeHash(input)).Replace("-","");
                    if(!String.Equals(actual,hash,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The update download did not pass its integrity check. Try again.");
                }
            }
            using(var zip=ZipFile.OpenRead(archive)) {
                foreach(string filename in new[]{"YouTubeDownloader.exe","SetupTools.cmd","README.txt"}) {
                    var matches=zip.Entries.Where(e=>e.FullName==filename).ToArray();
                    if(matches.Length!=1 || matches[0].Length>32*1024*1024) throw new InvalidOperationException("The update package is incomplete.");
                    matches[0].ExtractToFile(Path.Combine(temp,filename));
                }
            }
            if(Comparable(AssemblyName.GetAssemblyName(Path.Combine(temp,"YouTubeDownloader.exe")).Version)!=Comparable(release.Version)) throw new InvalidOperationException("The update version does not match the release.");
            return temp;
        }
        private static string Literal(string value) { return "'"+value.Replace("'","''")+"'"; }
        public static void InstallAndRestart(string prepared) {
            string current=Assembly.GetExecutingAssembly().Location,dir=Path.GetDirectoryName(current);
            // Verify write access before closing the app. Keep the installed app if replacement fails.
            string probe=Path.Combine(dir,".update-write-"+Guid.NewGuid().ToString("N")); File.WriteAllText(probe,""); File.Delete(probe);
            string script="$ErrorActionPreference='Stop'\n"+
                "$target="+Literal(current)+"\n$package="+Literal(prepared)+"\n$backup=$target+'.previous'\n"+
                "Wait-Process -Id "+Process.GetCurrentProcess().Id+" -Timeout 60 -ErrorAction SilentlyContinue\n"+
                "try {\n  Copy-Item -LiteralPath $target -Destination $backup -Force\n  Copy-Item -LiteralPath (Join-Path $package 'YouTubeDownloader.exe') -Destination $target -Force\n"+
                "  foreach($file in @('SetupTools.cmd','README.txt')) { Copy-Item -LiteralPath (Join-Path $package $file) -Destination (Join-Path (Split-Path -Parent $target) $file) -Force }\n"+
                "  Remove-Item -LiteralPath $backup -Force\n  foreach($file in @('YouTubeDownloader.exe','SetupTools.cmd','README.txt','update.zip')) { Remove-Item -LiteralPath (Join-Path $package $file) -Force }; Remove-Item -LiteralPath $package\n"+
                "} catch { if(Test-Path -LiteralPath $backup) { Copy-Item -LiteralPath $backup -Destination $target -Force }; Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.MessageBox]::Show('The update could not be installed. The previous app will reopen.','YouTube Downloader') | Out-Null }\n"+
                "Start-Process -FilePath $target\n";
            string shell=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe");
            Process.Start(new ProcessStartInfo(shell,"-NoProfile -NonInteractive -EncodedCommand "+Convert.ToBase64String(Encoding.Unicode.GetBytes(script))) { UseShellExecute=false,CreateNoWindow=true });
        }
    }
}
