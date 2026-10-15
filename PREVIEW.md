# ViFree 4.1.0 Preview 21

Unreleased preview refines the language picker with compact equal rows and a quieter frame. Stable remains 4.0.0; owner testing is pending.

Extract the ZIP and open **ViFree.exe**. The runtime is bundled; missing media tools prepare automatically. Internet is required. Data uses `LocalAppData/ViFreePreview`; pending queue links are not saved. App updates are disabled.

Try:

- Open the language picker in both languages/themes at minimum, default and wide window sizes. Check equal rows and placement inside the app.
- Open by click, Down or AppsKey; test Escape and outside dismissal.
- Select **English/العربية** repeatedly. Check the selected mark, font/direction and preserved Audio format/quality.
- Reopen **Audio**; check anchoring, then **More options**, the compact queue, **Settings**, **Contact** and Recent downloads.

264 focused native checks, package verification and EXE-only startup passed. Scoped visual review passed. Earlier checks remain baseline evidence. Screenshot thumbnails are synthetic fixtures. Clean Windows 10/11 and actual browser loading remain owner tests. Protected/sign-in media is unsupported.

Develop with .NET 10 SDK and Node.js 20+. Run `pwsh -File scripts/check.ps1`. Package using `pwsh -File scripts/package.ps1 -Version 4.1.0-preview.21 -Preview`, then run `pwsh -File scripts/smoke-exe.ps1 -Version 4.1.0-preview.21`.
