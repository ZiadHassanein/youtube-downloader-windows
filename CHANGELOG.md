# Changelog

All notable user-facing changes are recorded here. Use the versioned release links below for published Windows packages.

## [Unreleased]

### Changed
- Use the owner-approved Vydrop repository and Pages URLs. The canonical app-update endpoint is prepared in source; installed versions through 4.2.1 need manual app updates after the rename.
- Use a compact download button with the release-download count directly underneath on GitHub and the documentation site.

## [4.2.1] - 2026-10-17

### Fixed
- Clean completed app-update stages on a later startup while preserving recovery and active-helper stages.
- Retain active and previous media-tool selections and prune older owned versions and old quarantine folders.
- Reuse metadata inspected during a queued download; keep revalidation for older main-window choices.
- Report a busy browser connection without launching another app; retry listener construction failures.
- Offer manual release download for an incompatible or unverifiable update instead of blaming the connection.
- Make theme-button test dimensions tolerate physical-pixel rounding at fractional display scales.
- Use explicit file inputs for media validation and contain expected process-stop errors during cancellation.
- Translate the link, queue and update error messages that still appeared in English in the Arabic interface, and drop stale translations left over from earlier versions.
- Remove abandoned update preparations older than a week once no helper or parent process remains.
- Show the browser companion's popup and context-menu entries in Arabic when the browser interface language is Arabic.

### Added
- Bilingual multi-site guides, product facts, crawler hints, structured data, issue templates and a published documentation site.
- A localization test that keeps every interface string and user-facing error message translated.
- A 1200 × 630 social-sharing card using the approved logo.
- GitHub release download badges and a free per-version ZIP counter, without app telemetry. Counts include repeat and automated downloads.

### Changed
- Builds run the recommended .NET analyzers and code-style rules with warnings treated as errors; sources use file-scoped namespaces and an explicit line-ending policy.

## [4.2.0] - 2026-10-17

### Changed
- Renamed the product to Vydrop with a red V/download mark, matching light/dark wordmarks and V-only app and browser icons.
- Added whole-wordmark hover shine, a stable theme button and matching contact-menu icon rows.
- Included browser companion 1.3.0 and illustrated Chrome, Edge and Firefox setup documentation.
- Upgrading from ViFree 4.1.0 requires a one-time manual extraction to a new writable folder and re-enabling browser integration.

## [4.1.0]

### Added
- ViFree maintenance with daily verified yt-dlp/Deno checks, retained previous tools and one compatible update-and-retry.
- Multi-site public links, actual output profiles, available subtitles and optional browser handoff.
- Settings feedback separation and a compact bilingual interface.

See the published release bodies for historical validation and download details.

[Unreleased]: https://github.com/ZiadHassanein/Vydrop/compare/v4.2.1...HEAD
[4.2.1]: https://github.com/ZiadHassanein/Vydrop/releases/tag/v4.2.1
[4.2.0]: https://github.com/ZiadHassanein/Vydrop/releases/tag/v4.2.0
[4.1.0]: https://github.com/ZiadHassanein/Vydrop/releases/tag/v4.1.0

## ملخص بالعربية

إصلاحات الإصدار 4.2.1 تقلّل تراكم ملفات التحديث وفحوص الوسائط المكررة، وتحسّن الربط بالمتصفح ورسائل التحديث واختبارات مقياس العرض، وتُترجم رسائل الأخطاء التي كانت تظهر بالإنجليزية في الواجهة العربية. أُضيفت أدلة عربية وإنجليزية ومعلومات منتج موحّدة وموقع منشور عبر GitHub Pages. تتضمن النسخة 4.2.1 هوية Vydrop الحمراء والإضافة 1.3.1، وتستلزم الترقية من ViFree 4.1.0 فك الضغط يدويًا مرة واحدة.
