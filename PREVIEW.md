# ViFree 4.1.0 Preview 18

Unreleased preview makes **Download queue** and its controls more compact. Stable remains 4.0.0; owner testing is pending.

Extract the ZIP and open **ViFree.exe**. The runtime is bundled; missing media tools prepare automatically. Internet is required. Data uses `LocalAppData/ViFreePreview`; pending queue links are not saved. App updates are disabled.

Try:

- Open **Download queue** in both languages/themes, at normal and short sizes. Check the smaller controls and fixed footer.
- Add one link per field; try Paste, Remove, Add another link and multiline Ctrl+V.
- Start directly; invalid/duplicate fields should keep local errors until corrected or removed.
- Start/cancel and check progress, scrolling and footer actions.
- Check the main window, **Audio**, **More options**, **Settings**, **Contact** and Recent downloads. Other surfaces keep their density.

123 native queue checks, package verification and EXE-only startup passed. Scoped visual review passed. Earlier checks remain baseline evidence. Screenshot thumbnails are synthetic fixtures. Clean Windows 10/11 and actual browser loading remain owner tests. Protected/sign-in media is unsupported.

Develop with .NET 10 SDK and Node.js 20+. Run `pwsh -File scripts/check.ps1`. Package using `pwsh -File scripts/package.ps1 -Version 4.1.0-preview.18 -Preview`, then run `pwsh -File scripts/smoke-exe.ps1 -Version 4.1.0-preview.18`.
