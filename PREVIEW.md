# ViFree 4.1.0 Preview 8

Unreleased palette preview: teal/mint actions, amber audio, neutral surfaces. Layout/workflows stay unchanged. Stable is 4.0.0; owner testing is pending.

Extract the ZIP and open **ViFree.exe**. The runtime is bundled; missing media tools prepare automatically. Internet is required. Data uses `LocalAppData/ViFreePreview`; pending queue links are not saved. App updates are disabled.

Try:

- Switch themes/languages; check text, focus, teal video and amber audio selection.
- Resize short, maximize/restore, and scroll: controls and footer must stay reachable.
- Download a video; check the Arabic file actions and recent history.
- Open **More options** modal for subtitles/SRT/VTT; close with **Done** or Escape.
- Try playlist selection/subtitle lookup, queue cancellation/retry, and status/file actions without preview.
- Check **Settings**, notices, browser integration and **Update downloader**.
- Open **Contact** (“تواصل معي”): check GitHub, LinkedIn and Email; dismiss with Escape or outside click.

91 final palette checks, package verification and EXE-only startup passed. Screenshot thumbnails are synthetic fixtures. Clean Windows 10/11 and actual browser loading remain owner tests. Protected/sign-in media is unsupported.

Develop with .NET 10 SDK and Node.js 20+. Run `pwsh -File scripts/check.ps1`. Package using `pwsh -File scripts/package.ps1 -Version 4.1.0-preview.8 -Preview`, then run `pwsh -File scripts/smoke-exe.ps1 -Version 4.1.0-preview.8`.
