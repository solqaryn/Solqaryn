// Angular 22.2.1 Phase 4 exact-head certification marker; no runtime behavior.
// Fase 2/4: autoridad exacta Node/npm y contrato declarativo del toolchain; Angular 22.2.1 se certifica en su gate dedicado y Vercel ejecuta este guard vía preinstall.
import fs from 'node:fs';

const expectedNode = '24.21.0';
const expectedNpm = '11.19.0';
const expectedNodeEngine = '24.x';
const expectedNpmEngine = '11.19.0';
const expectedPackageManager = 'npm@11.19.0';

const actualNode = process.versions.node;
const userAgent = process.env.npm_config_user_agent || '';
const npmMatch = userAgent.match(/^npm\/([^\s]+)/);
const actualNpm = npmMatch?.[1] || '';
const packageJson = JSON.parse(fs.readFileSync(new URL('../package.json', import.meta.url), 'utf8'));

const failures = [];
if (actualNode !== expectedNode) failures.push(`Node esperado ${expectedNode}, resuelto ${actualNode}`);
if (actualNpm !== expectedNpm) failures.push(`npm esperado ${expectedNpm}, resuelto ${actualNpm || 'desconocido'}`);
if (packageJson.engines?.node !== expectedNodeEngine) failures.push(`engines.node esperado ${expectedNodeEngine}, resuelto ${packageJson.engines?.node || 'ausente'}`);
if (packageJson.engines?.npm !== expectedNpmEngine) failures.push(`engines.npm esperado ${expectedNpmEngine}, resuelto ${packageJson.engines?.npm || 'ausente'}`);
if (packageJson.packageManager !== expectedPackageManager) failures.push(`packageManager esperado ${expectedPackageManager}, resuelto ${packageJson.packageManager || 'ausente'}`);

if (failures.length) {
  console.error('SOLQARYN_TOOLCHAIN=FAIL');
  for (const failure of failures) console.error(failure);
  process.exit(1);
}

console.log('SOLQARYN_TOOLCHAIN=PASS');
console.log(`NODE=${actualNode}`);
console.log(`NPM=${actualNpm}`);
console.log(`ENGINES_NODE=${packageJson.engines.node}`);
console.log(`ENGINES_NPM=${packageJson.engines.npm}`);
console.log(`PACKAGE_MANAGER=${packageJson.packageManager}`);
