import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { EventEmitter } from 'node:events';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import vm from 'node:vm';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const proxySource = readFileSync(path.join(root, 'api/backend-proxy.js'), 'utf8');

function harness({ validBinding = true, body = { nombreUsuario: 'privado', password: 'secreto' } } = {}) {
  const warnings = [];
  let upstreamCalls = 0;
  let timeoutCallback;
  let upstreamCallback;
  let upstreamRequest;
  const fakeHttps = {
    request(_target, options, callback) {
      upstreamCalls += 1;
      upstreamCallback = callback;
      assert.equal(options.headers['x-solqaryn-environment'], 'DEV');
      upstreamRequest = new EventEmitter();
      upstreamRequest.setTimeout = (ms, fn) => { assert.equal(ms, 30000); timeoutCallback = fn; };
      upstreamRequest.destroy = error => upstreamRequest.emit('error', error);
      upstreamRequest.end = () => {};
      return upstreamRequest;
    }
  };
  const module = { exports: {} };
  vm.runInNewContext(proxySource, {
    module, Buffer, URL,
    require(name) {
      if (name === 'node:https') return fakeHttps;
      if (name === '../server/environment-binding') {
        return {
          resolveEnvironmentBinding: () => {
            if (!validBinding) throw Error('Proyecto no autorizado');
            return { environment: 'DEV', apiUpstream: 'https://solqaryn-api-dev-fxx8.onrender.com' };
          }
        };
      }
      throw Error('Dependencia inesperada');
    },
    console: { warn: message => warnings.push(String(message)) }
  }, { filename: 'backend-proxy.js' });

  const req = {
    query: { path: 'auth/login' }, method: 'POST',
    headers: { authorization: 'Bearer SECRETO', host: 'localhost' }, body,
    pipe: dest => { dest.end(); return dest; }
  };
  const res = {
    headers: {}, headersSent: false, body: undefined, statusCode: 200,
    setHeader(name, value) { this.headers[name.toLowerCase()] = value; },
    end(value) { this.body = value; this.headersSent = true; },
    destroy() { this.destroyed = true; }
  };
  module.exports(req, res);
  return {
    res, warnings,
    triggerError(code) {
      const error = new Error('Dato sensible no divulgable');
      error.code = code;
      upstreamRequest.emit('error', error);
    },
    triggerTimeout() { timeoutCallback(); },
    triggerResponse(statusCode, headers = {}) {
      upstreamCallback({ statusCode, headers, pipe: out => out.end('{"success":true}') });
    },
    get upstreamCalls() { return upstreamCalls; }
  };
}

{
  const h = harness();
  assert.equal(h.upstreamCalls, 1);
  h.triggerError('ECONNRESET');
  assert.equal(h.res.statusCode, 503);
  assert.equal(h.res.headers['retry-after'], '3');
  assert.equal(h.res.headers['x-solqaryn-environment'], 'DEV');
  assert.equal(h.res.headers['cache-control'], 'private, no-store, max-age=0');
  assert.equal(JSON.parse(h.res.body).code, 'API_UPSTREAM_UNAVAILABLE');
  assert.equal(h.upstreamCalls, 1, 'Nunca reintentar un POST autenticado');
  assert.ok(h.warnings[0].includes('reason=ECONNRESET'));
  assert.ok(!h.warnings[0].includes('secreto') && !h.warnings[0].includes('privado'));
}

{
  const h = harness({ body: null });
  h.triggerTimeout();
  assert.equal(h.res.statusCode, 503);
  assert.ok(h.warnings[0].includes('reason=ETIMEDOUT'));
}

{
  const h = harness({ validBinding: false });
  assert.equal(h.upstreamCalls, 0, 'Binding Vercel desconocido debe fallar cerrado');
  assert.equal(h.res.statusCode, 503);
  assert.ok(!JSON.stringify(h.res).includes('API_UPSTREAM_UNAVAILABLE'));
}

{
  const h = harness();
  h.triggerResponse(401, { 'cache-control': 'private, no-store', 'www-authenticate': 'Bearer', connection: 'keep-alive' });
  assert.equal(h.res.statusCode, 401, 'Errores del backend se preservan sin mutar');
  assert.equal(h.res.headers['cache-control'], 'private, no-store');
  assert.equal(h.res.headers['www-authenticate'], 'Bearer');
  assert.equal(h.res.headers.connection, undefined);
  assert.equal(h.res.headers['x-solqaryn-environment'], 'DEV');
  assert.equal(h.upstreamCalls, 1);
}

console.log('Disponibilidad API: proxy aislado, 503 transitorio, sin reintentar POST ni revelar secretos (PASS).');
