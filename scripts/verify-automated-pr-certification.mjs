import { readFileSync } from 'node:fs';

// Trusted verifier: on later PRs the required Scope Lock check executes this file
// from the target DEV base, not from the untrusted pull-request head.
const REQUIRED = ['SOLQARYN - Seguridad multiempresa', 'SOLQARYN - Build y pruebas backend'];
const DIRECTED_CHECK = 'SOLQARYN - Directed commercial scope';
const TRUSTED_GITHUB_ACTIONS_APP_ID = 15368;
const SCOPE = /\b((?:P\d+|I8|G0)-\d{3}\.[A-Z])\b/i;
const INSTITUTIONAL_EXECUTOR = 'solqaryn-platform';
const INDEPENDENT_REVIEWER = ['jmejia', '31'].join('');
const BLOCKED_RUNTIME_PATHS = /(^|\/)(?:\.env(?:\..*)?|[^/]+\.(?:pem|p12|key)|appsettings\.Production\.json)$|^(?:qa|prod|main)\//i;
const GUARDED = new Set([
  'AGENTS.md', 'docs/PROJECT_SCOPE_LOCK.md', 'docs/AUTOMATION_STATUS.md', 'docs/GITHUB_ACCOUNT_ROLES.md',
  'docs/AUTOMATION_AUTHORITY.md',
  '.github/workflows/project-scope-lock.yml', '.github/workflows/solqaryn-core-ci.yml',
  'scripts/automation-stage-ownership.mjs',
  'scripts/automation-review-routing.mjs', 'scripts/automation-ci-throughput.mjs',
  'scripts/validate-dual-task-configuration.mjs',
            'scripts/validate-automation-authority.mjs', 'scripts/verify-project-scope.mjs',
  'scripts/verify-automated-pr-certification.mjs', 'scripts/verify-solqaryn-skills.mjs',
]);
const SECRET = /gh[pousr]_[a-zA-Z0-9_]{20,}|AKIA[A-Z0-9]{16}|-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|(?:password|token|secret)\s*[:=]\s*['"][^'"]{12,}['"]/i;
const isGuarded = name => GUARDED.has(name) || name.startsWith('.agents/skills/') || name.startsWith('.github/') || name.startsWith('scripts/security/');
const isProductionSource = name => /^(?:backend\/src\/|frontend\/src\/)/.test(name) && !/(?:\/tests?\/|\.spec\.|\.test\.)/.test(name);

// Only known, non-mandatory broad suites may finish after the fast lane.
// Never defer required security/backend, the directed scope or an unknown check.
const CI_IMPACT_CHECK = 'Impacto CI - clasificacion MySQL fail-closed';
const MYSQL_INTEGRATION_CHECK = 'Migraciones EF Oracle, variantes y cargas masivas en MySQL 8.4';
const BROAD_OPTIONAL = new Set([
  'Módulos, cargas, sesión, catálogos, variantes, accesibilidad, facturación y correo',
  'ERP - Contratos, pruebas y build',
  'Migraciones EF Oracle, variantes y cargas masivas en MySQL 8.4',
  'Authorization, files, secrets and tenant gate',
  'MySQL 8.4 isolated backup and restore proof',
  'Backend Release y pruebas',
  'Frontend producción',
  'SOLQARYN - Frontend tipado y build',
  'Docker y aislamiento de entornos',
  'Preflight, backfill y autoridad en MySQL 8.4',
]);
const ASYNC_REVIEWED_NON_RUNTIME=new Set([
 'AGENTS.md','docs/PROJECT_SCOPE_LOCK.md','docs/AUTOMATION_STATUS.md',
 'docs/AUTOMATION_AUTHORITY.md','scripts/automation-review-routing.mjs',
 'scripts/automation-stage-ownership.mjs','scripts/automation-ci-throughput.mjs',
 'scripts/validate-dual-task-configuration.mjs','scripts/security/ci-impact-routing.mjs',
]);
const CI_IMPACT_SOURCE='scripts/security/ci-impact-routing.mjs';
const DOCS_WITH_RUNTIME_CONTRACTS = new Set([
  'docs/PRODUCTION_READINESS_CONTRACT.md',
  'docs/PERFORMANCE_BENCHMARK_CONTRACT.md',
  'docs/DETALLES_PENDIENTES.md',
]);
// Test-only P1 changes are already exercised by required backend/security + directed scope jobs.
// Exclude integration-suite paths, runtime code and protected contracts from the fast lane.
const OFFLINE_COMMERCIAL_UNIT = /^backend\/tests\/Solqaryn\.Tests\/(?:[A-Za-z0-9_.-]+\/)*P\d{4}[A-Z][A-Za-z0-9_]*Tests\.cs$/;
const FRONTEND_TEST_ONLY = /^frontend\/src\/[A-Za-z0-9_./-]+\.spec\.ts$/;
const INTEGRATION_TEST_PATH = /(?:\/Integration(?:Tests)?\/|\/Database(?:Tests)?\/|IntegrationTests\.cs$)/i;

