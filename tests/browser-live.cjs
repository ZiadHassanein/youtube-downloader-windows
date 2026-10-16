// Opt-in, loopback-only source viewer for read-only tests in an existing browser tab.
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const server = http.createServer((request, response) => {
  if (request.url !== '/detector.txt') { response.writeHead(404).end(); return; }
  response.writeHead(200, { 'content-type': 'text/plain; charset=utf-8', 'cache-control': 'no-store', 'x-content-type-options': 'nosniff' });
  const scope = {};
  vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../Browser/common/video-detection.js'), 'utf8'), scope);
  response.end(scope.ViFreeVideoDetection.inspectVideoPage.toString());
});
server.listen(0, '127.0.0.1', () => console.log(`Read-only detector source: http://127.0.0.1:${server.address().port}/detector.txt`));
setTimeout(() => server.close(), 15 * 60 * 1000).unref();
