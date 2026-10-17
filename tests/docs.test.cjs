const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.join(__dirname, '..');
const read = file => fs.readFileSync(path.join(root, file), 'utf8').replaceAll('\r\n', '\n');
const version = read('Directory.Build.props').match(/<Version>([^<]+)<\/Version>/u)[1];
const pages = ['index', 'supported-sites', 'download-video-from-any-website-windows', 'tiktok-facebook-video-downloader-windows', 'youtube-to-mp3-windows', 'download-youtube-playlist-windows', 'youtube-subtitles-srt', 'browser-extension-setup'];

test('Canonical product facts, version and FAQ match README and site data', () => {
  assert.match(read('docs/_data/product.yml'), new RegExp(`version: "${version.replaceAll('.', '\\.')}"`, 'u'));
  assert.ok(read('README.md').includes(read('docs/_includes/facts.md').trim()));
  assert.equal(read('llms.txt'), read('docs/llms.txt'));
  assert.ok(read('llms.txt').split(/\s+/u).length < 400);
  assert.ok(read('llms.txt').includes(`version: ${version}`));
  assert.ok(read('CITATION.cff').includes(`version: "${version}"`));
  for (const file of ['brand-logo-light.svg', 'brand-logo-dark.svg', 'brand-mark.svg', 'brand-preview-dark.png', 'social-card.png'])
    assert.deepEqual(fs.readFileSync(path.join(root, 'docs/assets', file)), fs.readFileSync(path.join(root, 'assets', file)), `${file}: site artwork must match the app brand`);
  const faq = JSON.parse(read('docs/_data/faq.json'));
  const socialCard = fs.readFileSync(path.join(root, 'docs/assets/social-card.png'));
  assert.equal(socialCard.readUInt32BE(16), 1200);
  assert.equal(socialCard.readUInt32BE(20), 630);
  assert.ok(read('docs/_config.yml').includes('path: /assets/social-card.png'));
  assert.equal(faq.en.length, faq.ar.length);
  for (const locale of ['en', 'ar']) for (const item of faq[locale]) {
    assert.ok(read('README.md').includes(item.question));
    assert.ok(read('README.md').includes(item.answer));
    assert.ok(item.answer.split(/\s+/u).length < 60);
  }
});

for (const page of pages) for (const locale of ['en', 'ar']) test(`Public page has accurate metadata and reciprocal language routes: ${locale}/${page}`, () => {
  const file = `docs/${locale === 'ar' ? 'ar/' : ''}${page}.md`;
  const source = read(file);
  assert.ok(source.startsWith('---\n'));
  const front = source.split('---\n')[1];
  const title = JSON.parse(front.match(/^title: (.+)$/mu)[1]);
  const description = JSON.parse(front.match(/^description: (.+)$/mu)[1]);
  assert.ok(title.length <= 60, `title length ${title.length}`);
  assert.ok(description.length <= 155, `description length ${description.length}`);
  assert.match(front, new RegExp(`^lang: ${locale}$`, 'mu'));
  assert.match(front, new RegExp(`^dir: ${locale === 'ar' ? 'rtl' : 'ltr'}$`, 'mu'));
  assert.match(front, /^alternate: true$/mu);
  const route = page === 'index' ? '/' : `/${page}.html`;
  assert.ok(front.includes(`english: ${route}\n`) && front.includes(`arabic: /ar${route}\n`));
  assert.equal((source.match(/^# /gmu) || []).length, 1);
  for (const match of source.matchAll(/!?\[[^\]]*\]\(([^)]+)\)/gu)) {
    const ref = match[1].split('#')[0];
    if (!ref || /^[a-z]+:/iu.test(ref)) continue;
    assert.ok(!ref.includes('{{'), `${file}: GitHub Markdown must not require Liquid for links`);
    assert.ok(fs.existsSync(path.resolve(root, path.dirname(file), ref)), `${file}: missing Markdown link ${ref}`);
  }
  for (const match of source.matchAll(/\{\{ ['"]([^'"]+)['"] \| relative_url \}\}/gu)) {
    const ref = match[1];
    const target = ref.endsWith('.html') ? `docs${ref.slice(0, -5)}.md` : `docs${ref}`;
    assert.ok(fs.existsSync(path.join(root, target)), `${file}: ${ref}`);
  }
});

test('Documentation retains update identities and states current security limitations', () => {
  assert.ok(read('CONTRIBUTING.md').includes('Keep the repository slug `ViFree`'));
  assert.ok(!read('ARCHITECTURE.md').includes('No persistent content scripts'));
  assert.ok(read('ARCHITECTURE.md').includes('Content scripts run on HTTP(S) pages and frames'));
  assert.ok(read('SECURITY.md').includes('not Authenticode-signed'));
  assert.ok(read('SECURITY.md').includes('same publisher accounts'));
  assert.ok(read('SECURITY.md').includes('not a malware scanner'));
});

test('Site has no remote scripts, tracking or unsupported review ratings', () => {
  for (const file of ['docs/_layouts/default.html', 'docs/_includes/head.html', 'docs/_includes/jsonld.html']) {
    const text = read(file);
    assert.doesNotMatch(text, /<script[^>]+src=|aggregateRating|trackingId|gtag\(/u);
  }
  assert.ok(read('docs/_includes/jsonld.html').includes('site.data.product.version'));
  assert.ok(read('docs/_includes/jsonld.html').includes('site.data.faq[page.lang]'));
  for (const bot of ['GPTBot', 'ClaudeBot', 'PerplexityBot', 'Google-Extended', 'Bingbot']) assert.ok(read('docs/robots.txt').includes(`User-agent: ${bot}\nAllow: /`));
  assert.ok(read('docs/_config.yml').includes('baseurl: /ViFree'));
  assert.ok(read('docs/_config.yml').includes('jekyll-relative-links'));
});

test('Native screenshot set is complete, optimized and actually PNG', () => {
  for (const language of ['en', 'ar']) for (const theme of ['dark', 'light']) {
    const bytes = fs.readFileSync(path.join(root, `docs/images/screenshots/vydrop-${theme}-${language}-download.png`));
    assert.equal(bytes.subarray(1, 4).toString(), 'PNG');
    assert.ok(bytes.length < 300 * 1024);
    assert.equal(bytes.readUInt32BE(16), 724);
  }
});
