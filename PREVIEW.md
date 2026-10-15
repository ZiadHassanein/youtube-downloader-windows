# ViFree 4.1.0 Preview 12

Unreleased preview with overlay scrollbars that preserve content width. Stable remains 4.0.0; owner testing is pending.

Extract the ZIP and open **ViFree.exe**. The runtime is bundled; missing media tools prepare automatically. Internet is required. Data uses `LocalAppData/ViFreePreview`; pending queue links are not saved. App updates are disabled.

Try:

- Scroll the main page, **Download queue**, **More options**, Recent downloads and playlist selection in both languages/themes and short windows.
- Check content width stays fixed when scrollbars appear; Arabic bars should be left.
- Open long format/quality lists; check labels, keyboard selection and scrolling.
- Add one queue link per field; try Paste, Remove, multiline Ctrl+V and Start.
- Check **Settings**, **Contact**, Arabic Show in folder, browser integration and **Update downloader**.

98 focused/894 full WPF checks, package verification and EXE-only startup passed; scoped scrollbar review passed. Screenshot thumbnails are synthetic fixtures. Clean Windows 10/11 and actual browser loading remain owner tests. Protected/sign-in media is unsupported.

Develop with .NET 10 SDK and Node.js 20+. Run `pwsh -File scripts/check.ps1`. Package using `pwsh -File scripts/package.ps1 -Version 4.1.0-preview.12 -Preview`, then run `pwsh -File scripts/smoke-exe.ps1 -Version 4.1.0-preview.12`.
