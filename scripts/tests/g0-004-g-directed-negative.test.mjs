// G0-004.G: targeted rejection matrix for Playwright false-green evidence.
// Synthetic JSON only: a passed Node test does not establish a browser E2E pass.
import test from 'node:test';
import assert from 'node:assert/strict';
import { classifyPlaywrightReport } from '../quality/playwright-outcome-classifier.mjs';

const item = (status, expectedStatus, statuses) => ({
  status, expectedStatus, results: statuses.map(status => ({ status })),
});
const report = (...tests) => ({ suites: [{ specs: [{ tests }] }] });
const clean = item('expected', 'passed', ['passed']);

test('G0-004.G positive control: only clean Playwright evidence is PASS', () => {
  assert.equal(classifyPlaywrightReport(report(clean)).status, 'PASS');
});

test('G0-004.G retry after failure never hides the original failure', () => {
  for (const failed of ['failed', 'timedOut', 'interrupted']) {
    const result = classifyPlaywrightReport(report(clean, item('flaky', 'passed', [failed, 'passed'])));
    assert.equal(result.status, 'FLAKY_REVIEW');
    assert.equal(result.counts.flaky, 1);
  }
});

test('G0-004.G misleading expected status with an earlier failed retry fails closed', () => {
  const r = classifyPlaywrightReport(report(item('expected', 'passed', ['failed', 'passed'])));
  assert.equal(r.status, 'INVALID_REPORT');
});

test('G0-004.G skipped test behind an unavailable secret never makes suite green', () => {
  // The classifier only receives status, never secret text or private config.
  const r = classifyPlaywrightReport(report(clean, item('skipped', 'passed', ['skipped'])));
  assert.equal(r.status, 'INCOMPLETE');
  assert.equal(r.counts.skipped, 1);
});

test('G0-004.G unsupported browser skip remains incomplete even with clean Chrome', () => {
  const r = classifyPlaywrightReport(report(clean, item('skipped', 'passed', [])));
  assert.equal(r.status, 'INCOMPLETE');
  assert.notEqual(r.status, 'PASS');
});

test('G0-004.G global worker error remains failure when test retries or skips', () => {
  const r = classifyPlaywrightReport({
    ...report(item('flaky', 'passed', ['failed', 'passed']), item('skipped', 'passed', [])),
    errors: [{ message: 'SYNTHETIC_PRIVATE_ERROR_MARKER' }],
  });
  assert.equal(r.status, 'FAIL');
  assert.equal(r.globalErrors, 1);
  assert.ok(!JSON.stringify(r).includes('SYNTHETIC_PRIVATE_ERROR_MARKER'));
});
