# ViFree 4.1.0 Preview 20

Unreleased preview keeps Audio's format menu anchored to Audio across English/Arabic. Stable remains 4.0.0; owner testing is pending.

Extract the ZIP and open **ViFree.exe**. The runtime is bundled; missing media tools prepare automatically. Internet is required. Data uses `LocalAppData/ViFreePreview`; pending queue links are not saved. App updates are disabled.

Try:

- Open **Audio** in both languages/themes at narrow and wide widths. The menu should follow Audio and stay inside the app.
- Repeat language changes and reopen menus. Check row direction, font and unchanged checkmark shape.
- Choose formats, including the current choice; test keyboard selection, Escape and outside dismissal.
- Check quality choices, **More options**, SRT/VTT outlines, the compact queue, **Settings**, **Contact** and Recent downloads.

188 native picker/header checks, package verification and EXE-only startup passed. Scoped visual review passed. Earlier checks remain baseline evidence. Screenshot thumbnails are synthetic fixtures. Clean Windows 10/11 and actual browser loading remain owner tests. Protected/sign-in media is unsupported.

Develop with .NET 10 SDK and Node.js 20+. Run `pwsh -File scripts/check.ps1`. Package using `pwsh -File scripts/package.ps1 -Version 4.1.0-preview.20 -Preview`, then run `pwsh -File scripts/smoke-exe.ps1 -Version 4.1.0-preview.20`.
