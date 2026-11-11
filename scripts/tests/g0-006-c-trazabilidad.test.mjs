import test from 'node:test';
import assert from 'node:assert/strict';
import { rollingListoThroughput } from '../automation-ci-throughput.mjs';

// G0-006.C: one traceability boundary from approved scope to code, test,
// protected merge and institutional readbacks. Synthetic receipts are NOT
// GitHub checks or a certification of the real G0-006.C microtask.
const NOW = '2026-11-08T15:00:00Z';
const completeReceipt = () => ({
  scopeId: 'G0-006.C',
  state: 'LISTO',
  closedAt: '2026-11-08T14:40:00Z',
  mergeSha: 'a'.repeat(40),
  mergeVerified: true,
  requiredCiVerified: true,
  independentReviewVerified: true,
  planReadbackVerified: true,
  colaReadbackVerified: true,
  bitacoraReadbackVerified: true,
});

test('G0-006.C: only complete, explicitly verified evidence enters LISTO metrics', () => {
  const complete = rollingListoThroughput([completeReceipt()], NOW, true);
  assert.equal(complete.certified, 1);
  assert.equal(complete.deficit, 5);
  assert.equal(complete.status, 'BELOW_TARGET');
  const unverified = rollingListoThroughput([completeReceipt()], NOW, false);
  assert.equal(unverified.status, 'UNVERIFIED_LEDGER_COVERAGE');
  assert.equal(unverified.certified, null);
});

test('G0-006.C: no proof or missing readback cannot be counted as a closure', () => {
  const missingEvidence = {
    mergeSha: 'historical-sha',
    mergeVerified: false,
    requiredCiVerified: false,
    independentReviewVerified: false,
    planReadbackVerified: false,
    colaReadbackVerified: false,
    bitacoraReadbackVerified: false,
  };
  for (const [field, value] of Object.entries(missingEvidence)) {
    const incomplete = { ...completeReceipt(), [field]: value };
    assert.equal(rollingListoThroughput([incomplete], NOW, true).certified, 0, field);
  }
  assert.equal(rollingListoThroughput([
    { ...completeReceipt(), state: 'VALIDANDO' },
  ], NOW, true).certified, 0);
});

test('G0-006.C: historic/future stages and duplicate events cannot inflate certification', () => {
  const complete = completeReceipt();
  const historical = { ...complete, scopeId: 'FASE8-001.A' };
  const expired = { ...complete, scopeId: 'P2-010.A', closedAt: '2026-11-07T00:00:00Z' };
  const future = { ...complete, scopeId: 'I8-010.D', closedAt: '2026-11-08T16:00:00Z' };
  const result = rollingListoThroughput([
    complete, complete, historical, expired, future,
  ], NOW, true);
  assert.equal(result.certified, 1);
  assert.equal(result.deficit, 5);
});
