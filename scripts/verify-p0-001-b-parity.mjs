#!/usr/bin/env node
// P0-001.B: pure offline gate. A snapshot is evidence, never live authorization.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const shaRegex = /^[0-9a-f]{40}$/;
const requiredChecks = [
  'verify-solqaryn-project-scope',
  'SOLQARYN - Seguridad multiempresa',
  'SOLQARYN - Build y pruebas backend'
];
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const input = path.join(root, 'docs/baselines/P0-001-B-DEV-20261025.json');

export function evaluateParity(s) {
  const reasons = [];
  const head = s?.dev_head;
  if (!shaRegex.test(head ?? '')) reasons.push('DEV_HEAD_INVALID');
  if (s?.repository !== 'solqaryn/Solqaryn' || s?.branch !== 'dev') reasons.push('IDENTITY_MISMATCH');
  const seen = new Set();
  for (const name of requiredChecks) {
    const found = (s?.ci ?? []).filter(c => c.name === name);
    if (found.length !== 1 || found[0].conclusion !== 'success' || found[0].head_sha !== head ||
      !/^https:\/\/github\.com\/solqaryn\/Solqaryn\/actions\/runs\/\d+\/job\/\d+$/.test(found[0].url ?? '')) {
      reasons.push('CI_INVALID:' + name);
    }
    seen.add(name);
  }
  if (!Array.isArray(s?.ci) || s.ci.length !== seen.size) reasons.push('CI_EXTRA_OR_DUPLICATED');
  if (s?.render?.service_name !== 'solqaryn-api-dev' ||
      s?.render?.status !== 'live' || !shaRegex.test(s?.render?.sha ?? '') || s.render.sha !== head) {
    reasons.push('RENDER_NOT_LIVE_AT_DEV_SHA');
  }
  if (s?.vercel?.project_name !== 'solqaryn-dev' ||
      s?.vercel?.state !== 'READY' || !shaRegex.test(s?.vercel?.sha ?? '') || s.vercel.sha !== head) {
    reasons.push('VERCEL_NOT_READY_AT_DEV_SHA');
  }
  return { decision: reasons.length ? 'NO_GO' : 'MATCH', reasons };
}

export function verifySnapshot(s) {
  assert.equal(s?.schema, 'solqaryn.p0-001-b.dev-parity.v1');
  assert.equal(s?.microtask, 'P0-001.B');
  assert.match(s?.observed_at_utc ?? '', /^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(?:\.\d+)?Z$/);
  assert.match(s?.dev_head ?? '', shaRegex);
  assert.ok(/^https:\/\/dashboard\.render\.com\/web\/srv-[\w-]+$/.test(s?.render?.url ?? ''));
  assert.ok(/^https:\/\/vercel\.com\/solqaryn\/solqaryn-dev\/[A-Za-z0-9]+$/.test(s?.vercel?.url ?? ''));
  const out = evaluateParity(s);
  assert.equal(out.decision, s.parity_result, 'Recorded decision must match the fail-closed gate');
  return out;
}

function main() {
  const s = JSON.parse(readFileSync(input, 'utf8'));
  const out = verifySnapshot(s);
  if (process.argv.includes('--self-test')) {
    const positive = structuredClone(s);
    positive.vercel.state = 'READY';
    positive.parity_result = 'MATCH';
    assert.equal(verifySnapshot(positive).decision, 'MATCH');
    const cases = [
      ['render SHA mismatch', x => x.render.sha = '0'.repeat(40)],
      ['vercel SHA mismatch', x => x.vercel.sha = '0'.repeat(40)],
      ['render no live', x => x.render.status = 'building'],
      ['vercel canceled', x => x.vercel.state = 'CANCELED'],
      ['missing CI', x => x.ci.pop()],
      ['duplicate CI', x => x.ci.push({...x.ci[0]})],
      ['foreign CI SHA', x => x.ci[0].head_sha = '0'.repeat(40)],
      ['spoofed CI URL', x => x.ci[0].url = 'https://github.com.evil.invalid/runs/12'],
      ['foreign repo', x => x.repository = 'foreign/repo'],
      ['invalid head', x => x.dev_head = 'invalid']
    ];
    for (const [name, mutate] of cases) {
      const t = structuredClone(positive);
      mutate(t);
      assert.equal(evaluateParity(t).decision, 'NO_GO', name);
    }
    assert.equal(out.decision, 'NO_GO', 'Observed Vercel canceled must deny parity');
    console.log('PASS P0-001.B: ' + cases.length + ' negative mutations, one positive counterfactual, observed NO_GO');
  } else {
    console.log(JSON.stringify(out));
  }
}
main();
