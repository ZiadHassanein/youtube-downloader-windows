# ViFree 4.1.0 Preview 17

Unreleased preview gives Audio the same teal selection and icon color as Video. Stable remains 4.0.0; owner testing is pending.

Extract the ZIP and open **ViFree.exe**. The runtime is bundled; missing media tools prepare automatically. Internet is required. Data uses `LocalAppData/ViFreePreview`; pending queue links are not saved. App updates are disabled.

Try:

- Switch **Video/Audio** in both languages/themes. Shared teal styling should retain distinct labels, icons and checked states.
- Activate **Audio** and choose MP3/M4A/Opus/WAV/FLAC; check selection, keyboard focus and outside/Escape dismissal.
- Try **Best available**, numeric quality presets and lossless **Source quality**; conversion cannot improve source quality.
- Check the 960-DIP window, compact media, **More options**, **Settings**, **Contact**, overlay scrolling, queue fields/direct Start and Recent downloads. The footer credit stays removed.

195 focused palette/output-picker checks, package verification and EXE-only startup passed. Scoped visual review passed. Earlier checks remain baseline evidence. Screenshot thumbnails are synthetic fixtures. Clean Windows 10/11 and actual browser loading remain owner tests. Protected/sign-in media is unsupported.

Develop with .NET 10 SDK and Node.js 20+. Run `pwsh -File scripts/check.ps1`. Package using `pwsh -File scripts/package.ps1 -Version 4.1.0-preview.17 -Preview`, then run `pwsh -File scripts/smoke-exe.ps1 -Version 4.1.0-preview.17`.
