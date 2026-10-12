using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using YouTubeDownloader;

internal static class InterfaceChecks {
    private static int count;
    private static T Field<T>(MainWindow window,string name) { return (T)typeof(MainWindow).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window); }
    private static void Call(MainWindow window,string name,params object[] args) { typeof(MainWindow).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,args); }
    private static void Assert(bool condition,string message) { if(!condition) throw new Exception(message); count++; Console.WriteLine("PASS: "+message); }
    [STAThread] public static void Main() {
        Application.EnableVisualStyles();
        string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.json");
        string prior=File.Exists(path) ? File.ReadAllText(path) : null;
        try {
            File.WriteAllText(path,"{\"Folder\":\"C:\\\\Downloads\\\\YouTube\",\"LastUrl\":\"https://youtu.be/jNQXAC9IVRw\",\"Format\":0,\"Recent\":[]}");
            using(var window=new MainWindow()) {
                var url=Field<TextBox>(window,"url"); var theme=Field<ComboBox>(window,"theme");
                var video=Field<RadioButton>(window,"video"); var audio=Field<RadioButton>(window,"audio"); var quality=Field<ComboBox>(window,"quality");
                Assert(url.Text=="","A legacy saved link is not restored");
                var light=window.BackColor; theme.SelectedIndex=1;
                Assert(window.BackColor!=light,"Dark theme changes the app palette");
                Assert(File.ReadAllText(path).Contains("\"Theme\":\"Dark\""),"Theme preference is saved");
                audio.Checked=true;
                Assert(audio.Checked && !video.Checked,"MP3 radio selection is exclusive");
                Assert(quality.Text=="192 kbps" && !quality.Enabled,"MP3 displays its encoding bitrate");
                video.Checked=true;
                Assert(video.Checked && !audio.Checked,"MP4 radio selection is exclusive");
                Call(window,"SetBusy",true); url.Text="https://youtu.be/jNQXAC9IVRw";
                Call(window,"CompleteDownload",Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"completed.mp4"));
                Call(window,"SetBusy",false);
                Assert(url.Text=="","Successful download clears the link");
                Assert(Field<VideoInfo>(window,"checkedInfo")==null && !Field<Button>(window,"download").Enabled,"Success clears previous formats and disables another download");
                Assert(Field<Button>(window,"openFile").Enabled && Field<Label>(window,"stage").Text=="Download complete","Completed-file access and success status remain visible");
                Assert(!File.ReadAllText(path).Contains("LastUrl") && !File.ReadAllText(path).Contains("jNQXAC9IVRw"),"Links are never persisted to settings");
                Call(window,"SetBusy",true); url.Text="https://youtu.be/jNQXAC9IVRw";
                Call(window,"SetStatus","Download failed","Try again",true); Call(window,"SetBusy",false);
                Assert(url.Text!="","Failure keeps the link available for retry");
                Call(window,"SetStatus","Download cancelled","Try again",false);
                Assert(url.Text!="","Cancellation keeps the link available for retry");
                typeof(MainWindow).GetField("checkedInfo",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,new VideoInfo { HasAudio=true,Qualities=new System.Collections.Generic.List<VideoQuality> { new VideoQuality { Resolution=720,Selector="18" } } });
                Call(window,"UpdateQualityChoices"); Field<Timer>(window,"linkTimer").Start(); Call(window,"BeginUpdateInstallation");
                Assert(!Field<Timer>(window,"linkTimer").Enabled,"Installation stops a pending automatic link check");
                var inspect=(System.Threading.Tasks.Task)typeof(MainWindow).GetMethod("InspectAsync",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,null);
                var downloadTask=(System.Threading.Tasks.Task)typeof(MainWindow).GetMethod("DownloadAsync",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,null);
                Assert(inspect.IsCompleted && downloadTask.IsCompleted && Field<System.Threading.CancellationTokenSource>(window,"cancellation")==null,"Queued checks and downloads are blocked during installation");
                Call(window,"SetBusy",false); Call(window,"UpdateDownloadEnabled");
                Assert(!url.Enabled && !quality.Enabled && !Field<Button>(window,"download").Enabled,"An async completion cannot re-enable controls during an update");
            }
            using(var reopened=new MainWindow()) {
                Assert(Field<TextBox>(reopened,"url").Text=="","Restart always has an empty link");
                Assert(Field<ComboBox>(reopened,"theme").SelectedIndex==1,"Restart restores dark theme");
            }
            string prefix=AppUpdates.Repository+"/releases/download/v1.1.0/";
            string release="{\"tag_name\":\"v1.1.0\",\"draft\":false,\"prerelease\":false,\"assets\":[{\"name\":\"YouTubeDownloader-v1.1.0-windows-x64.zip\",\"browser_download_url\":\""+prefix+"app.zip\"},{\"name\":\"checksums.json\",\"browser_download_url\":\""+prefix+"checksums.json\"}]}";
            Assert(AppUpdates.Parse(release,new Version(1,0,0,0)).Version==new Version(1,1,0),"Updater detects a newer stable release");
            Assert(AppUpdates.Parse(release,new Version(1,1,0,0))==null,"Equal versions do not prompt an update");
            Assert(AppUpdates.Parse(release,new Version(2,0,0,0))==null,"Updater never offers a downgrade");
            Assert(AppUpdates.Parse(release.Replace("\"prerelease\":false","\"prerelease\":true"),new Version(1,0,0))==null,"Prereleases are ignored");
            Assert(AppUpdates.Parse(release.Replace("\"draft\":false","\"draft\":true"),new Version(1,0,0))==null,"Drafts are ignored");
            bool rejected=false; try { AppUpdates.Parse(release.Replace(AppUpdates.Repository,"https://example.com/other"),new Version(1,0,0)); } catch(InvalidOperationException) { rejected=true; }
            Assert(rejected,"Update assets outside the app repository are rejected");
            rejected=false; try { AppUpdates.Parse(release.Replace("checksums.json","other.json"),new Version(1,0,0)); } catch(InvalidOperationException) { rejected=true; }
            Assert(rejected,"A release without checksums cannot be installed");
            Console.WriteLine(count+" interface checks passed.");
        } finally { if(prior==null) File.Delete(path); else File.WriteAllText(path,prior); }
    }
}
