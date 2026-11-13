import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync } from 'node:fs';
import { assessOperationalReadbackClosure } from '../quality/g0-005-a-readback-closure.mjs';

const fixture = new URL('./fixtures/g0-005-f-operational-503.json', import.meta.url);
const baseline = () => JSON.parse(readFileSync(fixture, 'utf8')).proof;

const cases = [
  ['SSO alias 503 is anomalous', p => { p.dev_readback.api.upstream_status = 200; }, 'DEV_HTTP_ANOMALY'],
  ['API upstream 503 is rejected with SSO gate restored', p => {
    p.observations[0].http_status = 403;
    p.dev_readback.sso.status = 403;
  }, 'DEV_READBACK_NO_GO'],
  ['Vercel READY belongs to another project', p => {
    p.observations[0].http_status = 403;
    p.dev_readback.sso.status = 403;
    p.dev_readback.api.upstream_status = 200;
    p.dev_readback.vercel.project_id = 'prj_wrong';
  }, 'DEV_READBACK_NO_GO'],
  ['Vercel READY references a different GitHub HEAD', p => {
    p.observations[0].http_status = 403;
    p.dev_readback.sso.status = 403;
    p.dev_readback.api.upstream_status = 200;
    p.dev_readback.vercel.sha = 'c'.repeat(40);
  }, 'DEV_READBACK_NO_GO'],
  ['DEV deployment observation differs from GitHub SHA', p => {
    p.observations[0].deployment_sha = 'b'.repeat(40);
  }, 'DEV_SHA_MISMATCH'],
  ['unexpected DEV origin is not accepted', p => {
    p.observations[0].origin = 'https://foreign.example/';
  }, 'DEV_ALIAS_UNTRUSTED']
];

for (const [name, change, expected] of cases) {
  test(`G0-005.G negative: ${name}`, () => {
    const proof = baseline();
    change(proof);
    const result = assessOperationalReadbackClosure(proof);
    assert.equal(result.decision, 'NO_GO');
    assert.ok(result.reasons.includes(expected), `${name}: ${result.reasons.join(',')}`);
  });
}

test('G0-005.G no false PASS and no sensitive response fields', () => {
  const proof = baseline();
  proof.bearer_token = 'synthetic-only-secret';
  proof.dev_readback.api.response_body = 'synthetic-only-private-body';
  const result = assessOperationalReadbackClosure(proof);
  assert.equal(result.decision, 'NO_GO');
  const emitted = JSON.stringify(result);
  assert.equal(emitted.includes('synthetic-only-secret'), false);
  assert.equal(emitted.includes('synthetic-only-private-body'), false);
});
