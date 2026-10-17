# Review fixes and validation — 17 October 2026

These source fixes and documentation changes are based on `7527095` and form part of Vydrop 4.2.1. The checks below record their initial pre-release validation; the Vydrop 4.2.0 ZIP has not been replaced. The supplied review brief is preserved unchanged. Documentation publishing is handled separately by the checked GitHub Pages workflow on `main`.

## Findings addressed

| Brief item | Result |
| --- | --- |
| F1 | An incompatible or unverifiable app release now offers a bilingual manual-download notice/action. Network failures retain the connection retry message. Already installed ViFree 4.1.0 binaries are unchanged. |
| F2 | Repository slug compatibility is documented in code and CONTRIBUTING. API URLs and browser identities are unchanged. |
| F3 | A later startup prunes validated committed/rolled-back stages after helper exit and week-old abandoned GUID stages. Recovery, live-helper, malformed, recent and unowned stages are preserved. Cleanup stays under the exact owned root and does not follow reparse points. |
| F4 | Tool promotion/rollback prunes obsolete owned versions while preserving active/previous selections. Old owned quarantine folders are pruned under the preparation lock. |
| F5 | Architecture now accurately describes HTTP(S) content scripts, selected-player detection and stable/preview transport. |
| F6 | Single queued downloads reuse their immediate metadata inspection. Main-window selections still revalidate; playlist entries clear collection metadata and inspect their own streams. |
| F7 | Native delivery distinguishes no listener, busy rejection and acceptance. A busy app does not cause another launch/retry. The busy code reaches localized overlay feedback. |
| F8 | Pipe construction is inside the retry handler; access/IO failures back off and recover. Cancellation stops the loop. |
| F9 | Current publisher-account and PATH trust boundaries are documented in SECURITY. Independent signing and PATH-policy changes remain owner decisions. |
| F10 | Theme dimensions allow one physical pixel of layout rounding. Popup tests establish and settle focus before synthetic opening so queued focus restoration does not close a newly opened menu. |
| F11 | Media validation uses an explicit absolute file input. Expected IO/Win32 process-stop failures cannot escape a cancellation callback. |

## Documentation and search work

- README leads with current native UI images and multi-site scope. Product facts, FAQs, version, citation and crawler hints are checked for consistency.
- Sixteen English/Arabic Jekyll pages cover installation, supported sites, public web video, TikTok/Facebook, MP3, playlists and SRT/VTT captions.
- Pages have bounded titles/descriptions, canonical URLs, reciprocal language alternates, Open Graph artwork, a sitemap and matching visible FAQ/SoftwareApplication JSON-LD.
- The existing GitHub setup guide keeps working relative Markdown links. A GitHub Pages-supported plugin rewrites document links for the built site.
- Four native WPF client renders are 724 × 761 at 96 DPI, under 45 KB each, with no pointer. They show the current source interface in an empty state with fixture services, not a live-site session or a screenshot of the published executable. Browser-step diagrams remain labelled illustrations. Provenance is in `images/capture-notes.json` and PNG metadata.
- Added changelog, bilingual issue prompts, security guidance, press drafts, metadata/topic suggestions and a manual visibility log. No analytics, third-party scripts, account connections or recurring automation were added.
- Independent documentation review marked four corrections resolved: semantic comparison tables, Arabic Markdown rendering, the exact Arabic connection label and GitHub-compatible links. This is a verdict on those corrections, not a live search-indexing certification.

## Checks

| Check | Evidence/result |
| --- | --- |
| Full `scripts/check.ps1` at native 125% scale | Passed: 51 core, 36 boundary, 116 workflow, 27 output-profile, 45 automatic-tool, 12 initial review-regression, 1,702 WPF and 75 browser/docs checks. |
| Latest core/infrastructure run | Passed: 51 core, 36 boundary, 116 workflow, 27 output-profile, 46 automatic-tool and 13 review regression checks, including promotion pruning and playlist metadata isolation. |
| 100% WPF compatibility | Passed: 1,703 checks, including an assertion that the process actually used DPI scale 1. The focused header run also passed 154 checks. Windows compatibility is applied before child startup so native popup threads share the same DPI context. |
| Jekyll production build | Passed with Windows and Linux platforms locked. |
| Built-site validation | All 16 pages: metadata, local links/anchors, language alternates, RTL, semantic tables, JSON-LD, assets and sitemap passed. |
| Headless browser | 32 page/viewport/theme combinations passed; no document overflow, missing images or browser errors. All 12 EN/AR home/setup captures were independently reviewed. |
| Browser and docs source tests | 75/75 passed. |
| Real direct media | W3Schools public `mov_bbb.mp4` inspected/downloaded/validated: 176p, measured AAC estimate 160.545 kbps, 788,493 bytes. This does not certify other sites. |
| Real tool discovery/update endpoints | All four cached official tools revalidated; a second preparation made no acquisition. Anonymous yt-dlp/Deno release/checksum checks passed and matched official digests. |
| Final review package | Passed: EXE-only bundled-runtime startup, complete app/companion manifest, native-host framing, accepted delivery, busy rejection and unapproved-origin rejection. This local ZIP contains the final code; it has not been published. |

