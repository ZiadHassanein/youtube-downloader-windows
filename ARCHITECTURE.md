# Application structure

ViFree uses three projects. Core depends only on .NET; operating-system and third-party code stay outside it.

| Project | Owns | Dependencies |
| --- | --- | --- |
| Downloader.Core | Media contracts, immutable selections, output profiles, request snapshots, operation ownership, playlist scheduling, session queue | .NET only, target net10.0 |
| Downloader.Infrastructure | yt-dlp adapter and link routing, process containment, file validation, verified tools, storage, updates, browser transport and registration | Core, Windows APIs |
| Downloader.Desktop | WPF views, commands, presentation state, localization, themes and desktop actions | Core, Infrastructure, CommunityToolkit.Mvvm |

Open `Downloader.slnx`. Windows x64 is the shipping target. Features on this branch are preview-only; main and stable publication await the owner's tests.

## Boundaries and extension points

`IMediaProvider` owns readiness, normalization/resolution, metadata inspection and an individual download. `DownloadService` accepts this interface, acquires operation leases, snapshots requests and orchestrates media/playlist work. Additional adapters can implement that contract without depending on WPF. `IMediaMaintenance` is separate so providers need not implement tool updates. `IPlaylistStorage` receives the complete output profile when validating completion; it does not assume that all audio is MP3.

`IMediaLinkResolver` and the injectable `MediaLinkRouter` separate YouTube-specific URL/scope rules from generic public HTTP(S) URLs. The yt-dlp adapter handles supported websites; routing a URL does not prove its current extractor works. Unsupported, login, DRM and access failures remain explicit. The current baseline has YouTube/Vimeo fixtures; live evidence is separate.

`DownloadOptions.Snapshot()` is the single copy boundary for all output-affecting options and playlist entries. `MediaSelection` is immutable, bound to the canonical URL, and created only by inspected metadata. The adapter revalidates it before downloading; lost selected formats fail with refresh guidance. New uninspected queue links and playlists use quality caps independently.

`OutputProfiles` defines extension/encoding and resume identity. Caption language/source/format belongs to that identity. `CaptionMetadata` reads actual manual/automatic tracks. `MediaFiles` checks contained, nonempty allowed outputs; `MediaValidator` checks duration, container and expected streams with FFprobe. Missing subtitles report a warning and prevent that incomplete subtitle profile from being skipped on resume.

## Operation and queue ownership

`OperationCoordinator` permits one inspection, download or update at a time, holds the lease through teardown, and rejects late progress. Input generation guards prevent old metadata from replacing a newer pasted link.

`DownloadQueue` is a session-only state machine with a 100-job limit. It snapshots each job, permits one sequential dispatcher, paces jobs, retries transient transport failure once, and halts on rate limits, access/authorization/storage failures or exhausted transport retries. Explicit retry changes failed/cancelled jobs back to pending; active jobs cannot be removed. Cancellation retains pending jobs and completed media. It never restarts downloads on app launch. `DownloadService.DownloadQueueItemAsync` owns metadata resolution and download execution; the view model projects state and commands.

`PlaylistScheduler` retains selected entry order, processes one video at a time, records completed outputs privately, and revalidates files before skipping. Output-profile identity prevents incorrect skips across encodings or captions. Individual unavailable entries are counted; site/storage failures stop the playlist.

## Processes, tools and persistence

`WindowsProcessRunner` starts children suspended, assigns a kill-on-close Windows job with restricted inherited handles, then resumes. Containment failure blocks execution. Bounded output, deadlines and cancellation confirm teardown. Arguments are separate values; external yt-dlp configuration, plugins and remote components are disabled.

`DependencyManager` discovers compatible installed tools or privately acquires missing pinned yt-dlp, FFmpeg/ffprobe and Deno with hash/size checks, bounded extraction, locks and staged promotion. Global installations remain unchanged. `ToolRefresh` is an explicit idle operation: bounded official release metadata and published SHA256 identify a newer yt-dlp executable, which is verified and staged in a versioned private cache. Failed refresh retains the previous tools. FFmpeg/Deno compatibility and app-managed pins remain separate from that extractor update.

`SettingsRepository` saves atomically and recovers backups. Links and pending queue items are not persisted. Preview settings/tools/resume records use `LocalAppData/ViFreePreview`; stable data remains in its existing location. No telemetry or credential/cookie import is added.

## Browser integration

The companion requests activeTab/contextMenus/nativeMessaging only. Explicit user action sends a versioned open-link JSON message, never a download command. No content scripts, website-wide permission or local HTTP server is used.

`BrowserIdentity` holds public development IDs. `BrowserProtocol` owns strict bounded framing/schema. `BrowserHost` validates the caller and handles stdin/stdout. `BrowserLinkTransport` uses a current-user-only named pipe to the preview app. `BrowserRegistration` owns explicit per-user manifests/registry values and removes only its own values. Moving the EXE requires re-enabling the connection. Production extension-store distribution is deferred.

These restrictions do not authenticate hostile same-user software exclusively or sandbox yt-dlp network redirects. Public-link filtering is not a complete network sandbox.

## Validation and delivery

`pwsh -File scripts/check.ps1` builds/runs Core, process/recovery/storage, media workflow, WPF and browser-JavaScript fixtures. Tests cover real operation teardown, stale progress, malformed archives/checksums, queue order/retry/cancel/halt, immutable selection across inspections, output profiles and caption resume, browser framing/origins/pipe delivery, languages/themes and monitor bounds. Live checks remain separately opt-in.

The preview workflow packages a self-contained win-x64 single EXE plus notices/instructions/manifest in one ZIP, and the optional companion separately. `smoke-exe.ps1` renders from an EXE-only directory and checks the runtime is embedded in the executable's own package, retaining a host trace. This is host evidence, not clean Windows 10/11 certification.

No tag publication or release job runs on the preview workflow. No merge, stable replacement, code-signing or browser-store submission is implied. macOS/Linux, compression and scheduling are deferred.