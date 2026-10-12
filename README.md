<p align="center"><img src="assets/logo.png" width="128" alt="YouTube Downloader YD logo"></p>

# YouTube Downloader for Windows

A desktop app for saving YouTube videos as MP4 or extracting MP3 audio. Paste a link, choose an available quality and a destination, then download.

**[Download the latest Windows release](https://github.com/ZiadHassanein/youtube-downloader-windows/releases/latest)**

| Light theme | Dark theme |
| --- | --- |
| ![Downloader in light theme](assets/screenshot-light.png) | ![Downloader in dark theme](assets/screenshot-dark.png) |

## Get started

1. Download `YouTubeDownloader-v1.0.0-windows-x64.zip` from the release page.
2. Extract the ZIP to a writable folder.
3. If yt-dlp, FFmpeg, or Deno are missing, run `SetupTools.cmd` once. It installs only missing tools through Windows Package Manager.
4. Open `YouTubeDownloader.exe`, paste a video link, and wait for its available qualities to appear.
5. Choose MP4 video or MP3 audio, select a save folder, and click **Download**.

Windows 10/11 x64 and .NET Framework 4.8 are required. Windows 11 includes the required framework. The first-time setup script requires WinGet.

## Features

- Automatic video inspection and actual available-resolution choices.
- A sensible default: the highest available resolution up to 1080p.
- Support for normal video links, Shorts, and shared links.
- MP4 output and MP3 audio encoding at 192 kbps.
- Progress, speed, cancellation, and resumable partial files.
- Separate video filenames for different quality selections.
- Existing completed files are preserved.
- Light and dark themes, with your choice remembered.
- Styled MP4 / MP3 radio buttons.
- Link cleared after a successful download; every launch starts with an empty link.
- Recent-download history and saved preferences.
- Checks GitHub for a new app version on launch; install it from inside the app.

Playlist-only links and sign-in-only videos are not supported. A video link containing a playlist downloads only the linked video. The app does not read browser cookies or automatically update its dependencies.

## App updates

The app checks this repository's latest stable release in the background when it opens. You can also choose **Check for updates**. When a newer version is available, choose **Install v…**. It downloads the Windows package, verifies its SHA-256 checksum and executable version, then restarts the app. Preferences and recent downloads are preserved. The app folder must be writable. This updates the downloader app only; yt-dlp, FFmpeg, and Deno are managed separately.

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

The logo was generated with ImageGen and selected for the app's identity.
