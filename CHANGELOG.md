# Changelog

All notable user-facing changes are recorded here. Unreleased fixes are not included in the published 4.2.0 ZIP.

## [Unreleased]

### Fixed
- Clean completed app-update stages on a later startup while preserving recovery and active-helper stages.
- Retain active and previous media-tool selections and prune older owned versions and old quarantine folders.
- Reuse metadata inspected during a queued download; keep revalidation for older main-window choices.
- Report a busy browser connection without launching another app; retry listener construction failures.
- Offer manual release download for an incompatible or unverifiable update instead of blaming the connection.
- Make theme-button test dimensions tolerate physical-pixel rounding at fractional display scales.
- Use explicit file inputs for media validation and contain expected process-stop errors during cancellation.

### Added
- Bilingual multi-site guides, product facts, crawler hints, structured data, issue templates and a Pages-ready site.

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

[Unreleased]: https://github.com/ZiadHassanein/ViFree/compare/v4.2.0...HEAD
[4.2.0]: https://github.com/ZiadHassanein/ViFree/releases/tag/v4.2.0
[4.1.0]: https://github.com/ZiadHassanein/ViFree/releases/tag/v4.1.0

## ملخص بالعربية

الإصلاحات غير المنشورة تقلّل تراكم ملفات التحديث وفحوص الوسائط المكررة، وتحسّن الربط بالمتصفح ورسائل التحديث واختبارات مقياس العرض. أُضيفت أدلة عربية وإنجليزية ومعلومات منتج موحّدة وموقع جاهز لـGitHub Pages. تتضمن النسخة المنشورة 4.2.0 هوية Vydrop الحمراء والإضافة 1.3.0، وتستلزم الترقية من ViFree 4.1.0 فك الضغط يدويًا مرة واحدة.
