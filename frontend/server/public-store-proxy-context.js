const { createHmac } = require('node:crypto');

const STORE_CONTEXT_HEADERS = Object.freeze({
  origin: 'x-solqaryn-store-origin',
  key: 'x-solqaryn-store-key',
  at: 'x-solqaryn-store-at',
  proof: 'x-solqaryn-store-proof'
});

function parsePublicStorePath(path) {
  const parts = String(path || '').split('/');
  if (parts[0] !== 'tiendas') return { path: '/' + path, storeKey: null, scoped: false };
  const key = parts[1];
  if (parts.length < 4 || parts[2] !== 'tienda' ||
      !/^[a-z][a-z0-9-]{0,46}[a-z0-9]$/.test(key || '') ||
      parts.slice(3).some(part => !part || part.includes('\\'))) return null;
  return { path: '/' + parts.slice(2).join('/'), storeKey: key, scoped: true };
}

function exactPublicHostname(raw) {
  if (typeof raw !== 'string' || raw.length < 4 || raw.length > 253 ||
      !raw.includes('.') || raw !== raw.trim() || raw.includes(':') ||
      raw.includes('/') || raw.includes('\\')) return null;
  const host = raw.toLowerCase();
  if (!host.split('.').every(label => label.length >= 1 && label.length <= 63 &&
      /^[a-z0-9]+(?:[a-z0-9-]*[a-z0-9])?$/.test(label))) return null;
  return host;
}

function buildPublicStoreProxyContext(req, canonicalPath, environment = process.env, nowSeconds = Math.floor(Date.now() / 1000)) {
  const route = parsePublicStorePath(canonicalPath);
  if (!route) return { error: 'INVALID_STORE_PATH', status: 400 };
  if (!route.path.startsWith('/tienda/') && route.path !== '/tienda') return { path: route.path, headers: {} };

  const requireRootProof = environment.SOLQARYN_PUBLIC_STORE_PROOF_ENABLED === 'true';
  if (!route.scoped && !requireRootProof) return { path: route.path, headers: {} };

  const secret = environment.SOLQARYN_PUBLIC_STORE_SIGNING_KEY;
  if (typeof secret !== 'string' || Buffer.byteLength(secret, 'utf8') < 32)
    return { error: 'STORE_BINDING_NOT_CONFIGURED', status: 503 };

  // Sólo el Host de la solicitud HTTPS terminada por Vercel; nunca forwarded-host.
  const host = exactPublicHostname(req.headers?.host);
  if (!host) return { error: 'INVALID_PUBLIC_HOST', status: 400 };

  const method = String(req.method || '').toUpperCase();
  if (!/^(GET|HEAD|POST|PUT|PATCH|DELETE|OPTIONS)$/.test(method))
    return { error: 'INVALID_PUBLIC_METHOD', status: 400 };

  const key = route.storeKey ?? '-';
  const at = String(nowSeconds);
  const canonical = ['v1', at, method, route.path, host, key].join('\n');
  const proof = createHmac('sha256', secret).update(canonical, 'utf8').digest('hex');
  return { path: route.path, headers: {
    [STORE_CONTEXT_HEADERS.origin]: host,
    [STORE_CONTEXT_HEADERS.key]: key,
    [STORE_CONTEXT_HEADERS.at]: at,
    [STORE_CONTEXT_HEADERS.proof]: proof
  }};
}

module.exports = { STORE_CONTEXT_HEADERS, parsePublicStorePath, exactPublicHostname, buildPublicStoreProxyContext };
