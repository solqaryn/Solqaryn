// G0-006.B: synthetic structural matrix regression; real receipts are checked
// separately through authenticated GitHub and institutional Google Sheets.
import test from 'node:test';
import assert from 'node:assert/strict';
import { assessG0006BMatrix } from '../quality/g0-006-b-traceability-matrix.mjs';

const sha = ['6a10d42e62005e253e52465dc6383b4fb2e15ca4',
  '9c6f215807b9068a8646f1c7774a1f2b972d262e',
  'a8de98250b3778cc8a4256df11d45a91db9f737c'];
const testPaths = ['g0-006-f-traceability-case','g0-006-g-legacy-angular-reopen',
  'g0-006-h-positive-traceability'];
const current = () => ({
  scopeId: 'G0-006.B', requirementId: 'G0-006.B',
  planId: '1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA',
  planAuthority: 'CURRENT_COMMERCIAL_PLAN_ONLY', futureMasterPlanExecutable: false,
  integrationMode: 'DIRECT_DEV_FOUR_AUDITS', state: 'LISTO',
  sourceAuthority: 'PLAN_COMERCIAL_INICIAL_V3', requestedAction: 'VERIFY_RECEIPTS',
  receipts: ['F','G','H'].map((letter, i) => ({
    id: 'G0-006.' + letter, state: 'LISTO', dependencyReady: 'SI',
    publishedSha: sha[i], commitReadbackVerified: true,
    codePath: 'scripts/quality/direct-dev-traceability.mjs',
    testPath: 'scripts/tests/' + testPaths[i] + '.test.mjs',
    planReadback: true, colaReadback: true, bitacoraEventVerified: true,
    evidence: 'Synthetic ledger and Git reference only; no live attestation',
    a5Status: i === 0 ? 'A5_PASS' : 'A5_PENDIENTE',
    promotionAuthorized: false,
  })),
  negativeLegacyPhaseTested: true, positiveExactShaTested: true,
  planReadbackVerified: true, colaReadbackVerified: true,
  bitacoraReadbackVerified: true, eventUniqueVerified: true,
});
function rejects(mutate, expected) {
  const r = current(); mutate(r);
  const result = assessG0006BMatrix(r);
  assert.equal(result.verified, false, JSON.stringify(result));
  assert.ok(result.errors.includes(expected), JSON.stringify(result));
}

test('G0-006.B: F/G/H complete structural evidence maps each scope to its test and SHA', () => {
  assert.deepEqual(assessG0006BMatrix(current()), {verified:true,errors:[]});
});

test('G0-006.B: historical phase 8/13 cannot reopen completed Angular migration', () => {
  rejects(r => {r.requestedAction='REOPEN_ANGULAR_MIGRATION_FASE_8';}, 'legacy_reopen');
  rejects(r => {r.legacyPhase='PHASE-13';}, 'legacy_reopen');
  rejects(r => {r.futureMasterPlanExecutable=true;}, 'authority');
});

test('G0-006.B: duplicate or missing F/G/H receipts are never closures', () => {
  rejects(r => {r.receipts.pop();}, 'predecessors');
  rejects(r => {r.receipts[1].id='G0-006.F';}, 'predecessors');
  rejects(r => {r.receipts[0].state='VALIDANDO';}, 'receipt_G0-006.F');
});

test('G0-006.B: SHA, test mapping and institutional readback must be exact', () => {
  rejects(r => {r.receipts[2].publishedSha='historical';}, 'code_test_sha_G0-006.H');
  rejects(r => {r.receipts[1].testPath='../other';}, 'code_test_sha_G0-006.G');
  rejects(r => {r.receipts[0].bitacoraEventVerified=false;}, 'receipt_G0-006.F');
  rejects(r => {r.colaReadbackVerified=false;}, 'institutional_readback');
});

test('G0-006.B: A5 pending cannot be used as promotion permission', () => {
  rejects(r => {r.receipts[2].promotionAuthorized=true;}, 'a5_promotion_G0-006.H');
  rejects(r => {r.receipts[1].a5Status='A5_PASS_SELF';}, 'a5_promotion_G0-006.G');
  rejects(r => {r.negativeLegacyPhaseTested=false;}, 'causal_tests');
});
