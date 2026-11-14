// G0-004.B: targeted evidence matrix for E2E outcome classification, not an actual browser run.
import test from 'node:test';
import assert from 'node:assert/strict';
import { classifyPlaywrightReport } from '../quality/playwright-outcome-classifier.mjs';

const receiptMatrix = Object.freeze([
  { id: 'G0-004.F', file: 'scripts/tests/g0-004-f-global-setup-error.test.mjs', requirements: ['global_setup_error', 'clean_control'] },
  { id: 'G0-004.G', file: 'scripts/tests/g0-004-g-directed-negative.test.mjs', requirements: ['failed_retry', 'secret_skip', 'unsupported_browser', 'worker_error'] },
  { id: 'G0-004.H', file: 'scripts/tests/g0-004-h-positive-regression.test.mjs', requirements: ['clean_nested_pass', 'cli_zero', 'invalid_report', 'title_redaction'] },
]);
const required = Object.freeze([
  'global_setup_error', 'clean_control', 'failed_retry', 'secret_skip',
  'unsupported_browser', 'worker_error', 'clean_nested_pass', 'cli_zero',
  'invalid_report', 'title_redaction',
]);
const item = (status, expectedStatus, statuses) => ({
  status, expectedStatus, results: statuses.map(status => ({ status })),
});
const report = (...tests) => ({ suites: [{ specs: [{ tests }] }], errors: [] });
const clean = item('expected', 'passed', ['passed']);

test('G0-004.B matrix: F/G/H cover each required scenario without duplicate receipt IDs', () => {
  assert.deepEqual(receiptMatrix.map(x => x.id), ['G0-004.F', 'G0-004.G', 'G0-004.H']);
  assert.equal(new Set(receiptMatrix.map(x => x.file)).size, 3);
  const covered = receiptMatrix.flatMap(x => x.requirements);
  assert.equal(new Set(covered).size, covered.length);
  assert.deepEqual([...covered].sort(), [...required].sort());
});

test('G0-004.B positive: real classifier accepts only three nested clean passed records', () => {
  const nested = { suites: [{ specs: [{ tests: [clean] }], suites: [{ specs: [{ tests: [clean, clean] }] }] }], errors: [] };
  const outcome = classifyPlaywrightReport(nested);
  assert.equal(outcome.status, 'PASS');
  assert.deepEqual(outcome.counts, { passed: 3, failed: 0, flaky: 0, skipped: 0, expectedFailed: 0, invalid: 0 });
  assert.equal(outcome.tests, 3);
});

test('G0-004.B negative: retries, secret skips and unsupported browser cannot be green', () => {
  const cases = [
    [report(clean, item('flaky', 'passed', ['failed', 'passed'])), 'FLAKY_REVIEW'],
    [report(clean, item('skipped', 'passed', ['skipped'])), 'INCOMPLETE'],
    [report(clean, item('skipped', 'passed', [])), 'INCOMPLETE'],
    [report(clean, item('unexpected', 'passed', ['failed'])), 'FAIL'],
  ];
  for (const [input, expected] of cases) {
    const outcome = classifyPlaywrightReport(input);
    assert.equal(outcome.status, expected);
    assert.notEqual(outcome.status, 'PASS');
  }
});

test('G0-004.B negative: global setup error dominates clean suite, does not leak details', () => {
  const input = report(clean);
  input.errors.push({ message: 'SYNTHETIC_SECRET_OR_USER_TITLE' });
  const result = classifyPlaywrightReport(input);
  assert.equal(result.status, 'FAIL');
  assert.equal(result.globalErrors, 1);
  assert.equal(result.counts.passed, 1);
  assert.ok(!JSON.stringify(result).includes('SYNTHETIC_SECRET_OR_USER_TITLE'));
  assert.equal(classifyPlaywrightReport({ suites: [] }).status, 'INVALID_REPORT');
});
