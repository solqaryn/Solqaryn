// C14-01.M03.05 — read-only acceptance CI evidence provider. Never grants application authority.
const REPO = 'solqaryn/Solqaryn';
const SHA40 = /^[0-9a-f]{40}$/;
const NAME = /^[A-Za-z0-9 _.:-]{1,160}$/;
const TRUSTED_ACTIONS_APP_ID = 15368;
const TRUSTED_ACTIONS_APP_SLUG = 'github-actions';
const TRUSTED_JOB_URL = /^https:\/\/github[.]com\/solqaryn\/Solqaryn\/actions\/runs\/[1-9][0-9]*\/job\/[1-9][0-9]*$/;
const trustedCheckIssuer = (check) =>
  check?.app?.id === TRUSTED_ACTIONS_APP_ID &&
  check.app.slug === TRUSTED_ACTIONS_APP_SLUG &&
  typeof check.details_url === 'string' &&
  TRUSTED_JOB_URL.test(check.details_url);

export function assessCheckRuns(payload, sha, required) {
  const denied = (reason) => ({ sourceValid: false, requiredPassed: false, reason, checked: 0,
    authorizesAccess: false, authorizesRelease: false });
  if (!SHA40.test(sha)) return denied('INVALID_SHA');
  if (!Array.isArray(required) || required.length === 0 ||
      new Set(required).size !== required.length || !required.every(n => typeof n === 'string' && NAME.test(n)))
    return denied('INVALID_REQUIRED_CHECKS');
  if (!payload || !Number.isInteger(payload.total_count) ||
      payload.total_count < 1 || payload.total_count > 100 ||
      !Array.isArray(payload.check_runs) || payload.check_runs.length !== payload.total_count)
    return denied('INCOMPLETE_OR_UNBOUNDED_RESPONSE');
  if (payload.check_runs.some(c => !c || c.head_sha !== sha || typeof c.name !== 'string' ||
       typeof c.status !== 'string' || !('conclusion' in c)))
    return denied('FOREIGN_SHA_OR_INVALID_CHECK');
  // A name and a successful SHA are insufficient: reject checks written by
  // arbitrary GitHub Apps or linked to a foreign/untrusted job.
  // The HTTPS response comes from the authenticated GitHub Checks API.
  const foreignIssuer = required.some(name => {
    const matches = payload.check_runs.filter(c => c.name === name);
    return matches.length === 1 && !trustedCheckIssuer(matches[0]);
  });
  if (foreignIssuer) return denied('UNTRUSTED_REQUIRED_CHECK_ISSUER');
  const findings = required.map(name => {
    const matches = payload.check_runs.filter(c => c.name === name);
    return { name, count: matches.length,
      outcome: matches.length === 1 ? matches[0].status + '/' + String(matches[0].conclusion) : 'MISSING_OR_AMBIGUOUS',
      passed: matches.length === 1 && matches[0].status === 'completed' && matches[0].conclusion === 'success' };
  });
  return { sourceValid: true, requiredPassed: findings.every(x => x.passed), reason: 'READ_ONLY_CI_EVIDENCE',
    checked: payload.check_runs.length, findings, authorizesAccess: false, authorizesRelease: false };
}

export async function readGitHubAcceptanceChecks({ repository, sha, token, required, fetchImpl = fetch }) {
  if (repository !== REPO || !SHA40.test(sha) || typeof token !== 'string' || !token.trim())
    throw new Error('INVALID_REPOSITORY_SHA_OR_TOKEN');
  const url = 'https://api.github.com/repos/' + REPO + '/commits/' + sha + '/check-runs?per_page=100';
  let response;
  try {
    response = await fetchImpl(url, {
      method: 'GET', redirect: 'error',
      headers: { Authorization: 'Bearer ' + token, Accept: 'application/vnd.github+json',
        'X-GitHub-Api-Version': '2022-11-28' }
    });
  } catch {
    throw new Error('CHECK_PROVIDER_UNAVAILABLE');
  }
  if (response?.status !== 200 || !String(response.headers?.get('content-type') || '').includes('application/json'))
    throw new Error('CHECK_PROVIDER_REJECTED');
  let payload;
  try { payload = await response.json(); } catch { throw new Error('INVALID_PROVIDER_JSON'); }
  return assessCheckRuns(payload, sha, required);
}
