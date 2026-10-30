import { readFileSync } from 'node:fs';

// Trusted verifier: on later PRs the required Scope Lock check executes this file
// from the target DEV base, not from the untrusted pull-request head.
const REQUIRED = ['SOLQARYN - Seguridad multiempresa', 'SOLQARYN - Build y pruebas backend'];
const SCOPE = /\b(P\d+-\d{3}\.[A-Z])\b/i;
const BLOCKED_RUNTIME_PATHS = /(^|\/)(?:\.env(?:\..*)?|[^/]+\.(?:pem|p12|key)|appsettings\.Production\.json)$|^(?:qa|prod|main)\//i;
const GUARDED = new Set([
  'AGENTS.md', 'docs/PROJECT_SCOPE_LOCK.md', 'docs/AUTOMATION_STATUS.md',
  'docs/AUTOMATION_AUTHORITY.md', 'docs/AUTOMATION_3X3_OPERATING_CONTRACT.md',
  '.github/workflows/project-scope-lock.yml', '.github/workflows/solqaryn-core-ci.yml',
  'scripts/validate-automation-authority.mjs', 'scripts/verify-project-scope.mjs',
  'scripts/verify-automated-pr-certification.mjs', 'scripts/verify-solqaryn-skills.mjs',
]);
const SECRET = /gh[pousr]_[a-zA-Z0-9_]{20,}|AKIA[A-Z0-9]{16}|-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|(?:password|token|secret)\s*[:=]\s*['"][^'"]{12,}['"]/i;
const isGuarded = name => GUARDED.has(name) || name.startsWith('.agents/skills/');
const isProductionSource = name => /^(?:backend\/src\/|frontend\/src\/)/.test(name) && !/(?:\/tests?\/|\.spec\.|\.test\.)/.test(name);

export function certify({ pr, files, runs, reviews = [], commits = [], repository, expectedHead }) {
  const failures = [];
  const fail = reason => failures.push(reason);
  if (!pr || pr.state !== 'open' || pr.draft) fail('PR_NOT_READY');
  if (!pr || pr.base?.ref !== 'dev') fail('BASE_NOT_DEV');
  if (!pr || !/^[a-f0-9]{40}$/i.test(pr.head?.sha || '') || pr.head?.sha !== expectedHead) fail('HEAD_SHA_MISMATCH');
  if (!pr || pr.head?.repo?.full_name !== repository || pr.base?.repo?.full_name !== repository) fail('FOREIGN_REPOSITORY');
  if (![['jmejia','31'].join(''), 'morales35alex', 'solqaryn-platform'].includes(pr?.user?.login)) fail('GITHUB_AUTHOR_NOT_ALLOWLISTED');
  if (!Array.isArray(files) || files.length === 0 || files.length > 200) fail('PR_FILE_COUNT_INVALID');
  const scope = pr?.title?.match(SCOPE)?.[1]?.toUpperCase();
  if (scope && pr?.user?.login === 'solqaryn-platform') fail('INSTITUTIONAL_GITHUB_IDENTITY_NOT_COMMERCIAL_TASK');
  const names = files?.map(x => x.filename) || [];
  if (new Set(names).size !== names.length || names.some(n => typeof n !== 'string' || n.includes('..') || n.includes('\\'))) fail('UNSAFE_FILE_PATH');
  if (names.some(n => BLOCKED_RUNTIME_PATHS.test(n))) fail('QA_PROD_SECRET_FILE_SCOPE_FORBIDDEN');
  if (!names.includes('CHANGELOG_AI.md')) fail('CHANGELOG_MISSING');
  if (scope) {
    if (pr.head.ref !== 'work/solqaryn-' + scope.toLowerCase().replace(/\./g, '-')) fail('NON_DETERMINISTIC_COMMERCIAL_BRANCH');
    if (!String(pr.body || '').includes(scope)) fail('SCOPE_EVIDENCE_MISSING');
  }
  for (const file of files || []) {
    if (isProductionSource(file.filename)) {
      const additions = (file.patch || '').split('\n').filter(line => line.startsWith('+') && !line.startsWith('+++'));
      if (additions.some(line => SECRET.test(line))) fail('POTENTIAL_SECRET_IN_SOURCE:' + file.filename);
    }
  }
  if (!Array.isArray(runs) || runs.length === 0 || runs.length > 100) fail('CHECK_RUNS_UNAVAILABLE_OR_UNBOUNDED');
  const byName = new Map((runs || []).filter(x => x.head_sha === expectedHead).map(x => [x.name, x]));
  for (const name of REQUIRED) {
    const item = byName.get(name);
    if (!item || item.status !== 'completed' || item.conclusion !== 'success') fail('REQUIRED_CHECK_NOT_GREEN:' + name);
  }
  const applicable = [...byName.values()].filter(x => x.name !== 'verify-solqaryn-project-scope');
  const incomplete = applicable.filter(x => x.status !== 'completed');
  if (incomplete.length) fail('OTHER_CHECKS_PENDING:' + incomplete.map(x => x.name).join(','));
  const bad = applicable.filter(x => !['success', 'skipped'].includes(x.conclusion));
  if (bad.length) fail('CHECK_FAILED:' + bad.map(x => x.name).join(','));
  if (scope && !applicable.some(x => x.name.toUpperCase().includes(scope) && x.status === 'completed' && x.conclusion === 'success')) fail('SCOPE_DIRECTED_TEST_MISSING:' + scope);
  const changesGuarded = names.some(isGuarded);
  if (changesGuarded) {
    const contributorLogins = new Set([pr?.user?.login]);
    for (const commit of commits || []) for (const login of [commit.author?.login, commit.committer?.login]) if (login) contributorLogins.add(login);
    const validReviews = (reviews || []).filter(review => review.state === 'APPROVED' && review.commit_id === expectedHead && review.user?.login && !contributorLogins.has(review.user.login));
    if (!validReviews.length) fail('GOVERNANCE_REQUIRES_INDEPENDENT_FORMAL_REVIEW');
  } else if (!scope) {
    fail('UNCLASSIFIED_AUTONOMOUS_CHANGE');
  }
  return { pass: failures.length === 0, failures, mode: changesGuarded ? 'GOVERNANCE_REVIEW' : 'AUTONOMOUS_COMMERCIAL_CERT', scope: scope || null, sha: expectedHead };
}

