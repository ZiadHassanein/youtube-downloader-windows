<p align="center"><img src="assets/header-logo.png" width="96" alt="YouTube Downloader YD logo"></p>

# YouTube Downloader for Windows — MP4, MP3 and Playlists

A free Windows app by **Ziad Hassanein**. Save YouTube videos, Shorts and playlists as MP4, or audio as MP3 at 192 or 320 kbps. Choose actual available video qualities in an English or Arabic interface, with light and dark themes.

**[Download for Windows (.zip)](https://github.com/ZiadHassanein/youtube-downloader-windows/releases/download/v2.0.1/YouTubeDownloader-v2.0.1-win-x64.zip)**

Extract one ZIP and open `YouTubeDownloader.exe`. No separate .NET runtime, installer, WinGet, or tool setup script is needed. Missing media tools are prepared privately when you first paste a link. Windows 10/11 x64.

[How to download](#how-to-download-youtube-videos-on-windows) · [Questions](#frequently-asked-questions) · [العربية](#برنامج-تنزيل-فيديوهات-يوتيوب-للويندوز)

<p align="center">
  <a href="assets/screenshot-en-dark.png"><img src="assets/screenshot-en-dark.png" width="400" alt="YouTube Downloader — English, dark theme (v1.0.14)"></a>
  <a href="assets/screenshot-ar-light.png"><img src="assets/screenshot-ar-light.png" width="400" alt="YouTube Downloader — Arabic, light theme (v1.0.14)"></a>
</p>

The two supplied screenshots show v1.0.14. Version 2.0 rebuilds the interface with a compact toolbar, an open layout, embedded English/Arabic fonts, and crisp line icons. Tap a screenshot to view it full size.

## How to download YouTube videos on Windows

1. Download the ZIP above and extract it into a writable folder.
2. Open `YouTubeDownloader.exe` and paste a video or playlist link.
3. Let the app prepare any missing media tools and inspect the link. This requires internet access and may need approximately 400 MB of free space.
4. Choose MP4 video or MP3 audio, a quality, and a save folder. For a video link containing a playlist, select **This video** or **Whole playlist**.
5. Click **Download**. Use **Show in folder** after completion, or find saved files in **Recent downloads**.

The default save location is your Windows Downloads folder. A successful download clears the pasted link, and every launch starts with an empty link.

### Moving from v1.0.14

Download the v2 ZIP manually and extract it into a new writable folder. This is a one-time migration from the previous .NET Framework app. Settings beside an old app can be selected using **Import settings**; the source file and existing media are retained. Later native desktop releases support verified, manual in-app updates.

## Video, audio and playlist features

- **Actual video qualities:** single-video choices reflect the source's formats; the recommendation is the highest available resolution up to 1080p.
- **MP3 audio:** choose 192 or 320 kbps encoding. A higher bitrate does not improve the original source audio.
- **Whole playlists:** a paced, sequential queue with per-video and overall progress. A selected resolution is a maximum for each video.
- **Resume:** reuse completed files and retain partial downloads. Private resume records keep download folders free of app JSON files.
- **Failure handling:** skip unavailable entries; stop further requests on rate-limit, network or storage failures. YouTube limits can still occur.
- **Cancellation:** stop the active media process and its children before another operation starts.
- **English and Arabic:** English LTR and Arabic RTL task layouts, with the English brand and author kept on the left.
- **Themes and alerts:** dark by default, a sharp sun/moon toggle, rounded controls, theme-colored scrolling, and transient notices inside the app.
- **Local file controls:** browse folders, reveal files, and view recent downloads in a modal.

The current provider supports YouTube. Other video sites and browser extensions are future work. Sign-in-only videos, protected content and active live streams are not supported. The app does not read browser cookies.

## Frequently asked questions

### Do I need to install yt-dlp, FFmpeg or Deno?

No manual setup is required. Compatible tools in the app's `tools` folder, your WinGet Links folder, or absolute PATH locations are checked and reused. Missing components are downloaded from pinned official releases, verified by size and SHA-256, probed, and activated in private app data. Existing global installations and PATH are not modified.

### Does it work without .NET installed?

The x64 executable includes its .NET runtime. It can run alone; the ZIP also includes instructions, the package manifest and third-party notices. Tool preparation keeps a private copy of those notices for an EXE-only launch.

### Which PCs are supported?

The target is Windows 10/11 x64. Local checks and a self-contained EXE startup check run on a Windows host; CI runs the Windows build and fixture suite. Clean Windows 10/11 machines, ARM64 and additional DPI settings have not been verified. macOS, Linux and phones are not supported.

### Will Windows show SmartScreen?

The app is unsigned, so Windows may show an unrecognized-app warning. This release does not disable Windows security or promise to remove that warning.

### Is this an official YouTube app?

This is an independent Windows program by Ziad Hassanein. Only save content you have permission to download, in accordance with [YouTube's terms](https://www.youtube.com/static?template=terms).

## App updates

Choose **Check for updates**, then **Install v…** if you want a newer release. There is no automatic startup check or installation. The app verifies the download hash, exact package files and executable version, keeps recoverable backups, and requires the new app to confirm startup before committing the update. If package files are protected, **Download update** opens the releases page while the current app remains available.

## Privacy

Preferences and recent local file paths stay in `%LOCALAPPDATA%\YouTubeDownloader`. Playlist resume records and privately prepared tools are stored there too. Video links are not saved. Personal settings, media, histories and test captures are excluded from the repository and release ZIP. Update checks contact GitHub; they do not send your video links or history. Media extraction contacts YouTube.

## Build and checks

Use PowerShell 7 and the .NET SDK specified in `global.json` on Windows:

```powershell
./scripts/test.ps1
./scripts/build.ps1
./scripts/smoke-exe.ps1
```

The test suite covers format selection, playlist resume, real child-process cancellation, dependency integrity, settings recovery, updater rollback and WPF state/layout checks. Optional live checks are separate from CI and download a small sample. The portable ZIP appears in `dist`.

See [ARCHITECTURE.md](ARCHITECTURE.md) for project boundaries and release behavior, and [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt) for dependency licenses and sources.

<div dir="rtl">

## برنامج تنزيل فيديوهات يوتيوب للويندوز

برنامج **YouTube Downloader** من **Ziad Hassanein** هو برنامج مجاني لويندوز 10 و11 بنظام 64 بت. يتيح حفظ الفيديوهات والمقاطع القصيرة وقوائم التشغيل بصيغة MP4، أو الصوت بصيغة MP3 بمعدل ترميز 192 أو 320 كيلوبت/ثانية. يدعم العربية والإنجليزية والوضعين الداكن والفاتح.

1. [نزّل ملف ZIP](https://github.com/ZiadHassanein/youtube-downloader-windows/releases/download/v2.0.1/YouTubeDownloader-v2.0.1-win-x64.zip) وفك الضغط في مجلد قابل للكتابة.
2. افتح `YouTubeDownloader.exe` والصق الرابط. يجهّز البرنامج الأدوات الناقصة تلقائيًا في مجلد بيانات خاص دون خطوات إعداد إضافية.
3. اختر الصيغة والجودة ومجلد الحفظ ثم اضغط **تنزيل**.

لا يحتاج البرنامج إلى تثبيت .NET أو WinGet بشكل منفصل. يتطلب تجهيز الأدوات اتصالًا بالإنترنت وقد يحتاج إلى نحو 400 ميجابايت من المساحة. ترميز 320 كيلوبت/ثانية لا يحسّن جودة الصوت الأصلي. التحديثات يدوية، والانتقال من الإصدار 1.0.14 يتطلب تنزيل ملف ZIP الجديد مرة واحدة. البرنامج غير موقّع وقد يظهر تحذير Windows. لا يدعم المحتوى المحمي أو الذي يتطلب تسجيل الدخول.

</div>

## Author

Created by **Ziad Hassanein**.

[GitHub](https://github.com/ZiadHassanein) · [LinkedIn](https://www.linkedin.com/in/ziad-hassanein/) · [Email](mailto:ziadmuhammed501@gmail.com)