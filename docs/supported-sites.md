---
title: "Which websites does Vydrop support?"
description: "Vydrop uses yt-dlp for public video and audio from 1,000+ sites. Check supported sites, direct media links, quality limits and access restrictions."
lang: en
dir: ltr
alternate: true
english: /supported-sites.html
arabic: /ar/supported-sites.html
---

# Which websites does Vydrop support?

Vydrop accepts public HTTP(S) pages supported by yt-dlp, which lists 1,000+ sites. This includes YouTube, TikTok, Facebook, Vimeo, X/Twitter, Reddit, Dailymotion and Twitch clips; compatible direct MP4/MP3 links are also supported.

## Check a particular video

Paste its own public link into Vydrop. Successful inspection shows the title and actual output choices. The [upstream supported-sites list](https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md) is the current reference; listing a site does not guarantee every URL works.

## Understand the limits

Private videos, sign-in-only pages and DRM-protected streams are unsupported. An extension button does not establish anonymous download access. Available resolution, audio quality and captions depend on the source. Live streams may need to finish first.

Vydrop checks verified yt-dlp and Deno updates once daily when a link is used, retaining a previous tool selection for recovery. A website change can still require an upstream fix. For a manual check, use **Settings → Update downloader**; this does not update the desktop application.

![Native Vydrop interface: review format and quality before downloading](images/screenshots/vydrop-dark-en-download.png)

*Native render of the 4.2.0 source interface, without a mouse pointer.*