const makeRun = (name, sha, conclusion = 'success') => ({ name, head_sha: sha, status: 'completed', conclusion });
function selfTest() {
  const h = 'a'.repeat(40);
  const pr = { state: 'open', draft: false, title: 'test(P0-010.B): validacion negativa', body: 'P0-010.B evidencia', user: { login: ['jmejia','31'].join('') },
    base: { ref: 'dev', repo: { full_name: 'solqaryn/Solqaryn' } }, head: { ref: 'work/solqaryn-p0-010-b', sha: h, repo: { full_name: 'solqaryn/Solqaryn' } } };
  const files = [{ filename: 'CHANGELOG_AI.md', patch: '+change' }, { filename: 'scripts/verify-p0-010-b.mjs', patch: '+test' }];
  const runs = [...REQUIRED.map(n => makeRun(n, h)), makeRun('P0-010.B - Directed negatives', h)];
  const fixture = { pr, files, runs, repository: 'solqaryn/Solqaryn', expectedHead: h };
  const clone = v => structuredClone(v);
  const assertPass = (label, obj) => { const x = certify(obj); if (!x.pass) throw Error('positive ' + label + ': ' + x.failures.join(';')); };
  const assertReject = (label, change) => { const x = certify(change(clone(fixture))); if (x.pass) throw Error('negative not rejected ' + label); };
  assertPass('ordinary commercial PR', fixture);
  assertReject('missing security check', x => ({...x, runs: x.runs.filter(c => c.name !== REQUIRED[0])}));
  assertReject('failed backend', x => { x.runs[1].conclusion = 'failure'; return x; });
  assertReject('stale head', x => ({...x, expectedHead: 'b'.repeat(40)}));
  assertReject('draft', x => { x.pr.draft = true; return x; });
  assertReject('foreign repo', x => { x.pr.head.repo.full_name = 'other/repo'; return x; });
  assertReject('forbidden author', x => { x.pr.user.login = 'unknown'; return x; });
  assertReject('platform cannot claim commercial scope', x => { x.pr.user.login = 'solqaryn-platform'; return x; });
  assertReject('missing targeted test', x => ({...x, runs: x.runs.slice(0, 2)}));
  assertReject('pending check', x => { x.runs[2].status = 'in_progress'; x.runs[2].conclusion = null; return x; });
  assertReject('non deterministic branch', x => { x.pr.head.ref = 'feature/random'; return x; });
  assertReject('production config', x => { x.files.push({filename:'backend/appsettings.Production.json'}); return x; });
  assertReject('secret in production source', x => { x.files.push({filename:'backend/src/API/Secrets.cs',patch:'@@ -0,0 +1 @@\n+private string password = "aVeryLongPassword1234";'}); return x; });
  assertReject('guarded workflow no review', x => { x.files.push({filename:'.github/workflows/project-scope-lock.yml'}); return x; });
  const human = clone(fixture); human.files.push({filename:'docs/AUTOMATION_STATUS.md'});
  human.reviews = [{ state:'APPROVED', commit_id:h, user:{login:'morales35alex'} }];
  assertPass('separate formal governance review', human);
  const institution = clone(human); institution.pr.user.login = 'solqaryn-platform'; institution.pr.title = 'docs(governance): Update policy'; institution.pr.head.ref = 'codex/governance-update'; institution.reviews = [{state:'APPROVED',commit_id:h,user:{login:'morales35alex'}}];
  assertPass('platform governance with independent review', institution);

  assertReject('self governance approval', x => { x.files.push({filename:'docs/AUTOMATION_STATUS.md'}); x.reviews = [{state:'APPROVED',commit_id:h,user:{login:['jmejia','31'].join('')}}];return x; });
  assertReject('stale governance review', x => { x.files.push({filename:'docs/AUTOMATION_STATUS.md'}); x.reviews = [{state:'APPROVED',commit_id:'b'.repeat(40),user:{login:'morales35alex'}}];return x; });
  console.log('AUTO_CERT_SELF_TEST=PASS (2 positive + 15 fail-closed negatives)');
}

