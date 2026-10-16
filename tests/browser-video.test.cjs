const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const root = path.join(__dirname, '..');
const scripts = name => fs.readFileSync(path.join(root, 'Browser/common', name), 'utf8');

// Synthetic DOM fixtures implement only standard traversal/layout operations, not detector logic.
class Element {
  constructor(tag, attrs = {}, children = []) {
    this.tag = tag; this.attrs = attrs; this.id = attrs.id ?? ''; this.children = children;
    this.paused = attrs.paused ?? false; this.ended = false; this.readyState = 4;
    this.src = attrs.src ?? 'blob:synthetic'; this.currentSrc = this.src;
    this.style = { display: 'block', visibility: 'visible', opacity: '1', ...attrs.style };
    this.box = { left: 100, top: 100, width: 600, height: 400, ...attrs.box };
    for (const child of children) child.parentElement = this;
  }
  get href() { return this.attrs.href; }
  getAttribute(name) { return this.attrs[name] ?? null; }
  dispatchEvent(event) { this.attrs.onEvent?.(event, this); return true; }
  getBoundingClientRect() { return { ...this.box, right: this.box.left + this.box.width, bottom: this.box.top + this.box.height }; }
  contains(node) { return this === node || this.children.some(child => child.contains(node)); }
  matches(selector) {
    return selector.split(',').some(value => value === this.tag || value === 'a[href]' && this.tag === 'a' && !!this.href ||
      /^\[([^=]+)="([^"]+)"\]$/u.test(value) && this.getAttribute(RegExp.$1) === RegExp.$2 ||
      /^\[([^=]+)\^="([^"]+)"\]$/u.test(value) && this.getAttribute(RegExp.$1)?.startsWith(RegExp.$2));
  }
  closest(selector) { for (let node = this; node; node = node.parentElement) if (node.matches(selector)) return node; return null; }
  querySelectorAll(selector) { return this.children.flatMap(child => [...(child.matches(selector) ? [child] : []), ...child.querySelectorAll(selector)]); }
}
const el = (tag, attrs, ...children) => new Element(tag, attrs, children);
const link = href => el('a', { href });
function fixture(url, ...children) {
  const body = el('body', {}, ...children);
  const context = vm.createContext({ URL, setTimeout, MouseEvent: class { constructor(type) { this.type = type; } }, location: { href: url }, document: { body, querySelectorAll: selector => body.querySelectorAll(selector) }, getComputedStyle: node => node.style, innerWidth: 1200, innerHeight: 800 });
  vm.runInContext(scripts('video-detection.js'), context);
  return { context, body, detect: (hint, warm) => context.ViFreeVideoDetection.inspectVideoPage(hint, warm) };
}
const tiktok = (id, author, attrs = {}) => el('article', { 'data-e2e': 'recommend-list-item-container' },
  el('section', { 'data-e2e': 'feed-video' }, el('div', { id: `xgwrapper-0-${id}` }, el('video', attrs)), link(`https://www.tiktok.com/@${author}`)));
const facebook = (id, attrs = {}) => el('article', { role: 'article' }, el('div', {}, el('video', attrs)), link(`https://www.facebook.com/watch/?v=${id}&tracking=discard`));

