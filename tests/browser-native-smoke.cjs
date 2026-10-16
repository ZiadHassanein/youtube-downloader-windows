// Opt-in native-host protocol smoke test. This does not certify the installed extension.
const { spawn } = require('node:child_process');
const fs = require('node:fs');
const crypto = require('node:crypto');
const path = require('node:path');
const [exe, value] = process.argv.slice(2);
if (!exe || !value) throw new Error('Usage: node browser-native-smoke.cjs <ViFree.exe> <public-video-url>');
const url = new URL(value);
if (!['https:', 'http:'].includes(url.protocol) || url.username || url.password || url.port) throw new Error('A public video URL is required.');
const key = fs.readFileSync(path.join(__dirname, '../browser/public-key.txt'), 'utf8').trim();
const id = [...crypto.createHash('sha256').update(Buffer.from(key, 'base64')).digest().subarray(0, 16)]
  .map(b => String.fromCharCode(97 + (b >> 4), 97 + (b & 15))).join('');
const payload = Buffer.from(JSON.stringify({ version: 1, type: 'open-link', url: url.href }));
const size = Buffer.alloc(4); size.writeUInt32LE(payload.length);
const child = spawn(exe, [`chrome-extension://${id}/`], { windowsHide: true, stdio: ['pipe', 'pipe', 'pipe'] });
let output = Buffer.alloc(0);
const timeout = setTimeout(() => { child.kill(); console.error('Native-host smoke test timed out.'); process.exitCode = 1; }, 17000);
child.on('error', error => { clearTimeout(timeout); console.error(error.message); process.exitCode = 1; });
child.stdout.on('data', chunk => { output = Buffer.concat([output, chunk]); if (output.length > 65536) child.kill(); });
child.on('close', code => {
  clearTimeout(timeout);
  try {
    if (code !== 0 || output.length < 4 || output.readUInt32LE(0) !== output.length - 4) throw new Error('Invalid native-host response.');
    const response = JSON.parse(output.subarray(4).toString('utf8'));
    if (!response.ok) throw new Error(response.message);
    console.log(JSON.stringify({ check: 'native-host CLI smoke; installed extension not exercised', url: url.href, response }));
  } catch (error) { console.error(error.message); process.exitCode = 1; }
});
child.stdin.end(Buffer.concat([size, payload]));
