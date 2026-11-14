// Angular 22.2.1 Phase 4 exact-head certification marker; no runtime behavior.
// Fase 2/4: autoridad exacta Node/npm y contrato declarativo del toolchain; Angular 22.2.1 se certifica en su gate dedicado y Vercel ejecuta este guard vía preinstall.
import fs from 'node:fs';
import { assessToolchain } from './toolchain-policy.mjs';

const actualNode = process.versions.node;
const userAgent = process.env.npm_config_user_agent || '';
const npmMatch = userAgent.match(/^npm\/([^\s]+)/);
const actualNpm = npmMatch?.[1] || '';
const packageJson = JSON.parse(fs.readFileSync(new URL('../package.json', import.meta.url), 'utf8'));

// Vercel sets VERCEL=1 and exposes a Node major (24.x), not an exact patch.
// CI/local checks remain exact and fail closed on version drift.
const { failures, mode } = assessToolchain({
  actualNode, actualNpm, isVercel: process.env.VERCEL === '1', packageJson,
});
if (failures.length) {
  console.error('SOLQARYN_TOOLCHAIN=FAIL');
  for (const failure of failures) console.error(failure);
  process.exit(1);
}

console.log('SOLQARYN_TOOLCHAIN=PASS');
console.log(`TOOLCHAIN_MODE=${mode}`);
console.log(`NODE=${actualNode}`);
console.log(`NPM=${actualNpm}`);
console.log(`ENGINES_NODE=${packageJson.engines.node}`);
console.log(`ENGINES_NPM=${packageJson.engines.npm}`);
console.log(`PACKAGE_MANAGER=${packageJson.packageManager}`);
