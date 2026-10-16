const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const crypto = require('node:crypto');
const root = path.join(__dirname, '..');

for (const channel of ['Vydrop', 'Vydrop Preview', 'ViFree', 'ViFree Preview']) test(`${channel} companion sends only explicit public links to its own channel`, async () => {
  const sent = [], menus = [], listeners = {};
  const api = {
    runtime: { id: 'test-extension', onMessage: { addListener: fn => { listeners.message = fn; } }, getManifest: () => ({ name: channel }), onInstalled: { addListener: fn => { listeners.install = fn; } }, sendNativeMessage: async (...args) => { sent.push(args); return { ok: true }; } },
    contextMenus: { removeAll: async () => {}, create: item => menus.push(item), onClicked: { addListener: fn => { listeners.click = fn; } } },
    action: { setBadgeText: async () => {}, setBadgeBackgroundColor: async () => {}, setTitle: async () => {} }
  };
  const context = vm.createContext({ browser: api, URL });
  vm.runInContext(fs.readFileSync(path.join(root, 'browser/common/connection.js'), 'utf8'), context);
  vm.runInContext(fs.readFileSync(path.join(root, 'browser/common/video-detection.js'), 'utf8'), context);
  vm.runInContext(fs.readFileSync(path.join(root, 'browser/common/background.js'), 'utf8'), context);
  assert.equal(sent.length, 0);
  await listeners.install();
  assert.equal(menus.length, 2);
  await listeners.click({ linkUrl: 'https://vimeo.com/76979871' }, { id: 1 });
  assert.equal(sent.length, 1);
  assert.equal(sent[0][0], channel.endsWith(' Preview') ? 'com.vifree.preview' : 'com.vifree');
  assert.equal(sent[0][1].type, 'open-link');
  assert.equal(sent[0][1].url, 'https://vimeo.com/76979871');
  await listeners.click({ linkUrl: 'file:///C:/private.txt' }, { id: 1 });
  await listeners.click({ linkUrl: 'https://user:secret@example.com/video' }, { id: 1 });
  assert.equal(sent.length, 1);
  const message = { version: 1, type: 'vifree-open-video', url: 'https://example.com/watched.mp4' };
  const sender = { id: 'test-extension', tab: { id: 42 }, frameId: 3, url: 'https://player.example.org/embed' };
  for (const invalid of [{ ...sender, id: 'another-extension' }, { ...sender, url: 'file:///local.html' }, { ...sender, tab: undefined }, { ...sender, frameId: -1 }]) {
    let response;
    assert.equal(listeners.message(message, invalid, value => { response = value; }), false);
    assert.equal(response.ok, false);
  }
  const response = await new Promise(resolve => assert.equal(listeners.message(message, sender, resolve), true));
  assert.equal(response.ok, true);
  assert.equal(sent.length, 2);
  assert.equal(sent[1][1].url, message.url);
  const rejected = await new Promise(resolve => listeners.message({ ...message, url: 'javascript:alert(1)' }, sender, resolve));
  assert.equal(rejected.ok, false);
  assert.equal(sent.length, 2);
  assert.equal(listeners.message({ ...message, type: 'something-else' }, sender, () => assert.fail()), false);
});

test('popup waits for user action and reports failure without opening hidden pages', async () => {
  let click, calls = 0;
  const button = { disabled: false, addEventListener: (_, fn) => { click = fn; } };
  const status = { textContent: '' };
  const api = { tabs: { query: async () => [{ url: 'https://example.com/video' }] }, runtime: { getManifest: () => ({ name: 'Vydrop' }), sendNativeMessage: async (host, message) => { calls++; assert.equal(host, 'com.vifree'); assert.equal(message.type, 'open-link'); return { ok: false }; } } };
  const context = vm.createContext({ browser: api, URL, document: { querySelector: name => name === '#send' ? button : status } });
  vm.runInContext(fs.readFileSync(path.join(root, 'browser/common/connection.js'), 'utf8'), context);
  vm.runInContext(fs.readFileSync(path.join(root, 'browser/common/video-detection.js'), 'utf8'), context);
  vm.runInContext(fs.readFileSync(path.join(root, 'browser/common/popup.js'), 'utf8'), context);
  assert.equal(calls, 0);
  await click();
  assert.equal(calls, 1);
  assert.equal(button.disabled, false);
  assert.match(status.textContent, /enable browser integration/iu);
});

