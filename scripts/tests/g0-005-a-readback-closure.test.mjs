import assert from 'node:assert/strict';
import test from 'node:test';
import { assessOperationalReadbackClosure } from '../quality/g0-005-a-readback-closure.mjs';

const head = 'a'.repeat(40);
const receipt = (id, sha) => ({ id, sha, state: 'LISTO', repository: 'solqaryn/Solqaryn',
  branch: 'dev', source: `https://github.com/solqaryn/Solqaryn/commit/${sha}` });
const pristine = () => ({
  receipts: ['G0-005.C', 'G0-005.D', 'G0-005.E'].map((id,i) => receipt(id, String(i+1).repeat(40))),
  observations: ['DEV','QA','PROD'].map(environment => ({ environment,
    origin: 'https://solqaryn-dev.vercel.app/', project: 'solqaryn-dev',
    github_sha: head, deployment_sha: head,
    evidence_status: 'OBSERVED', http_surface: 'SSO_ALIAS', http_status: 403 })),
  dev_readback: {
    repository: 'solqaryn/Solqaryn', branch: 'dev', head_sha: head,
    vercel: {project_id:'prj_1Anhx5mWyXEBX89lWC24Py6JXe7A',project_name:'solqaryn-dev',
      state:'READY',branch:'dev',target:'production',sha:head,
      aliases:['solqaryn-dev.vercel.app']},
    sso:{hostname:'solqaryn-dev-solqaryn.vercel.app',status:403},
    api:{environment:'DEV',status:200,upstream_status:200}
  }
});

test('G0-005.A positive synthetic matrix is structural only, not live attestation', () => {
  const r = assessOperationalReadbackClosure(pristine());
  assert.equal(r.decision, 'STRUCTURAL_EVIDENCE_CONSISTENT_NOT_LIVE_ATTESTATION');
  assert.equal(r.table.length, 3);
  assert.equal(r.reasons.length, 0);
});
const negatives = [
  ['missing inherited receipt', p => p.receipts.pop(), 'RECEIPT_CHAIN_INVALID'],
  ['wrong G0-005.C SHA', p => p.receipts[0].sha = 'invalid', 'G0_005_C_RECEIPT_UNVERIFIED'],
  ['wrong G0-005.D source', p => p.receipts[1].source = 'https://evil.example', 'G0_005_D_RECEIPT_UNVERIFIED'],
  ['G0-005.E not LISTO', p => p.receipts[2].state = 'VALIDANDO', 'G0_005_E_RECEIPT_UNVERIFIED'],
  ['duplicate receipt', p => p.receipts[1] = {...p.receipts[0]}, 'RECEIPT_CHAIN_INVALID'],
  ['DEV SSO alias 503', p => p.observations[0].http_status = 503, 'DEV_HTTP_ANOMALY'],
  ['QA API upstream 503', p => {p.observations[1].http_surface='API_UPSTREAM'; p.observations[1].http_status=503;}, 'QA_HTTP_ANOMALY'],
  ['PROD observation missing', p => p.observations[2].evidence_status='NOT_OBSERVED', 'PROD_NOT_OBSERVED'],
  ['wrong DEV alias', p => p.observations[0].origin='https://other.example/', 'DEV_ALIAS_UNTRUSTED'],
  ['READY different GitHub SHA', p => p.observations[0].deployment_sha='b'.repeat(40), 'DEV_SHA_MISMATCH'],
  ['DEV project mismatch', p => p.dev_readback.vercel.project_id='prj_foreign', 'DEV_READBACK_NO_GO'],
  ['API upstream 503 on DEV', p => p.dev_readback.api.upstream_status=503, 'DEV_READBACK_NO_GO'],
  ['missing QA row', p => p.observations.pop(), 'ENVIRONMENT_MATRIX_INVALID'],
  ['invalid project metadata', p => p.observations[0].project='invalid project with space', 'DEV_EVIDENCE_INVALID'],
  ['malformed receipt collection', p => p.receipts={ find: 'bad' }, 'RECEIPT_CHAIN_INVALID'],
  ['invalid origin with credentials', p => p.observations[0].origin='https://u:p@solqaryn-dev.vercel.app/', 'DEV_EVIDENCE_INVALID']
];
for (const [label, change, reason] of negatives) test(`G0-005.A fail-closed: ${label}`, () => {
  const p = pristine(); change(p);
  const r = assessOperationalReadbackClosure(p);
  assert.equal(r.decision, 'NO_GO');
  assert.ok(r.reasons.includes(reason), `${label}: ${r.reasons.join(',')}`);
});
test('G0-005.A output does not reveal bearer tokens, response bodies or PII', () => {
  const p = pristine(); p.bearer_token='synthetic-token';p.dev_readback.api.response_body='synthetic-pii';
  const s = JSON.stringify(assessOperationalReadbackClosure(p));
  assert.equal(s.includes('synthetic-token'), false);
  assert.equal(s.includes('synthetic-pii'), false);
});
