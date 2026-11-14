import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import { fileURLToPath } from 'node:url';
import { assessOperationalReadbackClosure } from '../quality/g0-005-a-readback-closure.mjs';

const fixturePath = new URL('./fixtures/g0-005-f-operational-503.json', import.meta.url);
const load = () => JSON.parse(readFileSync(fileURLToPath(fixturePath), 'utf8'));

test('G0-005.F measured synthetic negative does not pass as a live deployment', () => {
  const f = load();
  assert.equal(f.kind, 'G0-005.F_SYNTHETIC_NEGATIVE_NOT_PROVIDER_ATTESTATION');
  const result = assessOperationalReadbackClosure(f.proof);
  assert.equal(result.decision, f.expected_decision);
  for (const reason of f.expected_reasons) assert.ok(result.reasons.includes(reason), reason);
  assert.equal(result.table[0].http_category, 'HTTP_ANOMALOUS');
  assert.equal(result.table[1].http_category, 'NOT_OBSERVED');
});

test('G0-005.F SSO 503 is independently causal even if API upstream becomes healthy', () => {
  const f = load(); f.proof.dev_readback.api.upstream_status = 200;
  const r = assessOperationalReadbackClosure(f.proof);
  assert.equal(r.decision, 'NO_GO');
  assert.ok(r.reasons.includes('DEV_HTTP_ANOMALY'));
});

test('G0-005.F healthy DEV controls do not turn missing QA/PROD into PASS', () => {
  const f = load();
  f.proof.observations[0].http_status = 403;
  f.proof.dev_readback.sso.status = 403;
  f.proof.dev_readback.api.upstream_status = 200;
  const r = assessOperationalReadbackClosure(f.proof);
  assert.equal(r.decision, 'NO_GO');
  assert.equal(r.table[0].http_category, 'AUTH_GATED_EXPECTED');
  assert.ok(r.reasons.includes('QA_NOT_OBSERVED'));
  assert.ok(r.reasons.includes('PROD_NOT_OBSERVED'));
});

test('G0-005.F fixture reports only synthetic metadata and no secrets', () => {
  const f = load(); const output = JSON.stringify(assessOperationalReadbackClosure(f.proof));
  assert.equal(output.includes('access_token'), false);
  assert.equal(output.includes('private_key'), false);
  assert.equal(output.includes('user_email'), false);
});
