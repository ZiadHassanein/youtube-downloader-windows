# Contributing to ViFree

Open an issue before a substantial change. Use a feature branch and a focused pull request describing the behavior, validation, and any remaining limitations.

On Windows with the .NET 10 SDK and Node.js 20+, run `pwsh -File scripts/check.ps1`. The checks exercise Core, process/storage boundaries, browser messages, and native WPF behavior with local fixtures. Live-site checks require an internet connection and media you have permission to download.

Keep download policy and orchestration in Core, subprocesses/storage/tool acquisition in Infrastructure, and presentation in Desktop. Browser integrations send links for review; they must not read cookies, credentials, or start hidden downloads.

Preserve English/Arabic, both themes, keyboard access, actual quality detection, and automatic media-tool preparation. Avoid personal paths, test media, secrets, generated build files, or private diagnostic logs in commits.

ViFree's source is MIT licensed. Dependencies retain their own licenses; keep their notices and required distribution terms intact. Changes on preview branches need user testing before merging or stable publication.
