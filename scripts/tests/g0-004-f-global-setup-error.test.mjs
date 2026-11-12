// G0-004.F: one explicit, measurable E2E evidence condition, not a browser E2E run.
import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import { classifyPlaywrightReport } from '../quality/playwright-outcome-classifier.mjs';

const fixtureUrl = new URL('./fixtures/g0-004-f-global-setup-error.json', import.meta.url);
const fixture = () => JSON.parse(readFileSync(fixtureUrl, 'utf8'));

test('G0-004.F: green test with global setup error must classify FAIL', () => {
  const result = classifyPlaywrightReport(fixture());
  assert.equal(result.status, 'FAIL');
  assert.equal(result.counts.passed, 1);
  assert.equal(result.globalErrors, 1);
  assert.equal(result.tests, 1);
  assert.ok(!JSON.stringify(result).includes('SYNTHETIC_GLOBAL_SETUP_FAILURE'));
});

test('G0-004.F: removing only the synthetic global error yields clean control', () => {
  const input = fixture();
  const clean = classifyPlaywrightReport({ ...input, errors: [] });
  assert.equal(clean.status, 'PASS');
  assert.equal(clean.tests, 1);
});

test('G0-004.F: CLI denies green exit code and leaks no global error details', () => {
  const cli = new URL('../quality/playwright-outcome-classifier.mjs', import.meta.url).pathname;
  const result = spawnSync(process.execPath, [cli, fixtureUrl.pathname], { encoding: 'utf8' });
  assert.equal(result.status, 2);
  assert.equal(JSON.parse(result.stdout).status, 'FAIL');
  assert.ok(!(result.stdout + result.stderr).includes('SYNTHETIC_GLOBAL_SETUP_FAILURE'));
});
