import { test } from 'node:test';
import assert from 'node:assert/strict';
import { assessCheckRuns, readGitHubAcceptanceChecks } from './v19-c14-01-m03-05-acceptance-provider.mjs';
const sha = '0123456789abcdef0123456789abcdef01234567';
const trustedApp = Object.freeze({ id: 15368, slug: 'github-actions' });
const trustedJob = 'https://github.com/solqaryn/Solqaryn/actions/runs/123456/job/7890';
const good = (name, status = 'completed', conclusion = 'success', head_sha = sha) =>
  ({ name, head_sha, status, conclusion, app: trustedApp, details_url: trustedJob });
const payload = (...checks) => ({ total_count: checks.length, check_runs: checks });
const wrap = (data, status = 200) => ({
  status, headers: { get: () => 'application/json; charset=utf-8' }, json: async () => data
});
const args = (fetchImpl) => ({ repository: 'solqaryn/Solqaryn', sha, token: 'ephemeral-test-token',
  required: ['verify-solqaryn-project-scope'], fetchImpl });

test('success exact SHA is evidence only, never an access or release grant', async () => {
  const result = await readGitHubAcceptanceChecks(args(async () => wrap(payload(good('verify-solqaryn-project-scope')))));
  assert.equal(result.sourceValid, true);
  assert.equal(result.requiredPassed, true);
  assert.equal(result.authorizesAccess, false);
  assert.equal(result.authorizesRelease, false);
});
test('queued, in progress, skipped, cancelled and failed are never PASS', () => {
  for (const [status, conclusion] of [['queued', null], ['in_progress', null],
    ['completed', 'skipped'], ['completed', 'cancelled'], ['completed', 'failure']]) {
    const result = assessCheckRuns(payload(good('verify-solqaryn-project-scope', status, conclusion)), sha,
      ['verify-solqaryn-project-scope']);
    assert.equal(result.requiredPassed, false);
    assert.equal(result.authorizesAccess, false);
  }
});
test('foreign SHA and untrusted/missing/multiple check names fail closed', () => {
  assert.equal(assessCheckRuns(payload(good('verify-solqaryn-project-scope', 'completed', 'success', 'f'.repeat(40))), sha,
    ['verify-solqaryn-project-scope']).sourceValid, false);
  assert.equal(assessCheckRuns(payload(good('another')), sha, ['verify-solqaryn-project-scope']).requiredPassed, false);
  assert.equal(assessCheckRuns(payload(good('verify-solqaryn-project-scope'), good('verify-solqaryn-project-scope')), sha,
    ['verify-solqaryn-project-scope']).requiredPassed, false);
});
test('forged GitHub app identity is rejected even with same exact SHA, name and PASS', () => {
  const base = good('verify-solqaryn-project-scope');
  for (const app of [
    undefined, { id: 777, slug: 'github-actions' },
    { id: 15368, slug: 'other-app' },
    { id: '15368', slug: 'github-actions' }
  ]) {
    const result = assessCheckRuns(payload({ ...base, app }), sha, ['verify-solqaryn-project-scope']);
    assert.equal(result.sourceValid, false);
    assert.equal(result.requiredPassed, false);
    assert.equal(result.reason, 'UNTRUSTED_REQUIRED_CHECK_ISSUER');
  }
});
test('required check job origin must be this repository GitHub Actions URL', () => {
  const base = good('verify-solqaryn-project-scope');
  for (const details_url of [
    undefined, 'http://github.com/solqaryn/Solqaryn/actions/runs/1/job/2',
    'https://github.com/other/Solqaryn/actions/runs/1/job/2',
    'https://github.com/solqaryn/Solqaryn/actions/runs/1',
    'https://evil.example/actions/runs/1/job/2'
  ]) {
    const result = assessCheckRuns(payload({ ...base, details_url }), sha, ['verify-solqaryn-project-scope']);
    assert.equal(result.sourceValid, false);
    assert.equal(result.requiredPassed, false);
  }
  const valid = assessCheckRuns(payload(base), sha, ['verify-solqaryn-project-scope']);
  assert.equal(valid.requiredPassed, true);
});
test('partial pagination, malformed data and empty set never certify', () => {
  for (const p of [payload(), { total_count: 2, check_runs: [good('verify-solqaryn-project-scope')] },
      { total_count: 101, check_runs: Array(101).fill(good('verify-solqaryn-project-scope')) },
      { total_count: '1', check_runs: [good('verify-solqaryn-project-scope')] }]) {
    assert.equal(assessCheckRuns(p, sha, ['verify-solqaryn-project-scope']).sourceValid, false);
  }
});
test('repository, token, SHA and required-set inputs reject malformed values', async () => {
  await assert.rejects(readGitHubAcceptanceChecks({ ...args(async () => wrap(payload(good('x')))),
    repository: 'other/repo' }), /INVALID/);
  await assert.rejects(readGitHubAcceptanceChecks({ ...args(async () => wrap(payload(good('x')))),
    token: '' }), /INVALID/);
  assert.equal(assessCheckRuns(payload(good('x')), 'bad', ['x']).sourceValid, false);
  assert.equal(assessCheckRuns(payload(good('x')), sha, ['x', 'x']).sourceValid, false);
});
test('HTTP refusal, non-JSON, malformed JSON and network errors deny', async () => {
  await assert.rejects(readGitHubAcceptanceChecks(args(async () => wrap({}, 403))), /REJECTED/);
  await assert.rejects(readGitHubAcceptanceChecks(args(async () => ({
    status: 200, headers: { get: () => 'text/html' }, json: async () => ({})
  }))), /REJECTED/);
  await assert.rejects(readGitHubAcceptanceChecks(args(async () => ({
    status: 200, headers: { get: () => 'application/json' }, json: async () => { throw Error('secret'); }
  }))), /INVALID_PROVIDER_JSON/);
  await assert.rejects(readGitHubAcceptanceChecks(args(async () => { throw Error('secret'); })),
    /UNAVAILABLE/);
});
test('GET is restricted to exact repository, SHA and no redirect token forwarding', async () => {
  let seen;
  await readGitHubAcceptanceChecks(args(async (url, options) => {
    seen = { url, method: options.method, redirect: options.redirect };
    return wrap(payload(good('verify-solqaryn-project-scope')));
  }));
  assert.equal(seen.url, 'https://api.github.com/repos/solqaryn/Solqaryn/commits/' + sha + '/check-runs?per_page=100');
  assert.equal(seen.method, 'GET');
  assert.equal(seen.redirect, 'error');
});
test('real GitHub Checks GET for this commit, without inventing a CI success result', async () => {
  const current = process.env.GITHUB_SHA;
  const token = process.env.GITHUB_TOKEN;
  assert.match(current || '', /^[0-9a-f]{40}$/);
  assert.ok(token);
  const result = await readGitHubAcceptanceChecks({
    repository: process.env.GITHUB_REPOSITORY, sha: current, token,
    required: ['verify-solqaryn-project-scope']
  });
  assert.equal(result.sourceValid, true);
  assert.ok(result.checked > 0);
  assert.equal(result.authorizesAccess, false);
  assert.equal(result.authorizesRelease, false);
  console.log('C1401_M0305_PROVIDER_LIVE_GET=PASS;CHECKS=' + result.checked +
    ';REQUIRED_PASS=' + result.requiredPassed + ';AUTH_RELEASE=false');
});