test('TikTok current feed wrapper identifies the playing video behind a blob URL', () => {
  const { detect } = fixture('https://www.tiktok.com/en/', tiktok('7000000000000000001', 'playing'), tiktok('7000000000000000002', 'paused', { paused: true }));
  assert.equal(detect().url, 'https://www.tiktok.com/@playing/video/7000000000000000001');
});
test('TikTok re-evaluates the current player after feed scrolling', () => {
  const first = tiktok('7000000000000000001', 'first'), second = tiktok('7000000000000000002', 'second', { paused: true });
  const { detect } = fixture('https://www.tiktok.com/en/', first, second);
  assert.match(detect().url, /@first/u);
  first.querySelectorAll('video')[0].paused = true; second.querySelectorAll('video')[0].paused = false;
  assert.match(detect().url, /@second/u);
});
test('TikTok detects its real canvas-backed player while the video element is transparent', () => {
  const item = tiktok('7000000000000000001', 'canvas', { style: { opacity: '0' } });
  const player = item.querySelectorAll('video')[0].parentElement, canvas = el('canvas');
  canvas.parentElement = player; player.children.push(canvas);
  assert.equal(fixture('https://www.tiktok.com/en/', item).detect().url, 'https://www.tiktok.com/@canvas/video/7000000000000000001');
});
test('Facebook selects the playing article and removes tracking parameters', () => {
  assert.equal(fixture('https://www.facebook.com/', facebook('11111111111', { paused: true }), facebook('22222222222')).detect().url, 'https://www.facebook.com/watch/?v=22222222222');
});
test('a wrapping video permalink takes precedence over stale detail-page navigation', () => {
  assert.equal(fixture('https://www.facebook.com/watch/?v=11111111111', el('a', { href: 'https://www.facebook.com/reel/22222222222/' }, el('video'))).detect().url, 'https://www.facebook.com/reel/22222222222/');
});
test('repeated links for the same Facebook video do not create ambiguity', () => {
  const article = facebook('11111111111'); article.children.push(link('https://www.facebook.com/watch/?v=11111111111&fbclid=discard'));
  assert.equal(fixture('https://www.facebook.com/watch/', article).detect().url, 'https://www.facebook.com/watch/?v=11111111111');
});
test('Facebook player data identifies a video in its fullscreen dialog', () => {
  assert.equal(fixture('https://www.facebook.com/', el('div', { role: 'dialog', 'data-video-id': '11111111111' }, el('video'))).detect().url, 'https://www.facebook.com/watch/?v=11111111111');
});
test('a Facebook feed timestamp can identify its video post without confusing post and video IDs', () => {
  const url = 'https://www.facebook.com/author/posts/pfbidSyntheticPost';
  assert.equal(fixture('https://www.facebook.com/', el('article', {}, el('video'), link(url + '?tracking=discard'))).detect().url, url);
});
test('explicit Facebook player IDs take precedence over another linked post', () => {
  const scope = el('article', {}, el('div', { 'data-video-id': '11111111111' }, el('video'), link('https://www.facebook.com/reel/22222222222/')));
  assert.equal(fixture('https://www.facebook.com/', scope).detect().url, 'https://www.facebook.com/watch/?v=11111111111');
});
for (const [name, attrs] of [ ['offscreen', { box: { top: 1000 } }], ['hidden', { style: { visibility: 'hidden' } }], ['transparent', { style: { opacity: '0' } }], ['not displayed', { style: { display: 'none' } }] ]) {
  test(`a ${name} preloaded player cannot win over the visible video`, () => {
    assert.equal(fixture('https://www.facebook.com/', facebook('11111111111', attrs), facebook('22222222222', { paused: true })).detect().url, 'https://www.facebook.com/watch/?v=22222222222');
  });
}
test('equally visible playing videos are reported as ambiguous', () => {
  const found = fixture('https://www.facebook.com/', facebook('11111111111'), facebook('22222222222')).detect();
  assert.equal(found.url, ''); assert.equal(found.reason, 'ambiguous');
});
test('a transparent feed container cannot hide a competing playing video from visibility checks', () => {
  const hidden = facebook('11111111111'); hidden.style.opacity = '0';
  assert.equal(fixture('https://www.facebook.com/', hidden, facebook('22222222222', { paused: true })).detect().url, 'https://www.facebook.com/watch/?v=22222222222');
});
test('right-clicking a video identifies that player without sending its blob URL', () => {
  const found = fixture('https://www.facebook.com/', facebook('11111111111', { src: 'blob:first' }), facebook('22222222222', { src: 'blob:second' })).detect('blob:second');
  assert.equal(found.url, 'https://www.facebook.com/watch/?v=22222222222');
});
test('a neighboring article or sidebar cannot supply an unrelated video link', () => {
  const found = fixture('https://www.facebook.com/', el('article', { role: 'article' }, el('video')), el('aside', {}, link('https://www.facebook.com/reel/22222222222/'))).detect();
  assert.equal(found.url, ''); assert.equal(found.reason, 'missing-link');
});
test('multiple distinct links within one player fail rather than choosing an arbitrary video', () => {
  const found = fixture('https://www.facebook.com/', el('article', {}, el('video'), link('https://www.facebook.com/reel/11111111111/'), link('https://www.facebook.com/reel/22222222222/'))).detect();
  assert.equal(found.url, '');
});
for (const url of ['https://www.tiktok.com/en/', 'https://www.facebook.com/', 'https://www.facebook.com/login/']) test(`a non-video page is never passed as a detection result: ${url}`, () => {
  const found = fixture(url).detect(); assert.equal(found.url, ''); assert.equal(found.reason, 'no-video');
});
for (const url of ['https://www.tiktok.com/@author/video/7000000000000000001', 'https://www.facebook.com/reel/11111111111/', 'https://www.facebook.com/watch/?v=11111111111', 'https://fb.watch/Example/']) test(`direct media links remain usable without a loaded player: ${url}`, () => {
  assert.equal(fixture(url).detect().url, url);
});

