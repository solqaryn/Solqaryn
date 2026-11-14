import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync } from 'node:fs';
import { assessOperationalReadbackClosure } from '../quality/g0-005-a-readback-closure.mjs';
import { assessDevOperationalEvidence } from '../quality/g0-005-e-operational-readback.mjs';

const parentSha = '323e3333e73ce28369d82032c442705a325a9739';
const fixture = new URL('./fixtures/g0-005-f-operational-503.json', import.meta.url);
const healthyDev = () => {
  const proof = JSON.parse(readFileSync(fixture, 'utf8')).proof;
  proof.observations[0].http_status = 403;
  proof.observations[0].github_sha = parentSha;
  proof.observations[0].deployment_sha = parentSha;
  proof.dev_readback.head_sha = parentSha;
  proof.dev_readback.vercel.sha = parentSha;
  proof.dev_readback.sso.status = 403;
  proof.dev_readback.api.upstream_status = 200;
  return proof;
};

test('G0-005.H positive directed: canonical DEV SHA and protected SSO are structurally consistent', () => {
  const proof = healthyDev();
  assert.equal(assessDevOperationalEvidence(proof.dev_readback).decision,
    'EVIDENCE_CONSISTENT_NOT_LIVE_ATTESTATION');
  const result = assessOperationalReadbackClosure(proof);
  assert.equal(result.table[0].http_category, 'AUTH_GATED_EXPECTED');
  assert.equal(result.table[0].github_sha, parentSha);
  assert.equal(result.decision, 'NO_GO'); // QA/PROD intentionally not observed
  assert.ok(result.reasons.includes('QA_NOT_OBSERVED'));
});

test('G0-005.H positive synthetic full matrix: structural PASS cannot attest provider LIVE', () => {
  const proof = healthyDev();
  for (const environment of ['QA', 'PROD']) {
    const row = proof.observations.find(v => v.environment === environment);
    row.origin = `https://${environment.toLowerCase()}.synthetic.invalid/`;
    row.project = `synthetic_${environment.toLowerCase()}`;
    row.github_sha = parentSha;
    row.deployment_sha = parentSha;
    row.evidence_status = 'OBSERVED'; // synthetic fixture only, not vendor observation
    row.http_surface = 'API_UPSTREAM';
    row.http_status = 200;
  }
  const result = assessOperationalReadbackClosure(proof);
  assert.equal(result.decision, 'STRUCTURAL_EVIDENCE_CONSISTENT_NOT_LIVE_ATTESTATION');
  assert.equal(result.reasons.length, 0);
  assert.equal(result.table.length, 3);
  assert.ok(result.decision.includes('NOT_LIVE_ATTESTATION'));
});

test('G0-005.H positive reverts to NO_GO when DEV exact SHA changes', () => {
  const proof = healthyDev();
  proof.dev_readback.vercel.sha = 'b'.repeat(40);
  const result = assessOperationalReadbackClosure(proof);
  assert.equal(result.decision, 'NO_GO');
  assert.ok(result.reasons.includes('DEV_READBACK_NO_GO'));
});

test('G0-005.H positive reverts to NO_GO for foreign project despite READY', () => {
  const proof = healthyDev();
  proof.dev_readback.vercel.project_name = 'foreign';
  const result = assessOperationalReadbackClosure(proof);
  assert.equal(result.decision, 'NO_GO');
  assert.ok(result.reasons.includes('DEV_READBACK_NO_GO'));
});

test('G0-005.H positive output excludes synthetic secrets, private bodies and PII', () => {
  const proof = healthyDev();
  proof.session_token = 'fixture-secret';
  proof.dev_readback.api.private_body = 'fixture-private-body';
  const output = JSON.stringify(assessOperationalReadbackClosure(proof));
  assert.equal(output.includes('fixture-secret'), false);
  assert.equal(output.includes('fixture-private-body'), false);
});
