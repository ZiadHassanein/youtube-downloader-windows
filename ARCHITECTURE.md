# Application structure

ViFree uses three projects. Core depends only on .NET; operating-system and third-party code stay outside it.

| Project | Owns | Dependencies |
| --- | --- | --- |
| Downloader.Core | Media contracts, immutable selections, output profiles, request snapshots, operation ownership, playlist scheduling, session queue | .NET only, target net10.0 |
| Downloader.Infrastructure | yt-dlp adapter and link routing, process containment, file validation, verified tools, storage, updates, browser transport and registration | Core, Windows APIs |
| Downloader.Desktop | WPF views, commands, presentation state, localization, themes and desktop actions | Core, Infrastructure, CommunityToolkit.Mvvm |

Open `Downloader.slnx`. Windows x64 is the shipping target. The current release is v4.1.0; preview builds can still isolate their data with ViFreePreview=true.

## Boundaries and extension points

`IMediaProvider` owns readiness, normalization/resolution, metadata inspection and an individual download. `DownloadService` accepts this interface, acquires operation leases, snapshots requests and orchestrates media/playlist work. Additional adapters can implement that contract without depending on WPF. `IMediaMaintenance` is separate so providers need not implement tool updates. `IPlaylistStorage` receives the complete output profile when validating completion; it does not assume that all audio is MP3.

`IMediaLinkResolver` and the injectable `MediaLinkRouter` separate YouTube-specific URL/scope rules from generic public HTTP(S) URLs. The yt-dlp adapter handles supported websites; routing a URL does not prove its current extractor works. Unsupported, login, DRM and access failures remain explicit. The current baseline has YouTube/Vimeo fixtures; live evidence is separate.

`DownloadOptions.Snapshot()` is the single copy boundary for all output-affecting options and playlist entries. `MediaSelection` is immutable, bound to the canonical URL, and created only by inspected metadata. The adapter revalidates it before downloading; lost selected formats fail with refresh guidance. New uninspected queue links and playlists use quality caps independently.

`OutputProfiles` defines extension/encoding and resume identity, including the video container and caption language/source/format. MP4, MKV and WebM choices come from compatible inspected streams; immutable selections bind both URL and container. `AudioSourceInfo` retains the selected source codec and reported bitrate estimate. Numeric conversion targets are limited by that estimate and the encoder's supported presets; an unknown bitrate offers Automatic only. Audio preflight checks the source again before downloading. A target bitrate never claims to improve source quality. When yt-dlp would preserve an already matching codec, `AudioBitrateConversion` stages the requested conversion locally, verifies it and replaces the original only after success; failure or cancellation preserves the original.

`CaptionMetadata` reads actual manual/automatic tracks. `MediaFiles` checks contained, nonempty allowed outputs; `MediaValidator` checks duration, container and expected streams with FFprobe. Missing subtitles report a warning and prevent that incomplete subtitle profile from being skipped on resume. Desktop subtitle summaries use the current selection or active queue snapshot. Successful files increment a persisted unseen count in Recent downloads, cleared when history opens; ordinary completion suppresses the redundant inline block while warnings/errors remain explicit.

## Operation and queue ownership

`OperationCoordinator` permits one inspection, download or update at a time, holds the lease through teardown, and rejects late progress. Input generation guards prevent old metadata from replacing a newer pasted link.

`DownloadQueue` is a session-only state machine with a 100-job limit. It snapshots each job, permits one sequential dispatcher, paces jobs, retries transient transport failure once, and halts on rate limits, access/authorization/storage failures or exhausted transport retries. Explicit retry changes failed/cancelled jobs back to pending; active jobs cannot be removed. Cancellation retains pending jobs and completed media. It never restarts downloads on app launch. `DownloadService.DownloadQueueItemAsync` owns metadata resolution and download execution; the view model projects state and commands.

`PlaylistScheduler` retains selected entry order, processes one video at a time, records completed outputs privately, and revalidates files before skipping. Output-profile identity prevents incorrect skips across encodings or captions. Individual unavailable entries are counted; site/storage failures stop the playlist.

## Processes, tools and persistence

`WindowsProcessRunner` starts children suspended, assigns a kill-on-close Windows job with restricted inherited handles, then resumes. Containment failure blocks execution. Bounded output, deadlines and cancellation confirm teardown. Arguments are separate values; external yt-dlp configuration, plugins and remote components are disabled.

