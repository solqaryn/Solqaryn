import { readFileSync } from 'node:fs';
import { optionalPendingDeferred } from '../verify-automated-pr-certification.mjs';

// Runs only from protected dev after a trusted Actions workflow finishes.
// This workflow can rerun a failed exact-HEAD Scope Lock job. It cannot approve,
// merge, edit, or certify a pull request; all original gates remain mandatory.
const REPO = 'solqaryn/Solqaryn';
const SCOPE_WORKFLOW = '.github/workflows/project-scope-lock.yml';
const SCOPE_CHECK = 'verify-solqaryn-project-scope';
const TRUSTED_ACTIONS_APP_ID = 15368;
const SHA = /^[a-f0-9]{40}$/i;
const SOURCE_WORKFLOWS = new Map([
  ['.github/workflows/dev-ci.yml', 'DEV - Compilación y pruebas'],
  ['.github/workflows/priority3-erp-security.yml', 'Priority 3 - ERP security and isolated restore'],
  ['.github/workflows/solqaryn-core-ci.yml', 'SOLQARYN - CI permanente del ERP'],
  ['.github/workflows/solqaryn-commercial-directed.yml', 'SOLQARYN - Prueba comercial dirigida'],
]);
const REQUIRED = ['SOLQARYN - Seguridad multiempresa', 'SOLQARYN - Build y pruebas backend'];
const DIRECTED = 'SOLQARYN - Directed commercial scope';
const WAIT_ONLY = /^(?:OTHER_CHECKS_PENDING|REQUIRED_CHECK_NOT_GREEN|SCOPE_DIRECTED_TEST_MISSING):/;

export function decision({repository, event, pr, liveDevSha, checks, scopeRun, job, log, files = []}) {
  if (repository !== REPO || event?.repository?.full_name !== REPO) return 'FOREIGN_REPOSITORY';
  const trigger = event?.workflow_run;
  if (event?.action !== 'completed' || !trigger || trigger.event !== 'pull_request' ||
      trigger.status !== 'completed' || trigger.conclusion !== 'success' ||
      trigger.head_repository?.full_name !== REPO ||
      SOURCE_WORKFLOWS.get(trigger.path) !== trigger.name) return 'UNTRUSTED_SOURCE_RUN';
  if (!SHA.test(trigger.head_sha || '') || !SHA.test(liveDevSha || '')) return 'INVALID_SHA';
  if (!pr || pr.state !== 'open' || pr.draft || pr.mergeable !== true ||
      pr.base?.ref !== 'dev' || pr.base?.sha !== liveDevSha ||
      pr.base?.repo?.full_name !== REPO || pr.head?.repo?.full_name !== REPO ||
      pr.head?.sha !== trigger.head_sha) return 'STALE_OR_UNTRUSTED_PR';
  if (!Array.isArray(trigger.pull_requests) || trigger.pull_requests.length === 0 ||
      trigger.pull_requests.length > 10 || !trigger.pull_requests.some(x =>
        x.number === pr.number && x.head?.sha === pr.head.sha)) return 'MISSING_EXACT_PR_LINK';
  if (![['jmejia','31'].join(''), 'morales35alex', 'solqaryn-platform'].includes(pr.user?.login))
    return 'UNAUTHORIZED_AUTHOR';
  if (!scopeRun || scopeRun.path !== SCOPE_WORKFLOW ||
      scopeRun.event !== 'pull_request' || scopeRun.head_sha !== pr.head.sha ||
      scopeRun.head_branch !== pr.head.ref ||
      scopeRun.head_repository?.full_name !== REPO ||
      scopeRun.status !== 'completed' || scopeRun.conclusion !== 'failure' ||
      !Number.isInteger(scopeRun.run_attempt) ||
      scopeRun.run_attempt < 1 || scopeRun.run_attempt >= 3) return 'NO_RETRYABLE_SCOPE_RUN';
  if (!job || job.name !== SCOPE_CHECK || job.status !== 'completed' ||
      job.conclusion !== 'failure' || !Array.isArray(job.steps) ||
      !job.steps.some(s => s.name === 'Certificar automáticamente el PR desde código confiable del base' &&
        s.conclusion === 'failure')) return 'UNRELATED_FAILED_JOB';
  if (!Array.isArray(checks) || checks.length === 0 || checks.length >= 100 ||
      checks.some(x => x.head_sha !== pr.head.sha || x.app?.id !== TRUSTED_ACTIONS_APP_ID))
    return 'UNVERIFIED_CHECK_SET';
  const scoped = checks.filter(x => x.name === SCOPE_CHECK);
  if (scoped.length !== 1 || scoped[0].status !== 'completed' ||
      scoped[0].conclusion !== 'failure') return 'SCOPE_RUN_NOT_FAILED';
  const others = checks.filter(x => x.name !== SCOPE_CHECK);
  const current = new Map(others.map(x => [x.name, x]));
  // Do not block on KNOWN unrelated pending jobs after required exact-SHA CI.
  // Reuse the same conservative policy as the trusted Scope Lock certifier.
  const deferred = new Set(optionalPendingDeferred(files, current));
  if (current.size !== others.length || !others.length ||
      others.some(x => (x.status !== 'completed' && !deferred.has(x.name)) ||
        (x.status === 'completed' && !['success', 'skipped'].includes(x.conclusion))))
    return 'OTHER_CHECKS_NOT_ALL_GREEN';
  for (const required of REQUIRED) {
    const check = current.get(required);
    if (!check || check.conclusion !== 'success') return 'REQUIRED_CHECK_NOT_GREEN';
  }
  const match = pr.title?.match(/\b((?:P\d+|I8|G0)-\d{3}\.[A-Z])\b/i);
  if (match) {
    const scope = match[1].toLowerCase().replace(/\./g, '-');
    if (pr.head.ref !== 'work/solqaryn-' + scope ||
        current.get(DIRECTED)?.conclusion !== 'success') return 'DIRECTED_OR_SCOPE_MISSING';
  }
  // Never retry governance, data, test or authorization failures.
  const errors = [...String(log || '').matchAll(/Error: AUTO_CERT=NO_GO ([^\r\n]+)/g)];
  const last = errors.at(-1)?.[1]?.trim();
  const reasons = last?.split(/\s+\|\s+/) || [];
  if (!last || !reasons.length || reasons.some(x => !WAIT_ONLY.test(x)))
    return 'NOT_A_CI_TIMING_FAILURE';
  return 'RETRY';
}

