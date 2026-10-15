# ViFree 4.1.0 Preview 23

Unreleased preview narrows the window, inputs and Download action. Stable remains 4.0.0; owner testing is pending.

Extract the ZIP and open **ViFree.exe**. The runtime is bundled; missing media tools prepare automatically. Internet is required. Data uses `LocalAppData/ViFreePreview`; pending queue links are not saved. App updates are disabled.

Try:

- Check the 740-wide startup window and 660-wide minimum in both languages/themes. Check complete header actions and input borders.
- Paste a video or playlist, then open Settings. Check matching content widths and the smaller Download action at the left in English or right in Arabic.
- Resize wider, shorter and maximize/restore. Check scrolling, long titles and unchanged window bounds during language/media changes.
- Check quality dropdowns, More options, the queue, Contact and Recent downloads.

398 focused native checks passed with zero build warnings/errors. Scoped visual review, package verification and EXE-only startup passed; the delivered app opens normally. Earlier checks remain baseline evidence. Clean Windows 10/11 and actual browser loading remain owner tests. Protected/sign-in media is unsupported.

Develop with .NET 10 SDK and Node.js 20+. Run `pwsh -File scripts/check.ps1`. Package using `pwsh -File scripts/package.ps1 -Version 4.1.0-preview.23 -Preview`, then run `pwsh -File scripts/smoke-exe.ps1 -Version 4.1.0-preview.23`.
