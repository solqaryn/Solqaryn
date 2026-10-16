import assert from 'node:assert/strict';
import { createHmac } from 'node:crypto';
import { createRequire } from 'node:module';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import vm from 'node:vm';

const require = createRequire(import.meta.url);
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const { buildPublicStoreProxyContext } = require('../server/public-store-proxy-context');
const secret = 'ci-public-binding-not-a-live-secret-32-bytes-abcdef';
const environment = { SOLQARYN_PUBLIC_STORE_SIGNING_KEY: secret, SOLQARYN_PUBLIC_STORE_PROOF_ENABLED: 'true' };
const binding = { environment: 'DEV', publicOrigin: 'https://solqaryn-dev.vercel.app', apiUpstream: 'https://solqaryn-api-dev-fxx8.onrender.com' };
const now = 1792130000;
const fake = (host, method='GET', headers={}) => ({ method, headers: { host, ...headers } });
const sign = (at, method, route, host, key) =>
  createHmac('sha256', secret).update(['v1', at, method, route, host, key].join('\n')).digest('hex');

// Same hostname, different store keys -> different signed contexts.
const a = buildPublicStoreProxyContext(fake('solqaryn-dev.vercel.app', 'GET',
  { 'x-solqaryn-store-key': 'otra', 'x-forwarded-host': 'evil.example.test' }),
  'tiendas/comercio-a/tienda/bootstrap', environment, binding, now);
const b = buildPublicStoreProxyContext(fake('solqaryn-dev.vercel.app'),
  'tiendas/comercio-b/tienda/bootstrap', environment, binding, now);
assert.equal(a.path, '/tienda/bootstrap');
assert.equal(b.path, '/tienda/bootstrap');
assert.equal(a.headers['x-solqaryn-store-key'], 'comercio-a');
assert.equal(b.headers['x-solqaryn-store-key'], 'comercio-b');
assert.equal(a.headers['x-solqaryn-store-origin'], 'solqaryn-dev.vercel.app');
assert.equal(a.headers['x-solqaryn-store-proof'],
  sign(String(now), 'GET', '/tienda/bootstrap', 'solqaryn-dev.vercel.app', 'comercio-a'));
assert.notEqual(a.headers['x-solqaryn-store-proof'], b.headers['x-solqaryn-store-proof']);
assert.equal(buildPublicStoreProxyContext(fake('evil.example.test'),
  'tiendas/comercio-a/tienda/bootstrap', environment, binding, now).status, 403);
assert.equal(buildPublicStoreProxyContext(fake('solqaryn-dev.vercel.app'),
  'tiendas/../tienda/bootstrap', environment, binding, now).status, 400);
assert.equal(buildPublicStoreProxyContext(fake('solqaryn-dev.vercel.app'),
  'tiendas/%2f/tienda/bootstrap', environment, binding, now).status, 400);
assert.equal(buildPublicStoreProxyContext(fake('solqaryn-dev.vercel.app'),
  'tiendas/comercio-a/secret-admin', environment, binding, now).status, 400);
assert.equal(buildPublicStoreProxyContext(fake('solqaryn-dev.vercel.app'),
  'tiendas/comercio-a/tienda/bootstrap', {}, binding, now).status, 503);

// Compatibilidad legacy hasta activar prueba obligatoria: /api/tienda permanece intacto.
const legacy = buildPublicStoreProxyContext(fake('solqaryn-dev.vercel.app'),
  'tienda/bootstrap', {}, binding, now);
assert.equal(legacy.path, '/tienda/bootstrap');
assert.deepEqual(legacy.headers, {});

// Con opt-in firmado, el modo legacy mantiene su ruta pero lleva prueba del host real.
const legacySigned = buildPublicStoreProxyContext(fake('solqaryn-dev.vercel.app'),
  'tienda/bootstrap', environment, binding, now);
assert.equal(legacySigned.headers['x-solqaryn-store-key'], '-');
assert.equal(legacySigned.headers['x-solqaryn-store-proof'],
  sign(String(now), 'GET', '/tienda/bootstrap', 'solqaryn-dev.vercel.app', '-'));

// Proxy integrado: los headers falsificados nunca alcanzan Render.
const source = readFileSync(path.join(root, 'api/backend-proxy.js'), 'utf8');
let captured = null;
const https = {
  request(url, options, callback) {
    captured = { url, headers: options.headers, method: options.method };
    return { setTimeout() {}, end() {}, pipe() {}, on() {} };
  }
};
const module = { exports: {} };
vm.runInNewContext(source, {
  module, Buffer, URL,
  process: { env: environment },
  require(name) {
    if (name === 'node:https') return https;
    if (name === '../server/environment-binding')
      return { resolveEnvironmentBinding: () => binding };
    if (name === '../server/public-store-proxy-context')
      return { buildPublicStoreProxyContext };
    throw new Error('Require desconocido: ' + name);
  },
  console: { warn: () => {} }
});
const req = {
  query: { path: 'tiendas/comercio-a/tienda/bootstrap', page: '1' },
  headers: {
    host: 'solqaryn-dev.vercel.app',
    'x-solqaryn-store-origin': 'evil.example.test',
    'x-solqaryn-store-proof': 'fake',
    'x-solqaryn-store-key': 'comercio-b',
    'x-solqaryn-store-at': '123',
    'x-forwarded-host': 'evil.example.test',
    'x-empresa-id': '999',
    authorization: 'Bearer public-test-placeholder'
  },
  method: 'GET',
  pipe() {}
};
const response = { headersSent: false, setHeader() {}, end() {}, statusCode: 200 };
module.exports(req, response);
assert.equal(captured.url.pathname, '/tienda/bootstrap');
assert.equal(captured.url.searchParams.get('page'), '1');
assert.equal(captured.method, 'GET');
assert.equal(captured.headers['x-solqaryn-store-origin'], 'solqaryn-dev.vercel.app');
assert.equal(captured.headers['x-solqaryn-store-key'], 'comercio-a');
assert.equal(captured.headers['x-forwarded-host'], undefined);
assert.equal(captured.headers['x-solqaryn-environment'], 'DEV');
assert.equal(captured.headers['x-empresa-id'], '999', 'Header no concede autoridad; backend lo ignora en rutas públicas.');
assert.notEqual(captured.headers['x-solqaryn-store-proof'], 'fake');
assert.equal(captured.headers['x-solqaryn-store-proof'].length, 64);

console.log('PUBLIC_STORE_BINDING_PROXY PASS: hosts, rutas, prueba HMAC, headers hostiles y legacy.');