function canDeferUnrelatedSuites(files,byName){
 if(!Array.isArray(files)||!files.length||files.length>200)return false;
 if(files.some(f=>!f||!['added','modified'].includes(f.status)||
   typeof f.filename!=='string'||f.filename.includes('..')||
   f.filename.includes('\\')||BLOCKED_RUNTIME_PATHS.test(f.filename)||
   (isGuarded(f.filename)&&!ASYNC_REVIEWED_NON_RUNTIME.has(f.filename))))return false;
 let units=0,governance=0,classifier=false;
 for(const f of files){
  const path=f.filename;
  if(path==='CHANGELOG_AI.md')continue;
  if(ASYNC_REVIEWED_NON_RUNTIME.has(path)){
   governance++;if(path===CI_IMPACT_SOURCE)classifier=true;continue;
  }
  if(path.startsWith('docs/')&&path.endsWith('.md')&&
     !DOCS_WITH_RUNTIME_CONTRACTS.has(path)&&
     !path.startsWith('docs/evidencias/modernizacion/'))continue;
  if(!INTEGRATION_TEST_PATH.test(path)&&
     (OFFLINE_COMMERCIAL_UNIT.test(path)||FRONTEND_TEST_ONLY.test(path))){units++;continue;}
  return false;
 }
 if(units&&governance)return false;
 if(!units&&!classifier)return true;
 const impact=byName.get(CI_IMPACT_CHECK),mysql=byName.get(MYSQL_INTEGRATION_CHECK);
 return impact?.head_sha===mysql?.head_sha&&impact?.status==='completed'&&
   impact.conclusion==='success'&&impact.app?.id===TRUSTED_GITHUB_ACTIONS_APP_ID&&
   mysql?.app?.id===TRUSTED_GITHUB_ACTIONS_APP_ID&&
   (mysql.status==='completed'&&['success','skipped'].includes(mysql.conclusion));
}

function optionalPendingDeferred(files, byName) {
  if (!canDeferUnrelatedSuites(files, byName)) return [];
  return [...byName.values()].filter(check => BROAD_OPTIONAL.has(check.name) &&
    ['queued', 'in_progress'].includes(check.status) &&
    check.app?.id === TRUSTED_GITHUB_ACTIONS_APP_ID).map(check => check.name);
}

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
  // The owner authorized the institutional account to author commercial PRs;
  // those PRs need a genuine independent Javier review.
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
    if (!item || item.status !== 'completed' || item.conclusion !== 'success' || item.app?.id !== TRUSTED_GITHUB_ACTIONS_APP_ID) fail('REQUIRED_CHECK_NOT_GREEN:' + name);
  }
  const applicable = [...byName.values()].filter(x => x.name !== 'verify-solqaryn-project-scope');
  const deferred = optionalPendingDeferred(files, byName);
  const incomplete = applicable.filter(x => x.status !== 'completed' && !deferred.includes(x.name));
  if (incomplete.length) fail('OTHER_CHECKS_PENDING:' + incomplete.map(x => x.name).join(','));
  // Even in the fast lane, an already failed check is NEVER silently ignored.
  const bad = applicable.filter(x => x.status === 'completed' && !['success', 'skipped'].includes(x.conclusion));
  if (bad.length) fail('CHECK_FAILED:' + bad.map(x => x.name).join(','));
  if (scope && !applicable.some(x => x.name === DIRECTED_CHECK && x.status === 'completed' && x.conclusion === 'success' && x.app?.id === TRUSTED_GITHUB_ACTIONS_APP_ID)) fail('SCOPE_DIRECTED_TEST_MISSING:' + scope);
  const changesGuarded = names.some(isGuarded);
  const institutionalPR = pr?.user?.login === INSTITUTIONAL_EXECUTOR;
  if (changesGuarded || institutionalPR) {
    const contributorLogins = new Set([pr?.user?.login]);
    for (const commit of commits || []) for (const login of [commit.author?.login, commit.committer?.login]) if (login) contributorLogins.add(login);
    const validReviews = (reviews || []).filter(review => review.state === 'APPROVED' && review.commit_id === expectedHead &&
      review.user?.login && !contributorLogins.has(review.user.login) &&
      (!institutionalPR || review.user.login === INDEPENDENT_REVIEWER));
    if (!validReviews.length) fail(institutionalPR ? 'INSTITUTIONAL_REQUIRES_JAVIER_INDEPENDENT_REVIEW' : 'GOVERNANCE_REQUIRES_INDEPENDENT_FORMAL_REVIEW');
  } else if (!scope) {
    fail('UNCLASSIFIED_AUTONOMOUS_CHANGE');
  }
  return { pass: failures.length === 0, failures,
    mode: changesGuarded ? 'GOVERNANCE_REVIEW' : institutionalPR ? 'INSTITUTIONAL_REVIEW' : 'AUTONOMOUS_COMMERCIAL_CERT',
    scope: scope || null, sha: expectedHead,
    deferred, awaiting: incomplete.map(x => x.name),
  };
}

