# Vydrop 4.2.0 branding candidate

This local, unpublished build applies the approved Vydrop name, red download mark with slimmer lines, vector wordmark and matching light/dark colors to the Windows app and browser companion 1.3.0. It preserves the existing download workflows and layout. The public GitHub repository remains `ZiadHassanein/ViFree`; this work does not publish a release or rename the repository.

The canonical mark and lettering are in `src/Downloader.Desktop/Themes/Symbols.xaml`. `scripts/render-brand.ps1` exports transparent PNG, SVG lockups and the seven-size Windows icon. The final arrow shaft and tray are 5.5 units on the 64-unit canvas, with rounded ends retained. The lettering combines an original angular V with licensed Inter SemiBold outlines; its ink follows the theme and its direction stays physically LTR in Arabic. [DESIGN.md](../DESIGN.md) records the implemented roles and component rules.

## Compatibility

The new executable is `Vydrop.exe`. Stable preferences, history and media-tool storage retain their existing location. Native-host names, pipe names, Chromium key/ID and Firefox add-on IDs retain their earlier values deliberately. The earlier ViFree executable needs one manual ZIP upgrade to switch names; after extraction, enable Browser integration in Vydrop to update the executable path. Reload an older unpacked companion or load the new package's `Browser/chromium` folder.

## Verification on 16 October 2026

| Check | Evidence |
| --- | --- |
| Core and infrastructure | 51 core, 36 boundary, 116 media workflow, 27 output-profile and 45 automatic-tool checks passed. |
| Browser source | 53 checks passed, including old/new companion names and unchanged channel routing. |
| Focused desktop suites | 561 checks passed: header 125, output details 65, format picker 141, palette 92, redesign/settings 77, subtitle modal 61. |
| Final slimmer logo | 125 fresh header and 274 window-layout checks passed; scoped visual review shipped the slimmer mark. |
| Long combined desktop run | Incomplete: one attempt lost a native client during capture; another stopped because a native quality-popup presenter was absent. Isolated affected suites passed. This record does not certify the combined suite. |
| Visual finish | Fresh independent review shipped the scoped palette/logo rebrand and final English-dark/Arabic-light vector headers, with no material findings. Loaded-media captures use explicitly labeled synthetic fixtures. |
| Browser UI detector | One scan of popup HTML/CSS and video-overlay JavaScript returned no findings. |
| Package | App ZIP validates its single executable, manifest, version, architecture and both matching optional companions. |
| Runtime | EXE-only startup rendered with the bundled runtime and empty external .NET roots. |
| Local desktop | Packaged Vydrop 4.2.0 opened; Browser integration updated the existing stable Chromium/Firefox manifests to its executable. The existing Chromium preview compatibility alias was repointed without changing its allowlist. |
| Installed Chrome companion | Initial live page still injected the old ViFree/teal button after a reported reload. The exact Vydrop folder was supplied; the user deferred this live check. New companion handoff is not certified. |

Screenshots and logo rasters carry embedded origin metadata; [capture notes](images/capture-notes.json) distinguish current Vydrop renders, historical ViFree 4.1.0 native screenshots and labeled setup illustrations. No pointer is shown in the published image assets.

Local app ZIP SHA-256: `653DC57DCF4F5538D320732EE6EF3EE8FCEAAA731D57254434273E73130921DB`.

The previously published ViFree 4.1.0 ZIP remains byte-identical: SHA-256 `16DA65E3AF49D767ABD6A5359B99A6F814F723DF0C46EE9A96DB055EFF15ED05`.
