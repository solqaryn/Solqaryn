// G0-004.E - regressions for false-green E2E evidence.
// Synthetic Playwright JSON only; never embed secrets, tenant PII, or test titles.
import test from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtempSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { classifyPlaywrightReport } from '../quality/playwright-outcome-classifier.mjs';

const item = (status, expectedStatus, attempts) => ({
  status, expectedStatus, results: attempts.map(status => ({ status })),
});
const report = (...tests) => ({ suites: [{ specs: [{ tests }] }] });
const classifier = new URL('../quality/playwright-outcome-classifier.mjs', import.meta.url).pathname;

function assertNotGreen(payload, expected) {
  const out = classifyPlaywrightReport(payload);
  assert.equal(out.status, expected);
  assert.notEqual(out.status, 'PASS');
}

test('G0-004.E: clean passed result is the only positive baseline', () => {
  const result = classifyPlaywrightReport(report(item('expected', 'passed', ['passed'])));
  assert.equal(result.status, 'PASS');
  assert.equal(result.counts.passed, 1);
});

test('G0-004.E: failed-first then passed-on-retry remains flaky, not green', () => {
  for (const failure of ['failed', 'timedOut', 'interrupted']) {
    const out = classifyPlaywrightReport(report(item('flaky', 'passed', [failure, 'passed'])));
    assert.equal(out.status, 'FLAKY_REVIEW');
    assert.equal(out.counts.flaky, 1);
  }
});

test('G0-004.E: retry metadata inconsistent with expected must fail closed', () => {
  assertNotGreen(report(item('expected', 'passed', ['failed', 'passed'])), 'INVALID_REPORT');
  assertNotGreen(report(item('flaky', 'passed', ['passed'])), 'INVALID_REPORT');
});

test('G0-004.E: optional secret-gated test SKIPPED is incomplete evidence', () => {
  const out = classifyPlaywrightReport(report(
    item('expected', 'passed', ['passed']), item('skipped', 'passed', ['skipped'])));
  assert.equal(out.status, 'INCOMPLETE');
  assert.equal(out.counts.skipped, 1);
});

test('G0-004.E: unsupported browser SKIPPED cannot count as a suite pass', () => {
  assertNotGreen(report(item('skipped', 'passed', [])), 'INCOMPLETE');
  assertNotGreen(report(item('expected', 'skipped', [])), 'INVALID_REPORT');
});

test('G0-004.E: nested suite with one failing tenant test dominates pass', () => {
  const out = classifyPlaywrightReport({ suites: [{
    specs: [{ tests: [item('expected', 'passed', ['passed'])] }],
    suites: [{ specs: [{ tests: [item('unexpected', 'passed', ['failed'])] }] }],
  }] });
  assert.equal(out.status, 'FAIL');
  assert.deepEqual([out.counts.passed, out.counts.failed], [1, 1]);
});

test('G0-004.E: missing or zero-suite JSON never authorizes success', () => {
  assertNotGreen({ suites: [] }, 'INVALID_REPORT');
  assertNotGreen({ suites: [{ specs: [] }] }, 'INVALID_REPORT');
  assertNotGreen({ unexpected: 'JSON' }, 'INVALID_REPORT');
});

test('G0-004.E: CLI emits nonzero status for retries and skipped tests', () => {
  const dir = mkdtempSync(join(tmpdir(), 'g0-004-e-'));
  try {
    for (const [name, sample, expected] of [
      ['retry', report(item('flaky', 'passed', ['failed', 'passed'])), 'FLAKY_REVIEW'],
      ['skip', report(item('skipped', 'passed', ['skipped'])), 'INCOMPLETE'],
      ['pass', report(item('expected', 'passed', ['passed'])), 'PASS'],
    ]) {
      const path = join(dir, name + '.json');
      writeFileSync(path, JSON.stringify(sample));
      const run = spawnSync(process.execPath, [classifier, path], { encoding: 'utf8' });
      assert.equal(run.status, name === 'pass' ? 0 : 2);
      assert.equal(JSON.parse(run.stdout).status, expected);
    }
  } finally { rmSync(dir, { recursive: true, force: true }); }
});
