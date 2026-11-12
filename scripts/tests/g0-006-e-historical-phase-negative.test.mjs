// G0-006.E - directed negative proof: retired phase labels do not become
// valid direct-dev LISTO evidence. Synthetic fixtures, not institutional records.
import test from 'node:test';
import assert from 'node:assert/strict';
import { verifyDirectDevTraceability } from '../quality/direct-dev-traceability.mjs';

const tree = 'a'.repeat(40);
const commit = 'b'.repeat(40);
const proof = 'node --test g0-006-e-historical-phase-negative.test.mjs PASS';
const audits = () => Object.fromEntries(
  ['functional', 'security', 'integration', 'postIntegration'].map(name => [
    name, { status: 'PASS', sha: name === 'postIntegration' ? commit : tree,
      evidence: 'Synthetic ' + name + ' audit fixture, not external authorization' },
  ]));
const valid = () => ({
  scopeId: 'G0-006.E', requirementId: 'G0-006.E',
  state: 'LISTO', integrationMode: 'DIRECT_DEV_FOUR_AUDITS',
  candidateTreeSha: tree, publishedTreeSha: tree,
  publishedSha: commit, devReadbackSha: commit,
  commitFastForwardVerified: true, devBranchVerified: true,
  diffReviewed: true, negativeCasesVerified: true,
  codePaths: ['scripts/quality/direct-dev-traceability.mjs'],
  testPaths: ['scripts/tests/g0-006-e-historical-phase-negative.test.mjs'],
  testRuns: [{ status: 'PASS', evidence: proof, treeSha: tree }],
  audits: audits(), planReadbackVerified: true, colaReadbackVerified: true,
  dependencyFormulaVerified: true, bitacoraReadbackVerified: true,
  eventUniqueVerified: true, eventId: 'SQ-G0-006-E-SYNTHETIC-NOT-A-REAL-EVENT',
});

function rejects(receipt, code) {
  const outcome = verifyDirectDevTraceability(receipt);
  assert.equal(outcome.verified, false);
  assert.ok(outcome.errors.includes(code), JSON.stringify(outcome));
}

test('G0-006.E positive control: only complete direct-dev-shaped receipt passes', () => {
  assert.deepEqual(verifyDirectDevTraceability(valid()), { verified: true, errors: [] });
});

test('G0-006.E: retired phase 8/13 labels do not masquerade as current scope', () => {
  for (const obsolete of ['FASE8-001.A', 'FASE13-001.A', 'M13-001.A', 'FASE_8', 'ANGULAR-MIGRATION']) {
    rejects({ ...valid(), scopeId: obsolete, requirementId: obsolete }, 'contract');
  }
});

test('G0-006.E: PR-only, historic phases and staging cannot reopen a LISTO', () => {
  for (const patch of [
    { integrationMode: 'PR_MERGE_LEGACY' },
    { state: 'VALIDANDO' },
    { requirementId: 'FASE8-001.A' },
  ]) rejects({ ...valid(), ...patch }, 'contract');
});

test('G0-006.E: missing test/negative or a SKIPPED result is NO_GO', () => {
  rejects({ ...valid(), negativeCasesVerified: false }, 'causal_tests');
  rejects({ ...valid(), testRuns: [{ status: 'SKIPPED', evidence: proof, treeSha: tree }] }, 'causal_tests');
  rejects({ ...valid(), testPaths: [] }, 'code_and_tests');
});

test('G0-006.E: false SHA, missing A4 or absent Sheet readback cannot close', () => {
  rejects({ ...valid(), devReadbackSha: 'c'.repeat(40) }, 'dev_publication');
  rejects({ ...valid(), audits: { ...audits(), postIntegration: { status: 'PASS', sha: tree, evidence: proof } } }, 'audit_postIntegration');
  rejects({ ...valid(), bitacoraReadbackVerified: false }, 'institutional_readback');
});
