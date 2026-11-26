// Servidor estatico de auditoria QA; no se incluye en el runtime de Angular.
import { createServer } from 'node:http';
import { readFile, stat } from 'node:fs/promises';
import { resolve, extname, sep } from 'node:path';

const origin = 'http://127.0.0.1:4200';
if (process.env.CI !== 'true' || process.env.GITHUB_REF !== 'refs/heads/qa' ||
    process.env.PLAYWRIGHT_TEST_BASE_URL !== origin ||
    process.env.PHASE7_API_URL !== 'http://127.0.0.1:5005') {
  throw new Error('Servidor autorizado exclusivamente para la auditoria local de QA.');
}
const root = resolve('dist/solqaryn-frontend/browser');
const index = resolve(root, 'index.html');
await readFile(index);

const types = {
  '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8',
  '.mjs': 'text/javascript; charset=utf-8', '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8', '.svg': 'image/svg+xml',
  '.png': 'image/png', '.jpg': 'image/jpeg', '.jpeg': 'image/jpeg',
  '.webp': 'image/webp', '.avif': 'image/avif', '.gif': 'image/gif',
  '.ico': 'image/x-icon', '.woff': 'font/woff', '.woff2': 'font/woff2',
  '.ttf': 'font/ttf', '.txt': 'text/plain; charset=utf-8',
  '.map': 'application/json; charset=utf-8', '.wasm': 'application/wasm'
};

const server = createServer(async (request, response) => {
  const finish = (status) => { response.writeHead(status); response.end(); };
  if (!['GET', 'HEAD'].includes(request.method)) { finish(405); return; }
  try {
    const pathname = decodeURIComponent(new URL(request.url, origin).pathname);
    const candidate = resolve(root, '.' + pathname);
    if (candidate !== root && !candidate.startsWith(root + sep)) { finish(403); return; }
    let file = candidate;
    try {
      if (!(await stat(file)).isFile()) file = null;
    } catch (error) {
      if (error.code !== 'ENOENT' && error.code !== 'ENOTDIR') throw error;
      file = null;
    }
    // Solo navegaciones HTML reciben el fallback SPA; un asset ausente es 404.
    if (!file) {
      if (extname(pathname) || !request.headers.accept?.includes('text/html')) { finish(404); return; }
      file = index;
    }
    const body = await readFile(file);
    response.writeHead(200, {
      'Content-Type': types[extname(file).toLowerCase()] || 'application/octet-stream',
      'Content-Length': body.length,
      'Cache-Control': 'no-store',
      'X-Content-Type-Options': 'nosniff'
    });
    response.end(request.method === 'HEAD' ? undefined : body);
  } catch (error) {
    if (error instanceof URIError) { finish(400); return; }
    console.error('Error de lectura del frontend local:', error.code || error.name);
    finish(500);
  }
});
server.on('error', error => { console.error(error.message); process.exitCode = 1; });
server.listen(4200, '127.0.0.1', () => console.log('QA static frontend listening on ' + origin));
