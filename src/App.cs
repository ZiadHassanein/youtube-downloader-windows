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
        public int FolderVersion;
        public string Theme = "Dark";
        public bool ThemeSelected;
        public string Language="en";
        public int Format, Quality;
        public int AudioBitrate=320;
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
        private readonly FormatChoice oneVideo=new FormatChoice(), wholePlaylist=new FormatChoice();
        private TableLayoutPanel playlistRow;
        private readonly Label playlistSummary=new Label();
        private bool configuringScope;
        private PlaylistInfo checkedPlaylist;
        private DownloadProgress latestProgress;
        private object[] statusArgs;
        private string statusSuffix;
        private readonly ThemeButton paste=new ThemeButton(), browse=new ThemeButton(), download=new ThemeButton(), cancel=new ThemeButton(), openFolder=new ThemeButton(), openFile=new ThemeButton(), showInFolder=new ThemeButton(), update=new ThemeButton(), noticeDismiss=new ThemeButton(), language=new ThemeButton(), recentButton=new ThemeButton();
        private readonly Label stage=new Label(), progressDetail=new Label(), toolsStatus=new Label();
        private readonly Label updateHeading=new Label(), updateMessage=new Label();
        private readonly StatusIcon statusIcon=new StatusIcon(), updateIcon=new StatusIcon();
        private NoticePopover updateNotice;
        private Control noticeAnchor;
        private TableLayoutPanel statusRow, followUpRow;
        private readonly ThemeProgress progress=new ThemeProgress();
        private TableLayoutPanel content=new TableLayoutPanel();
        private TableLayoutPanel workspace, taskContent;
        private ThemeScrollHost scroll;
        private TableLayoutPanel branding, updateRow, authorRow;
        private AppRelease availableUpdate;
        private bool checkingUpdate, installingUpdate;
        private readonly ToolSet tools;
        private readonly DownloadEngine engine;
        private CancellationTokenSource cancellation;
        private bool busy, initializing=true;
        private bool fittingHeight, fitQueued;
        private int statusMood;
        private bool IsAudio { get { return audio.Checked; } }
        private bool IsPlaylist { get { return wholePlaylist.Checked; } }
        private bool IsArabic { get { return state!=null && state.Language=="ar"; } }
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
        private string lastFile, checkedUrl;
        private VideoInfo checkedInfo;
        private readonly System.Windows.Forms.Timer linkTimer=new System.Windows.Forms.Timer { Interval=450 };
        private readonly System.Windows.Forms.Timer updateNoticeTimer=new System.Windows.Forms.Timer { Interval=5000 };
        private readonly string statePath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.json");
        private readonly SavedState state;
        private Image brandLogo;
        private PictureBox logoView;
        private bool statusIsProgress;
        private string statusHeading="Ready for a video link", statusMessage="Paste a link to get started.", updateHeadingKey="", updateMessageKey="";
        private object[] updateMessageArgs;
        private readonly List<LayoutState> layoutStates=new List<LayoutState>();
        private readonly List<TableState> tableStates=new List<TableState>();
        private sealed class LayoutState { public Control Control; public Padding Margin; public AnchorStyles Anchor; public int Column; }
        private sealed class TableState { public TableLayoutPanel Table; public ColumnStyle[] Columns; }
        private sealed class AudioQuality { public int Bitrate; public override string ToString() { return Bitrate+" kbps"; } }
        private sealed class PlaylistQuality { public int Height; public string Text; public override string ToString() { return Text; } }
        [DllImport("user32.dll")] private static extern bool RedrawWindow(IntPtr window,IntPtr rectangle,IntPtr region,uint flags);
        [DllImport("shell32.dll",CharSet=CharSet.Unicode)] private static extern int SHParseDisplayName(string path,IntPtr bindContext,out IntPtr item,uint attributes,out uint returnedAttributes);
        [DllImport("shell32.dll")] private static extern int SHOpenFolderAndSelectItems(IntPtr item,uint count,IntPtr children,uint flags);

        public MainWindow() : this(true) { }
        internal MainWindow(bool checkUpdatesOnStart) {
            Text="YouTube Downloader"; StartPosition=FormStartPosition.CenterScreen; ClientSize=new Size(980,640); MinimumSize=new Size(820,400);
            Font=new Font("Segoe UI",11f); BackColor=Paper; ForeColor=Ink; AutoScaleMode=AutoScaleMode.Dpi; DoubleBuffered=true;
            tools=ToolSet.Discover(); engine=new DownloadEngine(tools); state=LoadState();
            RightToLeft=RightToLeft.No; RightToLeftLayout=false;
            colors=Palette.For(!state.ThemeSelected || state.Theme=="Dark");
            using(var logoStream=Assembly.GetExecutingAssembly().GetManifestResourceStream("YouTubeDownloader.Logo")) {
                if(logoStream!=null) using(var original=Image.FromStream(logoStream)) brandLogo=new Bitmap(original);
            }
            using(var iconStream=Assembly.GetExecutingAssembly().GetManifestResourceStream("YouTubeDownloader.Icon")) {
                if(iconStream!=null) using(var original=new Icon(iconStream)) Icon=(Icon)original.Clone();
            }
            BuildInterface(); CaptureLayout(content); ApplyLanguage(); initializing=false; ApplyTheme(); SaveState();
            updateNoticeTimer.Tick+=delegate { HideUpdateNotice(); };
            Move+=delegate { if(updateNotice!=null && updateNotice.Visible) HideUpdateNotice(); };
            ResizeBegin+=delegate { HideUpdateNotice(); };
            engine.Progress += p => SafeUi(delegate { ApplyProgress(p); });
            FormClosing += OnClosing;
            Shown += async delegate { FitContentHeight(); ApplyTitleBarTheme(); url.Focus(); if(checkUpdatesOnStart) await CheckUpdateAsync(false); };
        }
        private string T(string text) { return Texts.For(state.Language,text); }
        private string F(string text,params object[] args) { return Texts.Format(state.Language,text,args); }
        private Label LabelFor(string text) { return new LocalizedLabel { TextKey=text,Text=T(text),AutoSize=true,ForeColor=Ink,Font=new Font(Font,FontStyle.Bold),Margin=new Padding(0,0,0,8) }; }
        private void SetButtonText(ThemeButton button,string text) {
            button.TextKey=text; button.Text=T(text); button.AccessibleName=button.Text.Replace("&","");
            if(button==update) button.Glyph=text.StartsWith("Install v",StringComparison.Ordinal) || text=="Downloading update…" ? ButtonGlyph.Download : ButtonGlyph.Refresh;
        }
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
            for(int i=0;i<controls.Length;i++) { panel.ColumnStyles.Add(i==0 ? new ColumnStyle(SizeType.Percent,100) : new ColumnStyle(SizeType.AutoSize)); controls[i].Anchor=AnchorStyles.Left|AnchorStyles.Right; if(i>0) controls[i].Margin=new Padding(16,0,0,0); panel.Controls.Add(controls[i],i,0); }
            return panel;
        }
        private void Add(Control control,int spacing) { content.RowStyles.Add(new RowStyle(SizeType.AutoSize)); control.Dock=DockStyle.Fill; control.Margin=new Padding(0,0,0,spacing); content.Controls.Add(control,0,content.RowCount++); }
        private ThemeField FieldFor(Control input,int height=48) {
            var field=new ThemeField { Height=height,Dock=DockStyle.Fill,Margin=new Padding(0),Padding=new Padding(14,input is ComboBox ? 5 : 12,14,5),Tag="field" };
            input.Dock=DockStyle.Fill; input.Margin=new Padding(0); field.Controls.Add(input); return field;
        }
        private void BuildInterface() {
            scroll=new ThemeScrollHost { Dock=DockStyle.Fill,Padding=new Padding(32,28,32,24) };
            scroll.ContentHeightChanged+=delegate { QueueFitHeight(); };
            content.AutoSize=true; content.ColumnCount=1; content.RowCount=0; content.Margin=new Padding(0); content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); scroll.SetContent(content); Controls.Add(scroll);
            branding=new TableLayoutPanel { AutoSize=true,ColumnCount=4,RowCount=1,Margin=new Padding(0) };
            branding.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,90)); branding.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); branding.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,106)); branding.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,44));
            logoView=new PictureBox { Image=brandLogo,SizeMode=PictureBoxSizeMode.Zoom,Size=new Size(72,72),AccessibleName="YouTube Downloader logo",TabStop=false,Margin=new Padding(0,0,18,0) };
            var wordmark=new TableLayoutPanel { AutoSize=true,ColumnCount=1,RowCount=1,Dock=DockStyle.Fill,Margin=new Padding(0),Padding=new Padding(0,16,0,0) };
            wordmark.Controls.Add(new LocalizedLabel { TextKey="YouTube Downloader",Text=T("YouTube Downloader"),AutoSize=true,Font=new Font("Segoe UI",22f,FontStyle.Bold),Margin=new Padding(0,0,0,2) },0,0);
            ConfigureButton(language,"العربية"); language.Height=44; language.Width=94; language.Anchor=AnchorStyles.Top|AnchorStyles.Right; language.Margin=new Padding(0,12,12,0); language.Click+=delegate { SetLanguage(IsArabic ? "en" : "ar"); };
            theme.Checked=!state.ThemeSelected || state.Theme=="Dark"; theme.Anchor=AnchorStyles.Top|AnchorStyles.Right; theme.Margin=new Padding(0,12,0,0);
            themeTip.SetToolTip(theme,theme.AccessibleName);
            theme.CheckedChanged+=delegate { state.ThemeSelected=true; themeTip.SetToolTip(theme,theme.AccessibleName); ApplyTheme(); SaveState(); };
            branding.Controls.Add(logoView,0,0); branding.Controls.Add(wordmark,1,0); branding.Controls.Add(language,2,0); branding.Controls.Add(theme,3,0); Add(branding,32);
            var page=content;
            workspace=new TableLayoutPanel { AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Dock=DockStyle.Fill,Padding=new Padding(32),Margin=new Padding(0,0,0,16),Tag="surface",ColumnCount=1,RowCount=1 };
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); workspace.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content=new TableLayoutPanel { AutoSize=true,Dock=DockStyle.Fill,ColumnCount=1,RowCount=0,Margin=new Padding(0) }; content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); workspace.Controls.Add(content,0,0);
            taskContent=content;
            page.RowStyles.Add(new RowStyle(SizeType.AutoSize)); page.Controls.Add(workspace,0,page.RowCount++);
            Add(LabelFor("YouTube &link"),8);
            url.Font=new Font("Segoe UI",13f); url.AutoSize=false; url.AccessibleName=T("YouTube link"); url.TabIndex=0; url.RightToLeft=RightToLeft.No;
            url.Placeholder=T("Videos, Shorts, and shared links"); url.AccessibleDescription=url.Placeholder;
            ConfigureButton(paste,"&Paste"); paste.Width=96; paste.Height=48;
            paste.Click+=async delegate { try { if(Clipboard.ContainsText()) { url.Text=Clipboard.GetText().Trim(); url.Focus(); linkTimer.Stop(); await InspectAsync(); } else SetStatus("Clipboard is empty","Copy a YouTube video link, then choose Paste.",true); } catch { SetStatus("Could not paste","Copy the video link again, then choose Paste.",true); } };
            url.TextChanged+=delegate {
                linkTimer.Stop();
                if(!busy && !installingUpdate && checkedUrl!=url.Text.Trim()) {
                    checkedUrl=null; checkedInfo=null; checkedPlaylist=null; PrepareScope(); UpdateQualityChoices(); download.Enabled=false;
                    try { VideoLink.Normalize(url.Text); linkTimer.Start(); }
                    catch(ArgumentException) { if(!String.IsNullOrWhiteSpace(url.Text)) linkTimer.Start(); else if(statusMood==1) SetStatus("Ready for a video link","Paste a link to get started.",false); }
                }
            };
            linkTimer.Tick+=async delegate { linkTimer.Stop(); if(!busy && !installingUpdate) await InspectAsync(); };
            Add(Row(FieldFor(url),paste),28);
            playlistRow=new TableLayoutPanel { ColumnCount=3,RowCount=2,AutoSize=true,Margin=new Padding(0) };
            playlistRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50)); playlistRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,16)); playlistRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            playlistRow.RowStyles.Add(new RowStyle(SizeType.AutoSize)); playlistRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            oneVideo.Text=T("&This video"); wholePlaylist.Text=T("&Whole playlist"); oneVideo.Checked=true;
            foreach(var choice in new[]{oneVideo,wholePlaylist}) { choice.Height=48; choice.Dock=DockStyle.Fill; choice.Margin=new Padding(0); }
            playlistRow.Controls.Add(oneVideo,0,0); playlistRow.Controls.Add(wholePlaylist,2,0);
            playlistSummary.AutoSize=true; playlistSummary.Tag="muted"; playlistSummary.Margin=new Padding(0,10,0,0); playlistSummary.Dock=DockStyle.Fill;
            playlistRow.Controls.Add(playlistSummary,0,1); playlistRow.SetColumnSpan(playlistSummary,3); Add(playlistRow,28); playlistRow.Visible=false;
            EventHandler scopeChanged=async delegate(object sender,EventArgs e) {
                if(configuringScope || !((RadioButton)sender).Checked) return;
                linkTimer.Stop(); UpdateQualityChoices(); UpdateDownloadEnabled(); await InspectAsync();
            };
            oneVideo.CheckedChanged+=scopeChanged; wholePlaylist.CheckedChanged+=scopeChanged;
            var settings=new TableLayoutPanel { ColumnCount=5,RowCount=2,AutoSize=true,Dock=DockStyle.Fill,Margin=new Padding(0) };
            for(int column=0;column<5;column++) settings.ColumnStyles.Add(column%2==0 ? new ColumnStyle(SizeType.Percent,100f/3f) : new ColumnStyle(SizeType.Absolute,16));
            var formatLabel=LabelFor("Save &as"); settings.Controls.Add(formatLabel,0,0); settings.SetColumnSpan(formatLabel,3);
            video.Text=T("&Video · MP4"); video.AccessibleName=T("Video MP4"); video.Dock=DockStyle.Fill; video.Margin=new Padding(0);
            audio.Text=T("&Audio · MP3"); audio.AccessibleName=T("Audio MP3"); audio.Dock=DockStyle.Fill; audio.Margin=new Padding(0); settings.Controls.Add(video,0,1); settings.Controls.Add(audio,2,1);
            video.Height=audio.Height=48;
            audio.Checked=state.Format==1; video.Checked=!audio.Checked;
            quality.DropDownStyle=ComboBoxStyle.DropDownList; quality.AccessibleName="Available video quality"; UpdateQualityChoices();
            EventHandler formatChanged=delegate(object sender,EventArgs e) { if(((RadioButton)sender).Checked) { UpdateQualityChoices(); UpdateDownloadEnabled(); SaveState(); } }; video.CheckedChanged+=formatChanged; audio.CheckedChanged+=formatChanged;
            quality.SelectedIndexChanged+=delegate { var bitrate=quality.SelectedItem as AudioQuality; if(bitrate!=null) state.AudioBitrate=bitrate.Bitrate; SaveState(); };
            quality.Dock=DockStyle.Fill; quality.Margin=new Padding(0); settings.Controls.Add(quality,4,1); Add(settings,28);
            Add(LabelFor("Save &to"),8);
            folder.Text=InitialFolder(state.Folder,state.FolderVersion==0); state.FolderVersion=1;
            folder.Font=new Font("Segoe UI",12f); folder.AccessibleName=T("Save folder"); folder.RightToLeft=RightToLeft.No; folder.TextChanged+=delegate { SaveState(); };
            ConfigureButton(browse,"&Browse…"); browse.Width=112; browse.Height=48; browse.Click+=delegate { using(var dialog=new FolderBrowserDialog { Description=T("Choose where to save your YouTube downloads"),SelectedPath=Directory.Exists(folder.Text) ? folder.Text : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),ShowNewFolderButton=true }) { if(dialog.ShowDialog(this)==DialogResult.OK) folder.Text=dialog.SelectedPath; } };
            ConfigureButton(openFolder,"Open &folder"); openFolder.Width=144; openFolder.Height=48; openFolder.Click+=delegate { OpenDestination(); };
            Add(Row(FieldFor(folder),browse,openFolder),32);
            ConfigureButton(download,"&Download",true); download.Width=360; download.Height=56; download.Font=new Font(Font,FontStyle.Bold); download.Click+=async delegate { await DownloadAsync(); };
            ConfigureButton(recentButton,"Recent downloads"); recentButton.Width=184; recentButton.Height=56; recentButton.Margin=new Padding(16,0,0,0); recentButton.Click+=delegate { ShowRecentDownloads(); };
            var mainActions=new FlowLayoutPanel { AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Anchor=AnchorStyles.None,Margin=new Padding(0),WrapContents=false };
            mainActions.Controls.Add(download); mainActions.Controls.Add(recentButton);
            var centeredActions=new TableLayoutPanel { AutoSize=true,ColumnCount=1,RowCount=1,Margin=new Padding(0) };
            centeredActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); centeredActions.Controls.Add(mainActions,0,0); Add(centeredActions,12);
            ConfigureButton(cancel,"&Cancel"); cancel.Width=96; cancel.Height=48; cancel.Enabled=false; cancel.Visible=false; cancel.Click+=delegate { if(cancellation!=null) cancellation.Cancel(); cancel.Enabled=false; SetStatus("Cancelling","Stopping the download…",false); };
            ConfigureButton(openFile,"Open &file"); openFile.Width=104; openFile.Height=48; openFile.Quiet=true; openFile.Enabled=false; openFile.Visible=false; openFile.Click+=delegate { OpenCompletedFile(); };
            ConfigureButton(showInFolder,"Show &in folder"); showInFolder.Width=168; showInFolder.Height=48; showInFolder.Quiet=true; showInFolder.Enabled=false; showInFolder.Visible=false;
            showInFolder.Click+=delegate { ShowFileInFolder(lastFile,delegate(string heading,string message) { ShowContextNotice(heading,message,StatusTone.Error,followUpRow); }); };
            var followUpActions=new FlowLayoutPanel { AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Anchor=AnchorStyles.None,Margin=new Padding(0),WrapContents=false };
            openFile.Margin=new Padding(16,0,0,0); showInFolder.Margin=new Padding(16,0,0,0); followUpActions.Controls.Add(cancel); followUpActions.Controls.Add(openFile); followUpActions.Controls.Add(showInFolder);
            followUpRow=new TableLayoutPanel { AutoSize=true,ColumnCount=1,RowCount=1,Margin=new Padding(0) };
            followUpRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); followUpRow.Controls.Add(followUpActions,0,0); Add(followUpRow,12); followUpRow.Visible=false;
            statusHeading=tools.Ready ? "Ready for a video link" : "Downloader tools are missing"; statusMessage=tools.Ready ? "Paste a link to get started." : "Run SetupTools.cmd in the app folder, then reopen the app.";
            stage.Text=T(statusHeading); stage.AutoSize=true; stage.Font=new Font(Font,FontStyle.Bold); stage.Margin=new Padding(0,0,0,4);
            progressDetail.Text=T(statusMessage); progressDetail.AutoSize=true; progressDetail.Tag="muted"; progressDetail.MaximumSize=new Size(696,0); progressDetail.Margin=new Padding(0);
            statusRow=StatusRow(statusIcon,stage,progressDetail); Add(statusRow,10); statusRow.Visible=!tools.Ready;
            progress.Height=5; progress.Minimum=0; progress.Maximum=1000; progress.Visible=false; Add(progress,0);
            if(!tools.Ready) { statusMood=1; statusIcon.Tone=StatusTone.Error; }
            content=page;
            ConfigureButton(update,"Check for updates"); update.Width=200; update.Height=44; update.Quiet=true; update.Click+=async delegate { if(availableUpdate==null) await CheckUpdateAsync(true); else await InstallUpdateAsync(); };
            toolsStatus.Text=VersionText(); toolsStatus.AutoSize=true; toolsStatus.Tag="muted"; toolsStatus.Dock=DockStyle.Fill; toolsStatus.TextAlign=ContentAlignment.MiddleRight; toolsStatus.Margin=new Padding(12,0,0,0);
            updateRow=new TableLayoutPanel { AutoSize=true,ColumnCount=2,RowCount=1,Margin=new Padding(0) };
            updateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,200)); updateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            update.Anchor=AnchorStyles.Left; updateRow.Controls.Add(update,0,0); updateRow.Controls.Add(toolsStatus,1,0); Add(updateRow,0);
            var author=new Label { Text="By Ziad Hassanein",AutoSize=true,ForeColor=Muted,Font=new Font(Font.FontFamily,10f),Margin=new Padding(0) };
            var socials=new FlowLayoutPanel { AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Margin=new Padding(0),WrapContents=false };
            foreach(var entry in new[]{new[]{"GitHub","https://github.com/ZiadHassanein"},new[]{"LinkedIn","https://www.linkedin.com/in/ziad-hassanein/"},new[]{"Email","mailto:ziadmuhammed501@gmail.com"}}) {
                string target=entry[1]; var link=new LinkLabel { Text=entry[0],AutoSize=true,LinkColor=Accent,ActiveLinkColor=Accent,VisitedLinkColor=Accent,LinkBehavior=LinkBehavior.HoverUnderline,Margin=new Padding(12,0,0,0),Font=author.Font,Tag="social",AccessibleName=entry[0]+" · Ziad Hassanein" };
                link.LinkClicked+=delegate { try { Process.Start(new ProcessStartInfo(target) { UseShellExecute=true }); } catch { ShowContextNotice("Could not open link","Open the link in your browser and try again.",StatusTone.Error,socials); } }; socials.Controls.Add(link);
            }
            authorRow=Row(author,socials); Add(authorRow,0); authorRow.Margin=new Padding(0,16,0,0);
            updateHeading.AutoSize=true; updateHeading.Font=new Font(Font,FontStyle.Bold); updateHeading.Margin=new Padding(0,0,0,4);
            updateMessage.AutoSize=true; updateMessage.Tag="muted"; updateMessage.MaximumSize=new Size(696,0); updateMessage.Margin=new Padding(0);
            ConfigureButton(noticeDismiss,"Dismiss"); noticeDismiss.Quiet=true; noticeDismiss.Width=80; noticeDismiss.Height=32; noticeDismiss.AccessibleName="Dismiss update message"; noticeDismiss.Click+=delegate { HideUpdateNotice(); };
            updateNotice=new NoticePopover(updateIcon,updateHeading,updateMessage,noticeDismiss,Font);
            Controls.Add(updateNotice);
            download.Enabled=false; AcceptButton=download;
            quality.FlatStyle=FlatStyle.Flat; quality.DrawMode=DrawMode.OwnerDrawFixed; quality.ItemHeight=38; quality.DrawItem+=DrawChoice;
            bool aligningQualityHeight=false, alignmentQueued=false;
            Action alignQualityHeight=delegate {
                if(aligningQualityHeight || quality.IsDisposed || !quality.IsHandleCreated) return;
                aligningQualityHeight=true;
                try { int frame=quality.Height-quality.ItemHeight; if(quality.Height!=video.Height) quality.ItemHeight=Math.Max(1,video.Height-frame); }
                finally { aligningQualityHeight=false; }
            };
            EventHandler queueQualityAlignment=delegate {
                if(alignmentQueued || quality.IsDisposed || !quality.IsHandleCreated) return;
                alignmentQueued=true;
                quality.BeginInvoke(new Action(delegate { alignmentQueued=false; alignQualityHeight(); }));
            };
            video.SizeChanged+=queueQualityAlignment; quality.HandleCreated+=queueQualityAlignment;
            foreach(var box in new[]{url,folder}) box.BorderStyle=BorderStyle.None;
            foreach(var dynamicRow in new Control[]{playlistRow,followUpRow,statusRow,progress}) dynamicRow.VisibleChanged+=delegate { QueueFitHeight(); };
            playlistSummary.TextChanged+=delegate { QueueFitHeight(); }; progressDetail.TextChanged+=delegate { QueueFitHeight(); };
            Resize+=delegate { int available=Math.Max(240,scroll.ClientSize.Width-scroll.Padding.Horizontal-workspace.Padding.Horizontal); progressDetail.MaximumSize=new Size(Math.Max(240,available-44),0); playlistSummary.MaximumSize=new Size(available,0); QueueFitHeight(); };
        }
        private void QueueFitHeight() {
            if(fitQueued || fittingHeight || !IsHandleCreated || IsDisposed || Disposing) return;
            fitQueued=true; BeginInvoke(new Action(delegate { fitQueued=false; if(!IsDisposed && !Disposing) FitContentHeight(); }));
        }
        private void FitContentHeight() {
            if(fittingHeight || !IsHandleCreated) return;
            fittingHeight=true;
            try {
                // Reserve the entire task height and both insets before sizing the outer rows.
                // This also settles visibility changes when the form itself cannot grow further.
                taskContent.PerformLayout();
                int width=Math.Max(1,workspace.ClientSize.Width-workspace.Padding.Horizontal);
                int required=taskContent.GetPreferredSize(new Size(width,0)).Height+workspace.Padding.Vertical;
                if(workspace.MinimumSize.Height!=required) workspace.MinimumSize=new Size(0,required);
                workspace.PerformLayout(); content.PerformLayout(); scroll.PerformLayout();
                if(WindowState!=FormWindowState.Normal) return;
                int chrome=Height-ClientSize.Height;
                int maximum=Math.Max(360,Screen.FromControl(this).WorkingArea.Height-chrome-32);
                int desired=Math.Max(360,Math.Min(maximum,scroll.ContentHeight+scroll.Padding.Vertical));
                if(ClientSize.Height!=desired) ClientSize=new Size(ClientSize.Width,desired);
                if(updateNotice.Visible && noticeAnchor!=null) updateNotice.ShowNear(this,noticeAnchor);
            } finally { fittingHeight=false; }
        }
        private TableLayoutPanel StatusRow(StatusIcon icon,Label heading,Label message) {
            var copy=new TableLayoutPanel { AutoSize=true,Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=new Padding(0) }; copy.Controls.Add(heading,0,0); copy.Controls.Add(message,0,1);
            var row=new TableLayoutPanel { AutoSize=true,Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=new Padding(0) }; row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,44)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            icon.Margin=new Padding(12,1,0,0); row.Controls.Add(icon,0,0); row.Controls.Add(copy,1,0); return row;
        }
        private string VersionText() { return "v"+AppUpdates.Current.ToString(3); }
        private void SetLanguage(string value) {
            if(busy || installingUpdate) return;
            state.Language=value=="ar" ? "ar" : "en"; ApplyLanguage(); ApplyTitleBarTheme(); RepaintInterface(); SaveState();
        }
        private void CaptureLayout(Control parent) {
            if(parent==branding || parent==updateRow || parent==authorRow) return;
            var table=parent as TableLayoutPanel;
            if(table!=null && table.ColumnCount>1) tableStates.Add(new TableState { Table=table,Columns=table.ColumnStyles.Cast<ColumnStyle>().Select(c=>new ColumnStyle(c.SizeType,c.Width)).ToArray() });
            foreach(Control child in parent.Controls) {
                layoutStates.Add(new LayoutState { Control=child,Margin=child.Margin,Anchor=child.Anchor,Column=table==null ? -1 : table.GetColumn(child) });
                CaptureLayout(child);
            }
        }
        private void ApplyDirection() {
            scroll.IsArabic=IsArabic;
            update.IsArabic=IsArabic;
            int updateColumn=IsArabic ? 1 : 0,versionColumn=IsArabic ? 0 : 1;
            updateRow.ColumnStyles[updateColumn].SizeType=SizeType.Absolute; updateRow.ColumnStyles[updateColumn].Width=update.Width;
            updateRow.ColumnStyles[versionColumn].SizeType=SizeType.Percent; updateRow.ColumnStyles[versionColumn].Width=100;
            updateRow.SetColumn(update,updateColumn); updateRow.SetColumn(toolsStatus,versionColumn);
            update.Anchor=IsArabic ? AnchorStyles.Right : AnchorStyles.Left;
            toolsStatus.TextAlign=IsArabic ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleRight;
            int versionGap=Math.Max(toolsStatus.Margin.Left,toolsStatus.Margin.Right);
            toolsStatus.Margin=IsArabic ? new Padding(0,0,versionGap,0) : new Padding(versionGap,0,0,0);
            foreach(var saved in tableStates) {
                for(int i=0;i<saved.Columns.Length;i++) {
                    var original=saved.Columns[IsArabic ? saved.Columns.Length-1-i : i];
                    saved.Table.ColumnStyles[i].SizeType=original.SizeType; saved.Table.ColumnStyles[i].Width=original.Width;
                }
            }
            foreach(var saved in layoutStates) {
                var control=saved.Control; var margin=saved.Margin;
                control.Margin=IsArabic ? new Padding(margin.Right,margin.Top,margin.Left,margin.Bottom) : margin;
                var anchor=saved.Anchor;
                if(IsArabic) { anchor &= ~(AnchorStyles.Left|AnchorStyles.Right); if((saved.Anchor & AnchorStyles.Left)!=0) anchor|=AnchorStyles.Right; if((saved.Anchor & AnchorStyles.Right)!=0) anchor|=AnchorStyles.Left; }
                // Setting Anchor clears Dock in WinForms; only mirror undocked controls.
                if(control.Dock==DockStyle.None) control.Anchor=anchor;
                var parent=control.Parent as TableLayoutPanel;
                if(parent!=null && parent.ColumnCount>1 && saved.Column>=0) parent.SetColumn(control,IsArabic ? parent.ColumnCount-saved.Column-parent.GetColumnSpan(control) : saved.Column);
                var flow=control as FlowLayoutPanel; if(flow!=null) flow.FlowDirection=IsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                var label=control as Label; if(label!=null) label.TextAlign=IsArabic ? ContentAlignment.TopRight : ContentAlignment.TopLeft;
                var button=control as ThemeButton; if(button!=null) button.IsArabic=IsArabic;
                var choice=control as FormatChoice; if(choice!=null) choice.IsArabic=IsArabic;
                var combo=control as ThemeCombo; if(combo!=null) combo.IsArabic=IsArabic;
                var meter=control as ThemeProgress; if(meter!=null) meter.IsArabic=IsArabic;
            }
        }
        private void ApplyLanguage() {
            var controls=new List<Control>(); CollectControls(this,controls);
            scroll.BeginContentUpdate(); foreach(var control in controls) control.SuspendLayout();
            try {
                // Mirror managed content while keeping native direction styles and HWNDs stable.
                ApplyDirection(); Text=T("YouTube Downloader");
                foreach(var control in controls) {
                    var label=control as LocalizedLabel; if(label!=null) label.Text=T(label.TextKey);
                    var button=control as ThemeButton; if(button!=null && button!=language && button.TextKey!=null) SetButtonText(button,button.TextKey);
                    var bar=control as ThemeScrollBar; if(bar!=null) { bar.AccessibleName=T("Scroll page"); bar.AccessibleDescription=T("Use the arrow keys, Page Up, Page Down, Home, or End to scroll."); }
                    var icon=control as StatusIcon; if(icon!=null) { icon.Language=state.Language; icon.Tone=icon.Tone; }
                }
                language.Text=IsArabic ? "English" : "العربية"; language.AccessibleName=T(IsArabic ? "Switch to English" : "Switch to Arabic");
                themeTip.SetToolTip(language,language.AccessibleName); logoView.AccessibleName=T("YouTube Downloader logo");
                theme.Language=state.Language; theme.UpdateActionName(); themeTip.SetToolTip(theme,theme.AccessibleName);
                url.Placeholder=T("Videos, Shorts, and playlists"); url.PlaceholderRightToLeft=IsArabic; url.AccessibleName=T("YouTube link"); url.AccessibleDescription=url.Placeholder; url.Invalidate();
                folder.AccessibleName=T("Save folder"); video.Text=T("&Video · MP4"); video.AccessibleName=T("Video MP4"); audio.Text=T("&Audio · MP3"); audio.AccessibleName=T("Audio MP3");
                oneVideo.Text=T("&This video"); oneVideo.AccessibleName=T("This video"); wholePlaylist.Text=T("&Whole playlist"); wholePlaylist.AccessibleName=T("Whole playlist"); UpdatePlaylistSummary();
                var selected=quality.SelectedItem as VideoQuality; var selectedPlaylist=quality.SelectedItem as PlaylistQuality; UpdateQualityChoices();
                if(selectedPlaylist!=null && IsPlaylist && !IsAudio) { foreach(var option in quality.Items.Cast<object>().OfType<PlaylistQuality>()) if(option.Height==selectedPlaylist.Height) quality.SelectedItem=option; }
                if(selected!=null && checkedInfo!=null && !IsAudio) { int index=checkedInfo.Qualities.FindIndex(q=>q.Resolution==selected.Resolution && q.Selector==selected.Selector); if(index>=0) quality.SelectedIndex=index; }
                stage.Text=T(statusHeading); progressDetail.Text=statusIsProgress && latestProgress!=null ? ProgressText(latestProgress) : F(statusMessage,statusArgs)+(statusSuffix==null ? "" : "\n"+T(statusSuffix));
                toolsStatus.Text=VersionText(); toolsStatus.AccessibleName=T("App version"); updateHeading.Text=T(updateHeadingKey); updateMessage.Text=F(updateMessageKey,updateMessageArgs);
                updateNotice.AccessibleName=T("App update status"); noticeDismiss.AccessibleName=T("Dismiss update message");
                updateNotice.UpdateLook(colors,IsArabic);
            } finally { for(int i=controls.Count-1;i>=0;i--) controls[i].ResumeLayout(true); scroll.EndContentUpdate(); if(updateNotice.Visible && noticeAnchor!=null) updateNotice.ShowNear(this,noticeAnchor); }
        }
        private async Task InspectAsync() {
            if(busy || installingUpdate) return; linkTimer.Stop();
            string link; try { link=VideoLink.Normalize(url.Text,IsPlaylist); } catch(Exception ex) { SetStatus("Check the link",ex.Message,true); url.Focus(); return; }
            Control focusBeforeCheck=ActiveControl; SetBusy(true); cancellation=new CancellationTokenSource();
            try {
                if(IsPlaylist) {
                    if(checkedPlaylist==null) checkedPlaylist=await Task.Run(()=>engine.InspectPlaylist(link,cancellation.Token));
                    checkedUrl=url.Text.Trim(); UpdatePlaylistSummary(); UpdateQualityChoices(); SetStatus("Ready for a video link","Paste a link to get started.",false);
                } else {
                    VideoInfo info=checkedInfo ?? await Task.Run(()=>engine.Inspect(link,cancellation.Token));
                    if(info.Live) throw new InvalidOperationException("This video is live right now. Try again once the stream has ended.");
                    if(info.Qualities.Count==0 && !info.HasAudio) throw new InvalidOperationException("YouTube did not return downloadable formats for this video.");
                    checkedUrl=url.Text.Trim(); checkedInfo=info; UpdateQualityChoices();
                    SetStatus("Ready for a video link","Paste a link to get started.",false);
                }
                progress.Style=ProgressBarStyle.Continuous; progress.Value=0;
            } catch(OperationCanceledException) { SetStatus("Check cancelled","You can paste another link or try again.",false); }
            catch(Exception ex) { if(IsPlaylist) checkedPlaylist=null; else checkedInfo=null; checkedUrl=null; UpdatePlaylistSummary(); UpdateQualityChoices(); SetStatus(IsPlaylist ? "Could not check playlist" : "Could not check video",FriendlyError(ex.Message),true); }
            finally { SetBusy(false); cancellation.Dispose(); cancellation=null; if(focusBeforeCheck!=null && !focusBeforeCheck.IsDisposed && focusBeforeCheck.Enabled) focusBeforeCheck.Focus(); }
        }
        private async Task DownloadAsync() {
            if(busy || installingUpdate) return;
            if((IsPlaylist ? checkedPlaylist==null : checkedInfo==null) || checkedUrl!=url.Text.Trim()) { await InspectAsync(); if(IsPlaylist ? checkedPlaylist==null : checkedInfo==null) return; }
            DownloadOptions options;
            try {
                var chosen=quality.SelectedItem as VideoQuality;
                var cap=quality.SelectedItem as PlaylistQuality;
                if(!IsAudio && chosen==null && cap==null) throw new ArgumentException("Choose an available video quality first.");
                if(IsAudio && !IsPlaylist && !checkedInfo.HasAudio) throw new ArgumentException("An audio download is not available for this video.");
                options=new DownloadOptions { Url=VideoLink.Normalize(url.Text,IsPlaylist),Folder=DownloadFolder(),Audio=IsAudio,AudioBitrate=state.AudioBitrate,Height=cap!=null ? cap.Height : chosen==null ? 0 : chosen.Resolution,FormatSelector=chosen==null ? null : chosen.Selector,Playlist=IsPlaylist ? checkedPlaylist : null }; Arguments.ForDownload(tools,options);
            }
            catch(Exception ex) { ShowContextNotice("Check your download",ex.Message,StatusTone.Error,folder.Parent); return; }
            SetBusy(true); lastFile=null; openFile.Enabled=showInFolder.Enabled=false; openFile.Visible=showInFolder.Visible=false; cancellation=new CancellationTokenSource(); SaveState();
            try {
                DownloadResult result=await Task.Run(()=>engine.Download(options,cancellation.Token));
                if(options.Playlist!=null) { CompletePlaylist(result); }
                else if(result.Cancelled) { SetStatus("Download cancelled","Partial files are kept so the same download can resume later.",false); progress.Style=ProgressBarStyle.Continuous; progress.Value=0; }
                else { CompleteDownload(result.FilePath); }
            } catch(OperationCanceledException) { SetStatus("Download cancelled","You can start another download.",false); }
            catch(Exception ex) { SetStatus("Download failed",FriendlyError(ex.Message),true); }
            finally { SetBusy(false); cancellation.Dispose(); cancellation=null; }
        }
        private void CompleteDownload(string file) {
            lastFile=file; openFile.Enabled=showInFolder.Enabled=true; followUpRow.Visible=true; openFile.Visible=showInFolder.Visible=true;
            ClearLink();
            SetStatus("Download complete",Path.GetFileName(lastFile),false); statusMood=2; statusIcon.Tone=StatusTone.Success; stage.ForeColor=Accent;
            progress.Style=ProgressBarStyle.Continuous; progress.Value=1000; progress.Visible=true; AddRecent(lastFile);
        }
        private bool HasVideoLink() { try { return VideoLink.Parse(url.Text).VideoUrl!=null; } catch(ArgumentException) { return false; } }
        private void PrepareScope() {
            if(playlistRow==null) return;
            YouTubeLink link=null; try { link=VideoLink.Parse(url.Text); } catch(ArgumentException) { }
            configuringScope=true;
            try { bool hasPlaylist=link!=null && link.PlaylistUrl!=null; playlistRow.Visible=hasPlaylist; wholePlaylist.Checked=hasPlaylist && link.VideoUrl==null; oneVideo.Checked=!wholePlaylist.Checked; oneVideo.Enabled=link!=null && link.VideoUrl!=null; }
            finally { configuringScope=false; }
            UpdatePlaylistSummary();
        }
        private void UpdatePlaylistSummary() {
            if(playlistRow==null) return;
            playlistSummary.Text=IsPlaylist && checkedPlaylist!=null ? F("{0} · {1} videos\nDownloads one at a time. Quality is a maximum; lower resolutions are used when needed.",checkedPlaylist.Title,checkedPlaylist.Entries.Count) : T("Choose this video or the whole playlist.");
            playlistSummary.AccessibleName=playlistSummary.Text;
        }
        private void ClearLink() {
            checkedUrl=null; checkedInfo=null; checkedPlaylist=null; linkTimer.Stop(); url.Clear(); PrepareScope(); UpdateQualityChoices();
        }
        private void CompletePlaylist(DownloadResult result) {
            var history=state.Recent ?? new List<string>();
            foreach(string file in result.Files) { history.RemoveAll(x=>String.Equals(x,file,StringComparison.OrdinalIgnoreCase)); history.Insert(0,file); if(history.Count>5) history.RemoveAt(5); }
            state.Recent=history; SaveState();
            lastFile=result.FilePath; openFile.Enabled=showInFolder.Enabled=lastFile!=null; openFile.Visible=showInFolder.Visible=lastFile!=null; followUpRow.Visible=lastFile!=null;
            bool finished=!result.Cancelled && result.Failed==0 && result.Remaining==0 && result.StopReason==null;
            if(finished) ClearLink();
            string heading=finished ? "Playlist complete" : result.Cancelled ? "Playlist cancelled" : result.Remaining>0 ? "Playlist paused" : "Playlist finished with errors";
            string detail="{0} saved · {1} already downloaded · {2} failed · {3} remaining";
            SetStatus(heading,detail,!finished && !result.Cancelled); statusArgs=new object[]{result.Completed-result.Skipped,result.Skipped,result.Failed,result.Remaining};
            progressDetail.Text=F(detail,statusArgs);
            if(!finished) { statusSuffix=result.StopReason!=null ? FriendlyError(result.StopReason) : "Run the same playlist again to resume. Completed files are kept."; progressDetail.Text+="\n"+T(statusSuffix); }
            statusMood=finished ? 2 : result.Cancelled ? 0 : 1; statusIcon.Tone=finished ? StatusTone.Success : result.Cancelled ? StatusTone.Info : StatusTone.Error;
            progress.Style=ProgressBarStyle.Continuous; progress.Value=finished ? 1000 : (int)(1000.0*(result.Completed+result.Failed)/Math.Max(1,result.Total)); progress.Visible=true;
        }
        private void SetBusy(bool value) {
            busy=value; bool locked=value || installingUpdate; url.Enabled=paste.Enabled=folder.Enabled=browse.Enabled=language.Enabled=!locked; recentButton.Enabled=!installingUpdate;
            video.Enabled=audio.Enabled=!locked && tools.Ready; update.Enabled=!locked && !checkingUpdate;
            wholePlaylist.Enabled=!locked; oneVideo.Enabled=!locked && HasVideoLink();
            quality.Enabled=!locked && (IsAudio ? tools.Ready : IsPlaylist ? checkedPlaylist!=null : checkedInfo!=null && checkedInfo.Qualities.Count>0); UpdateDownloadEnabled(); cancel.Enabled=value && !installingUpdate; followUpRow.Visible=(value && !installingUpdate) || openFile.Enabled; cancel.Visible=value && !installingUpdate; Cursor=locked ? Cursors.AppStarting : Cursors.Default;
            if(!value && progress.Style==ProgressBarStyle.Marquee) { progress.Style=ProgressBarStyle.Continuous; progress.Value=0; }
        }
        private void UpdateDownloadEnabled() { download.Enabled=!busy && !installingUpdate && tools.Ready && (IsPlaylist ? checkedPlaylist!=null : checkedInfo!=null && (IsAudio ? checkedInfo.HasAudio : checkedInfo.Qualities.Count>0)); }
        private void UpdateQualityChoices() {
            quality.Items.Clear();
            if(IsAudio) { quality.AccessibleName=T("MP3 encoding bitrate"); int savedBitrate=state.AudioBitrate==192 ? 192 : 320; quality.Items.Add(new AudioQuality { Bitrate=192 }); quality.Items.Add(new AudioQuality { Bitrate=320 }); quality.SelectedIndex=savedBitrate==192 ? 0 : 1; quality.Enabled=!busy && !installingUpdate && tools.Ready; }
            else {
                quality.AccessibleName=T("Available video quality");
                if(IsPlaylist && checkedPlaylist!=null) {
                    quality.AccessibleName=T("Playlist maximum video quality");
                    foreach(int height in new[]{0,2160,1440,1080,720,480,360}) quality.Items.Add(new PlaylistQuality { Height=height,Text=height==0 ? T("Best available") : F("Up to {0}p",height) });
                    quality.SelectedIndex=3; quality.Enabled=!busy && !installingUpdate;
                }
                else if(IsPlaylist || checkedInfo==null || checkedInfo.Qualities.Count==0) { quality.Items.Add(T("Quality")); quality.SelectedIndex=0; quality.Enabled=false; }
                else { foreach(var q in checkedInfo.Qualities) quality.Items.Add(q); quality.SelectedIndex=AvailableFormats.RecommendedIndex(checkedInfo.Qualities); quality.Enabled=!busy && !installingUpdate; }
            }
        }
        private string ProgressText(DownloadProgress p) { return (p.Total>0 ? F("Video {0} of {1}",p.Item,p.Total)+" · "+p.ItemTitle+"\n" : "")+Texts.Progress(state.Language,p.Detail); }
        private void ApplyProgress(DownloadProgress p) { latestProgress=p; statusRow.Visible=true; statusHeading=p.Stage; statusMessage=p.Detail; statusArgs=null; statusIsProgress=true; statusMood=0; statusIcon.Tone=StatusTone.Info; stage.Text=T(p.Stage); stage.ForeColor=Ink; progressDetail.Text=ProgressText(p); progressDetail.ForeColor=Muted; progress.Visible=true; progress.Style=p.Indeterminate ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous; if(!p.Indeterminate) progress.Value=(int)Math.Max(0,Math.Min(1000,p.Percent*10)); }
        private void SetStatus(string heading,string message,bool error) { statusArgs=null; statusSuffix=null; statusRow.Visible=error || heading!="Ready for a video link"; statusHeading=heading; statusMessage=message; statusIsProgress=false; statusMood=error ? 1 : 0; statusIcon.Tone=error ? StatusTone.Error : StatusTone.Info; stage.Text=T(heading); stage.ForeColor=error ? colors.Error : Ink; progressDetail.Text=T(message); progressDetail.ForeColor=Muted; progress.Visible=false; if(error) { progress.Style=ProgressBarStyle.Continuous; progress.Value=0; } }
        private static string InitialFolder(string saved,bool migrateLegacy) {
            string downloads=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads");
            if(String.IsNullOrWhiteSpace(saved)) return downloads;
            if(migrateLegacy) {
                string previous=saved.Trim().TrimEnd('\\','/');
                foreach(string parent in new[]{downloads,Path.Combine(Path.GetPathRoot(downloads),"Downloads")})
                    if(String.Equals(previous,Path.Combine(parent,"YouTube"),StringComparison.OrdinalIgnoreCase)) return parent;
            }
            return saved;
        }
        private string DownloadFolder() { if(String.IsNullOrWhiteSpace(folder.Text)) throw new ArgumentException("Choose a save folder with Browse."); try { return Path.GetFullPath(folder.Text.Trim()); } catch { throw new ArgumentException("Choose a valid folder on this computer with Browse."); } }
        private void SafeUi(Action action) { if(IsDisposed || !IsHandleCreated) return; try { BeginInvoke(action); } catch(InvalidOperationException) { } }
        private static string FriendlyError(string message) {
            if(message=="This playlist is empty or unavailable. Try a public playlist." || message=="The playlist could not be read. Check the link and try again." || message=="Completed file saved, but resume information could not be saved.") return message;
            message=(message??"").ToLowerInvariant();
            if(message.Contains("429") || message.Contains("too many requests")) return "YouTube is limiting downloads right now. Wait a few minutes, then try again.";
            if(message.Contains("sign in") || message.Contains("private") || message.Contains("age-restricted")) return "This video requires sign-in. Try a publicly available video.";
            if(message.Contains("403")) return "YouTube refused this download. Wait a little, then paste the link again.";
            if(message.Contains("unavailable") || message.Contains("removed") || message.Contains("not available in your country")) return "This video is unavailable. Try another video link.";
            if(message.Contains("requested format") || message.Contains("no downloadable formats")) return "That quality is no longer available. Paste the link again to refresh the options.";
            if(DownloadErrors.IsDiskFull(message)) return "The drive is full. Free some space or choose another save folder.";
            if(message.Contains("permission") || message.Contains("access is denied")) return "We can't save in that folder. Choose another folder with Browse.";
            if(message.Contains("timed out") || message.Contains("network") || message.Contains("resolve") || message.Contains("connection")) return "Couldn't reach YouTube. Check your internet connection, then try again.";
            if(message.Contains("missing tools")) return "Run SetupTools.cmd in the app folder to install the missing tools, then reopen the app.";
            return "We couldn't finish this action. Check the link and save folder, then try again.";
        }
        private void OpenDestination() { try { string destination=Path.GetFullPath(folder.Text.Trim()); if(!Directory.Exists(destination)) { ShowContextNotice("Folder is not available","Choose an existing folder or download a video first.",StatusTone.Error,folder.Parent); return; } Process.Start(new ProcessStartInfo(destination) { UseShellExecute=true }); } catch { ShowContextNotice("Could not open folder","Choose a valid folder on this computer with Browse.",StatusTone.Error,folder.Parent); } }
        private void OpenCompletedFile() { try { if(lastFile==null || !File.Exists(lastFile)) { SetStatus("File is not available","The file may have been moved or deleted.",true); return; } Process.Start(new ProcessStartInfo(lastFile) { UseShellExecute=true }); } catch { SetStatus("Could not open file","Open the save folder and try opening the file there.",true); } }
        private void ShowFileInFolder(string file,Action<string,string> reportError) {
            if(String.IsNullOrWhiteSpace(file) || !File.Exists(file)) { reportError("File is not available","The file may have been moved or deleted."); return; }
            IntPtr item=IntPtr.Zero;
            try {
                uint attributes; Marshal.ThrowExceptionForHR(SHParseDisplayName(Path.GetFullPath(file),IntPtr.Zero,out item,0,out attributes));
                Marshal.ThrowExceptionForHR(SHOpenFolderAndSelectItems(item,0,IntPtr.Zero,0));
            } catch { reportError("Could not open folder","Open the save folder and try opening the file there."); }
            finally { if(item!=IntPtr.Zero) Marshal.FreeCoTaskMem(item); }
        }
        private void AddRecent(string file) { if(state.Recent==null) state.Recent=new List<string>(); state.Recent.RemoveAll(x=>String.Equals(x,file,StringComparison.OrdinalIgnoreCase)); state.Recent.Insert(0,file); state.Recent=state.Recent.Take(5).ToList(); SaveState(); }
        private void ShowRecentDownloads() {
            using(var dialog=CreateRecentDialog()) dialog.ShowDialog(this);
        }
        private Form CreateRecentDialog() {
            var dialog=new Form { Text=T("Recent downloads"),StartPosition=FormStartPosition.CenterParent,ClientSize=new Size(560,320),MinimumSize=new Size(480,300),MaximizeBox=false,MinimizeBox=false,ShowInTaskbar=false,Font=Font,BackColor=colors.Page,ForeColor=Ink,Padding=new Padding(20),Icon=Icon };
            var layout=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Margin=new Padding(0) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent,100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(new Label { Text=T("Recent downloads"),Font=new Font(Font,FontStyle.Bold),AutoSize=true,Dock=DockStyle.Fill,TextAlign=IsArabic ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft,Margin=new Padding(0,0,0,14) },0,0);
            var paths=(state.Recent??new List<string>()).Take(5).ToList();
            var reveal=new ThemeButton { Text=T("Show &in folder"),TextKey="Show &in folder",AccessibleName=T("Show &in folder").Replace("&",""),Colors=colors,IsArabic=IsArabic,Size=new Size(168,44),Enabled=false,Margin=new Padding(0) };
            var message=new Label { AutoSize=true,Dock=DockStyle.Fill,ForeColor=colors.Error,TextAlign=IsArabic ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft,Margin=new Padding(0,12,0,0),Visible=false,AccessibleRole=AccessibleRole.Alert };
            var messageTimer=new System.Windows.Forms.Timer { Interval=5000 }; messageTimer.Tick+=delegate { messageTimer.Stop(); message.Visible=false; };
            dialog.Disposed+=delegate { messageTimer.Dispose(); }; layout.Controls.Add(message,0,2);
            if(paths.Count==0) layout.Controls.Add(new Label { Text=T("Your completed downloads will appear here."),Dock=DockStyle.Fill,ForeColor=Muted,TextAlign=ContentAlignment.MiddleCenter,Margin=new Padding(0) },0,1);
            else {
                var list=new ListBox { Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,IntegralHeight=false,BackColor=colors.Page,ForeColor=Ink,DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=Font.Height+10,AccessibleName=T("Recent downloads"),Margin=new Padding(0) };
                foreach(string path in paths) list.Items.Add(Path.GetFileName(path));
                list.DrawItem+=delegate(object sender,DrawItemEventArgs e) {
                    if(e.Index<0) return; bool selected=(e.State & DrawItemState.Selected)!=0;
                    using(var brush=new SolidBrush(selected ? colors.Selected : colors.Page)) e.Graphics.FillRectangle(brush,e.Bounds);
                    TextRenderer.DrawText(e.Graphics,list.Items[e.Index].ToString(),list.Font,e.Bounds,Ink,(IsArabic ? TextFormatFlags.Right|TextFormatFlags.RightToLeft : TextFormatFlags.Left)|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix|TextFormatFlags.EndEllipsis);
                    if((e.State & DrawItemState.Focus)!=0) e.DrawFocusRectangle();
                };
                Action open=delegate { if(list.SelectedIndex>=0) { lastFile=paths[list.SelectedIndex]; dialog.Close(); OpenCompletedFile(); } };
                list.DoubleClick+=delegate { open(); }; list.KeyDown+=delegate(object sender,KeyEventArgs e) { if(e.KeyCode==Keys.Enter) { e.Handled=true; e.SuppressKeyPress=true; open(); } };
                list.SelectedIndexChanged+=delegate { reveal.Enabled=list.SelectedIndex>=0; messageTimer.Stop(); message.Visible=false; };
                reveal.Click+=delegate { if(list.SelectedIndex>=0) ShowFileInFolder(paths[list.SelectedIndex],delegate(string heading,string detail) { message.Text=T(heading)+" · "+T(detail); message.AccessibleName=message.Text; message.Visible=true; messageTimer.Stop(); messageTimer.Start(); }); };
                layout.Controls.Add(list,0,1);
                list.SelectedIndex=0;
            }
            var close=new ThemeButton { Text=T("Close"),TextKey="Close",AccessibleName=T("Close"),Colors=colors,IsArabic=IsArabic,Size=new Size(112,44),Margin=IsArabic ? new Padding(0,0,12,0) : new Padding(12,0,0,0),DialogResult=DialogResult.Cancel };
            var actions=new FlowLayoutPanel { AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Anchor=AnchorStyles.None,Margin=new Padding(0,16,0,0),WrapContents=false,FlowDirection=IsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            actions.Controls.Add(reveal); actions.Controls.Add(close); layout.Controls.Add(actions,0,3); dialog.CancelButton=close; dialog.Controls.Add(layout);
            dialog.Shown+=delegate { try { int dark=theme.Checked ? 1 : 0; DwmSetWindowAttribute(dialog.Handle,20,ref dark,4); } catch(DllNotFoundException) { } catch(EntryPointNotFoundException) { } };
            return dialog;
        }
        private SavedState LoadState() { try { if(File.Exists(statePath)) return new JavaScriptSerializer().Deserialize<SavedState>(File.ReadAllText(statePath)) ?? new SavedState(); } catch { } return new SavedState(); }
        private void SaveState() { if(initializing || state==null) return; try { state.Folder=folder.Text; state.Theme=theme.Checked ? "Dark" : "Light"; state.Format=IsAudio ? 1 : 0; state.Quality=quality.SelectedIndex; File.WriteAllText(statePath,new JavaScriptSerializer().Serialize(state)); } catch { toolsStatus.Text=T("Settings could not be saved"); } }
        private void ApplyTheme() {
            var controls=new List<Control>(); CollectControls(this,controls);
            foreach(var control in controls) control.SuspendLayout();
            try {
                colors=Palette.For(theme.Checked); ApplyColors(this,colors.Page);
                stage.ForeColor=statusMood==1 ? colors.Error : (statusMood==2 ? Accent : Ink); progressDetail.ForeColor=Muted;
                updateNotice.UpdateLook(colors,IsArabic);
                ApplyTitleBarTheme();
            } finally {
                for(int i=controls.Count-1;i>=0;i--) controls[i].ResumeLayout(false);
                RepaintInterface();
            }
        }
        private void RepaintInterface() {
            // Repaint without WM_SETREDRAW, which changes native control visibility.
            Invalidate(true);
            if(IsHandleCreated && Visible) RedrawWindow(Handle,IntPtr.Zero,IntPtr.Zero,0x0585);
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
            var link=item as LinkLabel; if(link!=null) link.LinkColor=link.ActiveLinkColor=link.VisitedLinkColor=Accent;
            foreach(Control child in item.Controls) ApplyColors(child,back);
        }
        private void DrawChoice(object sender,DrawItemEventArgs e) {
            if(e.Index<0) return; var box=(ComboBox)sender; bool selected=(e.State & DrawItemState.Selected)!=0, face=(e.State & DrawItemState.ComboBoxEdit)!=0;
            using(var brush=new SolidBrush(colors.Field)) e.Graphics.FillRectangle(brush,e.Bounds);
            var row=new Rectangle(e.Bounds.X+4,e.Bounds.Y+3,Math.Max(1,e.Bounds.Width-8),Math.Max(1,e.Bounds.Height-6));
            e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            if(selected && !face) using(var path=Drawing.Rounded(row,6)) using(var brush=new SolidBrush(colors.Selected)) e.Graphics.FillPath(brush,path);
            Color ink=!box.Enabled ? Muted : (selected && !face && SystemInformation.HighContrast ? colors.OnAccent : Ink);
            string text=box.Items[e.Index].ToString();
            TextRenderer.DrawText(e.Graphics,text,box.Font,new Rectangle(e.Bounds.X+(face ? 14 : (IsArabic ? 14 : 36)),e.Bounds.Y,Math.Max(1,e.Bounds.Width-(face ? 28 : 50)),e.Bounds.Height),ink,ThemeCombo.TextFlags(IsArabic,text));
            if(!face && e.Index==box.SelectedIndex) {
                int x=IsArabic ? e.Bounds.Right-20 : e.Bounds.Left+20,y=e.Bounds.Top+e.Bounds.Height/2;
                using(var pen=new Pen(selected && SystemInformation.HighContrast ? colors.OnAccent : Accent,1.8f)) { pen.StartCap=pen.EndCap=System.Drawing.Drawing2D.LineCap.Round; e.Graphics.DrawLines(pen,new[]{new Point(x-5,y),new Point(x-1,y+4),new Point(x+6,y-4)}); }
            }
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
        private void ShowUpdateNotice(string heading,string message,StatusTone tone,params object[] args) { ShowContextNotice(heading,message,tone,update,args); }
        private void ShowContextNotice(string heading,string message,StatusTone tone,Control anchor,params object[] args) {
            updateNoticeTimer.Stop(); updateHeadingKey=heading; updateMessageKey=message; updateMessageArgs=args; noticeAnchor=anchor;
            updateHeading.Text=T(heading); updateMessage.Text=F(message,args); updateIcon.Language=state.Language; updateIcon.Tone=tone;
            updateNotice.UpdateLook(colors,IsArabic); updateNotice.ShowNear(this,anchor); updateNoticeTimer.Start();
        }
        private void HideUpdateNotice() { updateNoticeTimer.Stop(); updateNotice.Hide(); noticeAnchor=null; updateHeadingKey=updateMessageKey=""; updateHeading.Text=""; updateMessage.Text=""; }
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
        protected override void Dispose(bool disposing) { if(disposing) { updateNoticeTimer.Dispose(); if(updateNotice!=null) updateNotice.Dispose(); linkTimer.Dispose(); themeTip.Dispose(); if(brandLogo!=null) brandLogo.Dispose(); if(engine!=null) engine.Dispose(); } base.Dispose(disposing); }
    }
}
