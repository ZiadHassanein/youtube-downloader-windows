# ViFree 4.1.0 Preview 10

Unreleased test build updating Download queue to the current design. Stable remains 4.0.0; owner testing is pending.

Extract the ZIP and open **ViFree.exe**. The runtime is bundled; missing media tools prepare automatically. Internet is required. Data uses `LocalAppData/ViFreePreview`; pending queue links are not saved. App updates are disabled.

Try:

- Open **Download queue** in both languages/themes and short windows.
- Paste one URL per line; add links and check count, feedback and row tooltips.
- Start/cancel; verify active-only progress, Retry/Remove and fixed Close/Start controls.
- Open **More options** modal for subtitles/SRT/VTT; close with **Done** or Escape.
- Check Recent downloads with long titles and Arabic Show in folder.
- Check **Settings**, **Contact**, browser integration and **Update downloader**.

87 targeted queue checks, package verification and EXE-only startup passed. Screenshot thumbnails are synthetic fixtures. Clean Windows 10/11 and actual browser loading remain owner tests. Protected/sign-in media is unsupported.

Develop with .NET 10 SDK and Node.js 20+. Run `pwsh -File scripts/check.ps1`. Package using `pwsh -File scripts/package.ps1 -Version 4.1.0-preview.10 -Preview`, then run `pwsh -File scripts/smoke-exe.ps1 -Version 4.1.0-preview.10`.
