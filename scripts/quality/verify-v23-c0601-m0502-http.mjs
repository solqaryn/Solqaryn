import assert from 'node:assert/strict';
const base = process.env.C0601_API_URL ?? 'http://127.0.0.1:5005';
const mode = process.argv[2] ?? 'healthy';
const forbidden = [/\/home\/runner\//i, /stacktrace/i, /password\s*[:=]/i, /jwt__secret/i, /connectionstrings__defaultconnection/i];
async function probe(path, method = 'GET') {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 10000);
  try {
    const response = await fetch(new URL(path, base), { method, headers: { Accept: 'application/json' }, redirect: 'manual', signal: controller.signal });
    const raw = await response.text();
    for (const marker of forbidden) assert.equal(marker.test(raw), false, 'Sensitive implementation details leaked');
    return { status: response.status, raw, contentType: response.headers.get('content-type') ?? '' };
  } finally { clearTimeout(timeout); }
}
if (mode === 'healthy') {
  const health = await probe('/health');
  assert.equal(health.status, 200);
  assert.match(health.contentType, /json/i);
  assert.equal(JSON.parse(health.raw).status, 'ok');
  assert.equal(JSON.parse(health.raw).service, 'Solqaryn API');
  const ready = await probe('/health/ready');
  assert.equal(ready.status, 200, 'Real disposable MySQL must be connected');
  assert.equal(JSON.parse(ready.raw).database, 'connected');
  for (const url of ['/tenant-context/empresas','/tenant-context/1']) {
    assert.equal((await probe(url)).status, 401, 'Anonymous tenant resource must deny');
  }
  assert.equal((await probe('/api/publico/v1/presentacion')).status, 404, 'Proposed M02 public endpoint does not yet exist');
  assert.equal((await probe('/health', 'POST')).status, 405, 'Public health is read-only');
  console.log('C0601_M0502_REAL_HTTP=PASS 200 JSON, MySQL ready 200, anonymous 401x2, proposed 404, POST 405');
} else if (mode === 'db-down') {
  const ready = await probe('/health/ready');
  assert.equal(ready.status, 503, 'Disconnected disposable MySQL must fail readiness');
  assert.equal(JSON.parse(ready.raw).database, 'unavailable');
  assert.equal(JSON.parse(ready.raw).status, 'not_ready');
  assert.equal((await probe('/health')).status, 200, 'Liveness is distinct from readiness');
  console.log('C0601_M0502_REAL_HTTP=PASS failure MySQL readyness 503 + liveness 200');
} else throw new Error('Unknown mode');
