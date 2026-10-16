# Vydrop 4.2.0 branding candidate

This local, unpublished build applies the approved Vydrop name, red download mark with slimmer lines, vector wordmark and matching light/dark colors to the Windows app and browser companion 1.3.0. It preserves the existing download workflows and layout. The public GitHub repository remains `ZiadHassanein/ViFree`; this work does not publish a release or rename the repository.

The canonical mark and lettering are in `src/Downloader.Desktop/Themes/Symbols.xaml`. `scripts/render-brand.ps1` exports transparent PNG, SVG lockups and the seven-size Windows icon. The final arrow shaft and tray are 5.5 units on the 64-unit canvas, with rounded ends retained. The compact lettering combines an original, larger red V with an angled terminal and original custom geometric “ydrop” shapes. `scripts/draw-brand-lettering.ps1` constructs the slim strokes, softly squared bowls, hooked y and angled terminals with a slight forward lean; no font outlines are used for the logo body. The remaining letters follow theme Ink. The logo stays physically LTR in Arabic. Its button has fixed bounds and an overlay focus ring, so clicking cannot push the form down. The Arabic audio label is now «جودة الصوت»; English remains “Audio bitrate.” [DESIGN.md](../DESIGN.md) records the implemented roles and component rules.

## Compatibility

The new executable is `Vydrop.exe`. Stable preferences, history and media-tool storage retain their existing location. Native-host names, pipe names, Chromium key/ID and Firefox add-on IDs retain their earlier values deliberately. The earlier ViFree executable needs one manual ZIP upgrade to switch names; after extraction, enable Browser integration in Vydrop to update the executable path. Reload an older unpacked companion or load the new package's `Browser/chromium` folder.

## Verification on 16 October 2026

| Check | Evidence |
| --- | --- |
| Core and infrastructure | 51 core, 36 boundary, 116 media workflow, 27 output-profile and 45 automatic-tool checks passed. |
| Browser source | 54 checks passed, including old/new channel names and valid SVG serialization of WPF path data. |
| Earlier rebrand suites | 561 checks passed before the latest lettering refinement: header 125, output details 65, format picker 141, palette 92, redesign/settings 77, subtitle modal 61. |
| Final custom lettering | 137 header and 274 window-layout checks passed for the original monoline letters beside the larger custom red V; fresh independent typography review: ship. Fixed-focus regression remains covered in both themes/languages. Earlier65 output-detail checks belong to the previous lettering iteration. |
| Motion scope | All four locale/theme logo-click stability checks passed. Broader motion run stopped at a separate native Paste-hover assertion (pointer moved, but MouseOver remained false); a full motion pass is not claimed. |
| Long combined desktop run | Incomplete: one attempt lost a native client during capture; another stopped because a native quality-popup presenter was absent. Isolated affected suites passed. This record does not certify the combined suite. |
| Visual finish | Fresh independent review shipped the scoped palette/logo rebrand and final English-dark/Arabic-light vector headers, with no material findings. Loaded-media captures use explicitly labeled synthetic fixtures. |
| Browser UI detector | One scan of popup HTML/CSS and video-overlay JavaScript returned no findings. |
| Package | App ZIP validates its single executable, manifest, version, architecture and both matching optional companions. |
| Runtime | EXE-only startup rendered with the bundled runtime and empty external .NET roots. |
| Local desktop | Packaged Vydrop 4.2.0 opened; Browser integration updated the existing stable Chromium/Firefox manifests to its executable. The existing Chromium preview compatibility alias was repointed without changing its allowlist. |
| Installed Chrome companion | Initial live page still injected the old ViFree/teal button after a reported reload. The exact Vydrop folder was supplied; the user deferred this live check. New companion handoff is not certified. |

Screenshots and logo rasters carry embedded origin metadata; [capture notes](images/capture-notes.json) distinguish current Vydrop renders, historical ViFree 4.1.0 native screenshots and labeled setup illustrations. No pointer is shown in the published image assets.

Local app ZIP SHA-256: `105F3DD3C20F0D2E2A7631DBDBEA6B0A47F5488B881B9A2D2A84BE3BF297ECAC`.

The previously published ViFree 4.1.0 ZIP remains byte-identical: SHA-256 `16DA65E3AF49D767ABD6A5359B99A6F814F723DF0C46EE9A96DB055EFF15ED05`.
