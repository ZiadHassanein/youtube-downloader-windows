# ViFree 4.1.0 Preview 13

Unreleased preview with an Audio format popup, expanded audio presets and More options beside quality. Stable remains 4.0.0; owner testing is pending.

Extract the ZIP and open **ViFree.exe**. The runtime is bundled; missing media tools prepare automatically. Internet is required. Data uses `LocalAppData/ViFreePreview`; pending queue links are not saved. App updates are disabled.

Try:

- Activate **Audio** in both languages/themes. Choose MP3/M4A/Opus/WAV/FLAC; repeat the current choice, click outside or press Escape to dismiss.
- Try highest-first bitrate presets after **Best available**; WAV/FLAC use **Source quality**. Reopen long quality lists; the selected row should be fully visible. Conversion cannot improve source quality.
- Open **More options** beside quality for subtitles/SRT/VTT; close with **Done** or Escape.
- Check short windows, overlay scrolling, queue fields/direct Start, Recent downloads, **Settings** and **Contact**.

106 focused/1,106 full WPF checks, package verification and EXE-only startup passed. Scoped visual review passed. Screenshot thumbnails are synthetic fixtures. Clean Windows 10/11 and actual browser loading remain owner tests. Protected/sign-in media is unsupported.

Develop with .NET 10 SDK and Node.js 20+. Run `pwsh -File scripts/check.ps1`. Package using `pwsh -File scripts/package.ps1 -Version 4.1.0-preview.13 -Preview`, then run `pwsh -File scripts/smoke-exe.ps1 -Version 4.1.0-preview.13`.
