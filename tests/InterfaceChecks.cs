using System;
using System.IO;
using System.Reflection;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using YouTubeDownloader;

internal static class InterfaceChecks {
    private static int count;
    private static T Field<T>(MainWindow window,string name) { return (T)typeof(MainWindow).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window); }
    private static void Call(MainWindow window,string name,params object[] args) { typeof(MainWindow).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,args); }
    private static void Assert(bool condition,string message) { if(!condition) throw new Exception(message); count++; Console.WriteLine("PASS: "+message); }
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern IntPtr GetProp(IntPtr window,string name);
    private static void Collect(Control parent,List<Control> result) { result.Add(parent); foreach(Control child in parent.Controls) Collect(child,result); }
    private static void CheckNativeWindows(MainWindow window) {
        var controls=new List<Control>(); Collect(window,controls);
        foreach(var control in controls) if(control.IsHandleCreated) {
            if(GetProp(control.Handle,"SysSetRedraw")!=IntPtr.Zero) throw new Exception("Drawing remains disabled: "+control.GetType().Name);
            if(control.Visible && !IsWindowVisible(control.Handle)) throw new Exception("A visible control has a hidden native window: "+control.GetType().Name);
        }
    }
    private static void CheckLanguageRepaint(string initialLanguage) {
        using(var window=new MainWindow(false)) {
            // Exercise a visible native window under its normal message loop.
            window.ShowInTaskbar=false; window.StartPosition=FormStartPosition.Manual;
            window.Location=Screen.PrimaryScreen.WorkingArea.Location;
            var theme=Field<CheckBox>(window,"theme"); var url=Field<TextBox>(window,"url");
            var stage=Field<Label>(window,"stage"); int painted=0, recreated=0;
            stage.Paint+=delegate { painted++; }; stage.HandleCreated+=delegate { recreated++; };
            Exception failure=null; int pass=0; string finalUrl=null, finalStage=null;
            var noticeClock=new System.Diagnostics.Stopwatch();
            var nativeHandles=new Dictionary<Control,IntPtr>();
            using(var steps=new Timer { Interval=200 }) {
                window.Shown+=delegate {
                    var controls=new List<Control>(); Collect(window,controls);
                    foreach(var control in controls) if(control.IsHandleCreated) nativeHandles.Add(control,control.Handle);
                    Assert(window.Text==Texts.For(initialLanguage,"YouTube Downloader") && Field<ThemeScrollHost>(window,"scroll").IsArabic==(initialLanguage=="ar"),"Persisted "+initialLanguage+" starts visibly with the correct content direction");
                    Call(window,"SetBusy",true); url.Text="https://youtu.be/jNQXAC9IVRw"; Call(window,"SetBusy",false);
                    Field<Timer>(window,"linkTimer").Stop();
                    typeof(MainWindow).GetField("checkedInfo",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,new VideoInfo { HasAudio=true,Qualities=new List<VideoQuality> { new VideoQuality { Resolution=720,Selector="22" } } });
                    Call(window,"UpdateQualityChoices"); Call(window,"SetStatus","Download failed","Try again",true); steps.Start();
                };
                steps.Tick+=delegate {
                    try {
                        if(pass<12) {
                            if(pass==6) { window.Width=window.MinimumSize.Width; window.PerformLayout(); }
                            int before=painted; Control focused=pass%2==0 ? (Control)url : Field<ComboBox>(window,"quality"); focused.Focus();
                            Call(window,"SetLanguage",(pass%2==0)==(initialLanguage=="en") ? "ar" : "en"); theme.Checked=!theme.Checked; window.Update();
                            CheckNativeWindows(window);
                            foreach(var entry in nativeHandles) if(!entry.Key.IsHandleCreated || entry.Key.Handle!=entry.Value || entry.Key.RightToLeft!=RightToLeft.No) throw new Exception("Language switching changed a native window or its LTR direction");
                            bool arabic=Field<ThemeScrollHost>(window,"scroll").IsArabic;
                            var paste=Field<Button>(window,"paste"); var quality=Field<ThemeCombo>(window,"quality");
                            if(((TableLayoutPanel)paste.Parent).GetColumn(paste)!=(arabic ? 0 : 1) || ((TableLayoutPanel)quality.Parent).GetColumn(quality)!=(arabic ? 0 : 4) || quality.IsArabic!=arabic || Field<FormatChoice>(window,"video").IsArabic!=arabic) throw new Exception("Content direction did not mirror the link and quality rows");
                            if(Field<TableLayoutPanel>(window,"content").Width<400 || stage.Width==0 || stage.Height==0) throw new Exception("Language switching collapsed the content layout");
                            if(!focused.Focused) throw new Exception("Language switching lost keyboard focus");
                            if(!Field<Button>(window,"paste").Visible || !Field<Label>(window,"toolsStatus").Visible || !url.Visible || !theme.Visible) throw new Exception("Language change hid an essential control");
                            var update=Field<ThemeButton>(window,"update"); var version=Field<Label>(window,"toolsStatus"); var footer=(TableLayoutPanel)update.Parent;
                            if(footer.GetColumn(update)!=(arabic ? 1 : 0) || footer.GetColumn(version)!=(arabic ? 0 : 1) || update.IsArabic!=arabic || version.TextAlign!=(arabic ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleRight) || version.Text!="v"+AppUpdates.Current.ToString(3) || (arabic ? update.Left<=version.Left : update.Left>=version.Left)) throw new Exception("Language change did not mirror the update/version footer or restored tool text");
                            var credits=Field<TableLayoutPanel>(window,"authorRow"); var author=(Label)credits.Controls[0]; var socials=(FlowLayoutPanel)credits.Controls[1];
                            if(credits.GetColumn(author)!=0 || credits.GetColumn(socials)!=1 || author.TextAlign!=ContentAlignment.TopLeft || socials.FlowDirection!=FlowDirection.LeftToRight || author.Left>=socials.Left) throw new Exception("Language change moved the author credit from the left or reordered the social links");
                            if(Field<Button>(window,"cancel").Visible || Field<Button>(window,"openFile").Visible || Field<ThemeProgress>(window,"progress").Visible || Field<NoticePopover>(window,"updateNotice").Visible) throw new Exception("Language change revealed an inactive control");
                            if(pass>=6) {
                                foreach(string name in new[]{"url","folder","paste","browse","openFolder","video","audio","quality","download","recentButton","update"}) {
                                    var control=Field<Control>(window,name);
                                    if(!control.Parent.ClientRectangle.Contains(control.Bounds)) throw new Exception("Minimum-width layout clips "+name);
                                }
                                var actions=Field<Button>(window,"download").Parent;
                                if(!actions.Parent.ClientRectangle.Contains(actions.Bounds)) throw new Exception("Minimum-width layout clips the centered actions");
                            }
                            if(painted<=before) throw new Exception("The status label was not repainted: stage="+stage.Bounds+", screen="+stage.PointToScreen(Point.Empty)+", content="+Field<TableLayoutPanel>(window,"content").Bounds+", viewport="+Field<ThemeScrollHost>(window,"scroll").Controls[0].Bounds);
                            pass++;
                        } else if(pass==12) {
                            Call(window,"ShowContextNotice","Folder is not available","Choose an existing folder or download a video first.",StatusTone.Error,Field<TextBox>(window,"folder").Parent,new object[0]);
                            Assert(Field<NoticePopover>(window,"updateNotice").Visible && Field<Control>(window,"noticeAnchor")==Field<TextBox>(window,"folder").Parent,"Folder errors appear in a popover anchored to the save-folder field");
                            Assert(Field<NoticePopover>(window,"updateNotice").Parent==window && window.ClientRectangle.Contains(Field<NoticePopover>(window,"updateNotice").Bounds),"Alerts are contained within the app client borders");
                            Field<Timer>(window,"updateNoticeTimer").Interval=80; noticeClock.Restart(); pass++;
                        } else if(pass==13) {
                            // WM_TIMER delivery can be delayed during a busy native repaint; allow a bounded delivery window.
                            if(Field<NoticePopover>(window,"updateNotice").Visible && noticeClock.ElapsedMilliseconds<2000) return;
                            Assert(!Field<NoticePopover>(window,"updateNotice").Visible && !Field<Timer>(window,"updateNoticeTimer").Enabled,"The popover timer actually clears the error and hides the popup automatically");
                            Field<Timer>(window,"updateNoticeTimer").Interval=5000;
                            Call(window,"ReportUpdateCheck",null,true); Call(window,"SetLanguage","ar"); CheckNativeWindows(window);
                            Assert(Field<NoticePopover>(window,"updateNotice").Visible && Field<Label>(window,"updateHeading").Text=="لديك أحدث إصدار","A visible update popover survives language changes and is translated"); pass++;
                        } else {
                            Call(window,"HideUpdateNotice"); Call(window,"SetLanguage","en"); CheckNativeWindows(window);
                            Assert(!Field<NoticePopover>(window,"updateNotice").Visible,"A dismissed update popover stays hidden after switching language");
                            finalUrl=url.Text; finalStage=stage.Text; steps.Stop(); window.Close();
                        }
                    } catch(Exception ex) { failure=ex; steps.Stop(); window.Close(); }
                };
                using(var watchdog=new System.Threading.Timer(delegate { Environment.FailFast("Native language test exceeded 30 seconds"); },null,30000,System.Threading.Timeout.Infinite)) Application.Run(window);
            }
            if(failure!=null) throw failure;
            Assert(recreated==1,"Repeated language changes repaint without recreating native label windows");
            Assert(finalUrl=="https://youtu.be/jNQXAC9IVRw" && finalStage=="Download failed","Language and theme changes preserve link text and the current error state");
            Assert(nativeHandles.Count>30,"More than 30 native windows retain handles, focus, and visibility through 12 RTL/LTR and theme changes");
            Assert(true,"The minimum-width form fits all task actions through both languages and themes from saved "+initialLanguage);
        }
    }
    private static void CheckPlaylistLayout() {
        using(var window=new MainWindow(false)) {
            window.ShowInTaskbar=false; window.StartPosition=FormStartPosition.Manual; window.Location=Screen.PrimaryScreen.WorkingArea.Location;
            Exception failure=null; int pass=0;
            using(var steps=new Timer { Interval=200 }) {
                window.Shown+=delegate {
                    Call(window,"SetBusy",true); Field<TextBox>(window,"url").Text="https://www.youtube.com/playlist?list=PLFixture"; Call(window,"SetBusy",false); Call(window,"PrepareScope"); Field<Timer>(window,"linkTimer").Stop();
                    typeof(MainWindow).GetField("checkedPlaylist",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,new PlaylistInfo { Id="PLFixture",Url="https://www.youtube.com/playlist?list=PLFixture",Title="A long playlist title to check wrapping and layout",Entries=new List<PlaylistEntry>{new PlaylistEntry { Id="jNQXAC9IVRw" }} });
                    Call(window,"UpdateQualityChoices"); Call(window,"UpdatePlaylistSummary"); steps.Start();
                };
                steps.Tick+=delegate {
                    try {
                        if(pass==0) { window.Width=window.MinimumSize.Width; Call(window,"SetLanguage","ar"); Field<CheckBox>(window,"theme").Checked=true; window.PerformLayout(); window.Update(); pass++; return; }
                        var controls=new List<Control>(); Collect(window,controls);
                        foreach(string name in new[]{"oneVideo","wholePlaylist","playlistSummary","quality","download","recentButton"}) {
                            var control=Field<Control>(window,name);
                            if(!control.Parent.ClientRectangle.Contains(control.Bounds)) throw new Exception("Playlist layout clips "+name+" "+control.Bounds+" in "+control.Parent.ClientRectangle);
                        }
                        var root=Field<TableLayoutPanel>(window,"content"); Panel workspace=null;
                        foreach(Control control in root.Controls) if(control is Panel && String.Equals(control.Tag,"surface")) workspace=(Panel)control;
                        var taskContent=workspace.Controls[0];
                        if(!workspace.ClientRectangle.Contains(taskContent.Bounds)) throw new Exception("Playlist workspace clips its content: "+taskContent.Bounds+" in "+workspace.ClientRectangle);
                        if(pass<4) { Call(window,"SetLanguage",pass%2==0 ? "ar" : "en"); Field<CheckBox>(window,"theme").Checked=pass<2; window.PerformLayout(); window.Update(); pass++; return; }
                        Assert(true,"The visible playlist form fits long copy and task controls at minimum width after all language/theme pairs");
                        steps.Stop(); window.Close();
                    } catch(Exception ex) { failure=ex; steps.Stop(); window.Close(); }
                };
                Application.Run(window);
            }
            if(failure!=null) throw failure;
        }
    }
    [STAThread] public static void Main() {
        Application.EnableVisualStyles();
        string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.json");
        string prior=File.Exists(path) ? File.ReadAllText(path) : null;
        try {
            string downloads=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads");
            var serializer=new System.Web.Script.Serialization.JavaScriptSerializer();
            foreach(var sample in new[]{
                new[]{"",downloads,"New settings default directly to Downloads"},
                new[]{Path.Combine(downloads,"YouTube"),downloads,"The previous default migrates to Downloads"},
                new[]{Path.Combine(Path.GetPathRoot(downloads),"Downloads","YouTube"),Path.Combine(Path.GetPathRoot(downloads),"Downloads"),"The previous example path drops its YouTube subfolder"},
                new[]{@"D:\Custom\YouTube",@"D:\Custom\YouTube","Custom save folders are preserved"}
            }) {
                File.WriteAllText(path,serializer.Serialize(new SavedState { Folder=sample[0] }));
                using(var sampleWindow=new MainWindow(false)) Assert(Field<TextBox>(sampleWindow,"folder").Text==sample[1],sample[2]);
            }
            string chosen=Path.Combine(downloads,"YouTube");
            File.WriteAllText(path,serializer.Serialize(new SavedState { Folder=chosen,FolderVersion=1 }));
            using(var sampleWindow=new MainWindow(false)) Assert(Field<TextBox>(sampleWindow,"folder").Text==chosen,"An explicitly chosen folder is not migrated again on later launches");
            double wheelRemainder=0; int wheelDistance=0;
            for(int step=0;step<8;step++) wheelDistance+=ThemeScrollHost.WheelDistance(15,3,200,16,ref wheelRemainder);
            Assert(wheelDistance==48,"Small trackpad wheel deltas accumulate to the same distance as one wheel notch");
            Assert(ThemeScrollHost.WheelDistance(120,0,200,16,ref wheelRemainder)==0,"The scrollbar respects Windows zero-line wheel scrolling");
            File.WriteAllText(path,"{\"Folder\":\"C:\\\\Downloads\\\\YouTube\",\"Language\":\"en\",\"Recent\":[]}");
            CheckLanguageRepaint("en");
            File.WriteAllText(path,"{\"Folder\":\"C:\\\\Downloads\\\\YouTube\",\"Language\":\"ar\",\"Recent\":[]}");
            CheckLanguageRepaint("ar");
            CheckPlaylistLayout();
            File.WriteAllText(path,"{\"Folder\":\"C:\\\\Downloads\\\\YouTube\",\"LastUrl\":\"https://youtu.be/jNQXAC9IVRw\",\"Format\":0,\"Recent\":[]}");
            using(var window=new MainWindow()) {
                var url=Field<TextBox>(window,"url"); var theme=Field<CheckBox>(window,"theme");
                var video=Field<RadioButton>(window,"video"); var audio=Field<RadioButton>(window,"audio"); var quality=Field<ComboBox>(window,"quality");
                Assert(url.Text=="","A legacy saved link is not restored");
                Assert(theme.Checked,"Dark is the default theme");
                Assert(window.RightToLeft==RightToLeft.No && !window.RightToLeftLayout,"English starts with an LTR layout");
                using(var history=(Form)typeof(MainWindow).GetMethod("CreateRecentDialog",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,null)) {
                    var controls=new List<Control>(); Collect(history,controls);
                    Assert(history.BackColor==window.BackColor && controls.Exists(c=>c is Label && c.Text=="Your completed downloads will appear here.") && history.CancelButton!=null,"Recent downloads opens a themed modal with an empty state and keyboard dismissal");
                    Assert(controls.Exists(c=>c is ThemeButton && c.AccessibleName=="Show in folder" && !c.Enabled),"An empty history keeps Show in folder disabled");
                }
                typeof(MainWindow).GetField("checkedInfo",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,new VideoInfo { HasAudio=true,Qualities=new System.Collections.Generic.List<VideoQuality> { new VideoQuality { Resolution=1080,Selector="137" },new VideoQuality { Resolution=720,Selector="22" } } });
                Call(window,"UpdateQualityChoices"); quality.SelectedIndex=1; Call(window,"SetLanguage","ar");
                Assert(Field<ThemeScrollHost>(window,"scroll").IsArabic && Field<FormatChoice>(window,"video").IsArabic && !window.RightToLeftLayout && window.Text=="YouTube Downloader" && url.RightToLeft==RightToLeft.No && Field<TextBox>(window,"folder").RightToLeft==RightToLeft.No,"Arabic mirrors content while the English app name and technical fields stay LTR");
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
                Assert(quality.Text=="320 kbps" && quality.Items.Count==2,"MP3 offers 192 and 320 kbps with 320 as the default");
                quality.SelectedIndex=0; Call(window,"SetLanguage","ar"); Call(window,"SetLanguage","en");
                Assert(quality.Text=="192 kbps","The selected MP3 bitrate survives language changes");
                video.Checked=true;
                Assert(video.Checked && !audio.Checked,"MP4 radio selection is exclusive");
                Call(window,"SetBusy",true); url.Text="https://youtu.be/jNQXAC9IVRw";
                Call(window,"CompleteDownload",Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"completed.mp4"));
                Call(window,"SetBusy",false);
                Assert(url.Text=="","Successful download clears the link");
                Assert(Field<VideoInfo>(window,"checkedInfo")==null && !Field<Button>(window,"download").Enabled,"Success clears previous formats and disables another download");
                Assert(Field<Button>(window,"openFile").Enabled && Field<Label>(window,"stage").Text=="Download complete","Completed-file access and success status remain visible");
                Assert(Field<Button>(window,"showInFolder").Enabled && Field<Button>(window,"showInFolder").AccessibleName=="Show in folder","A completed download offers Show in folder beside Open file");
                using(var history=(Form)typeof(MainWindow).GetMethod("CreateRecentDialog",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,null)) {
                    var controls=new List<Control>(); Collect(history,controls); var list=controls.Find(c=>c is ListBox) as ListBox;
                    Assert(list!=null && list.Items.Count==1 && list.Items[0].ToString()=="completed.mp4","The history modal lists completed filenames after a download");
                    var reveal=controls.Find(c=>c is ThemeButton && c.AccessibleName=="Show in folder") as ThemeButton;
                    Assert(list.SelectedIndex==0 && reveal.Enabled,"Recent downloads selects the latest file and enables its folder action");
                    list.SelectedIndex=-1; Assert(!reveal.Enabled,"Clearing the history selection disables the folder action"); list.SelectedIndex=0;
                    history.Show(window); Application.DoEvents(); reveal.PerformClick(); Application.DoEvents();
                    Assert(history.Visible && controls.Exists(c=>c is Label && c.Visible && c.Text.Contains("The file may have been moved or deleted.") && history.ClientRectangle.Contains(history.RectangleToClient(c.RectangleToScreen(c.ClientRectangle)))),"A missing recent file shows an in-dialog message without closing history");
                }
                Call(window,"SetLanguage","ar");
                Assert(Field<Button>(window,"showInFolder").AccessibleName=="إظهار في المجلد","The completed-file folder action is translated in Arabic"); Call(window,"SetLanguage","en");
                Assert(!File.ReadAllText(path).Contains("LastUrl") && !File.ReadAllText(path).Contains("jNQXAC9IVRw"),"Links are never persisted to settings");
                Call(window,"SetBusy",true); url.Text="https://www.youtube.com/playlist?list=PLFixture"; Call(window,"SetBusy",false); Call(window,"PrepareScope"); Field<Timer>(window,"linkTimer").Stop();
                Assert(Field<FormatChoice>(window,"wholePlaylist").Checked && !Field<FormatChoice>(window,"oneVideo").Enabled,"Playlist-only links select the whole playlist and disable the single-video choice");
                var fixturePlaylist=new PlaylistInfo { Id="PLFixture",Url=url.Text,Title="Test playlist",Entries=new List<PlaylistEntry> { new PlaylistEntry { Id="jNQXAC9IVRw" },new PlaylistEntry { Id="aqz-KE-bpKQ" } } };
                typeof(MainWindow).GetField("checkedPlaylist",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,fixturePlaylist); Call(window,"UpdateQualityChoices"); Call(window,"UpdatePlaylistSummary");
                Assert(quality.Text=="Up to 1080p" && Field<Label>(window,"playlistSummary").Text.Contains("2 videos"),"Playlist qualities explicitly describe a maximum and show the entry count");
                quality.SelectedIndex=4; Call(window,"SetLanguage","ar");
                Assert(quality.Text=="حتى 720p" && Field<FormatChoice>(window,"wholePlaylist").IsArabic && Field<Label>(window,"playlistSummary").Text.Contains("2 فيديو"),"Arabic translates and mirrors playlist controls while preserving the quality cap");
                Call(window,"ApplyProgress",new DownloadProgress { Stage="Downloading",Detail="512 KB of 1.0 MB",Item=2,Total=3,ItemTitle="Playlist fixture",Percent=50 });
                Assert(Field<Label>(window,"progressDetail").Text.Contains("الفيديو 2 من 3") && Field<ThemeProgress>(window,"progress").Value==500,"Playlist item position and overall progress are shown in Arabic");
                Call(window,"CompletePlaylist",new DownloadResult { Total=3,Completed=1,Failed=1,Remaining=1,Files=new List<string>{Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"playlist-completed.mp4")},FilePath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"playlist-completed.mp4"),StopReason="HTTP 429 Too Many Requests" });
                Call(window,"SetLanguage","en");
                Assert(url.Text!="" && Field<Label>(window,"stage").Text=="Playlist paused" && Field<Label>(window,"progressDetail").Text.Contains("1 remaining") && Field<Label>(window,"progressDetail").Text.Contains("limiting downloads"),"Partial playlists retain the link, counts, and recovery guidance through language changes");
                Call(window,"CompletePlaylist",new DownloadResult { Total=3,Completed=1,Failed=1,Remaining=1,Files=new List<string>{Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"playlist-completed.mp4")},FilePath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"playlist-completed.mp4"),StopReason="[WinError 112] There is not enough space on the disk" });
                Assert(url.Text!="" && Field<Label>(window,"stage").Text=="Playlist paused" && Field<Label>(window,"progressDetail").Text.Contains("Free some space") && Field<Button>(window,"openFile").Enabled,"Windows disk-full feedback keeps the resumable link, completed-file action, and free-space recovery");
                Call(window,"CompletePlaylist",new DownloadResult { Total=2,Completed=2,Skipped=1,Files=new List<string>{Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"playlist-completed.mp4")},FilePath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"playlist-completed.mp4") });
                Assert(url.Text=="" && !Field<FormatChoice>(window,"wholePlaylist").Checked && Field<Label>(window,"stage").Text=="Playlist complete" && Field<Label>(window,"progressDetail").Text.Contains("1 already downloaded"),"Complete playlists clear the link and report saved versus existing files");
                Call(window,"SetBusy",true); url.Text="https://youtu.be/jNQXAC9IVRw?list=PLFixture"; Call(window,"SetBusy",false); Call(window,"PrepareScope"); Field<Timer>(window,"linkTimer").Stop();
                Assert(Field<FormatChoice>(window,"oneVideo").Checked && Field<FormatChoice>(window,"oneVideo").Enabled,"Mixed watch/playlist links default safely to this video");
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
