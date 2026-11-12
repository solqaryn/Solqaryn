import assert from 'node:assert/strict';
import test from 'node:test';
import { assessDevOperationalEvidence } from '../quality/g0-005-e-operational-readback.mjs';

const head = 'a'.repeat(40);
const pristine = () => ({
  repository: 'solqaryn/Solqaryn', branch: 'dev', head_sha: head,
  vercel: { project_id: 'prj_1Anhx5mWyXEBX89lWC24Py6JXe7A',
    project_name: 'solqaryn-dev', state: 'READY', branch: 'dev',
    target: 'production', sha: head, aliases: ['solqaryn-dev.vercel.app'] },
  sso: { hostname: 'solqaryn-dev-solqaryn.vercel.app', status: 403 },
  api: { environment: 'DEV', status: 200, upstream_status: 200 }
});

const negative = [
  ['SSO alias returns 503', p => { p.sso.status = 503; }, 'SSO_ALIAS_NOT_VERIFIED'],
  ['SSO alias unexpectedly public', p => { p.sso.status = 200; }, 'SSO_ALIAS_NOT_VERIFIED'],
  ['SSO alias belongs to another host', p => { p.sso.hostname = 'foreign.example'; }, 'SSO_ALIAS_NOT_VERIFIED'],
  ['API returns 503', p => { p.api.status = 503; }, 'DEV_API_NOT_HEALTHY'],
  ['API upstream returns 503', p => { p.api.upstream_status = 503; }, 'DEV_UPSTREAM_UNAVAILABLE'],
  ['Vercel READY belongs to another project', p => { p.vercel.project_id = 'prj_foreign'; }, 'VERCEL_PROJECT_MISMATCH'],
  ['Vercel READY has a different HEAD', p => { p.vercel.sha = 'b'.repeat(40); }, 'VERCEL_SHA_MISMATCH'],
  ['Vercel build canceled', p => { p.vercel.state = 'CANCELED'; }, 'VERCEL_READY_NOT_VERIFIED'],
  ['Official public alias missing', p => { p.vercel.aliases = ['foreign.example']; }, 'VERCEL_READY_NOT_VERIFIED'],
  ['Untrusted repo', p => { p.repository = 'other/repo'; }, 'UNTRUSTED_REPOSITORY_OR_BRANCH'],
  ['Unexpected branch', p => { p.branch = 'main'; }, 'UNTRUSTED_REPOSITORY_OR_BRANCH'],
  ['Invalid SHA', p => { p.head_sha = 'abc'; }, 'INVALID_DEV_HEAD'],
  ['Missing upstream observation', p => { delete p.api.upstream_status; }, 'DEV_UPSTREAM_UNAVAILABLE']
];

test('G0-005.E clean synthetic DEV evidence is consistent, not deployment attestation', () => {
  const result = assessDevOperationalEvidence(pristine());
  assert.equal(result.decision, 'EVIDENCE_CONSISTENT_NOT_LIVE_ATTESTATION');
  assert.deepEqual(result.reasons, []);
});
for (const [name, mutate, expectedReason] of negative) {
  test(`G0-005.E fail closed: ${name}`, () => {
    const input = pristine();
    mutate(input);
    const result = assessDevOperationalEvidence(input);
    assert.equal(result.decision, 'NO_GO');
    assert.ok(result.reasons.includes(expectedReason));
  });
}
test('G0-005.E never leaks user-supplied secrets or private response bodies', () => {
  const input = pristine();
  input.api.status = 503;
  input.api.bearer_token = 'synthetic-secret-marker';
  input.api.response_body = 'synthetic-pii-marker';
  const output = JSON.stringify(assessDevOperationalEvidence(input));
  assert.equal(output.includes('synthetic-secret-marker'), false);
  assert.equal(output.includes('synthetic-pii-marker'), false);
  assert.equal(output.includes('DEV_API_NOT_HEALTHY'), true);
});
