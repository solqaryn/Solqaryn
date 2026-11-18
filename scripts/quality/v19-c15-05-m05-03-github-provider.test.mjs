import { test } from 'node:test';
import assert from 'node:assert/strict';

const REPO = 'solqaryn/Solqaryn';
const shaPattern = /^[a-f0-9]{40}$/;
const sha = process.env.GITHUB_SHA || '';
const repository = process.env.GITHUB_REPOSITORY || '';
const token = process.env.GITHUB_TOKEN || '';

function approveRequired(checks, expected, required) {
  if (!shaPattern.test(expected) || !Array.isArray(checks) || !required?.length)
    return false;
  if (checks.some(c => !c || c.head_sha !== expected)) return false;
  return required.every(name => {
    const matches = checks.filter(c => c.name === name);
    if (!matches.length) return false;
    const latest = matches.reduce((a, b) => Number(a.id || 0) > Number(b.id || 0) ? a : b);
    return latest.status === 'completed' && latest.conclusion === 'success';
  });
}

async function readProvider(options = {}) {
  const sourceRepo = options.repository ?? repository;
  const sourceSha = options.sha ?? sha;
  const sourceToken = options.token ?? token;
  const fetcher = options.fetcher ?? fetch;
  if (sourceRepo !== REPO) throw new Error('PROJECT_SCOPE_LOCK_MISMATCH');
  if (!shaPattern.test(sourceSha)) throw new Error('INVALID_EXACT_SHA');
  if (!sourceToken) throw new Error('READ_ONLY_TOKEN_REQUIRED');
  const response = await fetcher('https://api.github.com/repos/solqaryn/Solqaryn/commits/' +
    sourceSha + '/check-runs?per_page=100', {
      method: 'GET',
      headers: {
        Accept: 'application/vnd.github+json',
        Authorization: 'Bearer ' + sourceToken,
        'X-GitHub-Api-Version': '2022-11-28',
        'User-Agent': 'Solqaryn-V19-provider-test'
      },
      signal: AbortSignal.timeout(15000)
    });
  if (!response.ok) throw new Error('GITHUB_PROVIDER_HTTP_' + response.status);
  const payload = await response.json();
  if (!Number.isInteger(payload?.total_count) ||
      payload.total_count < 1 || payload.total_count > 100 ||
      !Array.isArray(payload.check_runs) ||
      payload.check_runs.length !== payload.total_count)
    throw new Error('GITHUB_PROVIDER_INCOMPLETE');
  if (payload.check_runs.some(c => !c || c.head_sha !== sourceSha || typeof c.name !== 'string'))
    throw new Error('GITHUB_PROVIDER_WRONG_SHA_OR_SCHEMA');
  return payload.check_runs;
}

const good = (id, name) => ({ id, name, head_sha: sha, status: 'completed', conclusion: 'success' });

test('live GitHub Checks provider returns exact SHA checks over HTTPS', async () => {
  const checks = await readProvider();
  assert.ok(checks.length > 0);
  assert.ok(checks.every(c => c.head_sha === sha));
  assert.ok(checks.some(c => c.name.includes('C15-05.M05.03')));
  console.log('LIVE_GITHUB_CHECKS_READ_OK count=' + checks.length);
});

test('completed success only passes exact required check names', () => {
  assert.equal(approveRequired([good(1, 'scope'), good(2, 'security')], sha, ['scope', 'security']), true);
});

test('skipped cancelled failure and incomplete checks never pass', () => {
  for (const conclusion of ['skipped', 'cancelled', 'failure', 'neutral', null]) {
    assert.equal(approveRequired([{ ...good(1, 'scope'), conclusion }], sha, ['scope']), false);
  }
  for (const status of ['queued', 'in_progress']) {
    assert.equal(approveRequired([{ ...good(1, 'scope'), status }], sha, ['scope']), false);
  }
});

test('missing, stale SHA, and latest failed retry fail closed', () => {
  assert.equal(approveRequired([good(1, 'scope')], sha, ['scope', 'security']), false);
  assert.equal(approveRequired([{ ...good(1, 'scope'), head_sha: 'f'.repeat(40) }],
    sha, ['scope']), false);
  assert.equal(approveRequired([good(1, 'scope'), { ...good(2, 'scope'), conclusion: 'failure' }],
    sha, ['scope']), false);
});

test('foreign repository and malformed SHA are rejected without HTTP', async () => {
  let called = false;
  const fetcher = async () => { called = true; throw Error('UNEXPECTED_NETWORK'); };
  await assert.rejects(readProvider({ repository: 'other/repo', token: 'test', fetcher }),
    /PROJECT_SCOPE_LOCK_MISMATCH/);
  await assert.rejects(readProvider({ sha: '../main', token: 'test', fetcher }),
    /INVALID_EXACT_SHA/);
  assert.equal(called, false);
});

test('403 from live provider is fail-closed without token in error', async () => {
  const fakeToken = 'test-only-do-not-log';
  await assert.rejects(readProvider({
    token: fakeToken, fetcher: async (_url, options) => {
      assert.equal(options.method, 'GET');
      assert.equal(options.headers.Authorization, 'Bearer ' + fakeToken);
      return { ok: false, status: 403 };
    }
  }), e => e.message === 'GITHUB_PROVIDER_HTTP_403' &&
    !e.message.includes(fakeToken));
});

test('partial pages and malformed provider JSON cannot be accepted', async () => {
  await assert.rejects(readProvider({
    token: 'test', fetcher: async () => ({
      ok: true, json: async () => ({ total_count: 2, check_runs: [good(1, 'scope')] })
    })
  }), /GITHUB_PROVIDER_INCOMPLETE/);
});
