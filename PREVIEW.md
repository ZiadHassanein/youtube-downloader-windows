# ViFree 4.1.0 Preview 27

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
