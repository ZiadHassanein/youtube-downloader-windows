# ViFree 4.1.0 Preview 14

Unreleased preview removes the footer credit, keeping Settings left and Contact right. Stable remains 4.0.0; owner testing is pending.

Extract the ZIP and open **ViFree.exe**. The runtime is bundled; missing media tools prepare automatically. Internet is required. Data uses `LocalAppData/ViFreePreview`; pending queue links are not saved. App updates are disabled.

Try:

- Check the footer in both languages/themes: **Settings** left, **Contact** right, without the author credit.
- Open **Settings** and **Contact**; verify GitHub, LinkedIn and Email actions.
- Try the **Audio** format popup and highest-first presets after **Best available**; WAV/FLAC use **Source quality**. Conversion cannot improve source quality.
- Open **More options** beside quality for subtitles/SRT/VTT; close with **Done** or Escape.
- Check short windows, overlay scrolling, queue fields/direct Start and Recent downloads.

184 footer/layout checks, package verification and EXE-only startup passed. Scoped visual review passed. Screenshot thumbnails are synthetic fixtures. Clean Windows 10/11 and actual browser loading remain owner tests. Protected/sign-in media is unsupported.

Develop with .NET 10 SDK and Node.js 20+. Run `pwsh -File scripts/check.ps1`. Package using `pwsh -File scripts/package.ps1 -Version 4.1.0-preview.14 -Preview`, then run `pwsh -File scripts/smoke-exe.ps1 -Version 4.1.0-preview.14`.