test('public Chromium key matches native-host identity and carries no private key', () => {
  const key = fs.readFileSync(path.join(root, 'browser/public-key.txt'), 'utf8').trim();
  const parsed = crypto.createPublicKey({ key: Buffer.from(key, 'base64'), type: 'spki', format: 'der' });
  assert.equal(parsed.type, 'public');
  const id = [...crypto.createHash('sha256').update(Buffer.from(key, 'base64')).digest().subarray(0, 16)].map(b => String.fromCharCode(97 + (b >> 4), 97 + (b & 15))).join('');
  const source = fs.readFileSync(path.join(root, 'src/Downloader.Infrastructure/Browser/BrowserIdentity.cs'), 'utf8');
  assert.ok(source.includes(`ChromiumId = "${id}"`));
});

for (const detected of [true, false]) test(`popup ${detected ? 'sends the detected TikTok video' : 'explains a missing video without sending its homepage'}`, async () => {
  let click, calls = 0, inspections = 0;
  const button = { disabled: false, addEventListener: (_, fn) => { click = fn; } }, status = { textContent: '' };
  const url = 'https://www.tiktok.com/@synthetic/video/7000000000000000001';
  const api = {
    tabs: { query: async () => [{ id: 42, url: 'https://www.tiktok.com/en/' }] },
    scripting: { executeScript: async () => { inspections++; return [{ frameId: 0, result: detected ? { url, reason: 'player' } : { url: '', reason: 'no-video' } }]; } },
    runtime: { getManifest: () => ({ name: 'Vydrop Preview' }), sendNativeMessage: async (host, request) => { calls++; assert.equal(host, 'com.vifree.preview'); assert.equal(request.url, url); return { ok: true }; } }
  };
  const context = vm.createContext({ browser: api, URL, document: { querySelector: name => name === '#send' ? button : status } });
  for (const name of ['connection.js', 'video-detection.js', 'popup.js']) vm.runInContext(fs.readFileSync(path.join(root, 'Browser/common', name), 'utf8'), context);
  assert.equal(inspections, 0); assert.equal(calls, 0);
  await click(); assert.equal(inspections, 1); assert.equal(calls, detected ? 1 : 0); assert.equal(button.disabled, false);
  assert.match(status.textContent, detected ? /Opened in Vydrop/u : /Share → Copy link/u);
});

test('missing native host has actionable guidance instead of a browser API error', async () => {
  const context = vm.createContext({ URL, browser: { runtime: { getManifest: () => ({ name: 'Vydrop' }), sendNativeMessage: async () => { throw new Error('Specified native messaging host not found'); } } } });
  vm.runInContext(fs.readFileSync(path.join(root, 'Browser/common/connection.js'), 'utf8'), context);
  await assert.rejects(context.ViFreeConnection.sendLink('https://example.com/video'), /enable Browser integration/u);
});

if (process.env.VIFREE_BROWSER_PACKAGE) test('packaged companions retain identity and enable on-video actions across web frames', () => {
  for (const browser of ['chromium', 'firefox']) {
    const folder = path.join(process.env.VIFREE_BROWSER_PACKAGE, browser), manifest = JSON.parse(fs.readFileSync(path.join(folder, 'manifest.json'), 'utf8'));
    assert.equal(manifest.version, '1.3.0'); assert.equal(manifest.name, 'Vydrop Preview');
    assert.deepEqual([...manifest.permissions].sort(), ['activeTab', 'clipboardWrite', 'contextMenus', 'nativeMessaging', 'scripting'].sort());
    assert.equal(manifest.host_permissions, undefined);
    assert.deepEqual(manifest.content_scripts, [{ matches: ['https://*/*', 'http://*/*'], js: ['video-detection.js', 'video-overlay.js'], all_frames: true, run_at: 'document_idle' }]);
    for (const name of ['connection.js', 'video-detection.js', 'video-overlay.js', 'popup.js', 'background.js']) assert.equal(fs.readFileSync(path.join(folder, name), 'utf8'), fs.readFileSync(path.join(root, 'Browser/common', name), 'utf8'));
    assert.match(fs.readFileSync(path.join(folder, 'popup.html'), 'utf8'), /video-detection\.js/u);
    if (browser === 'chromium') assert.equal(manifest.key, fs.readFileSync(path.join(root, 'Browser/public-key.txt'), 'utf8').trim());
    else { assert.deepEqual(manifest.background.scripts, ['connection.js', 'video-detection.js', 'background.js']); assert.equal(manifest.browser_specific_settings.gecko.id, 'vifree-preview@ziadhassanein.github.io'); }
  }
});
