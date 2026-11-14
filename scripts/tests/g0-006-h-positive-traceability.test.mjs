// G0-006.H positive regression for the current requirement->code->test->closure
// contract. All receipt SHAs are synthetic; no external GitHub/Sheet certification.
import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { verifyDirectDevTraceability } from '../quality/direct-dev-traceability.mjs';

const tree = 'a'.repeat(40), published = 'b'.repeat(40);
const evidence = 'Synthetic positive directed contract regression, not live proof';
const receipt = () => ({
  scopeId: 'G0-006.H', requirementId: 'G0-006.H',
  sourceAuthority: 'PLAN_COMERCIAL_INICIAL_V3', requestedAction: 'VERIFY_CURRENT_SCOPE',
  state: 'LISTO', integrationMode: 'DIRECT_DEV_FOUR_AUDITS',
  candidateTreeSha: tree, publishedTreeSha: tree, publishedSha: published,
  devReadbackSha: published, commitFastForwardVerified: true,
  devBranchVerified: true, diffReviewed: true,
  codePaths: ['scripts/quality/direct-dev-traceability.mjs'],
  testPaths: ['scripts/tests/g0-006-h-positive-traceability.test.mjs'],
  negativeCasesVerified: true,
  testRuns: [{ status: 'PASS', treeSha: tree, evidence }],
  audits: Object.fromEntries(['functional','security','integration','postIntegration']
    .map(name => [name, { status: 'PASS',
      sha: name === 'postIntegration' ? published : tree, evidence }])),
  planReadbackVerified: true, colaReadbackVerified: true,
  dependencyFormulaVerified: true, bitacoraReadbackVerified: true,
  eventUniqueVerified: true, eventId: 'SQ-G0-006-H-SYNTHETIC-IS-NOT-A-REAL-EVENT',
});
const reject = (mutate, expected) => {
  const r = receipt(); mutate(r);
  const result = verifyDirectDevTraceability(r);
  assert.equal(result.verified, false, JSON.stringify(result));
  assert.ok(result.errors.includes(expected), JSON.stringify(result));
};

test('G0-006.H: full current-plan traceability receipt passes synthetic structural contract', () => {
  const code = readFileSync(new URL('../quality/direct-dev-traceability.mjs', import.meta.url), 'utf8');
  assert.match(code, /export function verifyDirectDevTraceability/);
  assert.deepEqual(verifyDirectDevTraceability(receipt()), { verified: true, errors: [] });
});

test('G0-006.H: positive requires exact tree, published SHA and postintegration audit', () => {
  reject(r => { r.publishedTreeSha = 'c'.repeat(40); }, 'source_equivalence');
  reject(r => { r.devReadbackSha = 'c'.repeat(40); }, 'dev_publication');
  reject(r => { r.audits.postIntegration.sha = tree; }, 'audit_postIntegration');
});

test('G0-006.H: no positive without directed test path and negative evidence', () => {
  reject(r => { r.testPaths = []; }, 'code_and_tests');
  reject(r => { r.negativeCasesVerified = false; }, 'causal_tests');
  reject(r => { r.testRuns[0].status = 'SKIPPED'; }, 'causal_tests');
});

test('G0-006.H: legacy phase 8/13 cannot spoof current-plan positive proof', () => {
  reject(r => { r.sourceAuthority = 'FASE_8'; }, 'legacy_reopen');
  reject(r => { r.legacyPhase = 'PHASE-13'; }, 'legacy_reopen');
  reject(r => { r.reopenCompletedMigration = true; }, 'legacy_reopen');
});

test('G0-006.H: positive closure still requires all institutional readbacks', () => {
  reject(r => { r.planReadbackVerified = false; }, 'institutional_readback');
  reject(r => { r.colaReadbackVerified = false; }, 'institutional_readback');
  reject(r => { r.bitacoraReadbackVerified = false; }, 'institutional_readback');
});
