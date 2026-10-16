<p align="center"><img src="assets/header-logo.png" width="96" alt="ViFree video downloader logo"></p>

# ViFree — Free Video & Audio Downloader for Windows

**ViFree** is a free, open-source Windows desktop app by **Ziad Hassanein** for downloading YouTube videos, Shorts and playlists, saving audio, and organizing multiple downloads. It supports English and Arabic interfaces, light and dark themes, subtitles, and an optional browser extension. Other public websites depend on yt-dlp support and access.

**[Download the stable Windows app — ViFree v4.1.0 (.zip)](https://github.com/ZiadHassanein/ViFree/releases/download/v4.1.0/ViFree-v4.1.0-win-x64.zip)**

[Setup guide](#getting-started) · [Latest published release](https://github.com/ZiadHassanein/ViFree/releases/latest) · [Report a download problem](https://github.com/ZiadHassanein/ViFree/issues)

## Releases and current source

| Channel | Version | What to use |
| --- | --- | --- |
| Stable release | 4.1.0 | The Windows ZIP linked above, published in this repository. |
| Accepted preview baseline | 4.1.0 Preview 30 | Promoted to stable 4.1.0; see [Preview 30 validation](PREVIEW.md). |

This is the official repository for ViFree source code, Windows downloads, release notes and support. The source is MIT licensed.

## Video, audio and download features

Version 4.1.0 includes:

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

## Browser extension: Download by ViFree

The optional **Chrome, Microsoft Edge and Firefox** companion is included in the app ZIP's `Browser` folder. Companion **1.2.1**, included with v4.1.0, adds a **Download by ViFree** tab outside a playing video's top-right border. Clicking it copies the resolved link and opens ViFree for review; downloading still requires your action in the app.

Facebook, TikTok and YouTube detection uses the selected player's public permalink. Other HTML5 players may provide a direct media URL or a single-video page link. Available space, custom players and site restrictions can limit detection. Use the toolbar popup when the tab cannot be placed safely.

See [browser setup, permissions and limitations](browser/README.md). The extension does not import accounts or cookies and does not start hidden downloads. Reload an older unpacked companion after updating its folder, or load the new ZIP's `Browser` companion.

## Getting started

1. Download the [official stable Windows ZIP](https://github.com/ZiadHassanein/ViFree/releases/latest), extract it into a writable folder, and open **ViFree.exe**. Target platform: Windows 10/11 x64; the .NET runtime is bundled.
2. Paste a video or playlist link, choose output, quality and a save folder, then click **Download**.
3. Choose the Video/Audio format buttons and **Subtitles** for captions. Use **Choose videos** for playlist entries and **Queue** for multiple links.

Internet access is required for downloads and missing-tool preparation. No account, API key or paid service is required. Only save media you have permission to download; DRM-protected and sign-in-only content is unsupported.

**Upgrading from v4.0.0, an older version or a preview:** close the old app, download this ZIP once and extract it into a new writable folder. Older copies still check the retired downloads repository; v4.1.0 and later check this repository. Stable settings, history and downloaded media are retained; preview preferences stay separate. Enable Browser integration from the new stable app when using its companion.

## Frequently asked questions

### Is ViFree free and open source?

Yes. ViFree's source is available under the [MIT license](LICENSE). Media tools retain their own licenses; see the [third-party notices](THIRD-PARTY-NOTICES.txt).

### Can ViFree download YouTube videos as MP4 or MP3?

Yes. You can save supported YouTube videos, Shorts and playlist entries as video or audio. Available formats and quality depend on the media, app version and site access.

### Does ViFree need a browser extension?

No. Pasting a link into the app works without an extension. The companion is an optional way to send the video you are watching to ViFree.

### What happens when YouTube changes?

ViFree 4.1.0 checks for verified yt-dlp and Deno updates once daily when a link is used, retaining the previous tools and allowing one compatible update-and-retry. An upstream fix may still be needed. App-version updates are separate and manual; see [Preview 30 details](PREVIEW.md).

## Build from source

On Windows, install the **.NET 10 SDK**. **Node.js 20+** is also required for browser-companion tests.

```powershell
./scripts/check.ps1
./scripts/package.ps1 -Version 4.1.0
```

Preview builds use separate settings and tool state. Read [Contributing](CONTRIBUTING.md), [Architecture](ARCHITECTURE.md) and [Security](SECURITY.md) before making changes.

## ViFree بالعربية — برنامج تنزيل الفيديو والصوت لويندوز

<div dir="rtl">

**ViFree** برنامج مجاني ومفتوح المصدر من تطوير **زياد حسنين**، لتنزيل فيديوهات يوتيوب والمقاطع القصيرة وقوائم التشغيل وحفظ الصوت. يدعم العربية والإنجليزية والمظهرين الفاتح والداكن، مع قائمة تنزيلات واختيار فيديوهات القائمة وحفظ الترجمة المتاحة.

الإصدار المستقر المتاح للتنزيل هو **4.1.0**، المبني على **Preview 30** المقبول. هذا المستودع هو المكان الرسمي للشيفرة والتنزيلات والدعم. حمّل ملف ZIP وفكّ الضغط ثم افتح **ViFree.exe**. تُجهّز الأدوات الناقصة تلقائيًا، ولا يلزم حساب أو مفتاح API أو خدمة مدفوعة. إضافة المتصفح اختيارية، وتختلف الميزات المتاحة حسب الإصدار والموقع وجودة المصدر.

</div>

## Screenshots

<p align="center">
  <a href="assets/screenshot-en-dark.png"><img src="assets/screenshot-en-dark.png" width="48%" alt="ViFree v3.0.2 English interface in dark theme — historical screenshot"></a>
  <a href="assets/screenshot-ar-light.png"><img src="assets/screenshot-ar-light.png" width="48%" alt="ViFree v3.0.2 Arabic interface in light theme — historical screenshot"></a>
</p>

These historical screenshots show **v3.0.2**. Version 4.1.0 behavior is documented above and in the [accepted preview notes](PREVIEW.md).

## Author and support

Created by **Ziad Hassanein**. [GitHub](https://github.com/ZiadHassanein) · [LinkedIn](https://www.linkedin.com/in/ziad-hassanein/) · [Email](mailto:ziadmuhammed501@gmail.com).

Use [GitHub issues](https://github.com/ZiadHassanein/ViFree/issues) for app problems and development reports. Include your app version, Windows version and the public link involved; never post passwords or cookies.
