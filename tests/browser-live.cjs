// Opt-in, loopback-only source viewer for read-only tests in an existing browser tab.
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const server = http.createServer((request, response) => {
  const route = request.url.split('?')[0];
  const fixtures = { '/overlay': ['fixtures/video-overlay.html', 'text/html'], '/video-overlay.js': ['../browser/common/video-overlay.js', 'text/javascript'] };
  if (fixtures[route]) {
    const [file, type] = fixtures[route];
    response.writeHead(200, { 'content-type': type + '; charset=utf-8', 'cache-control': 'no-store', 'x-content-type-options': 'nosniff' });
    response.end(fs.readFileSync(path.join(__dirname, file))); return;
  }
  if (request.url !== '/detector.txt') { response.writeHead(404).end(); return; }
  response.writeHead(200, { 'content-type': 'text/plain; charset=utf-8', 'cache-control': 'no-store', 'x-content-type-options': 'nosniff' });
  const scope = {};
  vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../Browser/common/video-detection.js'), 'utf8'), scope);
  response.end(scope.ViFreeVideoDetection.inspectVideoPage.toString());
});
server.listen(0, '127.0.0.1', () => console.log(`Local synthetic overlay: http://127.0.0.1:${server.address().port}/overlay`));
setTimeout(() => server.close(), 15 * 60 * 1000).unref();
