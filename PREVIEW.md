# ViFree 4.1.0 Preview 1

Local, unreleased test build of the approved split-view redesign. Public stable remains 4.0.0. The four review fixes are resolved; owner testing is pending.

Extract the preview ZIP and open **ViFree.exe**. The .NET runtime is bundled; compatible missing media tools prepare automatically. Internet is required. Preview data uses `LocalAppData/ViFreePreview`; pending queue links are not saved. App updates are disabled.

Try:

- Inspect a public link, verify its thumbnail/title, choose format/quality, download, and show the file.
- Expand **More options** for subtitles; try audio formats.
- Select playlist entries; add queue links, cancel and retry.
- Switch English/Arabic and light/dark; resize/maximize.
- Open **Settings** during a download: progress and Cancel must remain reachable, including narrow windows.
- Try destination notices, browser integration and **Update downloader**.

The host's automated checks passed. Separate live thumbnail retrieval/cancellation passed without downloading video; screenshot thumbnails are synthetic fixtures. Clean Windows 10/11 and actual browser loading remain owner tests. Protected/sign-in media is unsupported.

Develop with .NET 10 SDK and Node.js 20+. Run `pwsh -File scripts/check.ps1`. Package using `pwsh -File scripts/package.ps1 -Version 4.1.0-preview.1 -Preview`, then run `pwsh -File scripts/smoke-exe.ps1 -Version 4.1.0-preview.1`.
