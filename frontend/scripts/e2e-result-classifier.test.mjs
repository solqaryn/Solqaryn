import test from 'node:test';
import assert from 'node:assert/strict';
import { classifyE2eTest, summarizeE2eJson } from './e2e-result-classifier.mjs';

const run = (...statuses) => ({ expectedStatus: 'passed', results: statuses.map((status) => ({ status })) });

test('G0-004.C distinguishes actual pass, skip, and failure', () => {
  assert.equal(classifyE2eTest(run('passed')), 'PASS');
  assert.equal(classifyE2eTest(run('skipped')), 'SKIPPED');
  assert.equal(classifyE2eTest(run('timedOut')), 'FAILURE');
  assert.equal(classifyE2eTest(run('interrupted')), 'FAILURE');
});

test('G0-004.C never hides first-attempt failures behind a successful retry', () => {
  assert.equal(classifyE2eTest(run('failed', 'passed')), 'FLAKY');
  assert.equal(classifyE2eTest(run('timedOut', 'passed')), 'FLAKY');
  assert.equal(classifyE2eTest({ ...run('passed'), status: 'flaky' }), 'FLAKY');
});

test('G0-004.C distinguishes expected failure and unexpected pass', () => {
  assert.equal(classifyE2eTest({ ...run('failed'), expectedStatus: 'failed' }), 'EXPECTED_FAILURE');
  assert.equal(classifyE2eTest({ ...run('passed'), expectedStatus: 'failed' }), 'UNEXPECTED_PASS');
  assert.equal(classifyE2eTest({ ...run('failed'), status: 'unexpected' }), 'FAILURE');
});

test('G0-004.C fails closed for missing, invalid, or empty evidence', () => {
  assert.equal(classifyE2eTest({ results: [] }), 'UNKNOWN');
  assert.equal(classifyE2eTest(run('mystery')), 'UNKNOWN');
  assert.throws(() => summarizeE2eJson({ suites: [] }), /NO_E2E_CASES/);
  assert.throws(() => summarizeE2eJson({}), /INVALID_PLAYWRIGHT_REPORT/);
});

test('G0-004.C traverses nested Playwright JSON suites and rejects flaky/skipped as unconditional pass', () => {
  const report = {
    suites: [{ suites: [{ specs: [{ title: 'tenancy', tests: [run('passed'), run('failed', 'passed'), run('skipped')] }] }] }]
  };
  const summary = summarizeE2eJson(report);
  assert.equal(summary.verdict, 'REVIEW_REQUIRED');
  assert.equal(summary.counts.PASS, 1);
  assert.equal(summary.counts.FLAKY, 1);
  assert.equal(summary.counts.SKIPPED, 1);
  assert.equal(summary.cases.length, 3);
});
