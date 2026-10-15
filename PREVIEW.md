# ViFree 4.1.0 Preview 22

Unreleased preview keeps dropdown scrollbars clear of option buttons. Stable remains 4.0.0; owner testing is pending.

Extract the ZIP and open **ViFree.exe**. The runtime is bundled; missing media tools prepare automatically. Internet is required. Data uses `LocalAppData/ViFreePreview`; pending queue links are not saved. App updates are disabled.

Try:

- Open short and long quality lists in both languages/themes at compact and wide window sizes. Check the clear scrollbar gutter and stable option widths.
- Select 256 kbps, reopen, then page to the last option. Check full selected-row visibility, keyboard selection and scrollbar targeting.
- Switch languages repeatedly and check preserved Audio format/quality. Check normal page and dialog scrolling too.

253 focused native checks, package verification and EXE-only startup passed. Scoped visual review passed; the delivered app opens normally. Earlier checks remain baseline evidence. Clean Windows 10/11 and actual browser loading remain owner tests. Protected/sign-in media is unsupported.

Develop with .NET 10 SDK and Node.js 20+. Run `pwsh -File scripts/check.ps1`. Package using `pwsh -File scripts/package.ps1 -Version 4.1.0-preview.22 -Preview`, then run `pwsh -File scripts/smoke-exe.ps1 -Version 4.1.0-preview.22`.
