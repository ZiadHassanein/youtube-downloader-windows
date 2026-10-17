// Validate Jekyll's real output, including canonical URLs and both language variants.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const site = path.resolve(process.argv[2] || 'build/site');
const origin = 'https://ziadhassanein.github.io';
const base = '/ViFree';
function walk(folder) { return fs.readdirSync(folder, { withFileTypes: true }).flatMap(e => e.isDirectory() ? walk(path.join(folder, e.name)) : path.join(folder, e.name)); }
const htmlFiles = walk(site).filter(file => file.endsWith('.html'));
const entities = text => text.replaceAll('&amp;', '&').replaceAll('&quot;', '"').replaceAll('&#39;', "'");
for (const file of htmlFiles) {
  const text = fs.readFileSync(file, 'utf8');
  const route = '/' + path.relative(site, file).replaceAll('\\', '/').replace(/index\.html$/u, '');
  const canonical = `${origin}${base}${route}`;
  const title = text.match(/<title>([^<]+)<\/title>/u)?.[1];
  assert.ok(title && entities(title).length <= 60, `${route}: title ${title}`);
  const desc = text.match(/<meta name="description" content="([^"]*)"/u)?.[1];
  assert.ok(desc && entities(desc).length <= 155, `${route}: description`);
  assert.ok(text.includes(`rel="canonical" href="${canonical}"`), `${route}: canonical`);
  assert.ok(text.includes('hreflang="en"') && text.includes('hreflang="ar"'), `${route}: alternates`);
  assert.ok(!text.includes('{{') && !text.includes('{%'), `${route}: unrendered Liquid`);
  assert.equal((text.match(/<h1\b/gu) || []).length, 1, `${route}: one H1`);
  if (route === '/' || route === '/ar/') assert.ok(text.includes('<table>') && text.includes('<thead>'), `${route}: semantic comparison table`);
  if (route === '/browser-extension-setup.html') {
    assert.ok(text.includes('dir="rtl" lang="ar"'), `${route}: localized Arabic block`);
    assert.ok(!text.includes('**فك الضغط:**'), `${route}: Arabic steps render as Markdown`);
  }
  assert.doesNotMatch(text, /<script[^>]+src=/u);
  if (route.startsWith('/ar/')) assert.ok(text.includes('<html lang="ar" dir="rtl">'), `${route}: Arabic direction`);
  for (const match of text.matchAll(/<script type="application\/ld\+json">([\s\S]*?)<\/script>/gu)) {
    const data = JSON.parse(match[1]);
    if (data['@graph']) {
      const faq = data['@graph'].find(item => item['@type'] === 'FAQPage');
      for (const item of faq.mainEntity) assert.ok(text.includes(entities(item.name)), `${route}: visible FAQ`);
    }
  }
  for (const match of text.matchAll(/(?:href|src|srcset)="([^"]+)"/gu)) {
    const href = entities(match[1]);
    if (/^(mailto:|#|chrome:|edge:|about:)/u.test(href)) continue;
    const url = new URL(href, canonical);
    if (url.origin !== origin) continue;
    assert.ok(url.pathname.startsWith(base + '/'), `${route}: base URL ${url.pathname}`);
    let local = path.join(site, decodeURIComponent(url.pathname.slice(base.length)));
    if (fs.existsSync(local) && fs.statSync(local).isDirectory()) local = path.join(local, 'index.html');
    assert.ok(fs.existsSync(local), `${route}: missing ${href}`);
    if (url.hash && local.endsWith('.html')) {
      const target = fs.readFileSync(local, 'utf8');
      const anchor = decodeURIComponent(url.hash.slice(1));
      assert.ok(target.includes(`id="${anchor}"`), `${route}: missing anchor ${href}`);
    }
  }
}
assert.ok(fs.existsSync(path.join(site, 'sitemap.xml')));
assert.ok(fs.readFileSync(path.join(site, 'robots.txt'), 'utf8').includes(`${origin}${base}/sitemap.xml`));
console.log(`PASS: ${htmlFiles.length} rendered pages: metadata, links, alternates, JSON-LD, RTL, assets and sitemap.`);
