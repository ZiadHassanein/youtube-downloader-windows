# Vydrop 4.2.0 branding candidate

This source build applies the approved Vydrop name, red download mark with slimmer lines, vector wordmark and matching light/dark colors to the Windows app and browser companion 1.3.0. It preserves the existing download workflows and layout. The approved source and documentation are published to `ZiadHassanein/ViFree`; the downloadable release remains ViFree 4.1.0. This work does not publish a new binary release or rename the repository.

Use the approved integrated angular wordmark: the symmetrical red V/down-arrow emblem is the first letter, followed by geometric ydrop in theme Ink. There is no separate symbol before the word. The V has equal mirrored arms, a centered negative-space arrow and a sharp lower point. Canonical contours are in assets/brand/wordmark.geometry.json, with the selected reference in assets/brand/approved-reference.png; scripts/draw-brand-lettering.ps1 applies them to Symbols.xaml. The 966 × 284-unit source fits a 128 × 38-DIP Viewbox inside the existing 42-DIP fixed-height button. Preserve physical LTR order in Arabic and the overlay focus ring. Light mode uses charcoal ydrop; dark mode uses light ydrop; BrandFill stays red. Taskbar, window, standalone app and extension icons contain only the V, from BrandMarkGeometry on its 64-unit canvas. Retain Inter for interface text and its existing license. scripts/render-brand.ps1 exports both theme variants and V-only app/browser icons. The Arabic audio label remains «جودة الصوت».

## Compatibility

The new executable is `Vydrop.exe`. Stable preferences, history and media-tool storage retain their existing location. Native-host names, pipe names, Chromium key/ID and Firefox add-on IDs retain their earlier values deliberately. The earlier ViFree executable needs one manual ZIP upgrade to switch names; after extraction, enable Browser integration in Vydrop to update the executable path. Reload an older unpacked companion or load the new package's `Browser/chromium` folder.

## Verification on 16 October 2026

| Check | Evidence |
| --- | --- |
| Core and infrastructure | 51 core, 36 boundary, 116 media workflow, 27 output-profile and 45 automatic-tool checks passed. |
| Browser source | 54 checks passed, including old/new channel names and valid SVG serialization of WPF path data. |
| Earlier rebrand suites | 561 checks passed before the latest lettering refinement: header 125, output details 65, format picker 141, palette 92, redesign/settings 77, subtitle modal 61. |
| Previous custom lettering | 137 header and 274 window-layout checks passed for the original monoline letters beside the larger custom red V; fresh independent typography review: ship. Fixed-focus regression remains covered in both themes/languages. Earlier65 output-detail checks belong to the previous lettering iteration. |
| Motion scope | All four locale/theme logo-click stability checks passed. Broader motion run stopped at a separate native Paste-hover assertion (pointer moved, but MouseOver remained false); a full motion pass is not claimed. |
| Long combined desktop run | Incomplete: one attempt lost a native client during capture; another stopped because a native quality-popup presenter was absent. Isolated affected suites passed. This record does not certify the combined suite. |
| Visual finish | Fresh independent review shipped the scoped palette/logo rebrand and final English-dark/Arabic-light vector headers, with no material findings. Loaded-media captures use explicitly labeled synthetic fixtures. |
| Browser UI detector | One scan of popup HTML/CSS and video-overlay JavaScript returned no findings. |
| Package | App ZIP validates its single executable, manifest, version, architecture and both matching optional companions. |
| Runtime | EXE-only startup rendered with the bundled runtime and empty external .NET roots. |
| Local desktop | Packaged Vydrop 4.2.0 opened; Browser integration updated the existing stable Chromium/Firefox manifests to its executable. The existing Chromium preview compatibility alias was repointed without changing its allowlist. |
| Installed Chrome companion | Initial live page still injected the old ViFree/teal button after a reported reload. The exact Vydrop folder was supplied; the user deferred this live check. New companion handoff is not certified. |

Screenshots and logo rasters carry embedded origin metadata; [capture notes](images/capture-notes.json) distinguish current Vydrop renders, historical ViFree 4.1.0 native screenshots and labeled setup illustrations. No pointer is shown in the published image assets.

Local app ZIP SHA-256: `75A75F5D1BACD7AAF3AA1D40B61933770F08561453DF4A996A03DF6398FD9EFC`.

The previously published ViFree 4.1.0 ZIP remains byte-identical: SHA-256 `16DA65E3AF49D767ABD6A5359B99A6F814F723DF0C46EE9A96DB055EFF15ED05`.

Current selected angular wordmark: approved reference faithfully converted to shared vector contours. The V is defined with exactly mirrored vertices and a sharp point; its 512-pixel raster exhibits normal antialias edge differences, so pixel-perfect raster symmetry is not claimed. Offline light/dark exports were inspected together. The app and companion use the same shapes; native visual verification remains with the user, without computer controls. Earlier native suite counts apply to earlier artwork.

Selected-logo validation: 54 browser source checks passed; the app and updated WPF test project compile with zero errors; package artifact validation passed. Existing icon assertions now cover the V-only silhouette and negative-space arrow. Native WPF interaction suites were not rerun, honoring the user request for screenshot-based visual checking without computer controls. Both cursor-free theme previews were inspected from the offline vector exports.

GitHub documentation refinement: The arrow inside the V has a white outline that is open at the top, with no horizontal cap. Its wordmark stroke is 7 source units and the V-only icon uses the proportional 1.562-unit stroke. README opens with labelled current-theme illustrations; separate Chrome, Edge and Firefox setup images accompany the guide, with temporary Firefox installation clearly documented. Official browser sources were checked; GitHub latest-release API confirms v4.1.0, so public links remain on that published package while 4.2 source branding is presented separately. No new native screenshots are claimed.

Approved GitHub update: final open-top thin-outline source passed 54 browser checks, local-reference/image validation, portable app build and package artifact validation. The public v4.1.0 ZIP returned HTTP 200. Both theme exports and all three browser setup illustrations received a bounded offline visual review. New native screenshots and full local WPF interaction reruns are not claimed.

GitHub verification follow-up: the initial main-branch run passed the core/infrastructure suites and reached the motion checks, where an obsolete test cast the fixed BrandArtwork container to ScaleTransform. The test now reads the actual animated BrandInitial transform and checks that both axes settle to 1. The application artwork and animation are unchanged. Local verification compiles the test project without opening or controlling the user's desktop; the full native run executes on the GitHub Windows runner.

Wider outline refinement: the user's follow-up increases the white arrow stroke from 4.5 to 7 source units (1.004 to 1.562 in the 64-unit icon), retaining the open top, sharp symmetric V and existing wordmark spacing. Both theme exports were reviewed together; browser checks passed 54/54, the changed companion design scan returned no findings, and the rebuilt app ZIP and documentation references passed validation.
