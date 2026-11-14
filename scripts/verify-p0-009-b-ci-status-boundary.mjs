#!/usr/bin/env node
// P0-009.B: synthetic regression only. No Vercel APIs, credentials or environment access.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { evaluateCiEvidence } from './verify-p0-009-a-ci-evidence.mjs';

const QUOTA = 'api-deployments-free-per-day';
export function classifyHistoricalStatus(input) {
  const result = evaluateCiEvidence(input?.devEvidence ?? {});
  const r = input?.prodRecord;
  let prodSignal = 'NO_PROD_PROVIDER_EVIDENCE';
  // Source claims NEVER equal provider readback. A failure report stays visible.
  if (r && r.environment === 'PROD' && r.projectScope === 'INSTITUTIONAL_PROD') {
    if (r.state === 'failure' && r.reason === QUOTA) {
      prodSignal = r.observedAt && r.sourceRef
        ? 'PROD_QUOTA_FAILURE_RECORDED_HISTORICAL'
        : 'PROD_QUOTA_FAILURE_REPORTED_UNVERIFIED';
    } else if (r.state === 'failure') {
      prodSignal = 'PROD_FAILURE_REPORTED_UNVERIFIED';
    } else prodSignal = 'PROD_STATUS_NOT_CURRENTLY_VERIFIED';
  }
  return {
    devCiValid: result.ciValid, devParity: result.devParity,
    prodSignal, prodProviderVerified: false, overallCommercialReady: false,
  };
}

const doc = readFileSync(join(dirname(fileURLToPath(import.meta.url)),
  '../docs/P0-009-A-INVENTARIO-CI-CHECKS-DEV.md'), 'utf8');
assert.match(doc, /48\/48/);
assert.match(doc, /21\/21/);
assert.match(doc, /NO_REVALIDAD/);
assert.match(doc, /NO_VERIFICADO/);

if (process.argv.includes('--self-test')) {
  const sha = 'a'.repeat(40);
  const goodDev = {
    devSha: sha,
    checks: ['verify-solqaryn-project-scope', 'SOLQARYN - Seguridad multiempresa',
      'SOLQARYN - Build y pruebas backend'].map(name => ({ name, sha, conclusion: 'success' })),
    vercelDev: { project: 'solqaryn-dev', gitRef: 'dev', state: 'READY', gitSha: sha },
  };
  const historical = {
    environment: 'PROD', projectScope: 'INSTITUTIONAL_PROD',
    state: 'failure', reason: QUOTA,
    observedAt: '2026-10-20T12:00:00Z', sourceRef: 'SYNTHETIC_FIXTURE_ONLY',
  };
  const fixture = (changes = {}) => ({
    devEvidence: structuredClone(goodDev), prodRecord: structuredClone(historical), ...changes,
  });
  const witness = classifyHistoricalStatus(fixture());
  assert.equal(witness.devCiValid, true);
  assert.equal(witness.prodSignal, 'PROD_QUOTA_FAILURE_RECORDED_HISTORICAL');
  assert.equal(witness.overallCommercialReady, false);
  // N1: no report is not success.
  assert.equal(classifyHistoricalStatus(fixture({ prodRecord: null })).prodSignal,
    'NO_PROD_PROVIDER_EVIDENCE');
  // N2: fictional CI + witness cannot become verified provider evidence.
  assert.equal(witness.prodProviderVerified, false);
  // N3: target=production on DEV project is not the institutional PROD project.
  assert.equal(classifyHistoricalStatus(fixture({
    prodRecord: { ...historical, environment: 'DEV', projectScope: 'solqaryn-dev', target: 'production' },
  })).prodSignal, 'NO_PROD_PROVIDER_EVIDENCE');
  // N4: stale DEV SHA is not parity.
  const stale = structuredClone(goodDev); stale.vercelDev.gitSha = 'b'.repeat(40);
  assert.equal(classifyHistoricalStatus(fixture({ devEvidence: stale })).devParity, false);
  // N5: synthetic READY does not create a native readback.
  assert.equal(classifyHistoricalStatus(fixture({
    prodRecord: { ...historical, state: 'READY' },
  })).prodSignal, 'PROD_STATUS_NOT_CURRENTLY_VERIFIED');
  // N6: incomplete timestamp/source preserves the failure.
  assert.equal(classifyHistoricalStatus(fixture({
    prodRecord: { ...historical, observedAt: undefined },
  })).prodSignal, 'PROD_QUOTA_FAILURE_REPORTED_UNVERIFIED');
  // N7: another failure must not disappear.
  assert.equal(classifyHistoricalStatus(fixture({
    prodRecord: { ...historical, reason: 'other-failure' },
  })).prodSignal, 'PROD_FAILURE_REPORTED_UNVERIFIED');
  // N8: missing CI check is not valid CI.
  const incomplete = structuredClone(goodDev); incomplete.checks.pop();
  assert.equal(classifyHistoricalStatus(fixture({ devEvidence: incomplete })).devCiValid, false);
  // N9: no combination of pure fixtures certifies release.
  for (const r of [fixture(), fixture({ prodRecord: null }),
    fixture({ prodRecord: { ...historical, state: 'READY' } })]) {
    assert.equal(classifyHistoricalStatus(r).overallCommercialReady, false);
  }
  console.log('P0-009.B SYNTHETIC PASS: 1 witness and 9 negative assertions.');
  console.log('LIMIT: no live PROD, QA or Vercel provider verification.');
} else console.log('Use --self-test. This script is offline and read-only.');
