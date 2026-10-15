# ViFree 4.0.0 Preview 1

Historical test-build notes: this preview was reviewed before promotion to v4.0.0.

Extract the preview ZIP and open `ViFree.exe`. The .NET runtime is bundled; missing compatible media tools are prepared automatically. An internet connection is required. Preview settings, tools and playlist records use `LocalAppData/ViFreePreview`; pending queue links are not persisted. App updates are disabled in the preview.

Try these workflows:

- Paste a public video, choose an actual quality, download and show it in its folder.
- Open **Output options**, select an audio format or available subtitles, and download.
- Paste a playlist and use **Choose videos** to select its entries.
- Open **Queue**, paste one link per line, add and start. Try cancellation and per-job retry. Quality for newly added links is a maximum; a checked video's saved selection stays tied to that video.
- Switch language/theme, resize/maximize, and check that long messages remain inside the window.
- Try **Update downloader** when a video link stops working after website changes. It checks for a verified yt-dlp update, shows progress and reports whether an update was installed or was already current. Missing tools are prepared privately; FFmpeg/Deno remain on compatible app-managed versions.
- Optionally test the [browser companion](browser/README.md). Ordinary app use requires no browser extension.

Verified on the development Windows host: Core and infrastructure regressions, queue/recovery fixtures, 56 WPF checks, browser JavaScript and packaged native messaging, standalone EXE startup, and real FFmpeg conversion/FFprobe validation for all five audio formats. A live YouTube inspection/download passed. Vimeo's fixture path passed; its live sample required login and remains unverified. Actual browser loading and clean Windows 10/11 execution remain owner tests. Protected and sign-in media and extension-store distribution are unsupported.

For development, use .NET 10 SDK and Node.js 20+ on Windows. Run `pwsh -File scripts/check.ps1`. Package with `pwsh -File scripts/package.ps1 -Version 4.0.0-preview.1 -Preview`, then `pwsh -File scripts/smoke-exe.ps1 -Version 4.0.0-preview.1` ; the optional companion is included in Browser in the app ZIP.
