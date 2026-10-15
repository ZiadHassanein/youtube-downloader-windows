# ViFree 4.1.0 Preview 16

Unreleased preview starts with a narrower 960-DIP window. Widen it for two columns. Stable remains 4.0.0; owner testing is pending.

Extract the ZIP and open **ViFree.exe**. The runtime is bundled; missing media tools prepare automatically. Internet is required. Data uses `LocalAppData/ViFreePreview`; pending queue links are not saved. App updates are disabled.

Try:

- Paste a video or playlist; compact media should appear above the form.
- Switch English/Arabic and open **Settings**; window height and position should stay fixed.
- Widen the window for two columns, then test short resizing, maximize/restore and overlay scrolling.
- Check language/Audio/quality button choices, **More options**, **Settings**, **Contact**, queue fields/direct Start and Recent downloads. The footer credit stays removed.

225 focused WPF checks, package verification and EXE-only startup passed. Scoped visual review passed. The previous full WPF suite remains baseline evidence. Screenshot thumbnails are synthetic fixtures. Clean Windows 10/11 and actual browser loading remain owner tests. Protected/sign-in media is unsupported.

Develop with .NET 10 SDK and Node.js 20+. Run `pwsh -File scripts/check.ps1`. Package using `pwsh -File scripts/package.ps1 -Version 4.1.0-preview.16 -Preview`, then run `pwsh -File scripts/smoke-exe.ps1 -Version 4.1.0-preview.16`.
