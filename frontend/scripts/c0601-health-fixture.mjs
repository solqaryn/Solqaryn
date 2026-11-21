import { createServer } from 'node:http';

const permitted = new Set(['success', 'empty', 'forbidden', 'missing', 'rate', 'error', 'slow']);
let scenario = 'success';
const server = createServer(async (request, response) => {
  response.setHeader('Access-Control-Allow-Origin', '*');
  response.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  response.setHeader('Access-Control-Allow-Headers', 'Content-Type, Accept');
  response.setHeader('Cache-Control', 'no-store');
  if (request.method === 'OPTIONS') { response.writeHead(204); response.end(); return; }
  if (request.url === '/__test/health-scenario' && request.method === 'POST') {
    let body = '';
    for await (const chunk of request) {
      body += chunk;
      if (body.length > 2000) { response.writeHead(413); response.end(); return; }
    }
    let next;
    try { next = JSON.parse(body).scenario; } catch { next = null; }
    if (!permitted.has(next)) { response.writeHead(400); response.end(); return; }
    scenario = next;
    response.writeHead(204);
    response.end();
    return;
  }
  if (request.method !== 'GET' || request.url !== '/health') {
    response.writeHead(404); response.end(); return;
  }
  if (scenario === 'slow') await new Promise(resolve => setTimeout(resolve, 450));
  const status = { success: 200, empty: 204, forbidden: 403, missing: 404,
    rate: 429, error: 500, slow: 200 }[scenario];
  response.writeHead(status, { 'Content-Type': 'application/json; charset=utf-8' });
  response.end(status === 204 ? '' : JSON.stringify(
    status === 200 ? { status: 'ok', service: 'Solqaryn API' } : { message: 'fixture non-private' }
  ));
});
server.listen(5005, 'localhost', () => console.log('C0601 test-only localhost HTTP fixture on 5005'));
