import test from 'node:test';
import assert from 'node:assert/strict';
import { assessG0005BReadback } from '../quality/g0-005-b-readback-policy.mjs';

const SHA = 'a'.repeat(40);
function proof() {
  const ids = ['G0-005.F', 'G0-005.G', 'G0-005.H'];
  const paths = ['g0-005-f-operational-503-fixture', 'g0-005-g-negative-readback', 'g0-005-h-positive-readback'];
  return {
    scope: 'G0-005.B', repository: 'solqaryn/Solqaryn', branch: 'dev', devSha: SHA,
    receipts: ids.map((id, i) => ({ id, state: 'LISTO', sha: String(i+1).repeat(40),
      testPath: `scripts/tests/${paths[i]}.test.mjs`, planReadback: true,
      colaReadback: true, bitacoraEventVerified: true })),
    environments: ['DEV', 'QA', 'PROD'].map((environment, i) => ({
      environment, origin: i === 0 ? 'https://solqaryn-dev.vercel.app/' : `https://${environment.toLowerCase()}.example.invalid/`,
      apiOrigin: `https://api-${environment.toLowerCase()}.example.invalid/`,
      project: `project_${environment.toLowerCase()}`, githubSha: SHA, deploymentSha: SHA,
      evidenceStatus: 'OBSERVED', providerEvidenceRef: `SYNTHETIC_${environment}_0123456789`,
      httpSurface: i === 0 ? 'SSO_ALIAS' : 'API_UPSTREAM', httpStatus: i === 0 ? 403 : 200,
      resourceRefs: { database: `db_${environment.toLowerCase()}_scope`,
        cache: `cache_${environment.toLowerCase()}_scope`,
        credentials: `secret_${environment.toLowerCase()}_scope` }
    })),
    devReadback: { sso: { hostname: 'solqaryn-dev-solqaryn.vercel.app', status: 403 },
      api: { status: 200, upstreamStatus: 200 },
      vercel: { project: 'solqaryn-dev', state: 'READY', sha: SHA } }
  };
}

test('G0-005.B structural positive is never live provider attestation', () => {
  const r = assessG0005BReadback(proof());
  assert.equal(r.decision, 'STRUCTURAL_CONSISTENCY_NOT_LIVE_ATTESTATION');
  assert.equal(r.liveVerified, false);
  assert.equal(r.qaProdPromotionAuthorized, false);
  assert.equal(r.table.length, 3);
});

test('G0-005.B mandatory negatives fail closed', () => {
  const bad = [
    p => { p.scope = 'G0-005.A'; },
    p => { p.branch = 'main'; },
    p => { p.devSha = 'invalid'; },
    p => { p.receipts.pop(); },
    p => { p.receipts[1].id = p.receipts[0].id; },
    p => { p.receipts[0].state = 'VALIDANDO'; },
    p => { p.receipts[1].sha = 'bad'; },
    p => { p.receipts[1].testPath = 'scripts/tests/foreign.test.mjs'; },
    p => { p.receipts[2].planReadback = false; },
    p => { p.receipts[0].colaReadback = false; },
    p => { p.receipts[2].bitacoraEventVerified = false; },
    p => { p.environments.pop(); },
    p => { p.environments[1].environment = 'DEV'; },
    p => { p.environments[1].origin = 'http://qa.example.invalid/'; },
    p => { p.environments[2].origin = 'https://user:pass@prod.example.invalid/'; },
    p => { p.environments[1].project = p.environments[0].project; },
    p => { p.environments[1].githubSha = 'b'.repeat(40); },
    p => { p.environments[1].evidenceStatus = 'NOT_OBSERVED'; },
    p => { p.environments[2].providerEvidenceRef = ''; },
    p => { p.environments[0].httpStatus = 503; },
    p => { p.environments[1].httpStatus = 503; },
    p => { p.environments[0].githubSha = 'b'.repeat(40); p.environments[0].deploymentSha = 'b'.repeat(40); },
    p => { p.environments[0].origin = 'https://foreign.example.invalid/'; },
    p => { p.environments[2].resourceRefs.database = p.environments[0].resourceRefs.database; },
    p => { p.environments[1].resourceRefs.cache = p.environments[2].resourceRefs.cache; },
    p => { p.environments[1].resourceRefs.credentials = p.environments[0].resourceRefs.credentials; },
    p => { p.environments[2].apiOrigin = p.environments[1].apiOrigin; },
    p => { p.environments[2].resourceRefs.database = 'password=secret'; },
    p => { p.devReadback.sso.status = 503; },
    p => { p.devReadback.api.upstreamStatus = 503; },
    p => { p.devReadback.vercel.project = 'foreign-project'; },
    p => { p.devReadback.vercel.sha = 'b'.repeat(40); },
    p => { p.devReadback.vercel.state = 'CANCELED'; },
  ];
  for (const [i, mutate] of bad.entries()) {
    const p = proof(); mutate(p);
    const r = assessG0005BReadback(p);
    assert.equal(r.decision, 'NO_GO', `case ${i + 1}`);
    assert.ok(r.reasons.length > 0, `case ${i + 1} reasons`);
  }
  assert.equal(bad.length, 33);
});

test('G0-005.B never emits sensitive or user-provided response fields', () => {
  const p = proof();
  p.devReadback.access_token = 'SYNTHETIC_SECRET_SENTINEL';
  p.environments[0].privateResponseBody = 'SYNTHETIC_PII_SENTINEL';
  const out = JSON.stringify(assessG0005BReadback(p));
  assert.ok(!out.includes('SYNTHETIC_SECRET_SENTINEL'));
  assert.ok(!out.includes('SYNTHETIC_PII_SENTINEL'));
});

test('G0-005.B malformed proofs and environment partition collisions remain NO_GO', () => {
  for (const x of [null, {}, [], { environments: [{ environment: 'DEV' }] }]) {
    const out = assessG0005BReadback(x);
    assert.equal(out.decision, 'NO_GO');
    assert.equal(out.liveVerified, false);
  }
});
