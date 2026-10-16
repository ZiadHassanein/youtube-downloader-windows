<p align="center"><picture><source media="(prefers-color-scheme: dark)" srcset="assets/brand-logo-dark.svg"><img src="assets/brand-logo-light.svg" width="238" alt="Vydrop video downloader logo"></picture></p>

# Vydrop — Free Video & Audio Downloader for Windows

**Vydrop (formerly ViFree)** is a free, open-source Windows desktop app by **Ziad Hassanein** for downloading YouTube videos, Shorts and playlists, saving audio, and organizing multiple downloads. It supports English and Arabic interfaces, light and dark themes, subtitles, and an optional browser extension. Other public websites depend on yt-dlp support and access.

**[Download the latest published Windows release — ViFree v4.1.0 (.zip)](https://github.com/ZiadHassanein/ViFree/releases/download/v4.1.0/ViFree-v4.1.0-win-x64.zip)**

[Setup guide](#getting-started) · [App screenshots](#screenshots) · [Extension setup with pictures](docs/browser-extension-setup.md) · [Report a download problem](https://github.com/ZiadHassanein/ViFree/issues)

## Releases and current source

The current source builds **Vydrop 4.2.0**, with a red download logo, matching light/dark colors and companion **1.3.0**. This branding build is prepared locally for review; it has not been published as a GitHub release yet. The download above remains the previously published ViFree 4.1.0.

| Channel | Version | What to use |
| --- | --- | --- |
| Current source | Vydrop 4.2.0 | Red branding build; not yet a published release. |
| Latest published release | ViFree 4.1.0 | The Windows ZIP linked above. |
| Accepted preview baseline | 4.1.0 Preview 30 | Promoted to stable 4.1.0; see [Preview 30 validation](PREVIEW.md). |

This is the official repository for Vydrop source code, Windows downloads, release notes and support. The source is MIT licensed.

## Video, audio and download features

Vydrop retains these download features:

| Task | Support |
| --- | --- |
| Download video | YouTube videos, Shorts and playlists; MP4, MKV and WebM when inspected streams fit the chosen container. |
| Save audio | MP3, M4A, Opus, WAV and FLAC. Conversion bitrate choices respect the reported source estimate; unknown sources offer Automatic. |
| Manage playlists and queues | Choose playlist entries, add individual links to a queue, follow progress and open completed files in their folder. |
| Save subtitles | Choose an available subtitle language and SRT or VTT format. The media details show the selection. |
| Review a link | Preview title, duration, artwork and measured quality before downloading. |
| Use your language | English/LTR and Arabic/RTL, light and dark themes, and keyboard-accessible controls. |
| Prepare media tools | Reuse compatible installed tools or automatically download and verify missing yt-dlp, FFmpeg/FFprobe and Deno tools. |

Quality depends on the source. A higher output bitrate or a lossless file does not restore detail missing from the original audio. Video choices reflect inspected or measured formats.

## Browser extension: Download by Vydrop

The optional **Chrome, Microsoft Edge and Firefox** companion is included in the app ZIP's `Browser` folder. Companion **1.3.0**, included with the Vydrop build, shows a **Download by Vydrop** tab outside a playing video's top-right border. Clicking it copies the resolved link and opens Vydrop for review; downloading still requires your action in the app.

Facebook, TikTok and YouTube detection uses the selected player's public permalink. Other HTML5 players may provide a direct media URL or a single-video page link. Available space, custom players and site restrictions can limit detection. Use the toolbar popup when the tab cannot be placed safely.

Follow the **[illustrated extension setup guide](docs/browser-extension-setup.md)** for Chrome, Edge and Firefox, including [Arabic instructions](docs/browser-extension-setup.md#arabic). See [permissions and limitations](browser/README.md) for more detail. The extension does not import accounts or cookies and does not start hidden downloads. Reload an older unpacked companion after updating its folder, or load the new ZIP's `Browser` companion.

## Getting started

1. Download the [official stable Windows ZIP](https://github.com/ZiadHassanein/ViFree/releases/latest), extract it into a writable folder, and open **ViFree.exe** for the published 4.1.0 package, or **Vydrop.exe** for the new branding build. Target platform: Windows 10/11 x64; the .NET runtime is bundled.
2. Paste a video or playlist link, choose output, quality and a save folder, then click **Download**.
3. Choose the Video/Audio format buttons and **Subtitles** for captions. Use **Choose videos** for playlist entries and **Queue** for multiple links.

Internet access is required for downloads and missing-tool preparation. No account, API key or paid service is required. Only save media you have permission to download; DRM-protected and sign-in-only content is unsupported.

**Upgrading from ViFree or YouTube Downloader to Vydrop:** close the old app and extract the Vydrop ZIP into a new writable folder once. Stable preferences, history and verified media tools retain their existing storage location. Enable Browser integration from Vydrop to refresh the executable path. Existing companion IDs and permissions are retained.

**Upgrading to the previously published ViFree 4.1.0:** close the old app, download this ZIP once and extract it into a new writable folder. Older copies still check the retired downloads repository; v4.1.0 and later check this repository. Stable settings, history and downloaded media are retained; preview preferences stay separate. Enable Browser integration from the new stable app when using its companion.

## Frequently asked questions

### Is Vydrop free and open source?

Yes. Vydrop's source is available under the [MIT license](LICENSE). Media tools retain their own licenses; see the [third-party notices](THIRD-PARTY-NOTICES.txt).

### Can Vydrop download YouTube videos as MP4 or MP3?

Yes. You can save supported YouTube videos, Shorts and playlist entries as video or audio. Available formats and quality depend on the media, app version and site access.

### Does Vydrop need a browser extension?

No. Pasting a link into the app works without an extension. The companion is an optional way to send the video you are watching to Vydrop.

### What happens when YouTube changes?

Vydrop retains the automatic maintenance introduced in ViFree 4.1.0: it checks for verified yt-dlp and Deno updates once daily when a link is used, retaining the previous tools and allowing one compatible update-and-retry. An upstream fix may still be needed. App-version updates are separate and manual; see [Preview 30 details](PREVIEW.md).

## Build from source

On Windows, install the **.NET 10 SDK**. **Node.js 20+** is also required for browser-companion tests.

```powershell
./scripts/check.ps1
./scripts/package.ps1 -Version 4.2.0
```

Preview builds use separate settings and tool state. Read [Contributing](CONTRIBUTING.md), [Architecture](ARCHITECTURE.md) and [Security](SECURITY.md) before making changes.

## Vydrop بالعربية — برنامج تنزيل الفيديو والصوت لويندوز

<div dir="rtl">

**Vydrop** برنامج مجاني ومفتوح المصدر من تطوير **زياد حسنين**، لتنزيل فيديوهات يوتيوب والمقاطع القصيرة وقوائم التشغيل وحفظ الصوت. يدعم العربية والإنجليزية والمظهرين الفاتح والداكن، مع قائمة تنزيلات واختيار فيديوهات القائمة وحفظ الترجمة المتاحة.

اسم البرنامج الجديد هو **Vydrop**، وإصدار إعادة الهوية **4.2.0** جاهز للمراجعة المحلية. آخر إصدار منشور متاح للتنزيل هو **ViFree 4.1.0**، المبني على **Preview 30** المقبول. هذا المستودع هو المكان الرسمي للشيفرة والتنزيلات والدعم. فكّ ضغط ZIP وافتح **Vydrop.exe** في الإصدار الجديد، أو **ViFree.exe** في الإصدار المنشور 4.1.0. تُجهّز الأدوات الناقصة تلقائيًا، ولا يلزم حساب أو مفتاح API أو خدمة مدفوعة. إضافة المتصفح اختيارية، وتختلف الميزات المتاحة حسب الإصدار والموقع وجودة المصدر.

</div>

## Screenshots

**Vydrop 4.2.0 — English / dark and العربية / light.** Click an image for its full-resolution lossless PNG. The app uses the approved red download logo and matching accent colors.

<p align="center">
  <a href="assets/screenshot-en-dark.png"><img src="assets/screenshot-en-dark.png" width="48%" alt="Vydrop 4.2.0 English dark interface with a red download logo, red controls and centered Download button"></a>
  <a href="assets/screenshot-ar-light.png"><img src="assets/screenshot-ar-light.png" width="48%" alt="واجهة Vydrop 4.2.0 العربية بالمظهر الفاتح والشعار الأحمر وألوان التنزيل الجديدة"></a>
</p>

These are cursor-free client-area captures from the current WPF app. The [extension setup guide](docs/browser-extension-setup.md) includes labeled folder/browser illustrations and the app connection steps.

## Author and support

Created by **Ziad Hassanein**. [GitHub](https://github.com/ZiadHassanein) · [LinkedIn](https://www.linkedin.com/in/ziad-hassanein/) · [Email](mailto:ziadmuhammed501@gmail.com).

Use [GitHub issues](https://github.com/ZiadHassanein/ViFree/issues) for app problems and development reports. Include your app version, Windows version and the public link involved; never post passwords or cookies.
