# Publication and search maintenance

Publication is managed by the Documentation site workflow. Every push checks the documentation; successful builds on `main` publish to GitHub Pages. Deployment status is available in the repository's Actions tab. No telemetry or third-party scripts are added.

## Owner decisions

- Keep the repository slug `ViFree`. Update its About description to: **Vydrop (formerly ViFree): free open-source Windows video and audio downloader for YouTube, TikTok, Facebook, Vimeo and 1,000+ sites via yt-dlp. Playlists, queue, subtitles. MP4, MKV, WebM, MP3, M4A, Opus, FLAC. English and Arabic.**
- Suggested topics, capped at 20: `vydrop`, `video-downloader`, `media-downloader`, `youtube-downloader`, `youtube-to-mp3`, `tiktok-downloader`, `facebook-video-downloader`, `yt-dlp`, `yt-dlp-gui`, `ffmpeg`, `deno`, `dotnet`, `wpf`, `windows`, `arabic`, `browser-extension`, `subtitles`, `mp3`, `mp4`, `playlist-downloader`. Remove `winforms` because the app is WPF.
- GitHub Pages uses **GitHub Actions**, with source in `main /docs`. Public base URL: `https://ziadhassanein.github.io/ViFree/`. The workflow publishes the exact Jekyll output only after source and built-site checks pass. Set this URL as the repository homepage after verifying deployment.
- Decide whether ViFree 4.1.0 needs a bridge release or a pinned manual-upgrade notice. A bridge must not become the latest release; the existing updater reads releases/latest and the normal release path promotes its single ZIP.
- Choose Authenticode/independent signing and PATH reuse policy separately. No keys, certificates or signatures have been added.
- Submit directories and winget only after the manifest has been validated for the target package manager. The ZIP includes a Browser tree that must be preserved for the optional companion.

## What is ready

The site has separate useful guides for public multi-site video, TikTok/Facebook, MP3, playlists and captions, with Arabic equivalents, reciprocal language alternates, canonical URLs, native UI images, sitemap, crawler allowances and visible FAQ facts matching JSON-LD. llms.txt is a lightweight factual aid, not a standard or ranking requirement. FAQ markup does not promise a Google rich result.

The Pages URL is on a shared github.io host: a project-level `/ViFree/robots.txt` is provided, but crawlers normally read the origin's `/robots.txt`. Owner-controlled origin policy takes precedence. All content is already publicly readable without a special crawler policy.

## Check and measure

Run the documentation tests and build Jekyll before publishing. After publication verify the homepage, Arabic alternates, images, canonicals and sitemap. Add the URL to Search Console, then record indexing, referrals and GitHub release download counts monthly in [geo-log.md](geo-log.md). Do not collect app telemetry. External analytics/accounts are not configured.

Search engines determine ranking. Focus on accurate, useful, crawlable content; do not create duplicate keyword pages or promise first place. Official references: [Google Search Essentials](https://developers.google.com/search/docs/essentials), [AI features guidance](https://developers.google.com/search/docs/fundamentals/ai-optimization-guide), [FAQ result limits](https://developers.google.com/search/blog/2023/08/howto-faq-changes), [yt-dlp supported sites](https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md).

## بالعربية

ينشر مسار Documentation site الأدلة العربية والإنجليزية على GitHub Pages بعد نجاح فحوص البناء والروابط في الفرع main. يحتفظ المستودع باسم ViFree لتوافق التحديثات. يبقى التوقيع وإصدار الترقية الوسيط والتقديم إلى الأدلة الخارجية قرارات منفصلة للمالك. لا يضمن llms.txt أو ترميز FAQ ظهورًا أو ترتيبًا محددًا، ولا تُضاف أي أدوات تتبع.
