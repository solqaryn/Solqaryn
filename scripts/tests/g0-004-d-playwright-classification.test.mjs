import test from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtempSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { classifyPlaywrightReport } from '../quality/playwright-outcome-classifier.mjs';

const item = (status, expectedStatus, attempts) => ({
  status, expectedStatus, results: attempts.map(x=>({status:x})),
});
const report = (...tests) => ({suites:[{specs:[{tests}]}]});

test('G0-004.D: only all clean expected/pass cases are green', () => {
  const r = classifyPlaywrightReport(report(item('expected','passed',['passed'])));
  assert.equal(r.status, 'PASS');
  assert.equal(r.counts.passed, 1);
});

test('G0-004.D: failed, flaky and skipped are distinct and never PASS', () => {
  const r = classifyPlaywrightReport(report(
    item('unexpected','passed',['failed']),
    item('flaky','passed',['timedOut','passed']),
    item('skipped','passed',['skipped']),
    item('expected','failed',['failed'])));
  assert.equal(r.status, 'FAIL');
  assert.deepEqual([r.counts.failed,r.counts.flaky,r.counts.skipped,r.counts.expectedFailed],[1,1,1,1]);
  assert.equal(classifyPlaywrightReport(report(item('flaky','passed',['failed','passed']))).status,'FLAKY_REVIEW');
  assert.equal(classifyPlaywrightReport(report(item('skipped','passed',['skipped']))).status,'INCOMPLETE');
});

test('G0-004.D: empty, malformed, unknown and impossible flaky fail closed', () => {
  assert.equal(classifyPlaywrightReport({suites:[]}).status,'INVALID_REPORT');
  assert.equal(classifyPlaywrightReport(null).status,'INVALID_REPORT');
  assert.equal(classifyPlaywrightReport(report(item('unknown','passed',['passed']))).status,'INVALID_REPORT');
  assert.equal(classifyPlaywrightReport(report(item('flaky','passed',['passed']))).status,'INVALID_REPORT');
  assert.equal(classifyPlaywrightReport(report(item('expected','passed',['failed','passed']))).status,'INVALID_REPORT');
});

test('G0-004.D: CLI does not silently green an E2E failure or missing file', () => {
  const dir=mkdtempSync(join(tmpdir(),'g0004d-'));
  try {
    const path=join(dir,'playwright.json');
    writeFileSync(path,JSON.stringify(report(item('unexpected','passed',['failed']))));
    const cmd=join(import.meta.dirname,'../quality/playwright-outcome-classifier.mjs');
    const bad=spawnSync(process.execPath,[cmd,path],{encoding:'utf8'});
    assert.equal(bad.status,2);
    assert.equal(JSON.parse(bad.stdout).status,'FAIL');
    const missing=spawnSync(process.execPath,[cmd,join(dir,'no.json')],{encoding:'utf8'});
    assert.equal(missing.status,3);
  } finally {rmSync(dir,{recursive:true,force:true});}
});
