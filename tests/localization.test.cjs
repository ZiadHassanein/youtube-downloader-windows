// Keeps the Arabic table in Texts.cs complete and free of stale entries.
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.join(__dirname, '..');
const read = file => fs.readFileSync(file, 'utf8').replaceAll('\r\n', '\n');
const walk = folder => fs.readdirSync(folder, { withFileTypes: true })
  .flatMap(entry => /^(bin|obj)$/u.test(entry.name) ? [] : entry.isDirectory() ? walk(path.join(folder, entry.name)) : [path.join(folder, entry.name)]);
const textsFile = path.join(root, 'src/Downloader.Desktop/Localization/Texts.cs');
const sources = walk(path.join(root, 'src')).filter(file => /\.(cs|xaml)$/u.test(file) && file !== textsFile);
const tests = walk(path.join(root, 'tests')).filter(file => file.endsWith('.cs'));
const unescape = text => text.replaceAll('\\"', '"').replaceAll('\\n', '\n');
const describe = key => JSON.stringify(key);

// Raw C# string literal bodies as written, so lookups match the source text exactly.
const entries = [...read(textsFile).matchAll(/\{\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*\}/gu)].map(match => [match[1], match[2]]);
const table = new Map(entries.map(([key, value]) => [unescape(key), unescape(value)]));

// Composed at runtime from a prefix, or handled by the fallbacks in Texts.For.
const composed = key => /^(Updating |Downloading |Install v)/u.test(key) || /^\d+$/u.test(key) || key === '';
// Thrown for programming errors or internal state; they are never displayed.
const internalMessages = new Set([
  'Supply at least one link resolver.',
  'Use the application playlist queue.',
  'Invalid playlist identity.',
  'Wait for the active operation before disposing.',
  'A media operation is already running.',
  'Could not confirm that the media process stopped.',
  'Could not confirm that the media output stopped.',
  'Could not confirm that every media process stopped.',
  'Could not safely start the media tool.',
]);

test('The Arabic table has no duplicate keys, empty values or placeholder mismatches', () => {
  const seen = new Set();
  for (const [key, value] of entries) {
    assert.ok(!seen.has(key), `duplicate key ${describe(key)}`);
    seen.add(key);
    assert.ok(value.trim().length > 0, `empty translation for ${describe(key)}`);
    const placeholders = text => (text.match(/\{\d\}/gu) || []).sort().join(',');
    assert.equal(placeholders(value), placeholders(key), `placeholders differ for ${describe(key)}`);
    if (key !== 'Vydrop') assert.notEqual(value, key, `untranslated value for ${describe(key)}`);
  }
});

test('Every string passed to a localization helper has an Arabic entry', () => {
  const missing = new Map();
  const note = (key, file) => { if (!table.has(key) && !composed(key)) missing.set(key, path.relative(root, file)); };
  const call = /\b(?:T|F|ShowNotice|SetStatus|SetError|translate|t|Texts\.For|Texts\.Format|Texts\.Progress)\(\s*(?:[A-Za-z_.]+,\s*)?"((?:[^"\\]|\\.)*)"(?:\s*,\s*"((?:[^"\\]|\\.)*)")?/gu;
  const ternary = /\b(?:T|ShowNotice|SetStatus|translate|t)\(\s*[^"()]*\?\s*"((?:[^"\\]|\\.)*)"\s*:\s*"((?:[^"\\]|\\.)*)"/gu;
  for (const file of sources) {
    const text = read(file);
    for (const match of text.matchAll(call)) { note(unescape(match[1]), file); if (match[2]) note(unescape(match[2]), file); }
    for (const match of text.matchAll(ternary)) { note(unescape(match[1]), file); note(unescape(match[2]), file); }
    if (file.includes('Downloader.Desktop'))
      for (const match of text.matchAll(/return "((?:[^"\\]|\\.)*)";/gu)) { const key = unescape(match[1]); if (key.length >= 12 && key.includes(' ')) note(key, file); }
  }
  assert.deepEqual([...missing], [], 'strings without Arabic: ' + [...missing].map(([key, file]) => `${describe(key)} (${file})`).join('; '));
});

test('Every user-facing exception message has an Arabic entry', () => {
  const missing = new Map();
  for (const file of sources) {
    for (const match of read(file).matchAll(/new (?:ArgumentException|InvalidOperationException)\(\s*"((?:[^"\\]|\\.)*)"/gu)) {
      const key = unescape(match[1]);
      if (!table.has(key) && !internalMessages.has(key)) missing.set(key, path.relative(root, file));
    }
  }
  assert.deepEqual([...missing], [], 'exception messages without Arabic: ' + [...missing].map(([key, file]) => `${describe(key)} (${file})`).join('; '));
  for (const key of internalMessages) assert.ok(!table.has(key), `${describe(key)} is translated; remove it from the internal list`);
});

test('Every Arabic entry is still used by the application or its tests', () => {
  const everything = [...sources, ...tests].map(read).join('\n');
  const dead = entries.map(([key]) => key).filter(key => !everything.includes(key) && !composed(unescape(key)));
  assert.deepEqual(dead, [], 'stale entries: ' + dead.map(describe).join('; '));
});

test('Arabic text lives only in the table, the language switch and the Arabic documentation', () => {
  const allowed = ['ViewModels/MainViewModel.cs', 'ViewModels/MainViewModel.Presentation.cs', 'Views/Components/AppHeaderView.xaml'];
  for (const file of sources) {
    if (!/[؀-ۿ]/u.test(read(file))) continue;
    assert.ok(allowed.some(name => file.replaceAll('\\', '/').endsWith(name)), `${path.relative(root, file)} contains Arabic outside the table`);
  }
});