function selfTest() {
  const h = 'a'.repeat(40);
  const base = {
    repository: REPO,
    event: {action:'completed', repository:{full_name:REPO},
      workflow_run:{event:'pull_request',status:'completed',conclusion:'success',
        name:'DEV - Compilación y pruebas',path:'.github/workflows/dev-ci.yml',
        head_sha:h,head_repository:{full_name:REPO},
        pull_requests:[{number:17,head:{sha:h}}]}},
    pr:{number:17,title:'test(I8-001.C): regression',state:'open',draft:false,
      mergeable:true,user:{login:['jmejia','31'].join('')},
      base:{ref:'dev',sha:h,repo:{full_name:REPO}},
      head:{ref:'work/solqaryn-i8-001-c',sha:h,repo:{full_name:REPO}}},
    liveDevSha:h,
    checks:[
      ...REQUIRED.map(name => ({name,head_sha:h,status:'completed',conclusion:'success',
        app:{id:TRUSTED_ACTIONS_APP_ID}})),
      {name:DIRECTED,head_sha:h,status:'completed',conclusion:'success',
        app:{id:TRUSTED_ACTIONS_APP_ID}},
      {name:SCOPE_CHECK,head_sha:h,status:'completed',conclusion:'failure',
        app:{id:TRUSTED_ACTIONS_APP_ID}}],
    scopeRun:{path:SCOPE_WORKFLOW,event:'pull_request',head_sha:h,
      head_branch:'work/solqaryn-i8-001-c',head_repository:{full_name:REPO},
      status:'completed',conclusion:'failure',run_attempt:1},
    job:{name:SCOPE_CHECK,status:'completed',conclusion:'failure',
      steps:[{name:'Certificar automáticamente el PR desde código confiable del base',
        conclusion:'failure'}]},
    log:'Error: AUTO_CERT=NO_GO OTHER_CHECKS_PENDING:Migraciones EF Oracle, variantes y cargas masivas en MySQL 8.4'
  };
  const clone = x => structuredClone(x);
  const check = (label, mutate, result) => {
    const x = clone(base); mutate(x);
    const got = decision(x);
    if (got !== result) throw Error('CI_COMPLETION_RETRY_SELF_TEST:' + label + ':' + got);
  };
  check('valid final green after timing error',()=>{},'RETRY');
  check('wrong repository',x=>{x.repository='other/repo'},'FOREIGN_REPOSITORY');
  check('event not completed',x=>{x.event.action='requested'},'UNTRUSTED_SOURCE_RUN');
  check('untrusted workflow',x=>{x.event.workflow_run.path='evil.yml'},'UNTRUSTED_SOURCE_RUN');
  check('failed source workflow',x=>{x.event.workflow_run.conclusion='failure'},'UNTRUSTED_SOURCE_RUN');
  check('moved HEAD',x=>{x.pr.head.sha='b'.repeat(40)},'STALE_OR_UNTRUSTED_PR');
  check('changed dev',x=>{x.liveDevSha='b'.repeat(40)},'STALE_OR_UNTRUSTED_PR');
  check('other PR event',x=>{x.event.workflow_run.pull_requests[0].number=18},'MISSING_EXACT_PR_LINK');
  check('scope run stale',x=>{x.scopeRun.head_sha='b'.repeat(40)},'NO_RETRYABLE_SCOPE_RUN');
  check('too many reruns',x=>{x.scopeRun.run_attempt=3},'NO_RETRYABLE_SCOPE_RUN');
  check('failure in preflight not certification',x=>{x.job.steps[0].name='Fail fast on immutable governance blockers'},'UNRELATED_FAILED_JOB');
  check('scope check now running',x=>{x.checks.at(-1).status='in_progress'},'SCOPE_RUN_NOT_FAILED');
  check('required check still pending',x=>{x.checks[0].status='in_progress'},'OTHER_CHECKS_NOT_ALL_GREEN');
  check('optional MySQL pending',x=>{x.checks.push({name:'Migraciones EF Oracle, variantes y cargas masivas en MySQL 8.4',
    head_sha:h,status:'in_progress',conclusion:null,app:{id:TRUSTED_ACTIONS_APP_ID}})},'OTHER_CHECKS_NOT_ALL_GREEN');
  // Same CI-impact policy as the trusted certifier; these cases MUST agree.
  const unrelated = 'Migraciones EF Oracle, variantes y cargas masivas en MySQL 8.4';
  const docs = [{filename:'CHANGELOG_AI.md',status:'modified'},
    {filename:'docs/AUTOMATION_STATUS.md',status:'modified'}];
  const withPending = x => {x.files=structuredClone(docs);
    x.checks.push({name:unrelated,head_sha:h,status:'in_progress',conclusion:null,
      app:{id:TRUSTED_ACTIONS_APP_ID}});};
  check('docs allow known unrelated pending after core green',withPending,'RETRY');
  check('runtime cannot defer unrelated pending',x=>{
    withPending(x);x.files.push({filename:'backend/src/API/Controllers/VentaController.cs',status:'modified'});
  },'OTHER_CHECKS_NOT_ALL_GREEN');
  check('unknown pending still blocks',x=>{
    withPending(x);x.checks.at(-1).name='Unknown external CI';
  },'OTHER_CHECKS_NOT_ALL_GREEN');
  check('untrusted check app denied',x=>{
    withPending(x);x.checks.at(-1).app.id=42;
  },'UNVERIFIED_CHECK_SET');
  check('completed failure never deferred',x=>{
    withPending(x);x.checks.at(-1).status='completed';x.checks.at(-1).conclusion='failure';
  },'OTHER_CHECKS_NOT_ALL_GREEN');

  check('completed failure',x=>{x.checks[1].conclusion='failure'},'OTHER_CHECKS_NOT_ALL_GREEN');
  check('forged check app',x=>{x.checks[0].app.id=42},'UNVERIFIED_CHECK_SET');
  check('non-scope branch',x=>{x.pr.head.ref='unknown';x.scopeRun.head_branch='unknown'},'DIRECTED_OR_SCOPE_MISSING');
  check('directed skipped',x=>{x.checks[2].conclusion='skipped'},'DIRECTED_OR_SCOPE_MISSING');
  check('governance failure not retried',x=>{x.log='Error: AUTO_CERT=NO_GO GOVERNANCE_REQUIRES_INDEPENDENT_FORMAL_REVIEW'},'NOT_A_CI_TIMING_FAILURE');
  check('secret scan failure not retried',x=>{x.log='Error: AUTO_CERT=NO_GO CHECK_FAILED:Authorization, files, secrets and tenant gate'},'NOT_A_CI_TIMING_FAILURE');
  console.log('SCOPE_CI_COMPLETION_RETRY_SELF_TEST=PASS (20 cases)');
}