test('scripting runs only on explicit Facebook/TikTok requests and returns the main-frame result', async () => {
  const { context } = fixture('https://www.tiktok.com/en/');
  let count = 0;
  context.ViFreeConnection = { api: { scripting: { executeScript: async request => { count++; assert.equal(request.target.tabId, 42); assert.equal(request.func.name, 'inspectVideoPage'); return [{ frameId: 7, result: { url: 'https://example.com/wrong' } }, { frameId: 0, result: { url: 'https://www.tiktok.com/@author/video/7000000000000000001' } }]; } } } };
  assert.equal(count, 0);
  assert.equal(await context.ViFreeVideoDetection.resolve({ id: 42, url: 'https://www.youtube.com/watch?v=jNQXAC9IVRw' }), 'https://www.youtube.com/watch?v=jNQXAC9IVRw');
  assert.equal(count, 0);
  assert.match(await context.ViFreeVideoDetection.resolve({ id: 42, url: 'https://www.tiktok.com/en/' }), /\/video\//u);
  assert.equal(count, 1);
  assert.equal(await context.ViFreeVideoDetection.resolve({ id: 42, url: 'https://www.tiktok.com/en/' }, { linkUrl: 'https://vimeo.com/76979871' }), 'https://vimeo.com/76979871');
  assert.equal(count, 1);
});
test('detection errors distinguish missing video links, ambiguous players and injection failures', async () => {
  const { context } = fixture('https://www.facebook.com/');
  for (const [reason, expected] of [['missing-link', /Copy link/u], ['ambiguous', /More than one/u]]) {
    context.ViFreeConnection = { api: { scripting: { executeScript: async () => [{ frameId: 0, result: { reason } }] } } };
    await assert.rejects(context.ViFreeVideoDetection.resolve({ id: 42, url: 'https://www.facebook.com/' }), expected);
  }
  context.ViFreeConnection.api.scripting.executeScript = async () => { throw new Error('denied'); };
  await assert.rejects(context.ViFreeVideoDetection.resolve({ id: 42, url: 'https://www.facebook.com/' }), /Reload the extension/u);
});
test('a cross-site result cannot be sent in place of the selected Facebook video', async () => {
  const { context } = fixture('https://www.facebook.com/');
  context.ViFreeConnection = { api: { scripting: { executeScript: async () => [{ frameId: 0, result: { url: 'https://www.tiktok.com/@unrelated/video/7000000000000000001' } }] } } };
  await assert.rejects(context.ViFreeVideoDetection.resolve({ id: 42, url: 'https://www.facebook.com/' }), /Copy link/u);
});

const nest = (node, count) => { while (count--) node = el('div', {}, node); return node; };
test('Facebook permalink survives the deeply nested player used by the live feed', () => {
  const post = el('div', {}, el('h3'), nest(el('video'), 25), link('https://www.facebook.com/reel/1038496899213671/'));
  assert.equal(fixture('https://www.facebook.com/', post).detect().url, 'https://www.facebook.com/reel/1038496899213671/');
});
test('a post heading bounds traversal before a neighboring video permalink', () => {
  const own = el('div', {}, el('h3'), nest(el('video'), 15));
  assert.equal(fixture('https://www.facebook.com/', own, link('https://www.facebook.com/reel/22222222222/')).detect().url, '');
});
test('Facebook public timestamps materialize after pointer entry without clicking or navigating', async () => {
  const events = [];
  const timestamp = el('a', { href: 'https://www.facebook.com/?__tn__=%2CO%2CP-R', onEvent: (event, node) => {
    events.push(event.type);
    if (event.type === 'mouseover') node.attrs.href = 'https://www.facebook.com/reel/1038496899213671/?tracking=discard';
  } });
  const { detect } = fixture('https://www.facebook.com/', el('div', {}, el('h3'), nest(el('video'), 15), timestamp));
  assert.equal(detect().reason, 'missing-link');
  assert.equal((await detect('', true)).url, 'https://www.facebook.com/reel/1038496899213671/');
  assert.deepEqual(events, ['mouseover', 'mouseenter']);
});
test('a lazy-link retry never enters a neighboring post', async () => {
  let touched = false;
  const other = el('a', { href: 'https://www.facebook.com/?__tn__=%2CO%2CP-R', onEvent: () => { touched = true; } });
  const found = await fixture('https://www.facebook.com/', el('article', {}, nest(el('video'), 15)), other).detect('', true);
  assert.equal(found.url, ''); assert.equal(touched, false);
});
test('a stale source hint cannot select another visible video', () => {
  assert.equal(fixture('https://www.facebook.com/', facebook('11111111111')).detect('blob:gone').reason, 'no-video');
});
test('the overlay identifies its exact player even when media is attached without a source URL', () => {
  const first = facebook('11111111111', { src: '' }), second = facebook('22222222222', { src: '' });
  const { context } = fixture('https://www.facebook.com/', first, second);
  assert.equal(context.ViFreeVideoDetection.inspectVideoPage('', false, second.querySelectorAll('video')[0]).url, 'https://www.facebook.com/watch/?v=22222222222');
  assert.equal(context.ViFreeVideoDetection.inspectVideoPage('', false, el('video')).reason, 'no-video');
});
test('deeply hidden containers are excluded', () => {
  const hidden = nest(facebook('11111111111'), 20); hidden.style.display = 'none';
  assert.equal(fixture('https://www.facebook.com/', hidden, facebook('22222222222', { paused: true })).detect().url, 'https://www.facebook.com/watch/?v=22222222222');
});
for (const url of ['https://www.youtube.com/watch?v=jNQXAC9IVRw&list=ignored', 'https://www.youtube.com/shorts/jNQXAC9IVRw', 'https://www.youtube-nocookie.com/embed/jNQXAC9IVRw', 'https://youtu.be/jNQXAC9IVRw']) {
  test(`YouTube player has a canonical video link: ${url}`, () => assert.equal(fixture(url, el('video')).detect().url, 'https://www.youtube.com/watch?v=jNQXAC9IVRw'));
}
test('a YouTube feed player uses its own card instead of the homepage', () => {
  assert.equal(fixture('https://www.youtube.com/', el('ytd-rich-item-renderer', {}, el('video'), link('/watch?v=jNQXAC9IVRw'))).detect().url, 'https://www.youtube.com/watch?v=jNQXAC9IVRw');
});
test('a generic HTML5 player sends its direct HTTPS media URL', () => {
  assert.equal(fixture('https://example.com/', el('video', { src: 'https://cdn.example.com/movie.mp4?token=sample' })).detect().url, 'https://cdn.example.com/movie.mp4?token=sample');
});
test('a single blob-backed player sends its own page and a wrapped player prefers its permalink', () => {
  assert.equal(fixture('https://example.com/film', el('video')).detect().url, 'https://example.com/film');
  assert.equal(fixture('https://example.com/feed', el('a', { href: 'https://example.com/film/42' }, el('video')), el('video', { paused: true })).detect().url, 'https://example.com/film/42');
});
test('multiple blob-backed generic players cannot cause homepage forwarding', () => {
  assert.equal(fixture('https://example.com/', el('video'), el('video', { paused: true })).detect('blob:synthetic').url, '');
});
test('non-web pages and credential-bearing sources are never returned', () => {
  assert.equal(fixture('file:///C:/private.html', el('video')).detect().url, '');
  assert.equal(fixture('https://example.com/film', el('video', { src: 'https://user:secret@example.com/private.mp4' })).detect().url, 'https://example.com/film');
});