const args = new Set(process.argv.slice(2));
if (args.has('--self-test')) selfTest();
else if (args.has('--pr')) {
  const event = JSON.parse(readFileSync(process.env.GITHUB_EVENT_PATH, 'utf8'));
  const token = process.env.GITHUB_TOKEN;
  const repository = process.env.GITHUB_REPOSITORY;
  if (!token || !repository || !/^[\w.-]+\/[\w.-]+$/.test(repository)) throw Error('GITHUB_API_AUTH_UNAVAILABLE');
  const endpoint = process.env.GITHUB_API_URL || 'https://api.github.com';
  const api = async path => {
    const response = await fetch(endpoint + '/repos/' + repository + path, {
      headers: { Authorization: 'Bearer ' + token, Accept: 'application/vnd.github+json', 'X-GitHub-Api-Version': '2022-11-28' },
      signal: AbortSignal.timeout(20000),
    });
    if (!response.ok) throw Error('GITHUB_API_READ_FAILED_' + response.status + ':' + path.split('?')[0]);
    return response.json();
  };
  const number = event.pull_request?.number;
  const expectedHead = event.pull_request?.head?.sha;
  if (!Number.isInteger(number) || !/^[a-f0-9]{40}$/i.test(expectedHead || '')) throw Error('INVALID_PULL_REQUEST_EVENT');
  const deadline = Date.now() + 30 * 60 * 1000;
  const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
  let report;
  for (;;) {
    const pr = await api('/pulls/' + number);
    const filesResp = await api('/pulls/' + number + '/files?per_page=100');
    const [checkList, reviews, commits] = await Promise.all([
      api('/commits/' + expectedHead + '/check-runs?per_page=100'),
      api('/pulls/' + number + '/reviews?per_page=100'),
      api('/pulls/' + number + '/commits?per_page=100'),
    ]);
    if (pr.changed_files > 100 || checkList.total_count > 100 || pr.commits > 100) throw Error('UNBOUNDED_PR_METADATA');
    report = certify({ pr, files: filesResp, runs: checkList.check_runs || [], reviews, commits, repository, expectedHead });
    if (report.pass) break;
    const transient = report.failures.every(f => f.startsWith('OTHER_CHECKS_PENDING:') || f.startsWith('REQUIRED_CHECK_NOT_GREEN:') || f === 'GOVERNANCE_REQUIRES_INDEPENDENT_FORMAL_REVIEW');
    if (!transient || Date.now() >= deadline) break;
    await sleep(20000);
  }
  console.log('AUTO_CERT_MODE=' + report.mode + ' SHA=' + report.sha + ' SCOPE=' + (report.scope || 'NONE'));
  if (!report.pass) throw Error('AUTO_CERT=NO_GO ' + report.failures.join(' | '));
  console.log('AUTO_CERT=PASS (checks exact-head, source scope, negative tests, trust boundary)');
} else throw Error('Provide --self-test or --pr');
