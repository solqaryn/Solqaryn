// G0-004.A closes the false-green gap when Playwright reports worker/setup errors.
// Synthetic evidence only; none of these fixtures establish an E2E browser pass.
import test from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtempSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { classifyPlaywrightReport } from '../quality/playwright-outcome-classifier.mjs';

const passed = () => ({ status: 'expected', expectedStatus: 'passed', results: [{ status: 'passed' }] });
const flaky = () => ({ status: 'flaky', expectedStatus: 'passed', results: [{ status: 'failed' }, { status: 'passed' }] });
const skipped = () => ({ status: 'skipped', expectedStatus: 'passed', results: [{ status: 'skipped' }] });
const report = (...tests) => ({ suites: [{ specs: [{ tests }] }] });

test('G0-004.A clean E2E evidence remains green when global error list is empty', () => {
  assert.equal(classifyPlaywrightReport({ ...report(passed()), errors: [] }).status, 'PASS');
});

test('G0-004.A global setup/worker errors veto an otherwise green test report', () => {
  const marker = 'PRIVATE_FAKE_TOKEN_SHOULD_NOT_APPEAR';
  const result = classifyPlaywrightReport({ ...report(passed()), errors: [{ message: marker }] });
  assert.equal(result.status, 'FAIL');
  assert.equal(result.counts.passed, 1);
  assert.equal(result.globalErrors, 1);
  assert.ok(!JSON.stringify(result).includes(marker));
});

test('G0-004.A global errors with no tests never yield PASS', () => {
  const result = classifyPlaywrightReport({ suites: [], errors: [{ message: 'synthetic worker crash' }] });
  assert.equal(result.status, 'FAIL');
  assert.equal(result.tests, 0);
});

test('G0-004.A malformed error arrays fail closed and zero-test reports stay invalid', () => {
  for (const errors of [null, {}, 'not an array']) {
    assert.equal(classifyPlaywrightReport({ ...report(passed()), errors }).status, 'INVALID_REPORT');
  }
  assert.equal(classifyPlaywrightReport({ suites: [], errors: [] }).status, 'INVALID_REPORT');
});

test('G0-004.A prior skips, browser incompatibility and retries still never count as clean', () => {
  assert.equal(classifyPlaywrightReport(report(flaky())).status, 'FLAKY_REVIEW');
  assert.equal(classifyPlaywrightReport(report(skipped())).status, 'INCOMPLETE');
  assert.equal(classifyPlaywrightReport(report(passed(), skipped())).status, 'INCOMPLETE');
});

test('G0-004.A CLI exits nonzero for global failure without printing secret-shaped report content', () => {
  const dir = mkdtempSync(join(tmpdir(), 'g0-004-a-'));
  const marker = 'PRIVATE_FAKE_TOKEN_SHOULD_NOT_APPEAR';
  try {
    const path = join(dir, 'report.json');
    writeFileSync(path, JSON.stringify({ ...report(passed()), errors: [{ message: marker }] }));
    const classifier = new URL('../quality/playwright-outcome-classifier.mjs', import.meta.url).pathname;
    const run = spawnSync(process.execPath, [classifier, path], { encoding: 'utf8' });
    assert.equal(run.status, 2);
    assert.equal(JSON.parse(run.stdout).status, 'FAIL');
    assert.ok(!(run.stdout + run.stderr).includes(marker));
  } finally { rmSync(dir, { recursive: true, force: true }); }
});
