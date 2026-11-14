const CANONICAL_API_UPSTREAMS = Object.freeze({
  DEV: 'https://solqaryn-api-dev-fxx8.onrender.com',
  QA: 'https://solqaryn-api-qa.onrender.com',
  PROD: 'https://solqaryn-api-prod.onrender.com'
});

const VERCEL_PROJECT_BINDINGS = Object.freeze({
  'prj_1Anhx5mWyXEBX89lWC24Py6JXe7A': Object.freeze({
    environment: 'DEV',
    apiUpstream: CANONICAL_API_UPSTREAMS.DEV,
    publicOrigin: 'https://solqaryn-dev.vercel.app',
    seoIndexingEnabled: false
  }),
  'prj_n5STx5F6VboqXd1oLUMR8AvZZtml': Object.freeze({
    environment: 'QA',
    apiUpstream: CANONICAL_API_UPSTREAMS.QA,
    publicOrigin: 'https://solqaryn-qa.vercel.app',
    seoIndexingEnabled: false
  }),
  'prj_si3ORH7lBhM4aSAYfYvXsbJT2lHA': Object.freeze({
    environment: 'PROD',
    apiUpstream: CANONICAL_API_UPSTREAMS.PROD,
    publicOrigin: 'https://solqaryn-prod.vercel.app',
    seoIndexingEnabled: true
  })
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

function validateOptionalOverride(env, key, expected, normalizer = value => String(value || '').trim()) {
  if (env[key] === undefined || env[key] === null || String(env[key]).trim() === '') return;
  const actual = normalizer(env[key], key);
  if (actual !== expected) throw new Error(`${key} no coincide con el binding canónico del proyecto Vercel.`);
}

function resolveExplicitBinding(env) {
  const environment = String(env.SOLQARYN_ENV || '').trim().toUpperCase();
  if (!Object.hasOwn(CANONICAL_API_UPSTREAMS, environment)) throw new Error('SOLQARYN_ENV debe ser DEV, QA o PROD.');
  const apiUpstream = normalizeHttpsOrigin(env.API_UPSTREAM, 'API_UPSTREAM');
  if (apiUpstream !== CANONICAL_API_UPSTREAMS[environment]) throw new Error('API_UPSTREAM no coincide con el entorno SOLQARYN declarado.');
  const publicOrigin = normalizeHttpsOrigin(env.PUBLIC_ORIGIN, 'PUBLIC_ORIGIN');
  const seoIndexingEnabled = parseBoolean(env.SEO_INDEXING_ENABLED, 'SEO_INDEXING_ENABLED');
  if (environment !== 'PROD' && seoIndexingEnabled) throw new Error('SEO_INDEXING_ENABLED sólo puede activarse en PROD.');
  return Object.freeze({ environment, apiUpstream, publicOrigin, seoIndexingEnabled, source: 'explicit' });
}

function resolveEnvironmentBinding(env = process.env) {
  const projectId = String(env.VERCEL_PROJECT_ID || '').trim();
  if (!projectId) return resolveExplicitBinding(env);

  const binding = VERCEL_PROJECT_BINDINGS[projectId];
  if (!binding) throw new Error('VERCEL_PROJECT_ID no pertenece a un proyecto SOLQARYN autorizado.');

  validateOptionalOverride(env, 'SOLQARYN_ENV', binding.environment, value => String(value || '').trim().toUpperCase());
  validateOptionalOverride(env, 'API_UPSTREAM', binding.apiUpstream, normalizeHttpsOrigin);
  validateOptionalOverride(env, 'PUBLIC_ORIGIN', binding.publicOrigin, normalizeHttpsOrigin);
  validateOptionalOverride(env, 'SEO_INDEXING_ENABLED', binding.seoIndexingEnabled, parseBoolean);

  return Object.freeze({ ...binding, projectId, source: 'vercel-project' });
}

module.exports = { CANONICAL_API_UPSTREAMS, VERCEL_PROJECT_BINDINGS, resolveEnvironmentBinding };
