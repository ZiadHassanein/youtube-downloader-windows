# Application structure

The desktop app uses WPF on .NET 10 and CommunityToolkit.Mvvm. The download engine, operating-system services and interface are separate projects.

| Project | Responsibility | Dependencies |
| --- | --- | --- |
| `Downloader.Core` | Provider contract, operation ownership, request snapshots, playlist scheduling and domain models | .NET only; platform-neutral |
| `Downloader.Infrastructure` | YouTube provider, native process containment, media validation, tool preparation, persistence and update transactions | Core, Windows APIs |
| `Downloader.Desktop` | WPF views, themes, localization, view models, dialogs and desktop actions | Core, Infrastructure, CommunityToolkit.Mvvm |

Open `Downloader.slnx` for the full solution. Features belong in their owning module rather than the window's code-behind. Window code handles native presentation, content sizing and lifecycle; view models coordinate commands. Normal sizing fits content within the current monitor's work area. Maximization uses Windows bounds with a centered width limit and bottom footer; minimization preserves restore bounds. Each new provider must implement `IMediaProvider`, including URL resolution and normalization, readiness, inspection and item downloading. The current adapter accepts YouTube links only. Multi-provider routing and browser extensions have not been implemented.

## Operation lifecycle

`OperationCoordinator` permits one inspection, download or update at a time. Its lease remains held through process teardown. Changing the pasted input cancels pending inspection; the view model checks input generation and operation identity before accepting results or progress. Providers receive a separate progress sink for each call. Core stamps progress with the source lease and rejects expired callbacks.

`DownloadService` snapshots a request before asynchronous work. Single-video quality keys are bound to inspected URLs by the YouTube provider. Playlist resolution choices are per-item maximums. The sequential playlist scheduler records unavailable entries and continues, waits between requests, and stops on rate-limit, connection, storage or repeated unexpected failures. Cancellation preserves completed media and resumable partial files.

## Native processes and tools

`WindowsProcessRunner` creates a process suspended, supplies a restricted inherited-handle list, assigns it to a kill-on-close Windows job, and only then resumes execution. Containment failure prevents execution. Output is drained with bounds; timeouts and cancellation terminate the job and check that no active children remain. Completed media must be inside the chosen folder and pass an FFprobe container/stream/duration check.

`DependencyManager` checks compatible private, app-local and installed executables without changing them. FFmpeg and FFprobe must be a coherent pair. Only missing component groups are acquired. `ToolManifest` pins official source URLs, versions, sizes, hashes and extraction limits. HTTPS origins and redirects are restricted; archives reject traversal, aliases, duplicate names and links. A private staging directory becomes active only after integrity, extraction and capability checks pass. A preparation lock prevents duplicate activation.

The executable includes its .NET runtime. Media-tool binaries remain vendor downloads and are privately prepared on first use. Their license/source notices are embedded and materialized for EXE-only use. `THIRD-PARTY-NOTICES.txt` records versions, licenses and vendor source/build links. Updating pinned tools requires reviewing that manifest and the notices together.

## Persistence

`SettingsRepository` stores validated preferences and a bounded recent-file history in LocalAppData. Writes are flushed and atomically replaced with a backup. A damaged primary can recover from the backup without overwriting it; importing relocated settings retains the source. Pasted URLs are never persisted.

ViFree keeps the existing `LocalAppData/YouTubeDownloader` data identity and desktop mutex. The public name and executable change to ViFree; settings, history, resume records and prepared tools remain available. Older YouTube Downloader copies require one manual ZIP download for this rename. Subsequent ViFree releases use the verified manual updater.

Playlist resume records live in private app data and are keyed by normalized destination, playlist and output profile. Completed media is rechecked before skipping an item. Legacy records found beside media are migrated privately. Existing downloaded files and global tool installations are retained.

## Manual updates and releases

Updates start only from an explicit user command. A downloaded release must match its SHA-256, four package files, manifest architecture, file hashes and executable version. The native package uses a distinct ZIP/checksum naming scheme; migration from the previous Framework app requires one manual ZIP download.

Before closing the app, replacement access is checked for the actual executable and companions. Protected files leave the app running with a manual download action. The helper waits for the original process, makes durable backups before replacement, and writes a private transaction journal. A new process must confirm startup with the journal identity and its PID/start time. Only then is the update committed and process containment released. Startup failure stops the candidate tree before restoration. Failed recovery retains backups and a recovery record; interrupted transactions are recovered by an external helper on a later launch.

The Windows workflow builds, runs fixture checks, publishes one portable ZIP and performs an EXE-only render check with an empty runtime root. Release publishing requires an explicit version tag whose commit is already on `main`; the build must pass first. A draft release is made public only after the ZIP and machine-readable checksum upload successfully. Branch builds do not publish releases. No signing certificate or Store package is included.

## Verification

- Core fixtures cover formats, safe arguments, queue counting, resume, migration and stop conditions.
- Native process fixtures exercise real child cancellation, descendants, containment failure and bounded probes.
- Boundary checks exercise malformed archives, bad hashes/origins, partial tool reuse, protected targets, durable rollback and startup acknowledgement.
- Service checks exercise mixed available/unavailable playlists and delayed callbacks across operations.
- WPF checks cover both languages and themes, minimum width, automatic inspection, quality choices, cancellation, expanded content and transient notice bounds.
- Optional live checks use a small sample and playlist metadata; they are separate from deterministic CI.

The target is Windows 10/11 x64. A self-contained host startup test does not prove clean-machine compatibility on every PC. Additional DPI settings, ARM64, clean Windows 10/11 images, real screen readers and a live high-contrast session remain separate validation work.
