# ViFree 4.1.0 Preview 30

## Stable promotion

Preview 30 was promoted to stable **v4.1.0** on 16 October 2026. Source, [releases and Windows downloads](https://github.com/ZiadHassanein/Vydrop/releases/tag/v4.1.0), support and future app-update checks use [ZiadHassanein/ViFree](https://github.com/ZiadHassanein/ViFree). The promotion changes version/channel identity and the release destination, preserving the accepted interface and download behavior. Older versions that check the separate downloads repository require one manual ZIP upgrade; preview preferences remain separate from stable preferences.

The [tagged release workflow](https://github.com/ZiadHassanein/Vydrop/actions/runs/38933637137) passed full regression, package, bundled-runtime startup and native-host checks before publishing. The public 61,902,718-byte ZIP was independently downloaded and passed package validation; SHA-256 `16DA65E3AF49D767ABD6A5359B99A6F814F723DF0C46EE9A96DB055EFF15ED05`. Its source tag points to `8f3a272849142bbb4554c147ef626c37feccb48f`. Clean Windows VM compatibility remains unverified.

## Preview acceptance and validation history

Preview 30 was accepted for merge into main on 16 October 2026. This acceptance included browser companion 1.2.1, the direct-media metadata correction and the Settings correction. At that acceptance, the public stable release was 4.0.0 and the source merge did not publish a release.

- The playing-video tab reads **Download by ViFree** and stays outside the picture, above its physical top-right corner or in clear space immediately to the right. Its explicit click copies the selected video's resolved public link and opens ViFree. Website access does not guarantee every player or protected video is supported.
- Incomplete Generic direct-file metadata is enriched by a bounded FFprobe check of the same resource. Quality choices use measured codecs, dimensions and source audio bitrate. The W3Schools sample passed inspection, an actual 788,493-byte download and saved-file validation at 320 × 176 with H.264/AAC.
- Settings excludes media errors, progress and completion results while retaining its own update and downloader-support notices. Returning to the download screen preserves its status and cancellation controls. Selected-menu checkmarks stay physically left in both languages.

Validation: 52 browser/package checks; 51 core, 36 boundary, 116 media-workflow, 27 output-profile and 45 automatic-tool checks, plus service/reliability checks; 77 focused WPF redesign and 63 maintenance checks. Real installed companion handoffs passed for Facebook and W3Schools. Installed Preview 30 was opened and its video-error exclusion was verified in English and Arabic with light and dark themes. Package validation and EXE-only startup passed. These are current-host checks; clean Windows VM compatibility and universal website support are not certified.

Accepted desktop ZIP: `ViFree-v4.1.0-preview.30-win-x64.zip`, SHA-256 `EE3ED9FBF7C6FE9206473FB0FB223B06536EEE3004537F713808C4347E07C777`. Included companion version: 1.2.1. Rebuild with `./scripts/package.ps1 -Version 4.1.0-preview.30 -Preview`.

## Preview 27 validation history

Preview 27 was accepted and merged into main. The desktop ZIP is unchanged. A separate browser companion 1.1.0 candidate adds click-triggered Facebook/TikTok player detection; its source remains on `fix/browser-active-video` for review. Extract the companion into a permanent folder, reload/load its `chromium` folder, and keep Preview 27's Browser integration enabled. Detection needs no account import or paid service. TikTok's actual feed was checked across two videos; Facebook returned a login wall, so signed-in feed detection remains unverified.

Unreleased preview with source-aware output choices and clearer download feedback. Stable remains 4.0.0; owner testing is pending.

Extract the ZIP and open **ViFree.exe**. Its runtime is bundled. Missing media tools download automatically on the first link; internet is required. No account, API key, paid service or manual setup is needed. Preview data stays separate; pending queue links are not saved. App-version updates are disabled.

Try:

- Paste a video, Short or playlist. The window expands right for thumbnail, details and progress; New download stays first in the form.
- Finish a download or matching queue: the window returns to its original width. Cancellation keeps context for retry. Manual resizing takes precedence.
- Open a completed queue and use its per-item Open folder action.
- Try a YouTube Mix: up to 50 videos are inspected, with the starting video preserved.
- Check both languages/themes, output options, menus, recent history and the existing silent animations.
- Finish a download: the Recent downloads badge counts unseen files; opening history clears it. Routine success keeps the main Show in folder action without a duplicate completion block.
- Choose Subtitles beside quality. Its outlined button highlights when enabled; the details panel shows the selected language and SRT/VTT format. CC lettering stays readable in Arabic.
- Select MP4, MKV or WebM from Video. Inspection offers only containers with compatible streams.
- Select audio conversion bitrates within the reported source estimate. Automatic remains available when that estimate is unknown; WAV/FLAC have no bitrate setting. Conversion cannot improve source quality.

Validation passed: 1,637 full WPF checks plus 65 final output-detail checks, 51 core, 36 boundary, 116 media workflow, 20 output-profile, 45 automatic-tool and four browser tests. Real FFmpeg checks verified lower-bitrate MP3/AAC/Opus conversions and three actual video containers; anonymous YouTube downloads passed in MP4, MKV and WebM. Clean Windows 10/11 and actual extension loading remain unverified. Nothing is merged or published.
