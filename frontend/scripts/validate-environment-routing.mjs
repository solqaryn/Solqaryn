import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';

const frontendDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const require = createRequire(import.meta.url);
const { CANONICAL_API_UPSTREAMS, VERCEL_PROJECT_BINDINGS, resolveEnvironmentBinding } = require('../server/environment-binding');
const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

const vercel = JSON.parse(await readFile(path.join(frontendDir, 'vercel.json'), 'utf8'));
const seoUtils = await readFile(path.join(frontendDir, 'server/seo-utils.js'), 'utf8');
const proxy = await readFile(path.join(frontendDir, 'api/backend-proxy.js'), 'utf8');
const ignoreBuild = await readFile(path.join(frontendDir, 'scripts/vercel-ignore-build.mjs'), 'utf8');

const DEV_PROJECT_ID = 'prj_1Anhx5mWyXEBX89lWC24Py6JXe7A';
const QA_PROJECT_ID = 'prj_n5STx5F6VboqXd1oLUMR8AvZZtml';
const PROD_PROJECT_ID = 'prj_si3ORH7lBhM4aSAYfYvXsbJT2lHA';

const apiRewrites = (vercel.rewrites || []).filter(item => item?.source === '/api/:path*');
expect(apiRewrites.length === 1, 'Routing: debe existir exactamente un rewrite /api/:path*.');
expect(apiRewrites[0]?.destination === '/api/backend-proxy?path=:path*', 'Routing: /api debe resolver al proxy local project-bound.');
expect(!apiRewrites.some(item => Array.isArray(item.has) && item.has.some(c => c?.type === 'host')), 'Routing: el backend no puede seleccionarse por hostname.');
expect(!(vercel.rewrites || []).some(item => typeof item?.destination === 'string' && /onrender\.com/i.test(item.destination)), 'Routing: vercel.json no puede hardcodear backends Render.');
expect(proxy.includes('resolveEnvironmentBinding()') && proxy.includes("headers['x-solqaryn-environment']"), 'Routing: el proxy debe exigir binding explícito.');
expect(ignoreBuild.includes("VERCEL_PROJECT_ID") && ignoreBuild.includes(PROD_PROJECT_ID) && ignoreBuild.includes("branch === 'dev'"), 'Routing: commits dev deben ser ignorados por el proyecto Vercel PROD mediante identidad de proyecto, no hostname.');
expect(seoUtils.includes("require('./environment-binding')") && !/PROD_API|DEV_API|PRODUCTION_HOST/.test(seoUtils), 'SEO: host no puede seleccionar API ni fallback.');

const resolves = env => { try { return resolveEnvironmentBinding(env); } catch { return null; } };
const rejects = env => { try { resolveEnvironmentBinding(env); return false; } catch { return true; } };

expect(VERCEL_PROJECT_BINDINGS[DEV_PROJECT_ID]?.environment === 'DEV', 'Binding de proyecto DEV debe existir.');
expect(VERCEL_PROJECT_BINDINGS[QA_PROJECT_ID]?.environment === 'QA', 'Binding de proyecto QA debe existir.');
expect(VERCEL_PROJECT_BINDINGS[PROD_PROJECT_ID]?.environment === 'PROD', 'Binding de proyecto PROD debe existir.');

expect(resolves({VERCEL_PROJECT_ID:DEV_PROJECT_ID})?.apiUpstream === CANONICAL_API_UPSTREAMS.DEV, 'Proyecto Vercel DEV debe resolver DEV sin depender de variables manuales.');
expect(resolves({VERCEL_PROJECT_ID:QA_PROJECT_ID})?.apiUpstream === CANONICAL_API_UPSTREAMS.QA, 'Proyecto Vercel QA debe resolver QA sin depender de variables manuales.');
expect(resolves({VERCEL_PROJECT_ID:QA_PROJECT_ID})?.publicOrigin === 'https://solqaryn-qa.vercel.app', 'Proyecto Vercel QA debe resolver origen público canónico.');
expect(resolves({VERCEL_PROJECT_ID:QA_PROJECT_ID})?.seoIndexingEnabled === false, 'Proyecto Vercel QA debe permanecer noindex.');
expect(resolves({VERCEL_PROJECT_ID:PROD_PROJECT_ID})?.apiUpstream === CANONICAL_API_UPSTREAMS.PROD, 'Proyecto Vercel PROD debe resolver PROD sin depender de variables manuales.');
expect(resolves({VERCEL_PROJECT_ID:PROD_PROJECT_ID})?.publicOrigin === 'https://solqaryn-prod.vercel.app', 'Proyecto Vercel PROD debe resolver origen público canónico.');
expect(resolves({VERCEL_PROJECT_ID:PROD_PROJECT_ID})?.seoIndexingEnabled === true, 'Proyecto Vercel PROD debe habilitar SEO canónico.');

