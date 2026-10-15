# ViFree 4.1.0 Preview 15

Unreleased preview gives language, audio-format and quality choices rounded button faces. Stable remains 4.0.0; owner testing is pending.

Extract the ZIP and open **ViFree.exe**. The runtime is bundled; missing media tools prepare automatically. Internet is required. Data uses `LocalAppData/ViFreePreview`; pending queue links are not saved. App updates are disabled.

Try:

- Open the language menu in both themes. Check rounded choices, selected checkmarks and keyboard focus.
- Activate **Audio**; choose MP3/M4A/Opus/WAV/FLAC. Repeat the current choice, click outside or press Escape to dismiss.
- Try highest-first quality presets after **Best available**. WAV/FLAC use **Source quality**. Reopen long lists; the selected row should remain fully visible.
- Check **More options**, **Settings**, **Contact**, short windows, overlay scrolling, queue fields/direct Start and Recent downloads. The footer credit stays removed.

337 focused checks, package verification and EXE-only startup passed. Scoped visual review passed. The previous full WPF suite remains baseline evidence. Screenshot thumbnails are synthetic fixtures. Clean Windows 10/11 and actual browser loading remain owner tests. Protected/sign-in media is unsupported.

Develop with .NET 10 SDK and Node.js 20+. Run `pwsh -File scripts/check.ps1`. Package using `pwsh -File scripts/package.ps1 -Version 4.1.0-preview.15 -Preview`, then run `pwsh -File scripts/smoke-exe.ps1 -Version 4.1.0-preview.15`.
