#!/usr/bin/env node
// P0-007.B: negative evidence test. Never translates source source list into UX certification.
import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const sourcePath = 'frontend/src/app/features/storefront/storefront-checkout.component.ts';
const requiredEvidence = [
  'browserE2E', 'desktop', 'tablet', 'mobile320', 'accessibility',
  'errorStates', 'backendRBAC', 'crossTenantIsolation',
];

function evaluateEvidence({ knownSource, evidence = {} }) {
  if (!knownSource) return { status: 'SOURCE_NOT_CONFIRMED', missing: requiredEvidence };
  const missing = requiredEvidence.filter(key => evidence[key] !== true);
  // Even a complete claim is not proof; run IDs, SHA, images and independent
  // provider readback are required before any UX attestation outside this test.
  return { status: missing.length ? 'UX_NOT_CERTIFIED' : 'INDEPENDENT_REVIEW_REQUIRED', missing };
}

function selfTest() {
  const staticOnly = evaluateEvidence({ knownSource: true });
  assert.equal(staticOnly.status, 'UX_NOT_CERTIFIED');
  assert.equal(staticOnly.missing.length, requiredEvidence.length);
  const buildAndRoute = evaluateEvidence({ knownSource: true, evidence: { browserE2E: false } });
  assert.equal(buildAndRoute.status, 'UX_NOT_CERTIFIED');
  const missingTenant = evaluateEvidence({ knownSource: true,
    evidence: Object.fromEntries(requiredEvidence.map(key => [key, key !== 'crossTenantIsolation'])) });
  assert.deepEqual(missingTenant.missing, ['crossTenantIsolation']);
  const missingMobile = evaluateEvidence({ knownSource: true,
    evidence: Object.fromEntries(requiredEvidence.map(key => [key, key !== 'mobile320'])) });
  assert.deepEqual(missingMobile.missing, ['mobile320']);
  const nonexistent = evaluateEvidence({ knownSource: false,
    evidence: Object.fromEntries(requiredEvidence.map(key => [key, true])) });
  assert.equal(nonexistent.status, 'SOURCE_NOT_CONFIRMED');
  const fabricatedComplete = evaluateEvidence({ knownSource: true,
    evidence: Object.fromEntries(requiredEvidence.map(key => [key, true])) });
  assert.equal(fabricatedComplete.status, 'INDEPENDENT_REVIEW_REQUIRED');
  for (const candidate of [staticOnly, buildAndRoute, missingTenant, missingMobile, nonexistent, fabricatedComplete]) {
    assert.notEqual(candidate.status, 'UX_CERTIFIED');
  }
  console.log('P0-007.B NEGATIVE PASS: 6 fixtures; source/CI/claimed proofs cannot auto-certify UX.');
}

if (process.argv.includes('--self-test-only')) {
  selfTest();
  process.exit(0);
}

const catalogValidator = join(root, 'scripts/verify-p0-007-a-component-catalog.mjs');
const catalogCheck = spawnSync(process.execPath, [catalogValidator, '--self-test'], {
  cwd: root, encoding: 'utf8', timeout: 120000,
});
if (catalogCheck.error || catalogCheck.status !== 0) {
  throw new Error('P0-007.B: catalog A drift or test failure: ' + (catalogCheck.stderr || catalogCheck.error));
}
const catalogDoc = readFileSync(join(root, 'docs/P0-007-A-INVENTARIO-COMPONENTES-DEV.md'), 'utf8');
const paths = [...catalogDoc.matchAll(/^- \x60(frontend\/src\/app\/(?:features|shared)\/[^\x60]+\.component\.ts)\x60$/gm)].map(x => x[1]);
assert.equal(paths.length, 137);
assert.ok(paths.includes(sourcePath));
assert.ok(existsSync(join(root, sourcePath)));
const verdict = evaluateEvidence({ knownSource: true });
assert.equal(verdict.status, 'UX_NOT_CERTIFIED');
const report = readFileSync(join(root, 'docs/P0-007-B-LIMITES-CERTIFICACION-UX-DEV.md'), 'utf8');
assert.match(report, /ESTADO_UX=NO_CERTIFICADO/);
assert.match(report, /E2E_NO_EJECUTADO/);
assert.doesNotMatch(report, /ESTADO_UX=CERTIFICADO/);
selfTest();
console.log('P0-007.B STATIC PASS: 137 source paths verified via A, example exists, UX not attested.');
console.log('LIMIT: no actual browser E2E, screenshots, a11y, backend RBAC, tenancy or deployment verified.');
