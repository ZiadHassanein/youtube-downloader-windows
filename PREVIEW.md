# ViFree 4.1.0 Preview 11

Unreleased preview with one input per queue link and direct Start. Stable remains 4.0.0; owner testing is pending.

Extract the ZIP and open **ViFree.exe**. The runtime is bundled; missing media tools prepare automatically. Internet is required. Data uses `LocalAppData/ViFreePreview`; pending queue links are not saved. App updates are disabled.

Try:

- Open **Download queue** in both languages/themes and short windows.
- Add one link per field; try Paste, Add another link, Remove and multiline Ctrl+V.
- Choose Start directly; invalid/duplicate fields should keep local errors until corrected or removed.
- Start/cancel; check progress and the fixed footer.
- Open **More options** modal for subtitles/SRT/VTT; close with **Done** or Escape.
- Check Recent downloads with long titles and Arabic Show in folder.
- Check **Settings**, **Contact**, browser integration and **Update downloader**.

115 targeted/790 full WPF checks, package verification and EXE-only startup passed. Screenshot thumbnails are synthetic fixtures. Clean Windows 10/11 and actual browser loading remain owner tests. Protected/sign-in media is unsupported.

Develop with .NET 10 SDK and Node.js 20+. Run `pwsh -File scripts/check.ps1`. Package using `pwsh -File scripts/package.ps1 -Version 4.1.0-preview.11 -Preview`, then run `pwsh -File scripts/smoke-exe.ps1 -Version 4.1.0-preview.11`.