const makeRun = (name, sha, conclusion = 'success') => ({ name, head_sha: sha, status: 'completed', conclusion, app: { id: TRUSTED_GITHUB_ACTIONS_APP_ID } });
function selfTest() {
  const h = 'a'.repeat(40);
  const pr = { state: 'open', draft: false, title: 'test(P0-010.B): validacion negativa', body: 'P0-010.B evidencia', user: { login: ['jmejia','31'].join('') },
    base: { ref: 'dev', repo: { full_name: 'solqaryn/Solqaryn' } }, head: { ref: 'work/solqaryn-p0-010-b', sha: h, repo: { full_name: 'solqaryn/Solqaryn' } } };
  const files = [{ filename: 'CHANGELOG_AI.md', patch: '+change' }, { filename: 'scripts/verify-p0-010-b.mjs', patch: '+test' }];
  const runs = [...REQUIRED.map(n => makeRun(n, h)), makeRun(DIRECTED_CHECK, h)];
  const fixture = { pr, files, runs, repository: 'solqaryn/Solqaryn', expectedHead: h };
  const clone = v => structuredClone(v);
  let positives = 0, negatives = 0, waitCases = 0;
  const assertPass = (label, obj) => { const x = certify(obj); if (!x.pass) throw Error('positive ' + label + ': ' + x.failures.join(';')); positives++; };
  const assertReject = (label, change) => { const x = certify(change(clone(fixture))); if (x.pass) throw Error('negative not rejected ' + label); negatives++; };
  assertPass('ordinary commercial PR', fixture);
  // V9: only authorized I8/G0 follow-ups; branch evidence and a trusted directed
  // exact-HEAD check remain mandatory just as for P-program scopes.
  for (const nextScope of ['I8-001.C', 'G0-006.C']) {
    const native = clone(fixture);
    native.pr.title = 'test(' + nextScope + '): causal commercial closure';
    native.pr.body = nextScope + ' evidence';
    native.pr.head.ref = 'work/solqaryn-' + nextScope.toLowerCase().replace(/\./g, '-');
    assertPass('authorized directed ' + nextScope, native);
    const unsigned = clone(native);
    unsigned.runs = unsigned.runs.filter(run => run.name !== DIRECTED_CHECK);
    const missing = certify(unsigned);
    if (missing.pass || !missing.failures.includes('SCOPE_DIRECTED_TEST_MISSING:' + nextScope))
      throw Error('NON_P_SCOPE_DIRECTED_CHECK_REQUIRED:' + nextScope);
    negatives++;
    const wrongBranch = clone(native);
    wrongBranch.pr.head.ref = 'feature/unverified';
    if (certify(wrongBranch).pass) throw Error('NON_P_SCOPE_BRANCH_LOCK_MISSING:' + nextScope);
    negatives++;
  }
  assertReject('unapproved external program cannot be certified', x => {
    x.pr.title = 'test(X9-001.C): unsupported';
    x.pr.body = 'X9-001.C';
    x.pr.head.ref = 'work/solqaryn-x9-001-c';
    return x;
  });
  assertReject('missing security check', x => ({...x, runs: x.runs.filter(c => c.name !== REQUIRED[0])}));
  assertReject('failed backend', x => { x.runs[1].conclusion = 'failure'; return x; });
  assertReject('stale head', x => ({...x, expectedHead: 'b'.repeat(40)}));
  assertReject('draft', x => { x.pr.draft = true; return x; });
  assertReject('foreign repo', x => { x.pr.head.repo.full_name = 'other/repo'; return x; });
  assertReject('forbidden author', x => { x.pr.user.login = 'unknown'; return x; });
  assertReject('institutional commercial needs Javier approval', x => { x.pr.user.login = INSTITUTIONAL_EXECUTOR; return x; });
  const commercialInstitution = clone(fixture);
  commercialInstitution.pr.user.login = INSTITUTIONAL_EXECUTOR;
  commercialInstitution.reviews = [{state:'APPROVED', commit_id:h, user:{login:INDEPENDENT_REVIEWER}}];
  assertPass('institutional commercial PR reviewed independently by Javier', commercialInstitution);
  assertReject('institutional commercial wrong reviewer', x => { x.pr.user.login = INSTITUTIONAL_EXECUTOR; x.reviews = [{state:'APPROVED',commit_id:h,user:{login:'morales35alex'}}];return x; });
  assertReject('institutional review by contributor', x => { x.pr.user.login = INSTITUTIONAL_EXECUTOR; x.reviews = [{state:'APPROVED',commit_id:h,user:{login:INDEPENDENT_REVIEWER}}]; x.commits = [{author:{login:INDEPENDENT_REVIEWER}}];return x; });
  assertReject('institutional stale review', x => { x.pr.user.login = INSTITUTIONAL_EXECUTOR; x.reviews = [{state:'APPROVED',commit_id:'b'.repeat(40),user:{login:INDEPENDENT_REVIEWER}}];return x; });
  assertReject('forged directed success from untrusted app', x => { x.runs[2].app.id = 1; return x; });
  assertReject('forged backend success from untrusted app', x => { x.runs[1].app.id = 1; return x; });
  assertReject('untrusted arbitrary check name cannot certify directed tests', x => { x.runs[2].name = 'P0-010.B - Arbitrary check'; return x; });
  assertReject('unexpanded workflow expression cannot certify directed tests', x => { x.runs[2].name = 'SOLQARYN - Directed ${{ github.event.pull_request.title }}'; return x; });
  assertReject('missing targeted test', x => ({...x, runs: x.runs.slice(0, 2)}));
  assertReject('pending check', x => { x.runs[2].status = 'in_progress'; x.runs[2].conclusion = null; return x; });
  assertReject('non deterministic branch', x => { x.pr.head.ref = 'feature/random'; return x; });
  assertReject('production config', x => { x.files.push({filename:'backend/appsettings.Production.json'}); return x; });
  assertReject('secret in production source', x => { x.files.push({filename:'backend/src/API/Secrets.cs',patch:'@@ -0,0 +1 @@\n+private string password = "aVeryLongPassword1234";'}); return x; });
  assertReject('guarded workflow no review', x => { x.files.push({filename:'.github/workflows/project-scope-lock.yml'}); return x; });
  const human = clone(fixture); human.files.push({filename:'docs/AUTOMATION_STATUS.md'});
  human.reviews = [{ state:'APPROVED', commit_id:h, user:{login:'morales35alex'} }];
  assertPass('separate formal governance review', human);
  const institution = clone(human); institution.pr.user.login = 'solqaryn-platform'; institution.pr.title = 'docs(governance): Update policy'; institution.pr.head.ref = 'codex/governance-update'; institution.reviews = [{state:'APPROVED',commit_id:h,user:{login:INDEPENDENT_REVIEWER}}];
  assertPass('platform governance with independent Javier review', institution);
  assertReject('institutional governance with wrong reviewer', x => { x.files.push({filename:'docs/AUTOMATION_STATUS.md'}); x.pr.user.login = INSTITUTIONAL_EXECUTOR; x.reviews = [{state:'APPROVED',commit_id:h,user:{login:'morales35alex'}}];return x; });

  assertReject('self governance approval', x => { x.files.push({filename:'docs/AUTOMATION_STATUS.md'}); x.reviews = [{state:'APPROVED',commit_id:h,user:{login:['jmejia','31'].join('')}}];return x; });
  assertReject('stale governance review', x => { x.files.push({filename:'docs/AUTOMATION_STATUS.md'}); x.reviews = [{state:'APPROVED',commit_id:'b'.repeat(40),user:{login:'morales35alex'}}];return x; });
  const pending = name => ({...makeRun(name, h), status:'in_progress', conclusion:null});
  const optional = 'Authorization, files, secrets and tenant gate';
  const docs = [{filename:'CHANGELOG_AI.md',status:'modified'},
    {filename:'docs/CI_NOTE.md',status:'modified'}];
  const docsLane = clone(fixture);
  docsLane.files = docs;
  docsLane.runs.push(pending(optional));
  assertPass('docs evidence can certify while unrelated ERP suite runs', docsLane);
  const docsResult = certify(docsLane);
  if (!docsResult.deferred.includes(optional)) throw Error('DOCS_FAST_LANE_MUST_RECORD_DEFERRED_CHECK');
  const unitPath = 'backend/tests/Solqaryn.Tests/P1003BSupportRouteCertificationTests.cs';
  const unitsLane = clone(docsLane);
  unitsLane.files = [docs[0],{filename:unitPath,status:'added'}];
  unitsLane.runs.push(makeRun(CI_IMPACT_CHECK,h), makeRun(MYSQL_INTEGRATION_CHECK,h,'skipped'));
  assertPass('scoped unit with exact-head trusted CI and skipped MySQL', unitsLane);
  assertReject('unit without CI classifier', x => { x.files = unitsLane.files; x.runs.push(pending(optional)); return x; });
  assertReject('unit classifier forged by untrusted app', x => {
    x.files = unitsLane.files; x.runs = clone(unitsLane.runs);
    x.runs.find(z=>z.name===CI_IMPACT_CHECK).app.id = 42; return x;
  });
  const unitMysqlComplete = clone(unitsLane);
  unitMysqlComplete.runs.find(z=>z.name===MYSQL_INTEGRATION_CHECK).conclusion='success';
  assertPass('test-only PR with successful optional MySQL is still fast', unitMysqlComplete);
  const mixedP1Lane = clone(docsLane);
  mixedP1Lane.files = [docs[0],
    {filename:'backend/tests/Solqaryn.Tests/API/Controllers/P1014EGuidedOnboardingPositiveTests.cs',status:'added'},
    {filename:'frontend/src/app/features/plataforma/plataforma-gestion-mvp.component.spec.ts',status:'modified'}];
  mixedP1Lane.runs.push(makeRun(CI_IMPACT_CHECK,h),makeRun(MYSQL_INTEGRATION_CHECK,h,'skipped'));
  assertPass('P1 nested backend + Angular specs defer broad optional CI', mixedP1Lane);
  const mixedReport=certify(mixedP1Lane);
  if(!mixedReport.deferred.includes(optional) || mixedReport.deferred.includes(MYSQL_INTEGRATION_CHECK))
    throw Error('P1_FAST_LANE_MUST_WAIT_MYSQL_TERMINAL');
  assertReject('P1 unit with active MySQL cannot claim safe skip',x=>{
    x.files=clone(mixedP1Lane.files);x.runs=clone(mixedP1Lane.runs);
    const m=x.runs.find(run=>run.name===MYSQL_INTEGRATION_CHECK);
    m.status='in_progress';m.conclusion=null;return x;
  });
  assertReject('P1 nested unit without trusted impact check',x=>{
    x.files=clone(mixedP1Lane.files);x.runs=clone(mixedP1Lane.runs).filter(run=>run.name!==CI_IMPACT_CHECK);return x;
  });
  assertReject('P1 actual database integration test cannot defer optional',x=>{
    x.files=[docs[0],{filename:'backend/tests/Solqaryn.Tests/Integration/P1014EIntegrationTests.cs',status:'added'}];
    x.runs=clone(mixedP1Lane.runs);return x;
  });
  assertReject('test-only with completed MySQL failure cannot certify',x=>{
    x.files=clone(mixedP1Lane.files);x.runs=clone(mixedP1Lane.runs);
    const mysql=x.runs.find(run=>run.name===MYSQL_INTEGRATION_CHECK);
    mysql.status='completed';mysql.conclusion='failure';return x;
  });
  const classifier=clone(docsLane);
  classifier.pr.user.login=INSTITUTIONAL_EXECUTOR;
  classifier.pr.title='fix(ci): classifier';
  classifier.pr.head.ref='work/solqaryn-ci-impact-p1-nested-v1';
  classifier.files=[docs[0],{filename:'docs/CI_IMPACT_ROUTING.md',status:'modified'},
    {filename:CI_IMPACT_SOURCE,status:'modified'}];
  classifier.reviews=[{state:'APPROVED',commit_id:h,user:{login:INDEPENDENT_REVIEWER}}];
  classifier.runs.push(makeRun(CI_IMPACT_CHECK,h),makeRun(MYSQL_INTEGRATION_CHECK,h,'success'));
  assertPass('reviewed classifier after MySQL SUCCESS',classifier);
  if(!certify(classifier).deferred.includes(optional))
    throw Error('CLASSIFIER_BROAD_OPTIONAL_NOT_DEFERRED');
  assertReject('classifier cannot defer active MySQL',x=>{
    x.pr=clone(classifier.pr);x.files=clone(classifier.files);x.reviews=clone(classifier.reviews);
    x.runs=clone(classifier.runs);const m=x.runs.find(z=>z.name===MYSQL_INTEGRATION_CHECK);
    m.status='in_progress';m.conclusion=null;return x;
  });
  assertReject('classifier without reviewer',x=>{
    x.pr=clone(classifier.pr);x.files=clone(classifier.files);x.runs=clone(classifier.runs);
    return x;});
  assertReject('classifier without trusted impact',x=>{
    x.pr=clone(classifier.pr);x.files=clone(classifier.files);x.reviews=clone(classifier.reviews);
    x.runs=clone(classifier.runs).filter(z=>z.name!==CI_IMPACT_CHECK);return x;});
  assertReject('classifier with completed optional failure',x=>{
    x.pr=clone(classifier.pr);x.files=clone(classifier.files);x.reviews=clone(classifier.reviews);
    x.runs=clone(classifier.runs);const m=x.runs.find(z=>z.name===MYSQL_INTEGRATION_CHECK);
    m.status='completed';m.conclusion='failure';return x;});
  const reviewedGov=clone(docsLane);
  reviewedGov.files=[docs[0],{filename:'AGENTS.md',status:'modified'},
   {filename:'scripts/automation-review-routing.mjs',status:'modified'}];
  reviewedGov.reviews=[{state:'APPROVED',commit_id:h,user:{login:'morales35alex'}}];
  assertPass('reviewed advisory governance with optional suite pending',reviewedGov);
  assertReject('governance without review',x=>{
   x.files=clone(reviewedGov.files);x.runs=clone(reviewedGov.runs);return x;});
  assertReject('governance with runtime file cannot defer',x=>{
   x.files=[...clone(reviewedGov.files),{filename:'backend/src/API/Startup.cs',status:'modified'}];
   x.runs=clone(reviewedGov.runs);x.reviews=clone(reviewedGov.reviews);return x;});
  assertReject('unknown script cannot defer', x => {
    x.files=[...docs,{filename:'scripts/ci/unknown.mjs',status:'added'}];
    x.runs.push(pending(optional)); return x;
  });
  assertReject('governance file cannot defer', x => {
    x.files=[...docs,{filename:'scripts/verify-automated-pr-certification.mjs',status:'modified'}];
    x.reviews=[{state:'APPROVED',commit_id:h,user:{login:'morales35alex'}}];
    x.runs.push(pending(optional)); return x;
  });
  assertReject('protected runtime contract document cannot defer', x => {
    x.files=[docs[0],{filename:'docs/PRODUCTION_READINESS_CONTRACT.md',status:'modified'}];
    x.runs.push(pending(optional)); return x;
  });
  assertReject('deleted evidence cannot defer', x => {
    x.files=[docs[0],{filename:'docs/CI_NOTE.md',status:'removed'}];
    x.runs.push(pending(optional)); return x;
  });
  assertReject('untrusted optional check cannot defer', x => {
    x.files=docs; const other=pending(optional); other.app.id=42;
    x.runs.push(other); return x;
  });
  assertReject('unknown optional check cannot defer', x => {
    x.files=docs; x.runs.push(pending('New mandatory audit with unknown name')); return x;
  });
  assertReject('known optional failure still blocks in fast lane', x => {
    x.files=docs; x.runs.push(makeRun(optional,h,'failure')); return x;
  });
  assertReject('required backend pending always blocks fast lane', x => {
    x.files=docs; x.runs[1]=pending(REQUIRED[1]);return x;
  });
  assertReject('directed scope pending always blocks fast lane', x => {
    x.files=docs; x.runs[2]=pending(DIRECTED_CHECK);return x;
  });
  const assertWait=(label,report,active,expected)=>{
    const actual=shouldWaitForActiveChecks(report,active,h);
    if(actual!==expected)throw Error('WAIT_CLASSIFICATION_WRONG:'+label+':'+actual);
    waitCases++;
  };
  const governanceBlocked = certify({...fixture, files:[...files,{filename:'.github/workflows/project-scope-lock.yml'}]});
  assertWait('governance blocked immediately',governanceBlocked,[pending(REQUIRED[0])],false);
  const missingDirected = certify({...fixture, runs:runs.slice(0,2)});
  assertWait('missing directed without active',missingDirected,[],false);
  assertWait('unrelated active cannot mask missing directed',missingDirected,[pending(optional)],false);
  assertWait('other SHA cannot mask missing directed',missingDirected,[{...pending(DIRECTED_CHECK),head_sha:'b'.repeat(40)}],false);
  const queuedRequired=certify({...docsLane,runs:[pending(REQUIRED[0]),runs[1],runs[2]]});
  assertWait('required check in progress remains waitable',queuedRequired,[pending(REQUIRED[0])],true);
  const queuedDirected=certify({...docsLane,runs:[runs[0],runs[1],pending(DIRECTED_CHECK)]});
  assertWait('directed check in progress remains waitable',queuedDirected,[pending(DIRECTED_CHECK)],true);
  assertWait('fast lane deferred optional does not delay',docsResult,[pending(optional)],false);
  const activeOptional = certify({...fixture,runs:[...runs,pending(optional)]});
  assertWait('risky scope still waits relevant optional',activeOptional,[pending(optional)],true);
  console.log('AUTO_CERT_SELF_TEST=PASS ('+positives+' positives; '+negatives+
    ' fail-closed negatives; '+waitCases+' wait conditions)');
}

