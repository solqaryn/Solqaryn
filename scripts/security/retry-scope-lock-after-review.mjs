import { readFileSync } from 'node:fs';

// Execute from trusted DEV, not pull-request code. Never approve or merge PRs.
const REPO = 'solqaryn/Solqaryn';
const WORKFLOW = '.github/workflows/project-scope-lock.yml';
const REVIEWERS = new Set([['jmejia','31'].join(''), 'morales35alex']);
const SHA = /^[a-f0-9]{40}$/i;
const approvalStepFailed = s => s?.conclusion === 'failure' &&
  (s.name === 'Fail fast on immutable governance blockers' ||
   s.name === 'Certificar automáticamente el PR desde código confiable del base');

export function decision({event,pr,reviews,commits,run,job,actor,repository}) {
  if (repository !== REPO || event?.repository?.full_name !== REPO) return 'WRONG_REPOSITORY';
  if (event?.action !== 'submitted' || String(event.review?.state).toUpperCase() !== 'APPROVED') return 'NO_APPROVAL';
  const reviewer = event.review?.user?.login;
  if (!REVIEWERS.has(reviewer) || actor !== reviewer || event.sender?.login !== reviewer) return 'UNTRUSTED_REVIEWER';
  if (!pr || pr.state !== 'open' || pr.draft || pr.base?.ref !== 'dev' ||
      pr.base?.repo?.full_name !== REPO || pr.head?.repo?.full_name !== REPO) return 'UNTRUSTED_PR';
  const head = pr.head?.sha;
  if (!SHA.test(head || '') || event.pull_request?.number !== pr.number ||
      event.pull_request?.head?.sha !== head || event.review?.commit_id !== head) return 'STALE_HEAD';
  if (![['jmejia','31'].join(''),'morales35alex','solqaryn-platform'].includes(pr.user?.login)) return 'UNAUTHORIZED_AUTHOR';
  if (pr.user.login === 'solqaryn-platform' && reviewer !== ['jmejia','31'].join('')) return 'JAVIER_REQUIRED';
  if (!Array.isArray(reviews) || !reviews.some(v => v.id === event.review.id &&
    v.state === 'APPROVED' && v.commit_id === head && v.user?.login === reviewer)) return 'REVIEW_NOT_CURRENT';
  if (!Array.isArray(commits) || !commits.length || commits.length >= 100) return 'NO_COMMIT_PROVENANCE';
  const contributors = new Set([pr.user?.login]);
  for (const c of commits) for (const login of [c.author?.login,c.committer?.login]) if(login) contributors.add(login);
  if (contributors.has(reviewer)) return 'REVIEWER_CONTRIBUTOR';
  if (!run || run.path !== WORKFLOW || run.event !== 'pull_request' ||
      run.head_sha !== head || run.head_branch !== pr.head.ref ||
      run.head_repository?.full_name !== REPO || run.status !== 'completed' ||
      run.conclusion !== 'failure' || !Number.isInteger(run.run_attempt) ||
      run.run_attempt < 1 || run.run_attempt > 3) return 'NO_FAILED_EXACT_HEAD_RUN';
  if (!job || job.name !== 'verify-solqaryn-project-scope' || job.status !== 'completed' ||
      job.conclusion !== 'failure' || !Array.isArray(job.steps) ||
      !job.steps.some(approvalStepFailed)) return 'NOT_REVIEW_BLOCKER';
  return 'RETRY';
}

function selfTest(){
  const h='a'.repeat(40),b='work/solqaryn-ci-review-test';
  const base={repository:REPO,actor:['jmejia','31'].join(''),
    event:{action:'submitted',repository:{full_name:REPO},sender:{login:['jmejia','31'].join('')},
      pull_request:{number:17,head:{sha:h}},
      review:{id:8,state:'approved',user:{login:['jmejia','31'].join('')},commit_id:h}},
    pr:{number:17,state:'open',draft:false,user:{login:'solqaryn-platform'},
      head:{ref:b,sha:h,repo:{full_name:REPO}},base:{ref:'dev',repo:{full_name:REPO}}},
    reviews:[{id:8,state:'APPROVED',commit_id:h,user:{login:['jmejia','31'].join('')}}],
    commits:[{author:{login:'solqaryn-platform'},committer:{login:'solqaryn-platform'}}],
    run:{path:WORKFLOW,event:'pull_request',head_sha:h,head_branch:b,
      head_repository:{full_name:REPO},status:'completed',conclusion:'failure',run_attempt:1},
    job:{name:'verify-solqaryn-project-scope',status:'completed',conclusion:'failure',
      steps:[{name:'Fail fast on immutable governance blockers',conclusion:'failure'}]}};
  let count=0;
  const check=(name,mutate,expected)=>{const obj=structuredClone(base);mutate(obj);
    const actual=decision(obj);if(actual!==expected)throw Error('RETRY_NEGATIVE_FAILED:'+name+':'+actual);count++;};
  check('positive',()=>{},'RETRY');
  check('not submitted',x=>{x.event.action='edited'},'NO_APPROVAL');
  check('unknown repository',x=>{x.repository='foreign/other'},'WRONG_REPOSITORY');
  check('untrusted reviewer',x=>{x.actor='stranger'},'UNTRUSTED_REVIEWER');
  check('stale head',x=>{x.event.review.commit_id='b'.repeat(40)},'STALE_HEAD');
  check('wrong base',x=>{x.pr.base.ref='main'},'UNTRUSTED_PR');
  check('fork',x=>{x.pr.head.repo.full_name='foreign/repo'},'UNTRUSTED_PR');
  check('dismissed',x=>{x.reviews[0].state='DISMISSED'},'REVIEW_NOT_CURRENT');
  check('contributor reviewer',x=>{x.commits[0].author.login=['jmejia','31'].join('')},'REVIEWER_CONTRIBUTOR');
  check('institutional Alex',x=>{x.actor='morales35alex';x.event.sender.login='morales35alex';
    x.event.review.user.login='morales35alex';x.reviews[0].user.login='morales35alex'},'JAVIER_REQUIRED');
  check('run stale',x=>{x.run.head_sha='b'.repeat(40)},'NO_FAILED_EXACT_HEAD_RUN');
  check('run success',x=>{x.run.conclusion='success'},'NO_FAILED_EXACT_HEAD_RUN');
  check('too many retries',x=>{x.run.run_attempt=4},'NO_FAILED_EXACT_HEAD_RUN');
  check('unrelated failure',x=>{x.job.steps[0].name='Verify SOLQARYN project scope'},'NOT_REVIEW_BLOCKER');
  check('job cancelled',x=>{x.job.conclusion='cancelled'},'NOT_REVIEW_BLOCKER');
  check('no commits',x=>{x.commits=[]},'NO_COMMIT_PROVENANCE');
  console.log('SCOPE_REVIEW_RETRY_SELF_TEST=PASS ('+count+' scenarios)');
}

