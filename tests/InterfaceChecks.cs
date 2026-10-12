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
            double wheelRemainder=0; int wheelDistance=0;
            for(int step=0;step<8;step++) wheelDistance+=ThemeScrollHost.WheelDistance(15,3,200,16,ref wheelRemainder);
            Assert(wheelDistance==48,"Small trackpad wheel deltas accumulate to the same distance as one wheel notch");
            Assert(ThemeScrollHost.WheelDistance(120,0,200,16,ref wheelRemainder)==0,"The scrollbar respects Windows zero-line wheel scrolling");
            File.WriteAllText(path,"{\"Folder\":\"C:\\\\Downloads\\\\YouTube\",\"LastUrl\":\"https://youtu.be/jNQXAC9IVRw\",\"Format\":0,\"Recent\":[]}");
            using(var window=new MainWindow()) {
                var url=Field<TextBox>(window,"url"); var theme=Field<CheckBox>(window,"theme");
                var video=Field<RadioButton>(window,"video"); var audio=Field<RadioButton>(window,"audio"); var quality=Field<ComboBox>(window,"quality");
                Assert(url.Text=="","A legacy saved link is not restored");
                Assert(theme.Checked,"Dark is the default theme");
                Assert(window.RightToLeft==RightToLeft.No && !window.RightToLeftLayout,"English starts with an LTR layout");
                typeof(MainWindow).GetField("checkedInfo",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,new VideoInfo { HasAudio=true,Qualities=new System.Collections.Generic.List<VideoQuality> { new VideoQuality { Resolution=1080,Selector="137" },new VideoQuality { Resolution=720,Selector="22" } } });
                Call(window,"UpdateQualityChoices"); quality.SelectedIndex=1; Call(window,"SetLanguage","ar");
                Assert(window.RightToLeft==RightToLeft.Yes && window.RightToLeftLayout && window.Text=="منزّل يوتيوب" && url.RightToLeft==RightToLeft.No && Field<TextBox>(window,"folder").RightToLeft==RightToLeft.No,"Arabic translates the app and uses RTL while technical fields stay LTR");
                Assert(((VideoQuality)quality.SelectedItem).Resolution==720 && File.ReadAllText(path).Contains("\"Language\":\"ar\""),"Language changes preserve the chosen quality and save the preference");
                Call(window,"SetStatus","Download failed","Couldn't reach YouTube. Check your internet connection, then try again.",true);
                Assert(Field<Label>(window,"stage").Text=="تعذّر التنزيل" && !Field<Label>(window,"progressDetail").Text.Contains("YouTube"),"Arabic errors include translated recovery guidance");
                Call(window,"SetLanguage","en"); typeof(MainWindow).GetField("checkedInfo",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,null); Call(window,"UpdateQualityChoices");
                var dark=window.BackColor; theme.Checked=false;
                Assert(window.BackColor!=dark,"Icon toggle switches to light theme"); theme.Checked=true;
                Assert(window.BackColor==dark,"Icon toggle switches back to dark theme");
                Assert(File.ReadAllText(path).Contains("\"Theme\":\"Dark\""),"Theme preference is saved");
                Call(window,"ReportUpdateCheck",null,true);
                Assert(Field<Label>(window,"updateHeading").Text=="You're up to date" && Field<Label>(window,"updateMessage").Text.Contains("latest release"),"A manual current-version check provides clear visible feedback");
                Assert(Field<Timer>(window,"updateNoticeTimer").Enabled && Field<Timer>(window,"updateNoticeTimer").Interval<=5000,"Up-to-date confirmation has a bounded automatic dismissal");
                Call(window,"HideUpdateNotice");
                Assert(!Field<Timer>(window,"updateNoticeTimer").Enabled && Field<Label>(window,"updateHeading").Text=="","Dismissal clears the update message and its timer");
                Call(window,"ReportUpdateCheck",new AppRelease { Version=new Version(1,1,0) },true);
                Assert(Field<Button>(window,"update").Text=="Install v1.1.0" && Field<Label>(window,"updateHeading").Text=="A new version is ready","A newer release keeps the install action and shows update feedback");
                string friendly=(string)typeof(MainWindow).GetMethod("FriendlyError",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{"ERROR: [youtube] abc123: extractor failed at https://youtube.com/watch?v=abc123"});
                Assert(!friendly.Contains("abc123") && !friendly.Contains("ERROR:") && friendly.Contains("try again"),"Unexpected downloader errors give a recovery step without raw technical output");
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
                Assert(Field<CheckBox>(reopened,"theme").Checked,"Restart restores dark theme");
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