function shouldWaitForActiveChecks(report, runs, expectedHead) {
  if (report.failures.includes('GOVERNANCE_REQUIRES_INDEPENDENT_FORMAL_REVIEW') || report.failures.includes('INSTITUTIONAL_REQUIRES_JAVIER_INDEPENDENT_REVIEW')) return false;
  const pendingOnly = report.failures.length > 0 && report.failures.every(f => f.startsWith('OTHER_CHECKS_PENDING:') || f.startsWith('REQUIRED_CHECK_NOT_GREEN:') || f.startsWith('SCOPE_DIRECTED_TEST_MISSING:'));
  if (!pendingOnly) return false;
  // Only actually relevant pending jobs may extend certification time.
  const names = new Set(report.awaiting || []);
  return runs.some(run => run.head_sha === expectedHead && names.has(run.name) &&
    run.name !== 'verify-solqaryn-project-scope' && run.status !== 'completed');
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
  // Bounded wait for required/relevant checks; never occupy a job for half an hour.
  // If still pending, fail closed so the native retry can run after CI completes.
  const deadline = Date.now() + 4 * 60 * 1000;
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
    if (!shouldWaitForActiveChecks(report, checkList.check_runs || [], expectedHead) || Date.now() >= deadline) break;
    console.log('AUTO_CERT_WAITING_FOR_ACTIVE_CHECKS=' + (checkList.check_runs || []).filter(run => run.head_sha === expectedHead && run.name !== 'verify-solqaryn-project-scope' && run.status !== 'completed').map(run => run.name).join(','));
    await sleep(20000);
  }
  console.log('AUTO_CERT_MODE=' + report.mode + ' SHA=' + report.sha + ' SCOPE=' + (report.scope || 'NONE'));
  console.log('AUTO_CERT_DEFERRED_UNRELATED_CHECKS=' + (report.deferred.length ? report.deferred.join(',') : 'NONE'));
  if (!report.pass) throw Error('AUTO_CERT=NO_GO ' + report.failures.join(' | '));
  console.log('AUTO_CERT=PASS (required and relevant checks exact-head, source scope, negative tests, trust boundary)');
} else throw Error('Provide --self-test or --pr');