`DependencyManager` discovers compatible installed tools or automatically acquires missing pinned yt-dlp, FFmpeg/ffprobe and Deno with hash/size checks, bounded extraction, locks and staged promotion. Global installations remain unchanged. The runtime is bundled in the EXE; tool acquisition requires internet but no consent/download/setup dialog.

`AutomaticToolUpdates` runs under the provider's existing operation lease, before the first link operation in a rolling 24-hour period. Attempts persist across launches, including offline failures; failed silent checks keep current tools. `ToolReleaseSource` validates bounded official stable release metadata and published SHA256 for yt-dlp and Deno, including Deno's Windows checksum format. `ToolUpdateClient` verifies exact sizes, hashes, executable compatibility and version before `ToolUpdateStore` atomically selects the pair. It retains previous files and can roll back without overwriting global tools. Rejected versions are not automatically reinstalled; FFmpeg remains on the verified app-managed baseline.

The adapter rebuilds its arguments and retries once after a recognized extractor/runtime failure if a newer verified set or retained baseline is available. Further compatible failure rolls back and reports that a working upstream fix may be unavailable. Access/authorization/rate limits, DRM, unavailable formats, storage and transport failures do not trigger this recovery. Exhausted recovery halts playlists and session queues without discarding pending jobs. Automatic checks are anonymous public GitHub requests with bounded timeouts; no token, private API or paid service is used. Settings retains an explicit idle check using the same updater.

`SettingsRepository` saves atomically and recovers backups. Links and pending queue items are not persisted. Preview settings/tools/resume records use `LocalAppData/ViFreePreview`; stable data remains in its existing location. No telemetry or credential/cookie import is added.

## Browser integration

The companion requests activeTab/scripting/contextMenus/nativeMessaging. Explicit user action on Facebook/TikTok injects a self-contained read-only detector into the main frame's isolated world. It ranks visible playing media, excludes hidden/preloaded players, supports TikTok canvas rendering, and associates the selected player with a local permalink or public player identifier. It never reads cookies, credentials or private application state. Ambiguous/missing media produces recovery guidance rather than a homepage or unrelated link. Other sites retain page forwarding; explicit clicked links retain their target. No persistent content scripts, website-wide host permission or runtime local HTTP server is used. The opt-in development source viewer is loopback-only and is not shipped.

Only a versioned open-link JSON message reaches the native host, never a download command. Discovery does not establish that the website permits an anonymous download. Popup and page/video context-menu paths share detection and transport; preview/stable identities remain separate.

`BrowserIdentity` holds public development IDs. `BrowserProtocol` owns strict bounded framing/schema. `BrowserHost` validates the caller and handles stdin/stdout. `BrowserLinkTransport` uses a current-user-only named pipe to the preview app. `BrowserRegistration` owns explicit per-user manifests/registry values and removes only its own values. Moving the EXE requires re-enabling the connection. Production extension-store distribution is deferred.

These restrictions do not authenticate hostile same-user software exclusively or sandbox yt-dlp network redirects. Public-link filtering is not a complete network sandbox.

## Validation and delivery

`pwsh -File scripts/check.ps1` builds/runs Core, process/recovery/storage, media workflow, WPF and browser-JavaScript fixtures. Tests cover real operation teardown, stale progress, malformed archives/checksums, queue order/retry/cancel/halt, immutable selection across inspections, output profiles and caption resume, browser framing/origins/pipe delivery, languages/themes and monitor bounds. Live checks remain separately opt-in.

The Windows workflow packages a self-contained win-x64 EXE plus notices/instructions/manifest and optional Browser companions in one ZIP. Updates verify the published GitHub asset SHA256 digest (with legacy checksum-asset fallback) and replace only app files. `smoke-exe.ps1` renders from an EXE-only directory and checks the runtime is embedded in the executable's own package, retaining a host trace. This is host evidence, not clean Windows 10/11 certification.

The build workflow uploads a test artifact. Release publishing requires an explicit version tag and an already-merged source commit; it uploads only the single app ZIP. Code-signing and browser-store submission remain separate. macOS/Linux, compression and scheduling are deferred.
