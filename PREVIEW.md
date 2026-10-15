# ViFree 4.1.0 Preview 19

Unreleased preview keeps subtitle-format borders and focus outlines visible in **More options**. Stable remains 4.0.0; owner testing is pending.

Extract the ZIP and open **ViFree.exe**. The runtime is bundled; missing media tools prepare automatically. Internet is required. Data uses `LocalAppData/ViFreePreview`; pending queue links are not saved. App updates are disabled.

Try:

- Open **More options** in English/Arabic and light/dark.
- Select and keyboard-focus both **SRT** and **VTT**. Check every border and focus outline, especially VTT's left edge in Arabic.
- Open from a short main window and a playlist; check scrolling, subtitle lookup and **Done**.
- Check **Audio**, quality choices, **Settings**, **Contact**, the compact queue and Recent downloads.

60 native options-modal checks, package verification and EXE-only startup passed. Scoped visual review passed. Earlier checks remain baseline evidence. Screenshot thumbnails are synthetic fixtures. Clean Windows 10/11 and actual browser loading remain owner tests. Protected/sign-in media is unsupported.

Develop with .NET 10 SDK and Node.js 20+. Run `pwsh -File scripts/check.ps1`. Package using `pwsh -File scripts/package.ps1 -Version 4.1.0-preview.19 -Preview`, then run `pwsh -File scripts/smoke-exe.ps1 -Version 4.1.0-preview.19`.