Native popup tests require an interactive Windows desktop. A trial on a private invisible desktop could not support native focus restoration and was discarded; no private-desktop launcher is shipped. A thread-only DPI override also mixed popup coordinate systems and was replaced with process-start compatibility. Its launcher uses an explicit child marker because Windows consumes the environment flag; a restart-loop trial was stopped and all test processes exited. Display scaling is changed only for the test process, never in the user's settings. No desktop computer controls were used.

The final smoke attempt initially ran out of disk space while copying its temporary executable. Only this run's temporary captures, smoke copies and documentation toolchain/cache were removed. The retry passed. The smoke script now removes its full executable copy while retaining small startup/host-trace receipts, avoiding another large copy on every run.

Local, unpublished review ZIP: `dist/Vydrop-v4.2.0-win-x64.zip`, retaining the existing version stamp. SHA-256: `00fd8e337a0b97a920b8d5c5915efb5180d32b68f6223e851cbaff55df2670cd`. This is not the published 4.2.0 asset's hash. Temporary-toolchain cleanup completed; the source, native UI images, built site, review ZIP and validation logs remain.

## Owner handoff

### Vydrop 4.2.1 follow-up

The owner subsequently authorized publishing the fixed app as v4.2.1. Claude's changes were reviewed before release preparation: completed Arabic error coverage, localized companion popup/context menus, abandoned prepared-stage cleanup, recommended analyzers with warnings as errors, culture-safe comparisons/parsing, namespace consistency and line-ending rules. The cleanup regression also retains week-old preparations while their parent process is alive. The companion version is 1.3.1; native-host names and extension IDs/key remain unchanged.

Claude's remaining social-image suggestion is addressed by an original 1200 × 630 card rendered from the approved vector logo. GitHub's existing release counters are exposed through cached Shields.io badges and `scripts/download-counts.ps1`. They measure release-asset download events, including repeats and automation, not unique people or app usage. No app telemetry or paid service is added. The release workflow verifies the package and uploaded digest before publication; Pages waits for the documented Windows release before deploying new download links.

The owner authorized pushing the source fixes and documentation and publishing the Pages site on 17 October 2026. The Documentation site workflow builds and validates the site before deployment; the public URL is `https://ziadhassanein.github.io/ViFree/`, with Arabic under `/ar/`. This publication does not create or replace a desktop release.

A ViFree bridge release or pinned migration notice, signing policy and external directory submissions remain separate owner decisions. The maintenance instructions are in `seo-maintenance.md`; press text is in `press.md`. Ignored `build/winget/` contains verified-hash drafts, explicitly not submission-ready until Browser-folder preservation and a clean package-manager install are validated. Existing native-host names, extension IDs/key and the `ViFree` repository slug remain intact.

Search engines determine ranking. llms.txt, crawler allowances and structured data help describe facts; they do not guarantee indexing, citation, FAQ rich results or first place.

## ملخص بالعربية

أُصلحت مشكلات تراكم التحديثات وتكرار فحص عناصر التنزيل والربط بالمتصفح ورسائل التحديث واختبارات مقياس العرض، مع حماية ملفات الاستعادة والأدوات النشطة. أُضيفت أدلة عربية وإنجليزية وصور أصلية للواجهة ومعلومات منتج موحّدة. وافق المالك على رفع الكود والأدلة ونشر الموقع عبر GitHub Pages بعد الفحوص. لم يُستبدل ملف التطبيق المنشور؛ التوقيع والإصدار الوسيط والتقديم إلى الأدلة الخارجية قرارات منفصلة، ولا توجد وعود بترتيب محدد في البحث.
