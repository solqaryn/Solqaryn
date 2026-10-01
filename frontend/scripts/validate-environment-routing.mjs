import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';

const frontendDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const require = createRequire(import.meta.url);
const { CANONICAL_API_UPSTREAMS, resolveEnvironmentBinding } = require('../server/environment-binding');
const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

const vercel = JSON.parse(await readFile(path.join(frontendDir, 'vercel.json'), 'utf8'));
const seoUtils = await readFile(path.join(frontendDir, 'server/seo-utils.js'), 'utf8');
const proxy = await readFile(path.join(frontendDir, 'api/backend-proxy.js'), 'utf8');

const apiRewrites = (vercel.rewrites || []).filter(item => item?.source === '/api/:path*');
expect(apiRewrites.length === 1, 'Routing: debe existir exactamente un rewrite /api/:path*.');
expect(apiRewrites[0]?.destination === '/api/backend-proxy?path=:path*', 'Routing: /api debe resolver al proxy local project-bound.');
expect(!apiRewrites.some(item => Array.isArray(item.has) && item.has.some(c => c?.type === 'host')), 'Routing: el backend no puede seleccionarse por hostname.');
expect(!(vercel.rewrites || []).some(item => typeof item?.destination === 'string' && /onrender\.com/i.test(item.destination)), 'Routing: vercel.json no puede hardcodear backends Render.');
expect(proxy.includes('resolveEnvironmentBinding()') && proxy.includes("headers['x-solqaryn-environment']"), 'Routing: el proxy debe exigir binding explícito.');
expect(seoUtils.includes("require('./environment-binding')") && !/PROD_API|DEV_API|PRODUCTION_HOST/.test(seoUtils), 'SEO: host no puede seleccionar API ni fallback.');

const resolves = env => { try { return resolveEnvironmentBinding(env); } catch { return null; } };
const rejects = env => { try { resolveEnvironmentBinding(env); return false; } catch { return true; } };

expect(resolves({SOLQARYN_ENV:'DEV',API_UPSTREAM:CANONICAL_API_UPSTREAMS.DEV,PUBLIC_ORIGIN:'https://solqaryn-dev.vercel.app',SEO_INDEXING_ENABLED:'false'})?.environment === 'DEV','Binding DEV válido.');
expect(resolves({SOLQARYN_ENV:'PROD',API_UPSTREAM:CANONICAL_API_UPSTREAMS.PROD,PUBLIC_ORIGIN:'https://solqaryn-prod.vercel.app',SEO_INDEXING_ENABLED:'true'})?.environment === 'PROD','Binding PROD válido.');
expect(rejects({}), 'Configuración ausente debe fallar cerrada.');
expect(rejects({SOLQARYN_ENV:'DEV',API_UPSTREAM:CANONICAL_API_UPSTREAMS.PROD,PUBLIC_ORIGIN:'https://solqaryn-dev.vercel.app',SEO_INDEXING_ENABLED:'false'}), 'DEV -> PROD debe rechazarse.');
expect(rejects({SOLQARYN_ENV:'PROD',API_UPSTREAM:CANONICAL_API_UPSTREAMS.DEV,PUBLIC_ORIGIN:'https://solqaryn-prod.vercel.app',SEO_INDEXING_ENABLED:'true'}), 'PROD -> DEV debe rechazarse.');
expect(rejects({SOLQARYN_ENV:'DEV',API_UPSTREAM:CANONICAL_API_UPSTREAMS.DEV,PUBLIC_ORIGIN:'https://solqaryn-dev.vercel.app',SEO_INDEXING_ENABLED:'true'}), 'DEV no puede indexarse como PROD.');

if (failures.length) {
  console.error('Environment routing contract FAILED:');
  failures.forEach(f => console.error(`- ${f}`));
  process.exit(1);
}
console.log('Environment routing contract: OK (project-bound, no host fallback, fail-closed).');
