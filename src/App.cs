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
        public string Theme = "Dark";
        public bool ThemeSelected;
        public string Language="en";
        public int Format, Quality;
        public List<string> Recent = new List<string>();
    }

    public sealed class MainWindow : Form {
        private Palette colors=Palette.For(false);
        private Color Ink { get { return colors.Ink; } }
        private Color Muted { get { return colors.Muted; } }
        private Color Accent { get { return colors.Accent; } }
        private Color Paper { get { return colors.Page; } }
        private readonly ThemeTextBox url=new ThemeTextBox();
        private readonly TextBox folder=new TextBox();
        private readonly ThemeCombo quality=new ThemeCombo();
        private readonly ThemeSwitch theme=new ThemeSwitch();
        private readonly ToolTip themeTip=new ToolTip();
        private readonly FormatChoice video=new FormatChoice(), audio=new FormatChoice();
        private readonly ThemeButton paste=new ThemeButton(), browse=new ThemeButton(), download=new ThemeButton(), cancel=new ThemeButton(), openFolder=new ThemeButton(), openFile=new ThemeButton(), update=new ThemeButton(), noticeDismiss=new ThemeButton(), language=new ThemeButton();
        private readonly Label title=new Label(), subtitle=new Label(), stage=new Label(), progressDetail=new Label(), toolsStatus=new Label();
        private readonly Label updateHeading=new Label(), updateMessage=new Label();
        private readonly StatusIcon statusIcon=new StatusIcon(), updateIcon=new StatusIcon();
        private TableLayoutPanel updateNotice;
        private readonly ThemeProgress progress=new ThemeProgress();
        private readonly ListBox recent=new ListBox();
        private TableLayoutPanel content=new TableLayoutPanel();
        private AppRelease availableUpdate;
        private bool checkingUpdate, installingUpdate;
        private readonly ToolSet tools;
        private readonly DownloadEngine engine;
        private CancellationTokenSource cancellation;
        private bool busy, initializing=true;
        private int statusMood;
        private bool IsAudio { get { return audio.Checked; } }
        private bool IsArabic { get { return state!=null && state.Language=="ar"; } }
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
        private string lastFile, checkedUrl;
        private VideoInfo checkedInfo;
        private readonly System.Windows.Forms.Timer linkTimer=new System.Windows.Forms.Timer { Interval=450 };
        private readonly System.Windows.Forms.Timer updateNoticeTimer=new System.Windows.Forms.Timer { Interval=4500 };
        private readonly string statePath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.json");
        private readonly SavedState state;
        private Image brandLogo;
        private PictureBox logoView;
        private string previewHeading="Choose a video to get started", previewMessage="Available qualities appear after you paste a link.";
        private bool previewIsMetadata, statusIsProgress;
        private string statusHeading="Ready for a video link", statusMessage="Paste a link to get started.", updateHeadingKey="", updateMessageKey="";
        private object[] updateMessageArgs;
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr window,int message,IntPtr value,IntPtr parameter);
        [DllImport("user32.dll")] private static extern bool RedrawWindow(IntPtr window,IntPtr rectangle,IntPtr region,uint flags);

        public MainWindow() {
            Text="YouTube Downloader"; StartPosition=FormStartPosition.CenterScreen; ClientSize=new Size(920,920); MinimumSize=new Size(820,800);
            Font=new Font("Segoe UI",10.5f); BackColor=Paper; ForeColor=Ink; AutoScaleMode=AutoScaleMode.Dpi;
            tools=ToolSet.Discover(); engine=new DownloadEngine(tools); state=LoadState();
            RightToLeft=IsArabic ? RightToLeft.Yes : RightToLeft.No; RightToLeftLayout=IsArabic;
            colors=Palette.For(!state.ThemeSelected || state.Theme=="Dark");
            using(var logoStream=Assembly.GetExecutingAssembly().GetManifestResourceStream("YouTubeDownloader.Logo")) {
                if(logoStream!=null) using(var original=Image.FromStream(logoStream)) brandLogo=new Bitmap(original);
            }
            using(var iconStream=Assembly.GetExecutingAssembly().GetManifestResourceStream("YouTubeDownloader.Icon")) {
                if(iconStream!=null) using(var original=new Icon(iconStream)) Icon=(Icon)original.Clone();
            }
            BuildInterface(); ApplyLanguage(); initializing=false; ApplyTheme(); SaveState();
            updateNoticeTimer.Tick+=delegate { HideUpdateNotice(); };
            engine.Progress += p => SafeUi(delegate { ApplyProgress(p); });
            engine.Title += text => SafeUi(delegate { SetPreview(text,"Your file is being prepared.",true); });
            FormClosing += OnClosing;
            Shown += async delegate { ApplyTitleBarTheme(); url.Focus(); await CheckUpdateAsync(false); };
        }
        private string T(string text) { return Texts.For(state.Language,text); }
        private string F(string text,params object[] args) { return Texts.Format(state.Language,text,args); }
        private Label LabelFor(string text) { return new LocalizedLabel { TextKey=text,Text=T(text),AutoSize=true,ForeColor=Ink,Font=new Font(Font,FontStyle.Bold),Margin=new Padding(0,0,0,8) }; }
        private void SetButtonText(ThemeButton button,string text) { button.TextKey=text; button.Text=T(text); button.AccessibleName=button.Text.Replace("&",""); }
        private void ConfigureButton(ThemeButton button,string text,bool primary=false) {
            SetButtonText(button,text); button.AutoSize=false; button.Height=42; button.FlatStyle=FlatStyle.Flat;
            button.BackColor=primary ? Accent : Color.White; button.ForeColor=primary ? Color.White : Ink;
            button.FlatAppearance.BorderColor=primary ? Accent : Color.FromArgb(166,179,186); button.FlatAppearance.BorderSize=1;
            button.FlatAppearance.MouseOverBackColor=primary ? Color.FromArgb(0,84,78) : Color.FromArgb(232,239,241);
            button.FlatAppearance.MouseDownBackColor=primary ? Color.FromArgb(0,69,65) : Color.FromArgb(219,229,233);
            button.Cursor=Cursors.Hand; button.Margin=new Padding(0); button.UseVisualStyleBackColor=false;
            button.Primary=primary;
        }
        private TableLayoutPanel Row(params Control[] controls) {
            var panel=new TableLayoutPanel { AutoSize=true,Dock=DockStyle.Fill,Margin=new Padding(0),ColumnCount=controls.Length,RowCount=1 };
            for(int i=0;i<controls.Length;i++) { panel.ColumnStyles.Add(i==0 ? new ColumnStyle(SizeType.Percent,100) : new ColumnStyle(SizeType.AutoSize)); controls[i].Anchor=AnchorStyles.Left|AnchorStyles.Right; if(i>0) controls[i].Margin=new Padding(12,0,0,0); panel.Controls.Add(controls[i],i,0); }
            return panel;
        }
        private void Add(Control control,int spacing) { content.RowStyles.Add(new RowStyle(SizeType.AutoSize)); control.Dock=DockStyle.Fill; control.Margin=new Padding(0,0,0,spacing); content.Controls.Add(control,0,content.RowCount++); }
        private ThemeField FieldFor(Control input,int height=44) {
            var field=new ThemeField { Height=height,Dock=DockStyle.Fill,Margin=new Padding(0),Padding=new Padding(12,input is ComboBox ? 5 : 10,12,5),Tag="field" };
            input.Dock=DockStyle.Fill; input.Margin=new Padding(0); field.Controls.Add(input); return field;
        }
        private void BuildInterface() {
            var scroll=new ThemeScrollHost { Dock=DockStyle.Fill,Padding=new Padding(28,24,28,20) };
            content.AutoSize=true; content.ColumnCount=1; content.RowCount=0; content.Margin=new Padding(0); content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); scroll.SetContent(content); Controls.Add(scroll);
            var branding=new TableLayoutPanel { AutoSize=true,ColumnCount=4,RowCount=1,Margin=new Padding(0) };
            branding.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,90)); branding.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); branding.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,106)); branding.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,44));
            logoView=new PictureBox { Image=brandLogo,SizeMode=PictureBoxSizeMode.Zoom,Size=new Size(72,72),AccessibleName="YouTube Downloader logo",TabStop=false };
            var wordmark=new TableLayoutPanel { AutoSize=true,ColumnCount=1,RowCount=1,Dock=DockStyle.Fill,Margin=new Padding(0),Padding=new Padding(0,16,0,0) };
            wordmark.Controls.Add(new LocalizedLabel { TextKey="YouTube Downloader",Text=T("YouTube Downloader"),AutoSize=true,Font=new Font("Segoe UI",22f,FontStyle.Bold),Margin=new Padding(0,0,0,2) },0,0);
            ConfigureButton(language,"العربية"); language.Height=44; language.Width=94; language.Anchor=AnchorStyles.Top|AnchorStyles.Right; language.Margin=new Padding(0,12,12,0); language.Click+=delegate { SetLanguage(IsArabic ? "en" : "ar"); };
            theme.Checked=!state.ThemeSelected || state.Theme=="Dark"; theme.Anchor=AnchorStyles.Top|AnchorStyles.Right; theme.Margin=new Padding(0,12,0,0);
            themeTip.SetToolTip(theme,theme.AccessibleName);
            theme.CheckedChanged+=delegate { state.ThemeSelected=true; themeTip.SetToolTip(theme,theme.AccessibleName); ApplyTheme(); SaveState(); };
            branding.Controls.Add(logoView,0,0); branding.Controls.Add(wordmark,1,0); branding.Controls.Add(language,2,0); branding.Controls.Add(theme,3,0); Add(branding,24);
            var page=content;
            var workspace=new Panel { AutoSize=true,Dock=DockStyle.Fill,Padding=new Padding(24,22,24,22),Margin=new Padding(0),Tag="surface" };
            content=new TableLayoutPanel { AutoSize=true,Dock=DockStyle.Top,ColumnCount=1,RowCount=0,Margin=new Padding(0) }; content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); workspace.Controls.Add(content);
            page.RowStyles.Add(new RowStyle(SizeType.AutoSize)); page.Controls.Add(workspace,0,page.RowCount++);
            Add(LabelFor("YouTube &link"),0);
            url.Font=new Font("Segoe UI",12f); url.AccessibleName=T("YouTube link"); url.TabIndex=0; url.RightToLeft=RightToLeft.No;
            url.Placeholder=T("Videos, Shorts, and shared links"); url.AccessibleDescription=url.Placeholder;
            ConfigureButton(paste,"&Paste"); paste.Width=88; paste.Height=44;
            paste.Click+=async delegate { try { if(Clipboard.ContainsText()) { url.Text=Clipboard.GetText().Trim(); url.Focus(); linkTimer.Stop(); await InspectAsync(); } else SetStatus("Clipboard is empty","Copy a YouTube video link, then choose Paste.",true); } catch { SetStatus("Could not paste","Copy the video link again, then choose Paste.",true); } };
            url.TextChanged+=delegate {
                linkTimer.Stop();
                if(!busy && !installingUpdate && checkedUrl!=url.Text.Trim()) {
                    checkedUrl=null; checkedInfo=null; UpdateQualityChoices(); download.Enabled=false;
                    try { VideoLink.Normalize(url.Text); SetPreview("Checking your next video","Getting the available qualities…"); linkTimer.Start(); }
                    catch(ArgumentException) { SetPreview("Choose a video to get started","Paste one YouTube video link."); if(!String.IsNullOrWhiteSpace(url.Text)) linkTimer.Start(); else if(statusMood==1) SetStatus("Ready for a video link","Paste a link to get started.",false); }
                }
            };
            linkTimer.Tick+=async delegate { linkTimer.Stop(); if(!busy && !installingUpdate) await InspectAsync(); };
            Add(Row(FieldFor(url),paste),18);
            var preview=new TableLayoutPanel { ColumnCount=1,RowCount=2,AutoSize=true,Padding=new Padding(16,12,16,12),Margin=new Padding(0),Tag="field" };
            title.Text=T(previewHeading); title.AutoSize=true; title.MaximumSize=new Size(740,0); title.Font=new Font(Font,FontStyle.Bold); title.Margin=new Padding(0,0,0,4);
            subtitle.Text=T(previewMessage); subtitle.AutoSize=true; subtitle.MaximumSize=new Size(740,0); subtitle.Tag="muted"; subtitle.Margin=new Padding(0);
            preview.Controls.Add(title,0,0); preview.Controls.Add(subtitle,0,1); Add(preview,20);
            var settings=new TableLayoutPanel { ColumnCount=5,RowCount=2,AutoSize=true,Dock=DockStyle.Fill,Margin=new Padding(0) };
            for(int column=0;column<5;column++) settings.ColumnStyles.Add(column%2==0 ? new ColumnStyle(SizeType.Percent,100f/3f) : new ColumnStyle(SizeType.Absolute,12));
            var formatLabel=LabelFor("Save &as"); settings.Controls.Add(formatLabel,0,0); settings.SetColumnSpan(formatLabel,3);
            video.Text=T("&Video · MP4"); video.AccessibleName=T("Video MP4"); video.Dock=DockStyle.Fill; video.Margin=new Padding(0);
            audio.Text=T("&Audio · MP3"); audio.AccessibleName=T("Audio MP3"); audio.Dock=DockStyle.Fill; audio.Margin=new Padding(0); settings.Controls.Add(video,0,1); settings.Controls.Add(audio,2,1);
            audio.Checked=state.Format==1; video.Checked=!audio.Checked;
            quality.DropDownStyle=ComboBoxStyle.DropDownList; quality.AccessibleName="Available video quality"; UpdateQualityChoices();
            EventHandler formatChanged=delegate(object sender,EventArgs e) { if(((RadioButton)sender).Checked) { UpdateQualityChoices(); UpdateDownloadEnabled(); SaveState(); } }; video.CheckedChanged+=formatChanged; audio.CheckedChanged+=formatChanged;
            quality.SelectedIndexChanged+=delegate { SaveState(); };
            quality.Dock=DockStyle.Fill; quality.Margin=new Padding(0); settings.Controls.Add(quality,4,1); Add(settings,18);
            Add(LabelFor("Save &to"),0);
            folder.Text=String.IsNullOrWhiteSpace(state.Folder) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads","YouTube") : state.Folder;
            folder.Font=new Font("Segoe UI",11f); folder.AccessibleName=T("Save folder"); folder.RightToLeft=RightToLeft.No; folder.TextChanged+=delegate { SaveState(); };
            ConfigureButton(browse,"&Browse…"); browse.Width=104; browse.Height=44; browse.Click+=delegate { using(var dialog=new FolderBrowserDialog { Description=T("Choose where to save your YouTube downloads"),SelectedPath=Directory.Exists(folder.Text) ? folder.Text : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),ShowNewFolderButton=true }) { if(dialog.ShowDialog(this)==DialogResult.OK) folder.Text=dialog.SelectedPath; } };
            Add(Row(FieldFor(folder),browse),22);
            ConfigureButton(download,"&Download",true); download.Width=204; download.Height=46; download.Font=new Font(Font,FontStyle.Bold); download.Click+=async delegate { await DownloadAsync(); };
            ConfigureButton(cancel,"&Cancel"); cancel.Width=96; cancel.Height=46; cancel.Enabled=false; cancel.Visible=false; cancel.Click+=delegate { if(cancellation!=null) cancellation.Cancel(); cancel.Enabled=false; SetStatus("Cancelling","Stopping the download…",false); };
            ConfigureButton(openFolder,"Open &folder"); openFolder.Width=112; openFolder.Height=46; openFolder.Quiet=true; openFolder.Click+=delegate { OpenDestination(); };
            ConfigureButton(openFile,"Open &file"); openFile.Width=104; openFile.Height=46; openFile.Quiet=true; openFile.Enabled=false; openFile.Visible=false; openFile.Click+=delegate { OpenCompletedFile(); };
            var primaryActions=new FlowLayoutPanel { AutoSize=true,Dock=DockStyle.Fill,Margin=new Padding(0),WrapContents=true };
            download.Margin=new Padding(10,0,0,0); primaryActions.Controls.Add(download); primaryActions.Controls.Add(cancel);
            Add(Row(primaryActions,openFile,openFolder),18);
            statusHeading=tools.Ready ? "Ready for a video link" : "Downloader tools are missing"; statusMessage=tools.Ready ? "Paste a link to get started." : "Run SetupTools.cmd in the app folder, then reopen the app.";
            stage.Text=T(statusHeading); stage.AutoSize=true; stage.Font=new Font(Font,FontStyle.Bold); stage.Margin=new Padding(0,0,0,4);
            progressDetail.Text=T(statusMessage); progressDetail.AutoSize=true; progressDetail.Tag="muted"; progressDetail.MaximumSize=new Size(696,0); progressDetail.Margin=new Padding(0);
            Add(StatusRow(statusIcon,stage,progressDetail),10);
            progress.Height=5; progress.Minimum=0; progress.Maximum=1000; progress.Visible=false; Add(progress,0);
            if(!tools.Ready) { statusMood=1; statusIcon.Tone=StatusTone.Error; }
            content=page;
            Add(new LocalizedLabel { TextKey="Recent downloads",Text=T("Recent downloads"),AutoSize=true,Font=new Font(Font,FontStyle.Bold),Margin=new Padding(0),Padding=new Padding(0,20,0,0) },8);
            recent.Height=56; recent.BorderStyle=BorderStyle.None; recent.IntegralHeight=false; recent.AccessibleName="Recent downloads";
            foreach(string file in (state.Recent??new List<string>()).Take(5)) recent.Items.Add(Path.GetFileName(file));
            if(recent.Items.Count==0) recent.Items.Add(T("Your completed downloads will appear here."));
            recent.DoubleClick+=delegate { if(recent.SelectedIndex>=0 && state.Recent!=null && recent.SelectedIndex<state.Recent.Count) { lastFile=state.Recent[recent.SelectedIndex]; OpenCompletedFile(); } }; Add(recent,10);
            ConfigureButton(update,"Check for updates"); update.Width=168; update.Height=38; update.Click+=async delegate { if(availableUpdate==null) await CheckUpdateAsync(true); else await InstallUpdateAsync(); };
            toolsStatus.Text=ToolsText(); toolsStatus.AutoSize=true; toolsStatus.Tag="muted";
            Add(Row(toolsStatus,update),0);
            updateHeading.AutoSize=true; updateHeading.Font=new Font(Font,FontStyle.Bold); updateHeading.Margin=new Padding(0,0,0,4);
            updateMessage.AutoSize=true; updateMessage.Tag="muted"; updateMessage.MaximumSize=new Size(696,0); updateMessage.Margin=new Padding(0);
            ConfigureButton(noticeDismiss,"Dismiss"); noticeDismiss.Quiet=true; noticeDismiss.Width=80; noticeDismiss.Height=32; noticeDismiss.AccessibleName="Dismiss update message"; noticeDismiss.Click+=delegate { HideUpdateNotice(); };
            updateNotice=StatusRow(updateIcon,updateHeading,updateMessage); updateNotice.ColumnCount=3; updateNotice.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); noticeDismiss.Margin=new Padding(0,0,12,0); updateNotice.Controls.Add(noticeDismiss,2,0);
            updateNotice.Visible=false; updateNotice.AccessibleName="App update status"; Add(updateNotice,0); updateNotice.Margin=new Padding(0,12,0,0);
            download.Enabled=false; AcceptButton=download;
            quality.FlatStyle=FlatStyle.Flat; quality.DrawMode=DrawMode.OwnerDrawFixed; quality.ItemHeight=38; quality.DrawItem+=DrawChoice;
            EventHandler alignQualityHeight=delegate { int frame=quality.Height-quality.ItemHeight; if(quality.Height!=video.Height) quality.ItemHeight=Math.Max(1,video.Height-frame); };
            video.SizeChanged+=alignQualityHeight; quality.HandleCreated+=alignQualityHeight;
            foreach(var box in new[]{url,folder}) box.BorderStyle=BorderStyle.None;
            Resize+=delegate { int available=Math.Max(400,scroll.ClientSize.Width-104); title.MaximumSize=new Size(available-32,0); subtitle.MaximumSize=new Size(available-32,0); progressDetail.MaximumSize=new Size(available-44,0); updateMessage.MaximumSize=new Size(available-44,0); };
        }
        private TableLayoutPanel StatusRow(StatusIcon icon,Label heading,Label message) {
            var copy=new TableLayoutPanel { AutoSize=true,Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=new Padding(0) }; copy.Controls.Add(heading,0,0); copy.Controls.Add(message,0,1);
            var row=new TableLayoutPanel { AutoSize=true,Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=new Padding(0) }; row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,44)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            icon.Margin=new Padding(12,1,0,0); row.Controls.Add(icon,0,0); row.Controls.Add(copy,1,0); return row;
        }
        private string ToolsText() { return tools.Ready ? F("v{0} · Tools ready",AppUpdates.Current.ToString(3)) : F("v{0} · Missing: {1}",AppUpdates.Current.ToString(3),tools.Missing); }
        private void SetPreview(string heading,string message,bool metadata=false) { previewHeading=heading; previewMessage=message; previewIsMetadata=metadata; title.Text=metadata ? heading : T(heading); subtitle.Text=T(message); }
        private string VideoSubtitle(VideoInfo info) { return info.Uploader+"  •  "+Duration(info.Duration)+"  •  "+(info.Qualities.Count>0 ? T("Available: ")+String.Join(", ",info.Qualities.Select(q=>q.Resolution+"p")) : T("Audio only")); }
        private void SetLanguage(string value) {
            if(busy || installingUpdate) return;
            state.Language=value=="ar" ? "ar" : "en"; ApplyLanguage(); ApplyTheme(); SaveState();
        }
        private void ApplyLanguage() {
            SuspendLayout();
            try {
                RightToLeft=IsArabic ? RightToLeft.Yes : RightToLeft.No; RightToLeftLayout=IsArabic; Text=T("YouTube Downloader");
                var controls=new List<Control>(); CollectControls(this,controls);
                foreach(var control in controls) {
                    var label=control as LocalizedLabel; if(label!=null) label.Text=T(label.TextKey);
                    var button=control as ThemeButton; if(button!=null && button!=language && button.TextKey!=null) SetButtonText(button,button.TextKey);
                    var bar=control as ThemeScrollBar; if(bar!=null) { bar.AccessibleName=T("Scroll page"); bar.AccessibleDescription=T("Use the arrow keys, Page Up, Page Down, Home, or End to scroll."); }
                    var icon=control as StatusIcon; if(icon!=null) { icon.Language=state.Language; icon.Tone=icon.Tone; }
                }
                language.Text=IsArabic ? "English" : "العربية"; language.AccessibleName=T(IsArabic ? "Switch to English" : "Switch to Arabic");
                themeTip.SetToolTip(language,language.AccessibleName); logoView.AccessibleName=T("YouTube Downloader logo");
                logoView.Margin=IsArabic ? new Padding(18,0,0,0) : new Padding(0,0,18,0);
                theme.Language=state.Language; theme.UpdateActionName(); themeTip.SetToolTip(theme,theme.AccessibleName);
                url.Placeholder=T("Videos, Shorts, and shared links"); url.PlaceholderRightToLeft=IsArabic; url.AccessibleName=T("YouTube link"); url.AccessibleDescription=url.Placeholder; url.Invalidate();
                folder.AccessibleName=T("Save folder"); video.Text=T("&Video · MP4"); video.AccessibleName=T("Video MP4"); audio.Text=T("&Audio · MP3"); audio.AccessibleName=T("Audio MP3");
                var selected=quality.SelectedItem as VideoQuality; UpdateQualityChoices();
                if(selected!=null && checkedInfo!=null && !IsAudio) { int index=checkedInfo.Qualities.FindIndex(q=>q.Resolution==selected.Resolution && q.Selector==selected.Selector); if(index>=0) quality.SelectedIndex=index; }
                title.Text=previewIsMetadata ? previewHeading : T(previewHeading); subtitle.Text=previewIsMetadata && checkedInfo!=null ? VideoSubtitle(checkedInfo) : T(previewMessage);
                stage.Text=T(statusHeading); progressDetail.Text=statusIsProgress ? Texts.Progress(state.Language,statusMessage) : T(statusMessage);
                toolsStatus.Text=ToolsText(); updateHeading.Text=T(updateHeadingKey); updateMessage.Text=F(updateMessageKey,updateMessageArgs);
                updateNotice.AccessibleName=T("App update status"); noticeDismiss.AccessibleName=T("Dismiss update message");
                recent.AccessibleName=T("Recent downloads"); if(state.Recent==null || state.Recent.Count==0) { recent.Items.Clear(); recent.Items.Add(T("Your completed downloads will appear here.")); }
            } finally { ResumeLayout(true); }
        }
        private async Task InspectAsync() {
            if(busy || installingUpdate) return; linkTimer.Stop();
            string link; try { link=VideoLink.Normalize(url.Text); } catch(Exception ex) { SetStatus("Check the link",ex.Message,true); url.Focus(); return; }
            SetBusy(true); cancellation=new CancellationTokenSource();
            try {
                VideoInfo info=await Task.Run(()=>engine.Inspect(link,cancellation.Token));
                if(info.Live) throw new InvalidOperationException("This video is live right now. Try again once the stream has ended.");
                if(info.Qualities.Count==0 && !info.HasAudio) throw new InvalidOperationException("YouTube did not return downloadable formats for this video.");
                checkedUrl=url.Text.Trim(); checkedInfo=info; UpdateQualityChoices(); SetPreview(info.Title,VideoSubtitle(info),true);
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
                options=new DownloadOptions { Url=VideoLink.Normalize(url.Text),Folder=DownloadFolder(),Audio=IsAudio,Height=chosen==null ? 0 : chosen.Resolution,FormatSelector=chosen==null ? null : chosen.Selector }; Arguments.ForDownload(tools,options);
            }
            catch(Exception ex) { SetStatus("Check your download",ex.Message,true); return; }
            SetBusy(true); lastFile=null; openFile.Enabled=false; cancellation=new CancellationTokenSource(); SaveState();
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
            SetPreview("Ready for another video","Paste a new link whenever you're ready.");
            SetStatus("Download complete",Path.GetFileName(lastFile),false); statusMood=2; statusIcon.Tone=StatusTone.Success; stage.ForeColor=Accent;
            progress.Style=ProgressBarStyle.Continuous; progress.Value=1000; progress.Visible=true; AddRecent(lastFile);
        }
        private void SetBusy(bool value) {
            busy=value; bool locked=value || installingUpdate; url.Enabled=paste.Enabled=folder.Enabled=browse.Enabled=language.Enabled=!locked;
            video.Enabled=audio.Enabled=!locked && tools.Ready; update.Enabled=!locked && !checkingUpdate;
            quality.Enabled=!locked && !IsAudio && checkedInfo!=null && checkedInfo.Qualities.Count>0; UpdateDownloadEnabled(); cancel.Enabled=value && !installingUpdate; cancel.Visible=value && !installingUpdate; Cursor=locked ? Cursors.AppStarting : Cursors.Default;
            if(!value && progress.Style==ProgressBarStyle.Marquee) { progress.Style=ProgressBarStyle.Continuous; progress.Value=0; }
        }
        private void UpdateDownloadEnabled() { download.Enabled=!busy && !installingUpdate && tools.Ready && checkedInfo!=null && (IsAudio ? checkedInfo.HasAudio : checkedInfo.Qualities.Count>0); }
        private void UpdateQualityChoices() {
            quality.Items.Clear();
            if(IsAudio) { quality.AccessibleName=T("MP3 encoding bitrate"); quality.Items.Add(T("192 kbps")); quality.SelectedIndex=0; quality.Enabled=false; }
            else {
                quality.AccessibleName=T("Available video quality");
                if(checkedInfo==null || checkedInfo.Qualities.Count==0) { quality.Items.Add(T("Quality")); quality.SelectedIndex=0; quality.Enabled=false; }
                else { foreach(var q in checkedInfo.Qualities) quality.Items.Add(q); quality.SelectedIndex=AvailableFormats.RecommendedIndex(checkedInfo.Qualities); quality.Enabled=!busy && !installingUpdate; }
            }
        }
        private void ApplyProgress(DownloadProgress p) { statusHeading=p.Stage; statusMessage=p.Detail; statusIsProgress=true; statusMood=0; statusIcon.Tone=StatusTone.Info; stage.Text=T(p.Stage); stage.ForeColor=Ink; progressDetail.Text=Texts.Progress(state.Language,p.Detail); progressDetail.ForeColor=Muted; progress.Visible=true; progress.Style=p.Indeterminate ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous; if(!p.Indeterminate) progress.Value=(int)Math.Max(0,Math.Min(1000,p.Percent*10)); }
        private void SetStatus(string heading,string message,bool error) { statusHeading=heading; statusMessage=message; statusIsProgress=false; statusMood=error ? 1 : 0; statusIcon.Tone=error ? StatusTone.Error : StatusTone.Info; stage.Text=T(heading); stage.ForeColor=error ? colors.Error : Ink; progressDetail.Text=T(message); progressDetail.ForeColor=Muted; progress.Visible=false; if(error) { progress.Style=ProgressBarStyle.Continuous; progress.Value=0; } }
        private string DownloadFolder() { if(String.IsNullOrWhiteSpace(folder.Text)) throw new ArgumentException("Choose a save folder with Browse."); try { return Path.GetFullPath(folder.Text.Trim()); } catch { throw new ArgumentException("Choose a valid folder on this computer with Browse."); } }
        private void SafeUi(Action action) { if(IsDisposed || !IsHandleCreated) return; try { BeginInvoke(action); } catch(InvalidOperationException) { } }
        private static string Duration(double seconds) { var time=TimeSpan.FromSeconds(seconds); return seconds>=3600 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"m\:ss"); }
        private static string FriendlyError(string message) {
            message=(message??"").ToLowerInvariant();
            if(message.Contains("429") || message.Contains("too many requests")) return "YouTube is limiting downloads right now. Wait a few minutes, then try again.";
            if(message.Contains("sign in") || message.Contains("private") || message.Contains("age-restricted")) return "This video requires sign-in. Try a publicly available video.";
            if(message.Contains("403")) return "YouTube refused this download. Wait a little, then paste the link again.";
            if(message.Contains("unavailable") || message.Contains("removed") || message.Contains("not available in your country")) return "This video is unavailable. Try another video link.";
            if(message.Contains("requested format") || message.Contains("no downloadable formats")) return "That quality is no longer available. Paste the link again to refresh the options.";
            if(message.Contains("no space") || message.Contains("disk full")) return "The drive is full. Free some space or choose another save folder.";
            if(message.Contains("permission") || message.Contains("access is denied")) return "We can't save in that folder. Choose another folder with Browse.";
            if(message.Contains("timed out") || message.Contains("network") || message.Contains("resolve") || message.Contains("connection")) return "Couldn't reach YouTube. Check your internet connection, then try again.";
            if(message.Contains("missing tools")) return "Run SetupTools.cmd in the app folder to install the missing tools, then reopen the app.";
            return "We couldn't finish this action. Check the link and save folder, then try again.";
        }
        private void OpenDestination() { try { string destination=Path.GetFullPath(folder.Text.Trim()); if(!Directory.Exists(destination)) { SetStatus("Folder is not available","Choose an existing folder or download a video first.",true); return; } Process.Start(new ProcessStartInfo(destination) { UseShellExecute=true }); } catch { SetStatus("Could not open folder","Choose a valid folder on this computer with Browse.",true); } }
        private void OpenCompletedFile() { try { if(lastFile==null || !File.Exists(lastFile)) { SetStatus("File is not available","The file may have been moved or deleted.",true); return; } Process.Start(new ProcessStartInfo(lastFile) { UseShellExecute=true }); } catch { SetStatus("Could not open file","Open the save folder and try opening the file there.",true); } }
        private void AddRecent(string file) { if(state.Recent==null) state.Recent=new List<string>(); state.Recent.RemoveAll(x=>String.Equals(x,file,StringComparison.OrdinalIgnoreCase)); state.Recent.Insert(0,file); state.Recent=state.Recent.Take(5).ToList(); recent.Items.Clear(); foreach(string path in state.Recent) recent.Items.Add(Path.GetFileName(path)); SaveState(); }
        private SavedState LoadState() { try { if(File.Exists(statePath)) return new JavaScriptSerializer().Deserialize<SavedState>(File.ReadAllText(statePath)) ?? new SavedState(); } catch { } return new SavedState(); }
        private void SaveState() { if(initializing || state==null) return; try { state.Folder=folder.Text; state.Theme=theme.Checked ? "Dark" : "Light"; state.Format=IsAudio ? 1 : 0; state.Quality=quality.SelectedIndex; File.WriteAllText(statePath,new JavaScriptSerializer().Serialize(state)); } catch { toolsStatus.Text=T("Settings could not be saved"); } }
        private void ApplyTheme() {
            var controls=new List<Control>(); CollectControls(this,controls);
            var windows=controls.Where(c=>c.IsHandleCreated && c.Visible).Select(c=>c.Handle).ToList();
            foreach(var control in controls) control.SuspendLayout();
            foreach(var window in windows) SendMessage(window,0x000B,IntPtr.Zero,IntPtr.Zero);
            try {
                colors=Palette.For(theme.Checked); ApplyColors(this,colors.Page);
                stage.ForeColor=statusMood==1 ? colors.Error : (statusMood==2 ? Accent : Ink); progressDetail.ForeColor=Muted;
                ApplyTitleBarTheme();
            } finally {
                for(int i=controls.Count-1;i>=0;i--) controls[i].ResumeLayout(false);
                for(int i=windows.Count-1;i>=0;i--) SendMessage(windows[i],0x000B,new IntPtr(1),IntPtr.Zero);
                if(IsHandleCreated && Visible) RedrawWindow(Handle,IntPtr.Zero,IntPtr.Zero,0x0585);
                else Invalidate(true);
            }
        }
        private static void CollectControls(Control control,List<Control> controls) { controls.Add(control); foreach(Control child in control.Controls) CollectControls(child,controls); }
        private void ApplyTitleBarTheme() { if(!IsHandleCreated) return; try { int dark=theme.Checked ? 1 : 0; DwmSetWindowAttribute(Handle,20,ref dark,4); } catch(DllNotFoundException) { } catch(EntryPointNotFoundException) { } }
        private void ApplyColors(Control item,Color inherited) {
            Color back=(string)item.Tag=="surface" ? colors.Surface : ((string)item.Tag=="field" ? colors.Field : inherited);
            if(item is TextBox || item is ComboBox || item is ListBox) back=item is ListBox ? colors.Page : colors.Field;
            if(item.BackColor!=back) item.BackColor=back;
            Color foreground=(string)item.Tag=="muted" ? Muted : Ink; if(item.ForeColor!=foreground) item.ForeColor=foreground;
            var button=item as ThemeButton; if(button!=null) button.Colors=colors;
            var choice=item as FormatChoice; if(choice!=null) choice.Colors=colors;
            var meter=item as ThemeProgress; if(meter!=null) meter.Colors=colors;
            var combo=item as ThemeCombo; if(combo!=null) combo.Colors=colors;
            var toggle=item as ThemeSwitch; if(toggle!=null) toggle.Colors=colors;
            var field=item as ThemeField; if(field!=null) field.Colors=colors;
            var input=item as ThemeTextBox; if(input!=null) input.Colors=colors;
            var icon=item as StatusIcon; if(icon!=null) icon.Colors=colors;
            var scrollbar=item as ThemeScrollBar; if(scrollbar!=null) scrollbar.Colors=colors;
            foreach(Control child in item.Controls) ApplyColors(child,back);
        }
        private void DrawChoice(object sender,DrawItemEventArgs e) {
            if(e.Index<0) return; var box=(ComboBox)sender; bool selected=(e.State & DrawItemState.Selected)!=0;
            using(var brush=new SolidBrush(selected ? colors.Selected : colors.Field)) e.Graphics.FillRectangle(brush,e.Bounds);
            TextRenderer.DrawText(e.Graphics,box.Items[e.Index].ToString(),box.Font,new Rectangle(e.Bounds.X+8,e.Bounds.Y,e.Bounds.Width-16,e.Bounds.Height),box.Enabled ? Ink : Muted,(IsArabic ? TextFormatFlags.Right : TextFormatFlags.Left)|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine);
            if(box.Enabled && box.Focused && (e.State & DrawItemState.Focus)!=0) ControlPaint.DrawFocusRectangle(e.Graphics,e.Bounds,Ink,selected ? colors.Selected : colors.Field);
        }
        private async Task CheckUpdateAsync(bool manual) {
            if(checkingUpdate || installingUpdate) return; checkingUpdate=true; update.Enabled=false; SetButtonText(update,"Checking updates…"); if(manual) ShowUpdateNotice("Checking for updates","Looking for the latest app release…",StatusTone.Info);
            try {
                availableUpdate=await Task.Run(()=>AppUpdates.Check());
                if(IsDisposed) return;
                ReportUpdateCheck(availableUpdate,manual);
            } catch { if(!IsDisposed) { SetButtonText(update,"Check for updates"); if(manual) ShowUpdateNotice("Couldn't check for updates","Check your internet connection, then choose Check for updates again.",StatusTone.Error); } }
            finally { checkingUpdate=false; if(!IsDisposed) update.Enabled=!busy && !installingUpdate; }
        }
        private void ReportUpdateCheck(AppRelease release,bool manual) {
            availableUpdate=release;
            if(release!=null) { SetButtonText(update,"Install v"+release.Version.ToString(3)); ShowUpdateNotice("A new version is ready","Version {0} is available. Choose Install to update the app.",StatusTone.Info,release.Version.ToString(3)); }
            else { SetButtonText(update,"Check for updates"); if(manual) ShowUpdateNotice("You're up to date","Version {0} is the latest release.",StatusTone.Success,AppUpdates.Current.ToString(3)); }
        }
        private void ShowUpdateNotice(string heading,string message,StatusTone tone,params object[] args) { updateNoticeTimer.Stop(); updateHeadingKey=heading; updateMessageKey=message; updateMessageArgs=args; updateHeading.Text=T(heading); updateMessage.Text=F(message,args); updateIcon.Tone=tone; updateNotice.Visible=true; if(tone==StatusTone.Success) updateNoticeTimer.Start(); }
        private void HideUpdateNotice() { updateNoticeTimer.Stop(); updateNotice.Visible=false; updateHeadingKey=updateMessageKey=""; updateHeading.Text=""; updateMessage.Text=""; }
        private async Task InstallUpdateAsync() {
            if(busy || installingUpdate || availableUpdate==null) return; BeginUpdateInstallation(); SetButtonText(update,"Downloading update…"); ShowUpdateNotice("Preparing your update","The app will restart when the verified update is ready.",StatusTone.Info);
            try {
                string prepared=await Task.Run(()=>AppUpdates.Prepare(availableUpdate));
                if(IsDisposed) return;
                AppUpdates.InstallAndRestart(prepared,T("The update could not be installed. The previous app will reopen."),T("YouTube Downloader")); Close();
            } catch { if(!IsDisposed) { ShowUpdateNotice("Update couldn't be installed","Your current app is ready to use. Check your connection and choose Retry update.",StatusTone.Error); SetButtonText(update,"Retry update"); installingUpdate=false; SetBusy(false); } }
        }
        private void BeginUpdateInstallation() { installingUpdate=true; linkTimer.Stop(); SetBusy(false); }
        private void OnClosing(object sender,FormClosingEventArgs e) { if(busy) { if(MessageBox.Show(this,T("A download is running. Cancel it and close?"),T("Close downloader"),MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2,IsArabic ? MessageBoxOptions.RtlReading|MessageBoxOptions.RightAlign : 0)!=DialogResult.Yes) { e.Cancel=true; return; } if(cancellation!=null) cancellation.Cancel(); } linkTimer.Stop(); linkTimer.Dispose(); SaveState(); engine.Dispose(); }
        protected override void Dispose(bool disposing) { if(disposing) { updateNoticeTimer.Dispose(); linkTimer.Dispose(); themeTip.Dispose(); if(brandLogo!=null) brandLogo.Dispose(); if(engine!=null) engine.Dispose(); } base.Dispose(disposing); }
    }
}
