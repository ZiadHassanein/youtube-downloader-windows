const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const crypto = require('node:crypto');
const root = path.join(__dirname, '..');

test('context-menu companion sends only explicit public links', async () => {
  const sent = [], menus = [], listeners = {};
  const api = {
    runtime: { onInstalled: { addListener: fn => { listeners.install = fn; } }, sendNativeMessage: async (...args) => { sent.push(args); return { ok: true }; } },
    contextMenus: { removeAll: async () => {}, create: item => menus.push(item), onClicked: { addListener: fn => { listeners.click = fn; } } },
    action: { setBadgeText: async () => {}, setBadgeBackgroundColor: async () => {}, setTitle: async () => {} }
  };
  const context = vm.createContext({ browser: api, URL });
  vm.runInContext(fs.readFileSync(path.join(root, 'browser/common/background.js'), 'utf8'), context);
  assert.equal(sent.length, 0);
  await listeners.install();
  assert.equal(menus.length, 2);
  await listeners.click({ linkUrl: 'https://vimeo.com/76979871' }, { id: 1 });
  assert.equal(sent.length, 1);
  assert.equal(sent[0][0], 'com.vifree.preview');
  assert.equal(sent[0][1].type, 'open-link');
  assert.equal(sent[0][1].url, 'https://vimeo.com/76979871');
  await listeners.click({ linkUrl: 'file:///C:/private.txt' }, { id: 1 });
  await listeners.click({ linkUrl: 'https://user:secret@example.com/video' }, { id: 1 });
  assert.equal(sent.length, 1);
});

test('popup waits for user action and reports failure without opening hidden pages', async () => {
  let click, calls = 0;
  const button = { disabled: false, addEventListener: (_, fn) => { click = fn; } };
  const status = { textContent: '' };
  const api = { tabs: { query: async () => [{ url: 'https://example.com/video' }] }, runtime: { sendNativeMessage: async (_, message) => { calls++; assert.equal(message.type, 'open-link'); return { ok: false }; } } };
  vm.runInNewContext(fs.readFileSync(path.join(root, 'browser/common/popup.js'), 'utf8'), { browser: api, URL, document: { querySelector: name => name === '#send' ? button : status } });
  assert.equal(calls, 0);
  await click();
  assert.equal(calls, 1);
  assert.equal(button.disabled, false);
  assert.match(status.textContent, /enable Browser integration/u);
});

test('public Chromium key matches native-host identity and carries no private key', () => {
  const key = fs.readFileSync(path.join(root, 'browser/public-key.txt'), 'utf8').trim();
  const parsed = crypto.createPublicKey({ key: Buffer.from(key, 'base64'), type: 'spki', format: 'der' });
  assert.equal(parsed.type, 'public');
  const id = [...crypto.createHash('sha256').update(Buffer.from(key, 'base64')).digest().subarray(0, 16)].map(b => String.fromCharCode(97 + (b >> 4), 97 + (b & 15))).join('');
  const source = fs.readFileSync(path.join(root, 'src/Downloader.Infrastructure/Browser/BrowserIdentity.cs'), 'utf8');
  assert.ok(source.includes(`ChromiumId = "${id}"`));
});