expect(resolves({SOLQARYN_ENV:'DEV',API_UPSTREAM:CANONICAL_API_UPSTREAMS.DEV,PUBLIC_ORIGIN:'https://solqaryn-dev.vercel.app',SEO_INDEXING_ENABLED:'false'})?.environment === 'DEV','Binding explícito DEV válido fuera de Vercel.');
expect(resolves({SOLQARYN_ENV:'QA',API_UPSTREAM:CANONICAL_API_UPSTREAMS.QA,PUBLIC_ORIGIN:'https://solqaryn-qa.vercel.app',SEO_INDEXING_ENABLED:'false'})?.environment === 'QA','Binding explícito QA válido fuera de Vercel.');
expect(resolves({SOLQARYN_ENV:'PROD',API_UPSTREAM:CANONICAL_API_UPSTREAMS.PROD,PUBLIC_ORIGIN:'https://solqaryn-prod.vercel.app',SEO_INDEXING_ENABLED:'true'})?.environment === 'PROD','Binding explícito PROD válido fuera de Vercel.');

expect(rejects({}), 'Configuración ausente fuera de Vercel debe fallar cerrada.');
expect(rejects({VERCEL_PROJECT_ID:'prj_desconocido'}), 'Proyecto Vercel desconocido debe fallar cerrado.');
expect(rejects({VERCEL_PROJECT_ID:DEV_PROJECT_ID,SOLQARYN_ENV:'QA'}), 'Proyecto DEV no puede declararse QA.');
expect(rejects({VERCEL_PROJECT_ID:DEV_PROJECT_ID,SOLQARYN_ENV:'PROD'}), 'Proyecto DEV no puede declararse PROD.');
expect(rejects({VERCEL_PROJECT_ID:QA_PROJECT_ID,SOLQARYN_ENV:'DEV'}), 'Proyecto QA no puede declararse DEV.');
expect(rejects({VERCEL_PROJECT_ID:QA_PROJECT_ID,SOLQARYN_ENV:'PROD'}), 'Proyecto QA no puede declararse PROD.');
expect(rejects({VERCEL_PROJECT_ID:QA_PROJECT_ID,API_UPSTREAM:CANONICAL_API_UPSTREAMS.DEV}), 'Proyecto QA no puede apuntar a API DEV.');
expect(rejects({VERCEL_PROJECT_ID:QA_PROJECT_ID,API_UPSTREAM:CANONICAL_API_UPSTREAMS.PROD}), 'Proyecto QA no puede apuntar a API PROD.');
expect(rejects({VERCEL_PROJECT_ID:PROD_PROJECT_ID,API_UPSTREAM:CANONICAL_API_UPSTREAMS.DEV}), 'Proyecto PROD no puede apuntar a API DEV.');
expect(rejects({VERCEL_PROJECT_ID:DEV_PROJECT_ID,PUBLIC_ORIGIN:'https://solqaryn-prod.vercel.app'}), 'Proyecto DEV no puede usar origen público PROD.');
expect(rejects({VERCEL_PROJECT_ID:QA_PROJECT_ID,PUBLIC_ORIGIN:'https://solqaryn-dev.vercel.app'}), 'Proyecto QA no puede usar origen público DEV.');
expect(rejects({VERCEL_PROJECT_ID:QA_PROJECT_ID,PUBLIC_ORIGIN:'https://solqaryn-prod.vercel.app'}), 'Proyecto QA no puede usar origen público PROD.');
expect(rejects({VERCEL_PROJECT_ID:DEV_PROJECT_ID,SEO_INDEXING_ENABLED:'true'}), 'Proyecto DEV no puede activar SEO PROD.');
expect(rejects({VERCEL_PROJECT_ID:QA_PROJECT_ID,SEO_INDEXING_ENABLED:'true'}), 'Proyecto QA no puede activar SEO PROD.');
expect(rejects({SOLQARYN_ENV:'DEV',API_UPSTREAM:CANONICAL_API_UPSTREAMS.QA,PUBLIC_ORIGIN:'https://solqaryn-dev.vercel.app',SEO_INDEXING_ENABLED:'false'}), 'DEV -> QA debe rechazarse.');
expect(rejects({SOLQARYN_ENV:'DEV',API_UPSTREAM:CANONICAL_API_UPSTREAMS.PROD,PUBLIC_ORIGIN:'https://solqaryn-dev.vercel.app',SEO_INDEXING_ENABLED:'false'}), 'DEV -> PROD debe rechazarse.');
expect(rejects({SOLQARYN_ENV:'QA',API_UPSTREAM:CANONICAL_API_UPSTREAMS.DEV,PUBLIC_ORIGIN:'https://solqaryn-qa.vercel.app',SEO_INDEXING_ENABLED:'false'}), 'QA -> DEV debe rechazarse.');
expect(rejects({SOLQARYN_ENV:'QA',API_UPSTREAM:CANONICAL_API_UPSTREAMS.PROD,PUBLIC_ORIGIN:'https://solqaryn-qa.vercel.app',SEO_INDEXING_ENABLED:'false'}), 'QA -> PROD debe rechazarse.');
expect(rejects({SOLQARYN_ENV:'PROD',API_UPSTREAM:CANONICAL_API_UPSTREAMS.DEV,PUBLIC_ORIGIN:'https://solqaryn-prod.vercel.app',SEO_INDEXING_ENABLED:'true'}), 'PROD -> DEV debe rechazarse.');

if (failures.length) {
  console.error('Environment routing contract FAILED:');
  failures.forEach(f => console.error(`- ${f}`));
  process.exit(1);
}
console.log('Environment routing contract: OK (VERCEL_PROJECT_ID canonical, no host fallback, fail-closed).');