async function live() {
  const event = JSON.parse(readFileSync(process.env.GITHUB_EVENT_PATH,'utf8'));
  const repository = process.env.GITHUB_REPOSITORY;
  const token = process.env.GITHUB_TOKEN;
  if (repository !== REPO || event.repository?.full_name !== REPO ||
      event.action !== 'completed' ||
      event.workflow_run?.event !== 'pull_request' ||
      event.workflow_run?.conclusion !== 'success' ||
      SOURCE_WORKFLOWS.get(event.workflow_run?.path) !== event.workflow_run?.name) {
    console.log('SCOPE_CI_COMPLETION_RETRY=SKIP_UNTRUSTED_EVENT'); return;
  }
  if (!token) throw Error('CI_RETRY_TOKEN_UNAVAILABLE');
  const api = async (path, method='GET', asText=false) => {
    const response = await fetch((process.env.GITHUB_API_URL || 'https://api.github.com') +
      '/repos/' + REPO + path, {method,
        headers:{Authorization:'Bearer ' + token,Accept:'application/vnd.github+json',
          'X-GitHub-Api-Version':'2022-11-28'}, signal:AbortSignal.timeout(20000)});
    if (!response.ok) throw Error('CI_RETRY_HTTP_' + response.status + ':' + path.split('?')[0]);
    return method === 'POST' ? response.status : asText ? response.text() : response.json();
  };
  const source = event.workflow_run;
  if (!SHA.test(source.head_sha || '') ||
      !Array.isArray(source.pull_requests) || source.pull_requests.length > 10 ||
      !source.pull_requests.length) {
    console.log('SCOPE_CI_COMPLETION_RETRY=SKIP_MISSING_HEAD_OR_PR'); return;
  }
  const dev = await api('/branches/dev');
  const liveDevSha = dev.commit?.sha;
  for (const link of source.pull_requests) {
    if (!Number.isInteger(link.number)) continue;
    const pr = await api('/pulls/' + link.number);
    if (pr.head?.sha !== source.head_sha) continue;
    const [checkList,runs,files] = await Promise.all([
      api('/commits/' + source.head_sha + '/check-runs?per_page=100'),
      api('/actions/runs?head_sha=' + source.head_sha + '&per_page=100'),
      api('/pulls/' + link.number + '/files?per_page=100')]);
    if (checkList.total_count >= 100 || runs.total_count >= 100 ||
        !Array.isArray(files) || files.length >= 100) {
      console.log('SCOPE_CI_COMPLETION_RETRY=SKIP_UNBOUNDED'); continue;
    }
    const candidates = (runs.workflow_runs || []).filter(x => x.path === SCOPE_WORKFLOW &&
      x.event === 'pull_request' && x.head_sha === source.head_sha &&
      x.head_branch === pr.head.ref && x.head_repository?.full_name === REPO)
      .sort((a,b) => b.id - a.id);
    const scopeRun = candidates[0];
    if (!scopeRun || scopeRun.conclusion !== 'failure' || scopeRun.run_attempt >= 3) {
      console.log('SCOPE_CI_COMPLETION_RETRY=SKIP_SCOPE_NOT_FAILED'); continue;
    }
    const jobs = await api('/actions/runs/' + scopeRun.id + '/jobs?per_page=100');
    if (jobs.total_count >= 100) {
      console.log('SCOPE_CI_COMPLETION_RETRY=SKIP_UNBOUNDED_JOBS'); continue;
    }
    const job = (jobs.jobs || []).find(x => x.name === SCOPE_CHECK);
    if (!job?.id) continue;
    const log = await api('/actions/jobs/' + job.id + '/logs','GET',true);
    if (log.length > 1000000) {
      console.log('SCOPE_CI_COMPLETION_RETRY=SKIP_OVERSIZE_LOG'); continue;
    }
    const evidence = {repository,event,pr,liveDevSha,checks:checkList.check_runs,
      scopeRun,job,log,files};
    const result = decision(evidence);
    if (result !== 'RETRY') {
      console.log('SCOPE_CI_COMPLETION_RETRY=SKIP_' + result); continue;
    }
    // A live second read prevents rerunning a moved PR or a check already recovered.
    const [current,checksNow] = await Promise.all([
      api('/pulls/' + link.number),
      api('/commits/' + source.head_sha + '/check-runs?per_page=100')]);
    if (decision({...evidence,pr:current,checks:checksNow.check_runs}) !== 'RETRY') {
      console.log('SCOPE_CI_COMPLETION_RETRY=SKIP_CHANGED_EVIDENCE'); continue;
    }
    const status = await api('/actions/jobs/' + job.id + '/rerun','POST');
    if (status !== 201) throw Error('CI_RETRY_UNEXPECTED_STATUS');
    console.log('SCOPE_CI_COMPLETION_RETRY=STARTED PR=' + link.number +
      ' SHA=' + source.head_sha + ' JOB=' + job.id);
  }
}
const args = new Set(process.argv.slice(2));
if (args.has('--self-test')) selfTest();
else if (args.has('--ci-event')) await live();
else throw Error('Specify --self-test or --ci-event');
