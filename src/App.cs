using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace YouTubeDownloader {
    internal static class Program {
        [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();
        [STAThread] private static void Main() {
            SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());
        }
    }
    public sealed class SavedState {
        public string Folder;
        public string Theme = "Light";
        public int Format, Quality;
        public List<string> Recent = new List<string>();
    }

    public sealed class MainWindow : Form {
        private Palette colors=Palette.For(false);
        private Color Ink { get { return colors.Ink; } }
        private Color Muted { get { return colors.Muted; } }
        private Color Accent { get { return colors.Accent; } }
        private Color Paper { get { return colors.Page; } }
        private readonly TextBox url=new TextBox(), folder=new TextBox(), log=new TextBox();
        private readonly ThemeCombo quality=new ThemeCombo(), theme=new ThemeCombo();
        private readonly FormatChoice video=new FormatChoice(), audio=new FormatChoice();
        private readonly ThemeButton paste=new ThemeButton(), check=new ThemeButton(), browse=new ThemeButton(), download=new ThemeButton(), cancel=new ThemeButton(), openFolder=new ThemeButton(), openFile=new ThemeButton(), details=new ThemeButton(), update=new ThemeButton();
        private readonly Label title=new Label(), subtitle=new Label(), stage=new Label(), progressDetail=new Label(), qualityLabel=new Label(), toolsStatus=new Label();
        private readonly ThemeProgress progress=new ThemeProgress();
        private readonly ListBox recent=new ListBox();
        private TableLayoutPanel content=new TableLayoutPanel();
        private AppRelease availableUpdate;
        private bool checkingUpdate, installingUpdate;
        private readonly ToolSet tools;
        private readonly DownloadEngine engine;
        private CancellationTokenSource cancellation;
        private bool busy, showDetails, initializing=true;
        private int statusMood;
        private bool IsAudio { get { return audio.Checked; } }
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
        private string lastFile, checkedUrl;
        private VideoInfo checkedInfo;
        private readonly System.Windows.Forms.Timer linkTimer=new System.Windows.Forms.Timer { Interval=700 };
        private readonly string statePath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.json");
        private readonly SavedState state;
        private Image brandLogo;

        public MainWindow() {
            Text="YouTube Downloader"; StartPosition=FormStartPosition.CenterScreen; ClientSize=new Size(920,920); MinimumSize=new Size(820,800);
            Font=new Font("Segoe UI",10.5f); BackColor=Paper; ForeColor=Ink; AutoScaleMode=AutoScaleMode.Dpi;
            tools=ToolSet.Discover(); engine=new DownloadEngine(tools); state=LoadState();
            colors=Palette.For(state.Theme=="Dark");
            using(var logoStream=Assembly.GetExecutingAssembly().GetManifestResourceStream("YouTubeDownloader.Logo")) {
                if(logoStream!=null) using(var original=Image.FromStream(logoStream)) brandLogo=new Bitmap(original);
            }
            using(var iconStream=Assembly.GetExecutingAssembly().GetManifestResourceStream("YouTubeDownloader.Icon")) {
                if(iconStream!=null) using(var original=new Icon(iconStream)) Icon=(Icon)original.Clone();
            }
            BuildInterface(); initializing=false; ApplyTheme(); SaveState();
            engine.Progress += p => SafeUi(delegate { ApplyProgress(p); });
            engine.Title += text => SafeUi(delegate { title.Text=text; subtitle.Text="Your file is being prepared."; });
            engine.Log += text => SafeUi(delegate { AppendLog(text); });
            FormClosing += OnClosing;
            Shown += async delegate { ApplyTitleBarTheme(); url.Focus(); await CheckUpdateAsync(false); };
            FormClosed += delegate { if(brandLogo!=null) brandLogo.Dispose(); };
        }
        private Label LabelFor(string text) { return new Label { Text=text,AutoSize=true,ForeColor=Ink,Font=new Font(Font,FontStyle.Bold),Margin=new Padding(0,0,0,8) }; }
        private void ConfigureButton(ThemeButton button,string text,bool primary=false) {
            button.Text=text; button.AutoSize=false; button.Height=42; button.FlatStyle=FlatStyle.Flat;
            button.BackColor=primary ? Accent : Color.White; button.ForeColor=primary ? Color.White : Ink;
            button.FlatAppearance.BorderColor=primary ? Accent : Color.FromArgb(166,179,186); button.FlatAppearance.BorderSize=1;
            button.FlatAppearance.MouseOverBackColor=primary ? Color.FromArgb(0,84,78) : Color.FromArgb(232,239,241);
            button.FlatAppearance.MouseDownBackColor=primary ? Color.FromArgb(0,69,65) : Color.FromArgb(219,229,233);
            button.Cursor=Cursors.Hand; button.Margin=new Padding(0); button.UseVisualStyleBackColor=false;
            button.AccessibleName=text.Replace("&","");
            button.Primary=primary;
        }
        private TableLayoutPanel Row(params Control[] controls) {
            var panel=new TableLayoutPanel { AutoSize=true,Dock=DockStyle.Fill,Margin=new Padding(0),ColumnCount=controls.Length,RowCount=1 };
            for(int i=0;i<controls.Length;i++) { panel.ColumnStyles.Add(i==0 ? new ColumnStyle(SizeType.Percent,100) : new ColumnStyle(SizeType.AutoSize)); controls[i].Anchor=AnchorStyles.Left|AnchorStyles.Right; if(i>0) controls[i].Margin=new Padding(12,0,0,0); panel.Controls.Add(controls[i],i,0); }
            return panel;
        }
        private void Add(Control control,int spacing) { content.RowStyles.Add(new RowStyle(SizeType.AutoSize)); control.Dock=DockStyle.Fill; control.Margin=new Padding(0,0,0,spacing); content.Controls.Add(control,0,content.RowCount++); }
        private Control FieldFor(Control input) {
            var field=new Panel { Height=44,Dock=DockStyle.Fill,Margin=new Padding(0),Padding=new Padding(12,input is ComboBox ? 5 : 10,12,5),Tag="field" };
            input.Dock=DockStyle.Fill; input.Margin=new Padding(0); field.Controls.Add(input); return field;
        }
        private void BuildInterface() {
            var scroll=new Panel { Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(28,24,28,20) };
            content.Dock=DockStyle.Top; content.AutoSize=true; content.ColumnCount=1; content.RowCount=0; content.Margin=new Padding(0); content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); scroll.Controls.Add(content); Controls.Add(scroll);
            var branding=new TableLayoutPanel { AutoSize=true,ColumnCount=3,RowCount=1,Margin=new Padding(0) };
            branding.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,68)); branding.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); branding.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,176));
            var logo=new PictureBox { Image=brandLogo,SizeMode=PictureBoxSizeMode.Zoom,Size=new Size(52,52),Margin=new Padding(0,0,16,0),AccessibleName="YouTube Downloader logo",TabStop=false };
            var wordmark=new TableLayoutPanel { AutoSize=true,ColumnCount=1,RowCount=2,Dock=DockStyle.Fill,Margin=new Padding(0) };
            wordmark.Controls.Add(new Label { Text="YouTube Downloader",AutoSize=true,Font=new Font("Segoe UI",22f,FontStyle.Bold),Margin=new Padding(0,0,0,2) },0,0);
            wordmark.Controls.Add(new Label { Text="Your videos. Ready to keep.",AutoSize=true,Tag="muted",Margin=new Padding(0) },0,1);
            theme.DropDownStyle=ComboBoxStyle.DropDownList; theme.AccessibleName="Appearance theme"; theme.Items.AddRange(new object[]{"Light theme","Dark theme"}); theme.SelectedIndex=state.Theme=="Dark" ? 1 : 0;
            theme.SelectedIndexChanged+=delegate { ApplyTheme(); SaveState(); };
            var themeField=FieldFor(theme); themeField.Anchor=AnchorStyles.Top|AnchorStyles.Right; themeField.Width=176; themeField.Dock=DockStyle.None; themeField.Margin=new Padding(0,5,0,0);
            branding.Controls.Add(logo,0,0); branding.Controls.Add(wordmark,1,0); branding.Controls.Add(themeField,2,0); Add(branding,24);
            var page=content;
            var workspace=new Panel { AutoSize=true,Dock=DockStyle.Fill,Padding=new Padding(24,22,24,22),Margin=new Padding(0),Tag="surface" };
            content=new TableLayoutPanel { AutoSize=true,Dock=DockStyle.Top,ColumnCount=1,RowCount=0,Margin=new Padding(0) }; content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); workspace.Controls.Add(content);
            page.RowStyles.Add(new RowStyle(SizeType.AutoSize)); page.Controls.Add(workspace,0,page.RowCount++);
            Add(LabelFor("YouTube &link"),0);
            url.Font=new Font("Segoe UI",12f); url.AccessibleName="YouTube link"; url.TabIndex=0;
            ConfigureButton(paste,"&Paste"); paste.Width=88; paste.Height=44;
            paste.Click+=delegate { try { if(Clipboard.ContainsText()) { url.Text=Clipboard.GetText().Trim(); url.Focus(); } else SetStatus("Clipboard is empty","Copy a YouTube video link, then choose Paste.",true); } catch(Exception ex) { SetStatus("Could not paste",ex.Message,true); } };
            ConfigureButton(check,"Check &video"); check.Quiet=true; check.Width=112; check.Height=30; check.Click+=async delegate { await InspectAsync(); };
            url.TextChanged+=delegate {
                linkTimer.Stop();
                if(!busy && !installingUpdate && checkedUrl!=url.Text.Trim()) {
                    checkedUrl=null; checkedInfo=null; UpdateQualityChoices(); download.Enabled=false;
                    try { VideoLink.Normalize(url.Text); title.Text="Checking your next video"; subtitle.Text="Getting the available qualities…"; linkTimer.Start(); }
                    catch(ArgumentException) { title.Text="Choose a video to get started"; subtitle.Text="Paste one YouTube video link."; }
                }
            };
            linkTimer.Tick+=async delegate { linkTimer.Stop(); if(!busy && !installingUpdate) await InspectAsync(); };
            Add(Row(FieldFor(url),paste),4);
            Add(Row(new Label { Text="Videos, Shorts, and shared links",AutoSize=true,Tag="muted",Margin=new Padding(0) },check),14);
            var preview=new TableLayoutPanel { ColumnCount=1,RowCount=2,AutoSize=true,Padding=new Padding(16,12,16,12),Margin=new Padding(0),Tag="field" };
            title.Text="Choose a video to get started"; title.AutoSize=true; title.MaximumSize=new Size(740,0); title.Font=new Font(Font,FontStyle.Bold); title.Margin=new Padding(0,0,0,4);
            subtitle.Text="Available qualities appear after you paste a link."; subtitle.AutoSize=true; subtitle.MaximumSize=new Size(740,0); subtitle.Tag="muted"; subtitle.Margin=new Padding(0);
            preview.Controls.Add(title,0,0); preview.Controls.Add(subtitle,0,1); Add(preview,20);
            var settings=new TableLayoutPanel { ColumnCount=2,RowCount=2,AutoSize=true,Dock=DockStyle.Fill,Margin=new Padding(0) };
            settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,60)); settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,40));
            settings.Controls.Add(LabelFor("Save &as"),0,0); qualityLabel.Text="Video &quality"; qualityLabel.AutoSize=true; qualityLabel.Font=new Font(Font,FontStyle.Bold); qualityLabel.Margin=new Padding(18,0,0,8); settings.Controls.Add(qualityLabel,1,0);
            var formats=new TableLayoutPanel { AutoSize=true,ColumnCount=2,RowCount=1,Dock=DockStyle.Fill,Margin=new Padding(0) }; formats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50)); formats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            video.Text="&Video · MP4"; video.AccessibleName="Video MP4"; video.Dock=DockStyle.Fill; video.Margin=new Padding(0,0,8,0);
            audio.Text="&Audio · MP3"; audio.AccessibleName="Audio MP3"; audio.Dock=DockStyle.Fill; audio.Margin=new Padding(0); formats.Controls.Add(video,0,0); formats.Controls.Add(audio,1,0);
            audio.Checked=state.Format==1; video.Checked=!audio.Checked;
            quality.DropDownStyle=ComboBoxStyle.DropDownList; quality.AccessibleName="Available video quality"; UpdateQualityChoices();
            EventHandler formatChanged=delegate(object sender,EventArgs e) { if(((RadioButton)sender).Checked) { UpdateQualityChoices(); UpdateDownloadEnabled(); SaveState(); } }; video.CheckedChanged+=formatChanged; audio.CheckedChanged+=formatChanged;
            quality.SelectedIndexChanged+=delegate { SaveState(); };
            var qualityField=FieldFor(quality); qualityField.Margin=new Padding(18,0,0,0); settings.Controls.Add(formats,0,1); settings.Controls.Add(qualityField,1,1); Add(settings,18);
            Add(LabelFor("Save &to"),0);
            folder.Text=String.IsNullOrWhiteSpace(state.Folder) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads","YouTube") : state.Folder;
            folder.Font=new Font("Segoe UI",11f); folder.AccessibleName="Save folder"; folder.TextChanged+=delegate { SaveState(); };
            ConfigureButton(browse,"&Browse…"); browse.Width=104; browse.Height=44; browse.Click+=delegate { using(var dialog=new FolderBrowserDialog { Description="Choose where to save your YouTube downloads",SelectedPath=Directory.Exists(folder.Text) ? folder.Text : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),ShowNewFolderButton=true }) { if(dialog.ShowDialog(this)==DialogResult.OK) folder.Text=dialog.SelectedPath; } };
            Add(Row(FieldFor(folder),browse),22);
            ConfigureButton(download,"&Download",true); download.Width=204; download.Height=46; download.Font=new Font(Font,FontStyle.Bold); download.Click+=async delegate { await DownloadAsync(); };
            ConfigureButton(cancel,"&Cancel"); cancel.Width=96; cancel.Height=46; cancel.Enabled=false; cancel.Visible=false; cancel.Click+=delegate { if(cancellation!=null) cancellation.Cancel(); cancel.Enabled=false; SetStatus("Cancelling","Stopping the download…",false); };
            ConfigureButton(openFolder,"Open &folder"); openFolder.Width=112; openFolder.Height=46; openFolder.Quiet=true; openFolder.Click+=delegate { OpenDestination(); };
            ConfigureButton(openFile,"Open &file"); openFile.Width=104; openFile.Height=46; openFile.Quiet=true; openFile.Enabled=false; openFile.Visible=false; openFile.Click+=delegate { OpenCompletedFile(); };
            var primaryActions=new FlowLayoutPanel { AutoSize=true,Dock=DockStyle.Fill,Margin=new Padding(0),WrapContents=true };
            download.Margin=new Padding(0,0,10,0); primaryActions.Controls.Add(download); primaryActions.Controls.Add(cancel);
            Add(Row(primaryActions,openFile,openFolder),18);
            stage.Text=tools.Ready ? "Ready for a video link" : "Downloader tools are missing"; stage.AutoSize=true; stage.Font=new Font(Font,FontStyle.Bold); Add(stage,6);
            progress.Height=5; progress.Minimum=0; progress.Maximum=1000; Add(progress,7);
            progressDetail.Text=tools.Ready ? "Paste a link to get started." : "Missing: "+tools.Missing; progressDetail.AutoSize=true; progressDetail.Tag="muted"; progressDetail.MaximumSize=new Size(740,0); Add(progressDetail,0);
            content=page;
            Add(new Label { Text="Recent downloads",AutoSize=true,Font=new Font(Font,FontStyle.Bold),Margin=new Padding(0),Padding=new Padding(0,20,0,0) },8);
            recent.Height=56; recent.BorderStyle=BorderStyle.None; recent.IntegralHeight=false; recent.AccessibleName="Recent downloads";
            foreach(string file in (state.Recent??new List<string>()).Take(5)) recent.Items.Add(Path.GetFileName(file));
            if(recent.Items.Count==0) recent.Items.Add("Your completed downloads will appear here.");
            recent.DoubleClick+=delegate { if(recent.SelectedIndex>=0 && state.Recent!=null && recent.SelectedIndex<state.Recent.Count) { lastFile=state.Recent[recent.SelectedIndex]; OpenCompletedFile(); } }; Add(recent,10);
            ConfigureButton(update,"Check for updates"); update.Quiet=true; update.Width=168; update.Height=32; update.Click+=async delegate { if(availableUpdate==null) await CheckUpdateAsync(true); else await InstallUpdateAsync(); };
            ConfigureButton(details,"Show details"); details.Quiet=true; details.Height=32; details.Width=112; details.Click+=delegate { showDetails=!showDetails; log.Visible=showDetails; details.Text=showDetails ? "Hide details" : "Show details"; };
            toolsStatus.Text="v"+AppUpdates.Current.ToString(3)+(tools.Ready ? " · Tools ready" : " · Missing: "+tools.Missing); toolsStatus.AutoSize=true; toolsStatus.Tag="muted";
            Add(Row(toolsStatus,update,details),0);
            log.Visible=false; log.Multiline=true; log.ReadOnly=true; log.ScrollBars=ScrollBars.Vertical; log.Height=130; log.AccessibleName="Download details"; log.Font=new Font("Segoe UI",9f); Add(log,0);
            download.Enabled=false; check.Enabled=tools.Ready; AcceptButton=download;
            foreach(var box in new[]{quality,theme}) { box.FlatStyle=FlatStyle.Flat; box.DrawMode=DrawMode.OwnerDrawFixed; box.ItemHeight=24; box.DrawItem+=DrawChoice; }
            foreach(var box in new[]{url,folder,log}) box.BorderStyle=BorderStyle.None;
            Resize+=delegate { int available=Math.Max(480,scroll.ClientSize.Width-104); title.MaximumSize=new Size(available-32,0); subtitle.MaximumSize=new Size(available-32,0); progressDetail.MaximumSize=new Size(available,0); };
        }
        private async Task InspectAsync() {
            if(busy || installingUpdate) return; linkTimer.Stop();
            string link; try { link=VideoLink.Normalize(url.Text); } catch(Exception ex) { SetStatus("Check the link",ex.Message,true); url.Focus(); return; }
            SetBusy(true); log.Clear(); cancellation=new CancellationTokenSource();
            try {
                VideoInfo info=await Task.Run(()=>engine.Inspect(link,cancellation.Token));
                if(info.Live) throw new InvalidOperationException("This video is live right now. Try again once the stream has ended.");
                if(info.Qualities.Count==0 && !info.HasAudio) throw new InvalidOperationException("YouTube did not return downloadable formats for this video.");
                title.Text=info.Title; checkedUrl=url.Text.Trim(); checkedInfo=info; UpdateQualityChoices();
                subtitle.Text=info.Uploader+"  •  "+Duration(info.Duration)+"  •  "+(info.Qualities.Count>0 ? "Available: "+String.Join(", ",info.Qualities.Select(q=>q.Resolution+"p")) : "Audio only");
                SetStatus("Video is ready",info.Qualities.Count>0 ? "Only qualities available for this video are shown." : "This video has an audio download available.",false); progress.Style=ProgressBarStyle.Continuous; progress.Value=0;
            } catch(OperationCanceledException) { SetStatus("Check cancelled","You can paste another link or try again.",false); }
            catch(Exception ex) { checkedInfo=null; checkedUrl=null; UpdateQualityChoices(); SetStatus("Could not check video",FriendlyError(ex.Message),true); }
            finally { SetBusy(false); cancellation.Dispose(); cancellation=null; }
        }
        private async Task DownloadAsync() {
            if(busy || installingUpdate) return;
            if(checkedInfo==null || checkedUrl!=url.Text.Trim()) { await InspectAsync(); if(checkedInfo==null) return; }
            DownloadOptions options;
            try {
                var chosen=quality.SelectedItem as VideoQuality;
                if(!IsAudio && chosen==null) throw new ArgumentException("Choose an available video quality first.");
                if(IsAudio && !checkedInfo.HasAudio) throw new ArgumentException("An audio download is not available for this video.");
                options=new DownloadOptions { Url=VideoLink.Normalize(url.Text),Folder=Path.GetFullPath(folder.Text.Trim()),Audio=IsAudio,Height=chosen==null ? 0 : chosen.Resolution,FormatSelector=chosen==null ? null : chosen.Selector }; Arguments.ForDownload(tools,options);
            }
            catch(Exception ex) { SetStatus("Check your download",ex.Message,true); return; }
            SetBusy(true); log.Clear(); lastFile=null; openFile.Enabled=false; cancellation=new CancellationTokenSource(); SaveState();
            try {
                DownloadResult result=await Task.Run(()=>engine.Download(options,cancellation.Token));
                if(result.Cancelled) { SetStatus("Download cancelled","Partial files are kept so the same download can resume later.",false); progress.Style=ProgressBarStyle.Continuous; progress.Value=0; }
                else { CompleteDownload(result.FilePath); }
            } catch(OperationCanceledException) { SetStatus("Download cancelled","You can start another download.",false); }
            catch(Exception ex) { SetStatus("Download failed",FriendlyError(ex.Message),true); }
            finally { SetBusy(false); cancellation.Dispose(); cancellation=null; }
        }
        private void CompleteDownload(string file) {
            lastFile=file; openFile.Enabled=true; openFile.Visible=true;
            checkedUrl=null; checkedInfo=null; linkTimer.Stop(); url.Clear(); UpdateQualityChoices();
            title.Text="Ready for another video"; subtitle.Text="Paste a new link whenever you're ready.";
            SetStatus("Download complete",Path.GetFileName(lastFile),false); statusMood=2; stage.ForeColor=Accent;
            progress.Style=ProgressBarStyle.Continuous; progress.Value=1000; AddRecent(lastFile);
        }
        private void SetBusy(bool value) {
            busy=value; bool locked=value || installingUpdate; url.Enabled=paste.Enabled=folder.Enabled=browse.Enabled=!locked;
            check.Enabled=video.Enabled=audio.Enabled=!locked && tools.Ready; update.Enabled=!locked && !checkingUpdate;
            quality.Enabled=!locked && !IsAudio && checkedInfo!=null && checkedInfo.Qualities.Count>0; UpdateDownloadEnabled(); cancel.Enabled=value && !installingUpdate; cancel.Visible=value && !installingUpdate; Cursor=locked ? Cursors.AppStarting : Cursors.Default;
            if(!value && progress.Style==ProgressBarStyle.Marquee) { progress.Style=ProgressBarStyle.Continuous; progress.Value=0; }
        }
        private void UpdateDownloadEnabled() { download.Enabled=!busy && !installingUpdate && tools.Ready && checkedInfo!=null && (IsAudio ? checkedInfo.HasAudio : checkedInfo.Qualities.Count>0); }
        private void UpdateQualityChoices() {
            quality.Items.Clear();
            if(IsAudio) { qualityLabel.Text="MP3 encoding bitrate"; quality.Items.Add("192 kbps"); quality.SelectedIndex=0; quality.Enabled=false; }
            else {
                qualityLabel.Text="Available video &quality";
                if(checkedInfo==null || checkedInfo.Qualities.Count==0) { quality.Items.Add("Check a video link first"); quality.SelectedIndex=0; quality.Enabled=false; }
                else { foreach(var q in checkedInfo.Qualities) quality.Items.Add(q); quality.SelectedIndex=AvailableFormats.RecommendedIndex(checkedInfo.Qualities); quality.Enabled=!busy && !installingUpdate; }
            }
        }
        private void ApplyProgress(DownloadProgress p) { statusMood=0; stage.Text=p.Stage; stage.ForeColor=Ink; progressDetail.Text=p.Detail; progressDetail.ForeColor=Muted; progress.Style=p.Indeterminate ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous; if(!p.Indeterminate) progress.Value=(int)Math.Max(0,Math.Min(1000,p.Percent*10)); }
        private void SetStatus(string heading,string message,bool error) { statusMood=error ? 1 : 0; stage.Text=heading; stage.ForeColor=error ? colors.Error : Ink; progressDetail.Text=message; progressDetail.ForeColor=error ? colors.Error : Muted; if(error) { progress.Style=ProgressBarStyle.Continuous; progress.Value=0; AppendLog(message); } }
        private void AppendLog(string text) { if(log.TextLength>24000) log.Text=log.Text.Substring(log.TextLength-16000); log.AppendText(text+Environment.NewLine); }
        private void SafeUi(Action action) { if(IsDisposed || !IsHandleCreated) return; try { BeginInvoke(action); } catch(InvalidOperationException) { } }
        private static string Duration(double seconds) { var time=TimeSpan.FromSeconds(seconds); return seconds>=3600 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"m\:ss"); }
        private static string FriendlyError(string message) {
            message=message.Replace("ERROR: ","");
            if(message.IndexOf("Sign in",StringComparison.OrdinalIgnoreCase)>=0 || message.IndexOf("private",StringComparison.OrdinalIgnoreCase)>=0) return "YouTube requires sign-in for this video. Try a publicly available video.";
            if(message.IndexOf("403",StringComparison.OrdinalIgnoreCase)>=0) return "YouTube refused the download. Try again later or check for a newer yt-dlp version separately.";
            return message;
        }
        private void OpenDestination() { try { string destination=Path.GetFullPath(folder.Text.Trim()); if(!Directory.Exists(destination)) { SetStatus("Folder is not available","Choose an existing folder or download a video first.",true); return; } Process.Start(new ProcessStartInfo(destination) { UseShellExecute=true }); } catch(Exception ex) { SetStatus("Could not open folder",ex.Message,true); } }
        private void OpenCompletedFile() { try { if(lastFile==null || !File.Exists(lastFile)) { SetStatus("File is not available","The file may have been moved or deleted.",true); return; } Process.Start(new ProcessStartInfo(lastFile) { UseShellExecute=true }); } catch(Exception ex) { SetStatus("Could not open file",ex.Message,true); } }
        private void AddRecent(string file) { if(state.Recent==null) state.Recent=new List<string>(); state.Recent.RemoveAll(x=>String.Equals(x,file,StringComparison.OrdinalIgnoreCase)); state.Recent.Insert(0,file); state.Recent=state.Recent.Take(5).ToList(); recent.Items.Clear(); foreach(string path in state.Recent) recent.Items.Add(Path.GetFileName(path)); SaveState(); }
        private SavedState LoadState() { try { if(File.Exists(statePath)) return new JavaScriptSerializer().Deserialize<SavedState>(File.ReadAllText(statePath)) ?? new SavedState(); } catch { } return new SavedState(); }
        private void SaveState() { if(initializing || state==null) return; try { state.Folder=folder.Text; state.Theme=theme.SelectedIndex==1 ? "Dark" : "Light"; state.Format=IsAudio ? 1 : 0; state.Quality=quality.SelectedIndex; File.WriteAllText(statePath,new JavaScriptSerializer().Serialize(state)); } catch { toolsStatus.Text="Settings could not be saved"; } }
        private void ApplyTheme() {
            colors=Palette.For(theme.SelectedIndex==1); SuspendLayout(); ApplyColors(this,colors.Page);
            stage.ForeColor=statusMood==1 ? colors.Error : (statusMood==2 ? Accent : Ink); progressDetail.ForeColor=statusMood==1 ? colors.Error : Muted;
            ApplyTitleBarTheme(); ResumeLayout(true); Invalidate(true);
        }
        private void ApplyTitleBarTheme() { if(!IsHandleCreated) return; try { int dark=theme.SelectedIndex==1 ? 1 : 0; DwmSetWindowAttribute(Handle,20,ref dark,4); } catch(DllNotFoundException) { } catch(EntryPointNotFoundException) { } }
        private void ApplyColors(Control item,Color inherited) {
            Color back=(string)item.Tag=="surface" ? colors.Surface : ((string)item.Tag=="field" ? colors.Field : inherited);
            item.BackColor=back; item.ForeColor=(string)item.Tag=="muted" ? Muted : Ink;
            var button=item as ThemeButton; if(button!=null) { button.Colors=colors; button.Invalidate(); }
            var choice=item as FormatChoice; if(choice!=null) { choice.Colors=colors; choice.Invalidate(); }
            var meter=item as ThemeProgress; if(meter!=null) { meter.Colors=colors; meter.Invalidate(); }
            var combo=item as ThemeCombo; if(combo!=null) { combo.Colors=colors; combo.Invalidate(); }
            if(item is TextBox || item is ComboBox || item is ListBox) { item.BackColor=item is ListBox ? colors.Page : colors.Field; item.ForeColor=Ink; }
            foreach(Control child in item.Controls) ApplyColors(child,back);
        }
        private void DrawChoice(object sender,DrawItemEventArgs e) {
            if(e.Index<0) return; var box=(ComboBox)sender; bool selected=(e.State & DrawItemState.Selected)!=0;
            using(var brush=new SolidBrush(selected ? colors.Selected : colors.Field)) e.Graphics.FillRectangle(brush,e.Bounds);
            TextRenderer.DrawText(e.Graphics,box.Items[e.Index].ToString(),box.Font,new Rectangle(e.Bounds.X+8,e.Bounds.Y,e.Bounds.Width-12,e.Bounds.Height),box.Enabled ? Ink : Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine);
            if(box.Enabled && box.Focused && (e.State & DrawItemState.Focus)!=0) ControlPaint.DrawFocusRectangle(e.Graphics,e.Bounds,Ink,selected ? colors.Selected : colors.Field);
        }
        private async Task CheckUpdateAsync(bool manual) {
            if(checkingUpdate || installingUpdate) return; checkingUpdate=true; update.Enabled=false; update.Text="Checking updates…";
            try {
                availableUpdate=await Task.Run(()=>AppUpdates.Check());
                if(IsDisposed) return;
                if(availableUpdate!=null) { update.Text="Install v"+availableUpdate.Version.ToString(3); toolsStatus.Text="A new version is available"; }
                else { update.Text="Check for updates"; if(manual) toolsStatus.Text="You're up to date · v"+AppUpdates.Current.ToString(3); }
            } catch { if(!IsDisposed) { update.Text="Check for updates"; if(manual) toolsStatus.Text="Couldn't check · Try again when online"; } }
            finally { checkingUpdate=false; if(!IsDisposed) update.Enabled=!busy && !installingUpdate; }
        }
        private async Task InstallUpdateAsync() {
            if(busy || installingUpdate || availableUpdate==null) return; BeginUpdateInstallation(); update.Text="Downloading update…";
            try {
                string prepared=await Task.Run(()=>AppUpdates.Prepare(availableUpdate));
                if(IsDisposed) return;
                AppUpdates.InstallAndRestart(prepared); Close();
            } catch(Exception ex) { if(!IsDisposed) { toolsStatus.Text="Update failed · Your app is unchanged"; AppendLog(ex.Message); update.Text="Retry update"; installingUpdate=false; SetBusy(false); } }
        }
        private void BeginUpdateInstallation() { installingUpdate=true; linkTimer.Stop(); SetBusy(false); }
        private void OnClosing(object sender,FormClosingEventArgs e) { if(busy) { if(MessageBox.Show(this,"A download is running. Cancel it and close?","Close downloader",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes) { e.Cancel=true; return; } if(cancellation!=null) cancellation.Cancel(); } linkTimer.Stop(); linkTimer.Dispose(); SaveState(); engine.Dispose(); }
    }
}
