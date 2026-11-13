// G0-004.H: positive regression of the real Playwright evidence classifier and CLI.
// Synthetic Playwright JSON only; this is not a claim that a browser E2E ran.
import test from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { classifyPlaywrightReport } from '../quality/playwright-outcome-classifier.mjs';

const passed = () => ({
  status: 'expected', expectedStatus: 'passed', results: [{ status: 'passed' }],
});
const cleanReport = () => ({
  suites: [{
    specs: [{ tests: [passed()] }],
    suites: [{ specs: [{ tests: [passed(), passed()] }] }],
  }],
  errors: [],
});

test('G0-004.H: clean nested Playwright report passes with exact total and no global errors', () => {
  const outcome = classifyPlaywrightReport(cleanReport());
  assert.deepEqual(outcome, {
    status: 'PASS',
    counts: { passed: 3, failed: 0, flaky: 0, skipped: 0, expectedFailed: 0, invalid: 0 },
    tests: 3,
  });
});

test('G0-004.H: clean report CLI emits PASS and exit zero, without titles or secrets', () => {
  const dir = mkdtempSync(join(tmpdir(), 'g0-004-h-'));
  try {
    const path = join(dir, 'passed.json');
    const report = cleanReport();
    report.suites[0].specs[0].title = 'SYNTHETIC_PRIVATE_TITLE';
    writeFileSync(path, JSON.stringify(report));
    const script = fileURLToPath(new URL('../quality/playwright-outcome-classifier.mjs', import.meta.url));
    const run = spawnSync(process.execPath, [script, path], { encoding: 'utf8' });
    assert.equal(run.status, 0, run.stderr);
    assert.equal(JSON.parse(run.stdout).status, 'PASS');
    assert.equal(JSON.parse(run.stdout).counts.passed, 3);
    assert.ok(!run.stdout.includes('SYNTHETIC_PRIVATE_TITLE'));
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
});

test('G0-004.H: regressions with failed, skipped, flaky or global-error evidence never pass', () => {
  const badCases = [
    ['failed', { status: 'unexpected', expectedStatus: 'passed', results: [{ status: 'failed' }] }, 'FAIL'],
    ['skipped', { status: 'skipped', expectedStatus: 'passed', results: [] }, 'INCOMPLETE'],
    ['flaky', { status: 'flaky', expectedStatus: 'passed', results: [{ status: 'timedOut' }, { status: 'passed' }] }, 'FLAKY_REVIEW'],
  ];
  for (const [name, adverse, expected] of badCases) {
    const report = cleanReport();
    report.suites[0].suites[0].specs[0].tests.push(adverse);
    const outcome = classifyPlaywrightReport(report);
    assert.equal(outcome.status, expected, name);
    assert.notEqual(outcome.status, 'PASS');
  }
  const globallyBroken = cleanReport();
  globallyBroken.errors = [{ message: 'SYNTHETIC_PRIVATE_FAILURE' }];
  const broken = classifyPlaywrightReport(globallyBroken);
  assert.equal(broken.status, 'FAIL');
  assert.equal(broken.globalErrors, 1);
  assert.ok(!JSON.stringify(broken).includes('SYNTHETIC_PRIVATE_FAILURE'));
});

test('G0-004.H: malformed or zero-test input is INVALID_REPORT, never false green', () => {
  for (const report of [{ suites: [] }, { suites: [{ specs: [] }] }, { suites: [], errors: 'invalid' }]) {
    assert.equal(classifyPlaywrightReport(report).status, 'INVALID_REPORT');
  }
});
