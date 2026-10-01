const CANONICAL_API_UPSTREAMS = Object.freeze({
  DEV: 'https://solqaryn-api-dev-fxx8.onrender.com',
  PROD: 'https://solqaryn-api-prod.onrender.com'
});

function normalizeHttpsOrigin(raw, key) {
  const value = String(raw || '').trim();
  if (!value) throw new Error(`${key} no configurado.`);
  let parsed;
  try { parsed = new URL(value); } catch { throw new Error(`${key} no es una URL válida.`); }
  if (parsed.protocol !== 'https:' || parsed.username || parsed.password || parsed.pathname !== '/' || parsed.search || parsed.hash) {
    throw new Error(`${key} debe ser un origen HTTPS sin credenciales, path, query ni fragmento.`);
  }
  return parsed.origin;
}

function parseBoolean(raw, key) {
  const value = String(raw || '').trim().toLowerCase();
  if (value === 'true') return true;
  if (value === 'false') return false;
  throw new Error(`${key} debe ser true o false.`);
}

function resolveEnvironmentBinding(env = process.env) {
  const environment = String(env.SOLQARYN_ENV || '').trim().toUpperCase();
  if (!Object.hasOwn(CANONICAL_API_UPSTREAMS, environment)) throw new Error('SOLQARYN_ENV debe ser DEV o PROD.');
  const apiUpstream = normalizeHttpsOrigin(env.API_UPSTREAM, 'API_UPSTREAM');
  if (apiUpstream !== CANONICAL_API_UPSTREAMS[environment]) throw new Error('API_UPSTREAM no coincide con el entorno SOLQARYN declarado.');
  const publicOrigin = normalizeHttpsOrigin(env.PUBLIC_ORIGIN, 'PUBLIC_ORIGIN');
  const seoIndexingEnabled = parseBoolean(env.SEO_INDEXING_ENABLED, 'SEO_INDEXING_ENABLED');
  if (environment !== 'PROD' && seoIndexingEnabled) throw new Error('SEO_INDEXING_ENABLED sólo puede activarse en PROD.');
  return Object.freeze({ environment, apiUpstream, publicOrigin, seoIndexingEnabled });
}

module.exports = { CANONICAL_API_UPSTREAMS, resolveEnvironmentBinding };
