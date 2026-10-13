<p align="center"><img src="assets/header-logo.png" width="128" alt="YouTube Downloader YD logo"></p>

# YouTube Downloader for Windows — MP4, MP3 and Playlists

A free YouTube video and playlist downloader for Windows 10 and 11, created by **Ziad Hassanein**. Save videos and Shorts as MP4 or convert YouTube audio to MP3 at 192 or 320 kbps. Choose available video qualities and a save folder in an English or Arabic interface, with light and dark themes.

**[Download for Windows (.zip)](https://github.com/ZiadHassanein/youtube-downloader-windows/releases/download/v1.0.14/YouTubeDownloader-v1.0.14-windows-x64.zip)**

One ZIP includes the app, tool setup, and instructions.

[How to download](#how-to-download-youtube-videos-on-windows) · [Frequently asked questions](#frequently-asked-questions) · [العربية](#برنامج-تنزيل-فيديوهات-يوتيوب-للويندوز)

<p align="center">
  <a href="assets/screenshot-en-dark.png"><img src="assets/screenshot-en-dark.png" width="400" alt="YouTube Downloader — English, dark theme (v1.0.14)"></a>
  <a href="assets/screenshot-ar-light.png"><img src="assets/screenshot-ar-light.png" width="400" alt="YouTube Downloader — Arabic, light theme (v1.0.14)"></a>
</p>

English · dark and Arabic · light (v1.0.14). Tap or click a screenshot to view it full size.

Current Windows download: **v1.0.14**. See the [release history](https://github.com/ZiadHassanein/youtube-downloader-windows/releases) for changes.

## How to download YouTube videos on Windows

1. Click **Download for Windows (.zip)** above to download the complete app package directly.
2. Extract the ZIP to a writable folder.
3. If yt-dlp, FFmpeg, or Deno are missing, run `SetupTools.cmd` once. It installs only missing tools through Windows Package Manager.
4. Open `YouTubeDownloader.exe` and paste a video or playlist link. For a video link that also contains a playlist, choose **This video** or **Whole playlist**.
5. Choose MP4 video or MP3 audio, select a save folder, and click **Download**.
6. Use **Show in folder** after a download to find its file in File Explorer, or select a file in **Recent downloads** to reveal it later.

Windows 10/11 x64 and .NET Framework 4.8 are required. Windows 11 includes the required framework. The first-time setup script requires WinGet.

## Video, audio and playlist features

- **MP4 video downloads:** supports YouTube videos, Shorts, and shared links, with automatic inspection when you paste a link.
- **Available video quality:** single-video choices reflect the source's available formats. The default is the highest available resolution up to 1080p.
- **YouTube to MP3 conversion:** choose 192 or 320 kbps MP3 encoding; 320 kbps is the default. Higher encoding bitrates do not improve the original audio source.
- **Whole YouTube playlists:** save a playlist as MP4 videos or MP3 audio, with an entry count, current-video progress, and overall progress.
- **Resumable downloads:** keep completed files, resume partial files, and skip completed playlist entries still present in the same destination and output profile.
- **Paced playlist queue:** downloads one video at a time, skips unavailable entries, and pauses on rate-limit, network, or storage errors. YouTube limits can still occur.
- **English and Arabic:** switch between English LTR and Arabic RTL layouts, with the language choice remembered.
- **Light and dark themes:** dark by default, with a sun/moon toggle, rounded controls, and remembered preferences.
- **Local file controls:** choose a save folder, view recent downloads, open completed files, or reveal them in File Explorer. The default folder is Downloads.
- **Progress and cancellation:** shows progress and speed, with cancellation that preserves completed and partial files. A successful download clears the pasted link; every launch starts with an empty link.
- **Manual app updates:** check GitHub releases from the app and install a verified update only when you choose to. Downloader dependencies are managed separately.

Sign-in-only videos and active live streams are not supported. A video link containing a playlist defaults to **This video**; **Whole playlist** is an explicit choice. Playlist-only links select the whole playlist. The app does not read browser cookies or automatically update its dependencies.

## Download a YouTube playlist

Each playlist is saved in `Playlist [playlist-id]` inside the selected folder. MP4 qualities such as **Up to 1080p** are caps rather than claims about every entry. A video without a suitable format may fail; **Best available** removes the cap. MP3 offers 192 or 320 kbps encoding; this does not improve the original audio source.

The playlist title list is read once per pasted link, without fetching every video's formats in advance. Videos are downloaded sequentially with bounded retries, request pacing, and cancellable waits. Progress shows the current video and an approximate overall percentage based on item count. Private, removed, unavailable, and live entries are reported as failures and skipped. Rate-limit, network, or disk errors pause the queue; three consecutive unexpected failures also pause it. The final summary separates saved, already downloaded, failed, and remaining entries. YouTube limits can still occur.

Resume records are stored privately under `%LOCALAPPDATA%\YouTubeDownloader\PlaylistState`, separately for each playlist destination, format, resolution cap, or MP3 bitrate. Completed downloads leave only media files in the playlist folder. Existing `.yd-completed-*.json` records from v1.0.12, including other output profiles, migrate into private storage the next time that playlist is downloaded. Deleted media is downloaded again. Cancellation keeps completed and partial files, and the link stays available after cancellation or partial failure. Fully successful downloads clear it.

## Frequently asked questions

### Is YouTube Downloader free?

Yes. The Windows app is free to download and use. The direct download above provides one ZIP containing the executable, setup script, and instructions.

### Can I download a whole YouTube playlist?

Yes. Paste a playlist link, choose MP4 or MP3 and a quality, then click **Download**. A video link that also contains a playlist lets you choose **This video** or **Whole playlist**. Playlist-only links select the whole playlist automatically.

### How do I convert YouTube audio to MP3 at 320 kbps?

Paste a video or playlist link, select **Audio · MP3**, choose **320 kbps**, and click **Download**. You can also choose 192 kbps. These are output encoding bitrates; they do not restore detail missing from the source audio.

### Which video qualities can I download?

For one video, the app lists the actual available resolutions after checking the link. For a playlist, the selected quality is a maximum: each video uses a suitable available format up to that limit. **Best available** removes the playlist resolution cap.

### Does the program work on every PC?

The supported target is Windows 10 or 11 on an x64 PC with .NET Framework 4.8. The app also needs yt-dlp, FFmpeg, and Deno. The setup script uses WinGet to install missing tools. macOS, Linux, and mobile devices are not supported by this Windows release.

### Where are downloaded videos and MP3 files saved?

Files go to Downloads by default, or to the writable folder you select. Playlists use a `Playlist [playlist-id]` subfolder. **Show in folder** reveals a completed file in File Explorer. Playlist resume records stay in private app data rather than in the media folder.

### Do I need to sign in to YouTube?

The app does not use YouTube sign-in or read browser cookies. Sign-in-only videos and active live streams are not supported. Removed, private, and otherwise unavailable playlist entries may be skipped.

### Is this an official YouTube app?

No. YouTube Downloader is an independent Windows program by Ziad Hassanein, not an official Google or YouTube product. Downloads must comply with [YouTube's terms](https://www.youtube.com/static?template=terms) and the relevant content permissions.

<div dir="rtl">

## برنامج تنزيل فيديوهات يوتيوب للويندوز

برنامج **YouTube Downloader** من **Ziad Hassanein** هو برنامج مجاني لويندوز 10 و11 بنظام 64 بت. يتيح تنزيل فيديوهات يوتيوب والمقاطع القصيرة وقوائم التشغيل بصيغة MP4، أو حفظ الصوت بصيغة MP3 بمعدل ترميز 192 أو 320 كيلوبت/ثانية. يدعم واجهة عربية من اليمين إلى اليسار وواجهة إنجليزية، مع الوضعين الداكن والفاتح.

### طريقة التحميل من يوتيوب

1. [نزّل ملف ZIP الخاص بالبرنامج](https://github.com/ZiadHassanein/youtube-downloader-windows/releases/download/v1.0.14/YouTubeDownloader-v1.0.14-windows-x64.zip) وفك الضغط في مجلد قابل للكتابة.
2. شغّل `SetupTools.cmd` إذا كانت أدوات yt-dlp أو FFmpeg أو Deno غير متوفرة، ثم افتح `YouTubeDownloader.exe`.
3. الصق رابط فيديو أو قائمة تشغيل، واختر صيغة MP4 أو MP3 والجودة ومجلد الحفظ، ثم اضغط **تنزيل**.

تظهر جودة الفيديو المتاحة بعد فحص الرابط. في قوائم التشغيل، الجودة المختارة هي حد أقصى وليست ضمانًا لجودة جميع الفيديوهات. ترميز MP3 بمعدل 320 كيلوبت/ثانية لا يحسّن جودة الصوت الأصلي. يتطلب البرنامج .NET Framework 4.8، ويستخدم إعداد الأدوات WinGet. لا يدعم هذا الإصدار الهواتف أو macOS أو Linux.

</div>

## App updates

The app checks this repository's latest stable release in the background when it opens. You can also choose **Check for updates**. Updates are installed only when you choose **Install v…**. It downloads the Windows package, verifies its SHA-256 checksum and executable version, then restarts the app. You can instead download and extract a release manually. Preferences and recent downloads are preserved. The app folder must be writable. This updates the downloader app only; yt-dlp, FFmpeg, and Deno are managed separately.

## Dependencies

The app calls these separately installed tools:

- [yt-dlp](https://github.com/yt-dlp/yt-dlp) for YouTube extraction and downloads.
- [FFmpeg](https://ffmpeg.org/) for media merging and MP3 conversion.
- [Deno](https://deno.com/) for yt-dlp's YouTube JavaScript support.

It checks a `tools` folder beside the app, the current user's WinGet Links folder, and PATH. Third-party binaries are not bundled in this release and retain their own licenses.

## Privacy

Preferences and recent local file paths stay in `settings.json` beside the app. Video links are not saved. That file, downloads, and personal histories are excluded from the repository and release ZIP. Update checks contact GitHub; they do not send your video links or history.

## Build

On Windows with .NET Framework installed:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build.ps1
```

The compiled app and release ZIP appear in `dist`. No NuGet packages are required.

Run the deterministic core checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test.ps1
```

## Author

Created by **Ziad Hassanein**.

[GitHub](https://github.com/ZiadHassanein) · [LinkedIn](https://www.linkedin.com/in/ziad-hassanein/) · [Email](mailto:ziadmuhammed501@gmail.com)
