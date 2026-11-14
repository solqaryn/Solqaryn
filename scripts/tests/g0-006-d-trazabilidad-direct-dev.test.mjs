import test from 'node:test';
import assert from 'node:assert/strict';
import { verifyDirectDevTraceability } from '../quality/direct-dev-traceability.mjs';

const tree = 'a'.repeat(40);
const commit = 'b'.repeat(40);
const evidence = 'node --test g0-006-d-trazabilidad-direct-dev.test.mjs: PASS';
const pass = () => ({
  scopeId: 'G0-006.D', requirementId: 'G0-006.D', state: 'LISTO',
  integrationMode: 'DIRECT_DEV_FOUR_AUDITS',
  publishedSha: commit, devReadbackSha: commit, candidateTreeSha: tree,
  publishedTreeSha: tree, commitFastForwardVerified: true,
  devBranchVerified: true, diffReviewed: true,
  codePaths: ['scripts/quality/direct-dev-traceability.mjs'],
  testPaths: ['scripts/tests/g0-006-d-trazabilidad-direct-dev.test.mjs'],
  testRuns: [{ status: 'PASS', treeSha: tree, evidence }],
  negativeCasesVerified: true,
  audits: Object.fromEntries(['functional','security','integration','postIntegration']
    .map(name => [name, { status: 'PASS', sha: name === 'postIntegration' ? commit : tree,
      evidence: name + ': reviewed expected test output and tree/commit SHA' }])),
  planReadbackVerified: true, colaReadbackVerified: true,
  dependencyFormulaVerified: true, bitacoraReadbackVerified: true,
  eventUniqueVerified: true, eventId: 'SQ-G0-006-D-J10-LISTO-BBBBBBBB-20261112',
});

test('G0-006.D: complete direct-dev requirement/code/test/four audits/readbacks accepted', () => {
  assert.deepEqual(verifyDirectDevTraceability(pass()), { verified: true, errors: [] });
});

test('G0-006.D: fail closed on old PR-only receipts, invented commit and missing evidence', () => {
  const sample = pass();
  const changes = [
    { integrationMode: 'PR_MERGE_LEGACY' },
    { devReadbackSha: 'c'.repeat(40) },
    { publishedTreeSha: 'c'.repeat(40) },
    { codePaths: ['../secret'] },
    { testRuns: [{ status: 'SKIPPED', treeSha: tree, evidence }] },
    { negativeCasesVerified: false },
    { planReadbackVerified: false },
    { dependencyFormulaVerified: false },
    { eventUniqueVerified: false },
    { audits: { ...sample.audits, security: { ...sample.audits.security, status: 'FAIL' } } },
    { audits: { ...sample.audits, postIntegration: { ...sample.audits.postIntegration, sha: tree } } },
  ];
  for (const patch of changes) {
    const result = verifyDirectDevTraceability({ ...sample, ...patch });
    assert.equal(result.verified, false, JSON.stringify(patch));
    assert.ok(result.errors.length > 0);
  }
});

test('G0-006.D: no flags or blank receipt ever certify LISTO', () => {
  assert.equal(verifyDirectDevTraceability(null).verified, false);
  assert.equal(verifyDirectDevTraceability({}).verified, false);
  assert.equal(verifyDirectDevTraceability({ ...pass(), state: 'VALIDANDO' }).verified, false);
  assert.equal(verifyDirectDevTraceability({ ...pass(), testPaths: [] }).verified, false);
});
