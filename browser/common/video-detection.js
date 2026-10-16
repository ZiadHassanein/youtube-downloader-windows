globalThis.ViFreeVideoDetection = (() => {
  // Self-contained: Chrome/Firefox serialize this function into the active tab's isolated world.
  function inspectVideoPage(sourceHint = "") {
    function platform(value) {
      const host = new URL(value).hostname.toLowerCase();
      return /(^|\.)tiktok\.com$/u.test(host) ? "tiktok" : /(^|\.)(facebook\.com|fb\.watch)$/u.test(host) ? "facebook" : "";
    }
    function permalink(value) {
      if (typeof value !== "string" || !value.trim()) return "";
      try {
        const url = new URL(value, location.href);
        if (!/^https?:$/u.test(url.protocol) || url.username || url.password || url.port || platform(url.href) !== platform(location.href)) return "";
        if (platform(url.href) === "tiktok") {
          const match = url.pathname.match(/^\/@([\w.-]+)\/video\/(\d+)\/?$/u);
          if (match) return `https://www.tiktok.com/@${match[1]}/video/${match[2]}`;
          if (/^(vm|vt)\.tiktok\.com$/u.test(url.hostname) && /^\/[\w-]+\/?$/u.test(url.pathname)) return url.origin + url.pathname;
          return "";
        }
        const reel = url.pathname.match(/^\/reel\/(\d+)\/?$/u);
        if (reel) return `https://www.facebook.com/reel/${reel[1]}/`;
        const video = url.pathname.match(/^\/[^/]+\/videos\/(?:[^/]+\/)?(\d+)\/?$/u);
        const id = video?.[1] ?? (/^\/(watch\/?|video\.php)$/u.test(url.pathname) ? url.searchParams.get("v") : "");
        if (/^\d+$/u.test(id ?? "")) return `https://www.facebook.com/watch/?v=${id}`;
        if (url.hostname === "fb.watch" && /^\/[\w-]+\/?$/u.test(url.pathname)) return url.origin + url.pathname;
        const post = url.pathname.match(/^\/(?:[^/]+\/posts|groups\/[^/]+\/(?:posts|permalink))\/(?:pfbid[A-Za-z0-9]+|\d+)\/?$/u);
        if (post) return "https://www.facebook.com" + url.pathname;
        if (/^\/(story|permalink)\.php$/u.test(url.pathname) && /^(pfbid[A-Za-z0-9]+|\d+)$/u.test(url.searchParams.get("story_fbid") ?? "")) {
          const owner = url.searchParams.get("id");
          return "https://www.facebook.com" + url.pathname + "?story_fbid=" + url.searchParams.get("story_fbid") + (/^\d+$/u.test(owner ?? "") ? "&id=" + owner : "");
        }
        const share = url.pathname.match(/^\/share\/(r|v)\/([\w-]+)\/?$/u);
        return share ? `https://www.facebook.com/share/${share[1]}/${share[2]}/` : "";
      } catch { return ""; }
    }
    function visible(video) {
      let box = video.getBoundingClientRect(), style = getComputedStyle(video);
      if (style.display === "none" || style.visibility === "hidden") return null;
      for (let parent = video.parentElement, depth = 0; parent && parent !== document.body && depth < 10; parent = parent.parentElement, depth++) {
        const paint = getComputedStyle(parent);
        if (paint.display === "none" || paint.visibility === "hidden" || Number(paint.opacity) === 0) return null;
      }
      // TikTok can paint the active video through a visible sibling canvas while hiding the video element.
      if (Number(style.opacity) === 0) {
        const canvas = platform(location.href) === "tiktok" && Array.from(video.parentElement?.querySelectorAll("canvas") ?? []).slice(0, 5).find(item => {
          const paint = getComputedStyle(item), rect = item.getBoundingClientRect();
          return paint.display !== "none" && paint.visibility !== "hidden" && Number(paint.opacity) > 0 && rect.width >= 80 && rect.height >= 60;
        });
        if (!canvas) return null;
        box = canvas.getBoundingClientRect();
      }
      const width = Math.max(0, Math.min(box.right, innerWidth) - Math.max(box.left, 0));
      const height = Math.max(0, Math.min(box.bottom, innerHeight) - Math.max(box.top, 0));
      const pip = document.pictureInPictureElement === video;
      if (!pip && (box.width < 80 || box.height < 60 || width * height < 2400)) return null;
      const playing = !video.paused && !video.ended && video.readyState >= 2;
      return { video, score: (pip ? 20000 : 0) + (playing ? 10000 : 0) + Math.min(width * height, 500000) / 1000 + width * height / Math.max(1, box.width * box.height) * 50 };
    }
    function localLink(video, visibleVideos) {
      const wrapped = permalink(video.closest("a[href]")?.href ?? "");
      if (wrapped) return wrapped;
      let scope = video, mediaId = "";
      for (let level = 0; scope && scope !== document.body && level < 10; level++, scope = scope.parentElement) {
        // Never borrow a permalink from a different visible feed item or a sidebar.
        if (visibleVideos.some(other => other !== video && scope.contains(other))) break;
        const id = scope.getAttribute("data-video-id") ?? scope.getAttribute("data-item-id") ?? (platform(location.href) === "tiktok" ? scope.id?.match(/^xgwrapper-\d+-(\d{15,25})$/u)?.[1] : "");
        if (/^\d{8,25}$/u.test(id ?? "")) mediaId = id;
        if (mediaId) {
          if (platform(location.href) === "facebook") return `https://www.facebook.com/watch/?v=${mediaId}`;
          const author = Array.from(scope.querySelectorAll('a[href]')).map(link => { try { return platform(link.href) === "tiktok" ? new URL(link.href).pathname.match(/^\/@([\w.-]+)\/?$/u)?.[1] : null; } catch { return null; } }).find(Boolean);
          if (author) return `https://www.tiktok.com/@${author}/video/${mediaId}`;
        }
        const links = new Set(Array.from(scope.querySelectorAll("a[href]")).slice(0, 200).map(link => permalink(link.href)).filter(Boolean));
        if (links.size === 1) return [...links][0];
        if (links.size > 1) return "";
        if (scope.matches('article,[role="article"],[role="dialog"],[data-e2e="feed-item"],[data-e2e="recommend-list-item-container"],[data-pagelet^="FeedUnit"]')) break;
      }
      return "";
    }
    if (!platform(location.href)) return { url: "", reason: "unsupported" };
    const videos = Array.from(document.querySelectorAll("video")).slice(0, 100).map(visible).filter(Boolean).sort((a, b) => b.score - a.score);
    const hinted = sourceHint && videos.find(item => item.video.currentSrc === sourceHint || item.video.src === sourceHint);
    const ranked = hinted ? [hinted] : videos;
    if (ranked.length > 1 && Math.abs(ranked[0].score - ranked[1].score) < 30) return { url: "", reason: "ambiguous" };
    if (ranked.length) {
      const local = localLink(ranked[0].video, videos.map(item => item.video));
      if (local) return { url: local, reason: "player" };
    }
    const current = permalink(location.href);
    if (current) return { url: current, reason: "page" };
    return { url: "", reason: ranked.length ? "missing-link" : "no-video" };
  }

  function socialPlatform(value) {
    try { const host = new URL(value).hostname; return /(^|\.)tiktok\.com$/u.test(host) ? "tiktok" : /(^|\.)(facebook\.com|fb\.watch)$/u.test(host) ? "facebook" : ""; }
    catch { return ""; }
  }
  async function resolve(tab, info = {}) {
    if (info.linkUrl) return info.linkUrl;
    const platform = socialPlatform(tab?.url ?? "");
    if (!platform) return tab?.url ?? "";
    if (!Number.isInteger(tab?.id)) throw new Error("Open the video in a browser tab, then try again.");
    let results;
    try { results = await globalThis.ViFreeConnection.api.scripting.executeScript({ target: { tabId: tab.id }, func: inspectVideoPage, args: [info.srcUrl ?? ""] }); }
    catch { throw new Error("Could not inspect this page. Reload the extension and page, then try again."); }
    const found = results?.find(result => result.frameId === 0)?.result;
    if (found?.url && socialPlatform(found.url) === platform) return found.url;
    throw new Error(found?.reason === "ambiguous" ? "More than one video is visible. Open the video you want, then try again." : "Could not find this video's link. Open the video itself or use Share → Copy link.");
  }
  return { resolve, inspectVideoPage };
})();
