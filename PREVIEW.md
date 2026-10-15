# ViFree 4.1.0 Preview 3

Local, unreleased test build. Preview shares the app background and appears only after a video or playlist is recognized. Public stable remains 4.0.0; owner testing is pending.

Extract the preview ZIP and open **ViFree.exe**. The .NET runtime is bundled; compatible missing media tools prepare automatically. Internet is required. Preview data uses `LocalAppData/ViFreePreview`; pending queue links are not saved. App updates are disabled.

Try:

- Start without a link: the centered form has no preview. Inspect a video/playlist; verify context, download and show the file.
- Open **More options** modal for subtitles/SRT/VTT; close with **Done** or Escape.
- Select playlist entries; try subtitle lookup, queue links, cancel and retry.
- Switch English/Arabic and light/dark; resize/maximize.
- Check status/errors and completed-file actions without preview, then in **Settings**, including narrow windows.
- Try destination notices, browser integration and **Update downloader**.

Modal checks and EXE-only startup passed. Separate thumbnail retrieval/cancellation passed without video; screenshot thumbnails are synthetic fixtures. Clean Windows 10/11 and actual browser loading remain owner tests. Protected/sign-in media is unsupported.

Develop with .NET 10 SDK and Node.js 20+. Run `pwsh -File scripts/check.ps1`. Package using `pwsh -File scripts/package.ps1 -Version 4.1.0-preview.3 -Preview`, then run `pwsh -File scripts/smoke-exe.ps1 -Version 4.1.0-preview.3`.
