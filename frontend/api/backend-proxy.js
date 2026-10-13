const https = require('node:https');
const { resolveEnvironmentBinding } = require('../server/environment-binding');

const HOP_BY_HOP_HEADERS = new Set(['connection','keep-alive','proxy-authenticate','proxy-authorization','te','trailer','transfer-encoding','upgrade']);
const UPSTREAM_TIMEOUT_MS = 30000;

function fail(res, statusCode, message, code) {
  if (res.headersSent) { res.destroy(); return; }
  res.statusCode = statusCode;
  res.setHeader('Content-Type', 'application/json; charset=utf-8');
  res.setHeader('Cache-Control', 'private, no-store, max-age=0');
  res.end(JSON.stringify({ success: false, message, ...(code ? { code } : {}) }));
}

function normalizePath(raw) {
  const value = Array.isArray(raw) ? raw.join('/') : String(raw || '');
  const segments = value.split('/').filter(Boolean);
  if (!segments.length || segments.some(segment => segment === '.' || segment === '..' || segment.includes('\\'))) return null;
  return segments.map(segment => encodeURIComponent(segment)).join('/');
}

function requestBodyBuffer(req) {
  if (req.body === undefined || req.body === null) return null;
  if (Buffer.isBuffer(req.body)) return req.body;
  if (typeof req.body === 'string') return Buffer.from(req.body);
  return Buffer.from(JSON.stringify(req.body));
}

function requestHeaders(req, environment, bodyBuffer) {
  const headers = {};
  for (const [name, value] of Object.entries(req.headers || {})) {
    const lower = name.toLowerCase();
    if (lower === 'host' || lower === 'content-length' || HOP_BY_HOP_HEADERS.has(lower) || value === undefined) continue;
    headers[name] = value;
  }
  headers['x-solqaryn-environment'] = environment;
  if (bodyBuffer) headers['content-length'] = String(bodyBuffer.length);
  return headers;
}

function responseHeaders(upstreamRes, res, environment) {
  for (const [name, value] of Object.entries(upstreamRes.headers || {})) {
    if (HOP_BY_HOP_HEADERS.has(name.toLowerCase()) || value === undefined) continue;
    res.setHeader(name, value);
  }
  res.setHeader('x-solqaryn-environment', environment);
}

function upstreamUnavailable(req, res, environment, error) {
  const reason = typeof error?.code === 'string' && /^[A-Z0-9_]{2,30}$/.test(error.code)
    ? error.code
    : 'UNKNOWN';
  // Solo metadatos no sensibles: nunca registrar URL/query, cabeceras, JWT ni body.
  console.warn('SOLQARYN_UPSTREAM_UNAVAILABLE env=' + environment + ' method=' + req.method + ' reason=' + reason);
  if (res.headersSent) { res.destroy(); return; }
  res.setHeader('Retry-After', '3');
  res.setHeader('x-solqaryn-environment', environment);
  fail(res, 503, 'El servidor no está disponible temporalmente. Intenta nuevamente en unos momentos.', 'API_UPSTREAM_UNAVAILABLE');
}

module.exports = function handler(req, res) {
  let binding;
  try { binding = resolveEnvironmentBinding(); }
  catch { fail(res, 503, 'Configuración de entorno no disponible.'); return; }

  const path = normalizePath(req.query?.path);
  if (!path || path === 'backend-proxy') { fail(res, 400, 'Ruta API inválida.'); return; }

  const target = new URL(`/${path}`, `${binding.apiUpstream}/`);
  for (const [key, raw] of Object.entries(req.query || {})) {
    if (key === 'path') continue;
    for (const value of (Array.isArray(raw) ? raw : [raw])) {
      if (value !== undefined && value !== null) target.searchParams.append(key, String(value));
    }
  }

  const bodyBuffer = requestBodyBuffer(req);
  const upstreamReq = https.request(target, { method: req.method, headers: requestHeaders(req, binding.environment, bodyBuffer) }, upstreamRes => {
    res.statusCode = upstreamRes.statusCode || 502;
    responseHeaders(upstreamRes, res, binding.environment);
    upstreamRes.pipe(res);
  });
  upstreamReq.setTimeout(UPSTREAM_TIMEOUT_MS, () => {
    const error = new Error('upstream socket timeout');
    error.code = 'ETIMEDOUT';
    upstreamReq.destroy(error);
  });
  upstreamReq.on('error', error => upstreamUnavailable(req, res, binding.environment, error));
  if (bodyBuffer) upstreamReq.end(bodyBuffer); else req.pipe(upstreamReq);
};
