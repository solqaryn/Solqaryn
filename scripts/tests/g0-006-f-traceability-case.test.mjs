// G0-006.F: prepare a measurable, synthetic mismatch case for traceability.
// A PASS in these unit tests never certifies GitHub, Sheets, QA or PROD.
import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { verifyDirectDevTraceability } from '../quality/direct-dev-traceability.mjs';

const fixture = JSON.parse(readFileSync(new URL('./fixtures/g0-006-f-requirement-mismatch.json', import.meta.url), 'utf8'));
const candidateTree = 'a'.repeat(40);
const publishedCommit = 'b'.repeat(40);
const evidence = 'Synthetic fixture only: no live GitHub or Sheet receipt';

function syntheticControl() {
  return {
    scopeId: fixture.scopeId, requirementId: fixture.scopeId,
    state: 'LISTO', integrationMode: 'DIRECT_DEV_FOUR_AUDITS',
    candidateTreeSha: candidateTree, publishedTreeSha: candidateTree,
    publishedSha: publishedCommit, devReadbackSha: publishedCommit,
    commitFastForwardVerified: true, devBranchVerified: true, diffReviewed: true,
    codePaths: [fixture.source], testPaths: [fixture.test],
    testRuns: [{ status: 'PASS', evidence, treeSha: candidateTree }],
    negativeCasesVerified: true,
    audits: Object.fromEntries(['functional', 'security', 'integration', 'postIntegration']
      .map(name => [name, { status: 'PASS',
        sha: name === 'postIntegration' ? publishedCommit : candidateTree, evidence }])),
    planReadbackVerified: true, colaReadbackVerified: true,
    dependencyFormulaVerified: true, bitacoraReadbackVerified: true,
    eventUniqueVerified: true, eventId: 'SYNTHETIC_G0_006_F_NOT_A_REAL_EVENT',
  };
}

test('G0-006.F: fixture describes a measurable negative, never a real certification', () => {
  assert.equal(fixture.kind, 'SYNTHETIC_TEST_FIXTURE_NOT_AN_INSTITUTIONAL_RECEIPT');
  assert.equal(fixture.externalCertification, false);
  assert.equal(fixture.scopeId, 'G0-006.F');
  assert.equal(fixture.expectedError, 'contract');
});

test('G0-006.F: complete synthetic control is accepted only as a unit-test baseline', () => {
  assert.deepEqual(verifyDirectDevTraceability(syntheticControl()), { verified: true, errors: [] });
});

test('G0-006.F: traceability rejects receipt replayed with another requirement ID', () => {
  const receipt = { ...syntheticControl(), requirementId: fixture.replayedRequirementId };
  const result = verifyDirectDevTraceability(receipt);
  assert.equal(result.verified, false);
  assert.ok(result.errors.includes(fixture.expectedError));
});

test('G0-006.F: stale test hash is rejected even if the identifiers match', () => {
  const receipt = syntheticControl();
  receipt.testRuns = [{ ...receipt.testRuns[0], treeSha: 'c'.repeat(40) }];
  const result = verifyDirectDevTraceability(receipt);
  assert.equal(result.verified, false);
  assert.ok(result.errors.includes('causal_tests'));
});

test('G0-006.F: missing institutional readback cannot be interpreted as closure', () => {
  const receipt = { ...syntheticControl(), bitacoraReadbackVerified: false };
  const result = verifyDirectDevTraceability(receipt);
  assert.equal(result.verified, false);
  assert.ok(result.errors.includes('institutional_readback'));
});
