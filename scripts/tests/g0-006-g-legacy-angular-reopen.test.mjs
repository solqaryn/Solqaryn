// G0-006.G: synthetic negative evidence. It cannot attest to external Sheets,
// GitHub, actual Angular migrations, or QA/production state.
import test from 'node:test';
import assert from 'node:assert/strict';
import { verifyDirectDevTraceability } from '../quality/direct-dev-traceability.mjs';

const tree = 'a'.repeat(40);
const commit = 'b'.repeat(40);
const evidence = 'Synthetic regression evidence; not an institutional attestation';
const control = () => ({
  scopeId: 'G0-006.G', requirementId: 'G0-006.G',
  state: 'LISTO', integrationMode: 'DIRECT_DEV_FOUR_AUDITS',
  sourceAuthority: 'PLAN_COMERCIAL_INICIAL_V3', requestedAction: 'VERIFY_CURRENT_SCOPE',
  candidateTreeSha: tree, publishedTreeSha: tree, publishedSha: commit,
  devReadbackSha: commit, commitFastForwardVerified: true,
  devBranchVerified: true, diffReviewed: true,
  codePaths: ['scripts/quality/direct-dev-traceability.mjs'],
  testPaths: ['scripts/tests/g0-006-g-legacy-angular-reopen.test.mjs'],
  testRuns: [{ status: 'PASS', treeSha: tree, evidence }], negativeCasesVerified: true,
  audits: Object.fromEntries(['functional','security','integration','postIntegration']
    .map(name => [name, { status: 'PASS',
      sha: name === 'postIntegration' ? commit : tree, evidence }])),
  planReadbackVerified: true, colaReadbackVerified: true,
  dependencyFormulaVerified: true, bitacoraReadbackVerified: true,
  eventUniqueVerified: true, eventId: 'SYNTHETIC_G0_006_G_NO_REAL_SHEET_EVENT',
});
const rejects = (patch, reason) => {
  const result = verifyDirectDevTraceability({ ...control(), ...patch });
  assert.equal(result.verified, false, JSON.stringify(patch));
  assert.ok(result.errors.includes(reason), JSON.stringify(result));
};

test('G0-006.G: current Plan V3 synthetic control remains valid', () => {
  assert.deepEqual(verifyDirectDevTraceability(control()), { verified: true, errors: [] });
});

test('G0-006.G: old phase 8 and 13 cannot reopen a completed Angular migration', () => {
  for (const authority of ['FASE_8', 'FASE-13', 'PHASE 8', 'M13', 'VAEP', 'JULES']) {
    rejects({ sourceAuthority: authority, migrationTarget: 'ANGULAR_MIGRATION' }, 'legacy_reopen');
  }
});

test('G0-006.G: disguised legacy authority rejects despite current scope, SHA and fake PASS', () => {
  for (const patch of [
    { phaseId: 'FASE_8', requestedAction: 'VERIFY_CURRENT_SCOPE' },
    { legacyPhase: 'PHASE-13' },
    { reopenSource: 'M13-OLD-MIGRATION' },
    { migrationTarget: 'angular-migration' },
    { requestedAction: 'REOPEN_COMPLETED_ANGULAR_MIGRATION' },
    { reopenCompletedMigration: true },
  ]) rejects(patch, 'legacy_reopen');
});

test('G0-006.G: historic mention in explanatory evidence does not grant a phase authority', () => {
  const c = control();
  c.testRuns[0].evidence = 'Historical FASE_8 is archived; this does not reopen its migration';
  assert.equal(verifyDirectDevTraceability(c).verified, true);
});

test('G0-006.G: invalid legacy scope and missing institutional readback still fail closed', () => {
  rejects({ scopeId: 'FASE8-001.A', requirementId: 'FASE8-001.A' }, 'contract');
  rejects({ bitacoraReadbackVerified: false }, 'institutional_readback');
  rejects({ testRuns: [{ status: 'SKIPPED', treeSha: tree, evidence }] }, 'causal_tests');
});