async function live(){
  const event=JSON.parse(readFileSync(process.env.GITHUB_EVENT_PATH,'utf8'));
  const repository=process.env.GITHUB_REPOSITORY,actor=process.env.GITHUB_ACTOR;
  if(repository!==REPO || event.repository?.full_name!==REPO || event.action!=='submitted' ||
     String(event.review?.state).toUpperCase()!=='APPROVED' || !REVIEWERS.has(actor)){
    console.log('SCOPE_REVIEW_RETRY=SKIP_UNTRUSTED_EVENT');return;}
  const token=process.env.GITHUB_TOKEN,number=event.pull_request?.number;
  if(!token || !Number.isInteger(number))throw Error('SCOPE_REVIEW_RETRY_CONTEXT_MISSING');
  const api=async (path,method='GET')=>{
    const response=await fetch((process.env.GITHUB_API_URL||'https://api.github.com')+'/repos/'+REPO+path,{
      method,headers:{Authorization:'Bearer '+token,Accept:'application/vnd.github+json',
      'X-GitHub-Api-Version':'2022-11-28'},signal:AbortSignal.timeout(20000)});
    if(!response.ok && method==='POST' && response.status===403 && /^\/actions\/jobs\/\d+\/rerun$/.test(path)) {
      console.log('SCOPE_REVIEW_RETRY=MANUAL_RETRY_REQUIRED_403; independent approval remains valid; mandatory Scope Lock still gates merge');
      return 403;
    }
    if(!response.ok)throw Error('SCOPE_REVIEW_RETRY_HTTP_'+response.status+':'+path.split('?')[0]);
    return method==='POST'?response.status:response.json();};
  const pr=await api('/pulls/'+number),head=pr.head?.sha;
  if(!SHA.test(head||'') || event.pull_request?.head?.sha!==head){
    console.log('SCOPE_REVIEW_RETRY=SKIP_MOVED_HEAD');return;}
  const [reviews,commits,runs]=await Promise.all([
    api('/pulls/'+number+'/reviews?per_page=100'),
    api('/pulls/'+number+'/commits?per_page=100'),
    api('/actions/runs?head_sha='+head+'&per_page=100')]);
  if(reviews.length>=100||commits.length>=100||runs.total_count>100)
    throw Error('SCOPE_REVIEW_RETRY_UNBOUNDED');
  const candidates=(runs.workflow_runs||[]).filter(x=>x.path===WORKFLOW&&
    x.event==='pull_request'&&x.head_sha===head&&x.head_branch===pr.head.ref&&
    x.head_repository?.full_name===REPO).sort((a,b)=>b.id-a.id);
  const run=candidates[0];
  if(!run||run.status!=='completed'||run.conclusion!=='failure'||run.run_attempt>3){
    console.log('SCOPE_REVIEW_RETRY=NO_FAILED_GATE');return;}
  const jobs=await api('/actions/runs/'+run.id+'/jobs?per_page=100');
  if(jobs.total_count>100)throw Error('SCOPE_REVIEW_RETRY_JOBS_UNBOUNDED');
  const job=(jobs.jobs||[]).find(x=>x.name==='verify-solqaryn-project-scope');
  const result=decision({event,pr,reviews,commits,run,job,actor,repository});
  if(result!=='RETRY'){console.log('SCOPE_REVIEW_RETRY=SKIP_'+result);return;}
  const recheck=await api('/pulls/'+number);
  if(recheck.state!=='open'||recheck.head?.sha!==head){
    console.log('SCOPE_REVIEW_RETRY=SKIP_CHANGED_HEAD');return;}
  const status=await api('/actions/jobs/'+job.id+'/rerun','POST');
  if(status===403) { console.log('SCOPE_REVIEW_RETRY=NOT_STARTED; operator must selectively re-run failed Scope Lock after dependent checks pass'); return; }
  if(status!==201)throw Error('SCOPE_REVIEW_RETRY_UNEXPECTED_STATUS');
  console.log('SCOPE_REVIEW_RETRY=STARTED JOB='+job.id+' PR='+number+' SHA='+head);
}
const args=new Set(process.argv.slice(2));
if(args.has('--self-test'))selfTest();
else if(args.has('--review-event'))await live();
else throw Error('Specify --self-test or --review-event');
