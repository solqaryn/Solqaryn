import { readFileSync } from 'node:fs';
import { join } from 'node:path';

const read = path => readFileSync(join(process.cwd(), path), 'utf8');
const authority = read('docs/AUTOMATION_AUTHORITY.md');
const status = read('docs/AUTOMATION_STATUS.md');
const agents = read('AGENTS.md');
const scope = read('docs/PROJECT_SCOPE_LOCK.md');
const contract = read('docs/AUTOMATION_3X3_OPERATING_CONTRACT.md');

const ownerByRole = Object.freeze({
  PRIMARY_01: 'JAVIER_CHATGPT',
  PRIMARY_DEV_02: 'JAVIER_CHATGPT',
  PRIMARY_DEV_03: 'JAVIER_CHATGPT',
  SECONDARY_02: 'ALEX_CHATGPT',
  SECONDARY_REVIEW_02: 'ALEX_CHATGPT',
  SECONDARY_REVIEW_03: 'ALEX_CHATGPT',
});
const expected = {
  PROJECT_SCOPE_LOCK: 'STRICT',
  PROJECT_ID: 'SOLQARYN',
  REPOSITORY: 'solqaryn/Solqaryn',
  BRANCH: 'dev',
  CONTRACT_VERSION: '2.0',
  PILOT_TASK_LIMIT: '6',
  PILOT_PRIMARY_TASKS: '3',
  PILOT_SECONDARY_TASKS: '3',
  AUTOMATION_RUNTIME: 'ACTIVE_DEV',
  SCHEDULED_CHATGPT_TASKS_AUTHORIZED: '1',
  VAEP_DISPATCH_AUTHORIZED: 'FALSE',
  JULES_WORKERS_AUTHORIZED: '0',
};
const approvalPolicy = {
  APPROVAL_POOL_MEMBERS: 'JAVIER,ALEX,SOLQARYN_PLATFORM',
  APPROVAL_SELECTION: 'ANY_AVAILABLE_ELIGIBLE',
  APPROVAL_NO_SELF_REVIEW: 'TRUE',
  APPROVAL_PROVIDER_GATES_MANDATORY: 'TRUE',
  APPROVAL_HUMAN_WHEN_REQUIRED: 'TRUE',
  APPROVAL_AVAILABILITY_MUST_BE_VERIFIED: 'TRUE',
};
function key(source, name) {
  const found = [...source.matchAll(new RegExp('^' + name + '=([^\\r\\n]+)$', 'gm'))];
  return found.length === 1 ? found[0][1] : null;
}
export function validateDocuments(src) {
  const errors = [];
  for (const [name,value] of Object.entries(expected)) {
    if (name !== 'CONTRACT_VERSION' && name !== 'SCHEDULED_CHATGPT_TASKS_AUTHORIZED') {
      if (key(src.authority,name) !== value) errors.push('authority ' + name);
      if (name !== 'PROJECT_ID' && name !== 'REPOSITORY' && name !== 'BRANCH' &&
          key(src.status,name) !== value) errors.push('status ' + name);
    }
  }
  if (key(src.authority,'CONTRACT_VERSION') !== '2.0') errors.push('contract version');
  const countA = Number(key(src.authority,'SCHEDULED_CHATGPT_TASKS_AUTHORIZED'));
  const countS = Number(key(src.status,'SCHEDULED_CHATGPT_TASKS_AUTHORIZED'));
  if (!Number.isInteger(countA) || countA < 1 || countA > 6 || countA !== countS) errors.push('authorized counter');
  if (key(src.scope,'PILOT_TASK_LIMIT') !== '6') errors.push('scope topology');
  if (key(src.scope,'PROJECT_SCOPE_LOCK') !== 'STRICT') errors.push('strict lock');
  if (!src.agents.includes('TASK_DESIGN=AUTONOMOUS_3_JAVIER_3_ALEX')) errors.push('agents topology');
  for (const [name,value] of Object.entries(approvalPolicy))
    if (key(src.authority,name) !== value) errors.push('approval ' + name);
  for (const phrase of ['DESIGN_TASK_LIMIT=6','DESIGN_JAVIER_TASKS=3',
    'DESIGN_ALEX_TASKS=3','MAX_OPEN_WORK_BRANCHES=2',
    'REVIEW_INDEPENDENCE_REQUIRED=TRUE','America/Tegucigalpa',
    'PENDIENTE -> EN_PROGRESO','EN_PROGRESO -> VALIDANDO',
    'VALIDANDO -> LISTO','VALIDANDO -> VALIDANDO']) {
    if (!src.contract.includes(phrase)) errors.push('contract ' + phrase);
  }
  if (!src.authority.includes('docs/AUTOMATION_3X3_OPERATING_CONTRACT.md') ||
      !src.status.includes('docs/AUTOMATION_3X3_OPERATING_CONTRACT.md'))
    errors.push('contract routing');
  return errors;
}
const isUrl = ref => typeof ref === 'string' && /^https:\/\/github\.com\/solqaryn\/Solqaryn\/pull\/[1-9]\d*$/.test(ref);
const validScope = id => typeof id === 'string' && /^P\d+-\d+\.[A-Z]$/.test(id);
export function dependenciesSatisfied(task, stateById) {
  return Array.isArray(task.dependencies) &&
    task.dependencies.every(id => stateById[id] === 'LISTO');
}
export function selectWork(tasks, stateById, account, activeReviewIds = new Set()) {
  const assigned = tasks.find(t => t.state === 'EN_PROGRESO' &&
    t.ownerAccount === account && t.resumeAuthorized === true &&
    t.exclusiveScopeVerified === true);
  if (assigned) return {action:'RESUME',task:assigned};
  const review = tasks.find(t => t.state === 'VALIDANDO' && t.authorAccount !== account &&
    !activeReviewIds.has(t.id) && t.prUrl && t.reviewerAllowed !== false);
  if (review) return {action:'REVIEW',task:review};
  const eligible = tasks.filter(t => t.state === 'PENDIENTE' &&
    validScope(t.id) && dependenciesSatisfied(t,stateById) &&
    !t.existingBranch && !t.conflictingClaim && t.permissionsVerified);
  eligible.sort((x,y)=>Number(x.priority.slice(1))-Number(y.priority.slice(1)) ||
    x.planOrder-y.planOrder);
  return eligible.length ? {action:'CLAIM',task:eligible[0]} : {action:'NO_ELIGIBLE'};
}
// Historical read-only evidence is optional metadata and NEVER a gate for DEV.
export function verifiedScheduledReadOnlyProof(proof,slot) {
  return Boolean(slot && proof && proof.taskId === slot.taskId &&
    proof.account === slot.account && proof.completed === true &&
    proof.ownerReadbackVerified === true && proof.independentReviewerVerified === true);
}
export function validateProposedRun(p) {
  const errors=[];
  if(p.repo!=='solqaryn/Solqaryn'||p.branch!=='dev')errors.push('identity');
  if(p.environment!=='dev')errors.push('environment');
  if(p.pilotMax!==6||p.primaryMax!==3||p.secondaryMax!==3)errors.push('cap');
  if(p.ownerChanged||p.sharedCredentials||p.legacyReactivate)errors.push('scope');
  if(!Array.isArray(p.slots)||p.slots.length>6)errors.push('slot_count');
  const seenId=new Set(),seenRole=new Set(),perAccount={JAVIER_CHATGPT:0,ALEX_CHATGPT:0};
  for(const slot of p.slots||[]) {
    if(ownerByRole[slot.role]!==slot.account)errors.push('slot_account');
    if(seenRole.has(slot.role)||seenId.has(slot.taskId))errors.push('slot_duplicate');
    if(!slot.taskId||slot.taskId==='NO_VERIFICADO'||!slot.permissionsVerified||!slot.schedulerVerified)
      errors.push('slot_unverified');
    perAccount[slot.account]=(perAccount[slot.account]||0)+1;
    seenRole.add(slot.role);seenId.add(slot.taskId);
  }
  if(perAccount.JAVIER_CHATGPT>3||perAccount.ALEX_CHATGPT>3)errors.push('account_over_cap');
  if(p.operation==='NONE')return errors;
  if(!p.authorizationVerified||p.mode!=='ACTIVE_DEV')errors.push('authorization');
  if(ownerByRole[p.actorRole]!==p.actorAccount ||
     !(p.slots||[]).some(s=>s.role===p.actorRole&&s.account===p.actorAccount))
    errors.push('actor_auth');
  if(!p.permissionVerified||!p.exactHeadVerified)errors.push('permission_or_head');
  if(p.operation==='WRITE'){
    if(!validScope(p.scopeId)||!p.ownerAuthorized||!p.writePermissionVerified||
      !p.exclusiveScopeVerified||!p.createdExclusively||!p.branchReadbackVerified||
      p.conflictingClaim||!p.idempotencyKey||!p.branchName?.startsWith('work/solqaryn-'))
      errors.push('unsafe_write');
    if(p.openWorkBranches>2)errors.push('branch_cap');
    if(p.activeEditorAccount&&p.activeEditorAccount!==p.actorAccount)
      errors.push('concurrent_editor');
  }else if(p.operation==='REVIEW'){
    if(!isUrl(p.reviewedPrRef)||p.authorAccount===p.actorAccount||
      !p.independentReviewerVerified||p.activeReviewerConflict)
      errors.push('review_integrity');
  }else if(p.operation==='CLOSE'){
    if(!isUrl(p.reviewedPrRef)||!p.mergedIntoDev||!p.mergeSha||
      !p.requiredChecksPassed||!p.reviewIndependenceSatisfied||
      !p.allContributionsReviewed||!p.sheetReadbackVerified||
      !p.ledgerReadbackVerified||!p.dependentsRecalculated)
      errors.push('close_integrity');
    if(p.actorAccount===p.authorAccount && !p.separateApprovalAccount)
      errors.push('self_signoff');
  }else if(p.operation!=='READ'){
    errors.push('unknown_operation');
  }
  return errors;
}
// No automatic approval; this only validates provider-side facts provided to caller.
export function validateApprovalDecision(p) {
  const errors=[];
  if(!['JAVIER','ALEX','SOLQARYN_PLATFORM'].includes(p.approverPrincipal))
    errors.push('unknown');
  if(!p.identityVerified||!p.permissionVerified||!p.providerRulesSatisfied||
     !p.nativeApprovalVerified||!p.exactHeadVerified||!p.readbackVerified)
    errors.push('provider_gate');
  if(!p.independenceVerified||p.approverAccount===p.authorAccount||
     !p.allContributionsReviewed)errors.push('self_or_unreviewed');
  if(p.environment!=='dev'||p.impersonated||p.accountBorrowed)errors.push('scope');
  return errors;
}
const documents={authority,status,agents,scope,contract};
const failures=validateDocuments(documents);
if(failures.length){
  console.error('SOLQARYN 3X3 AUTHORITY NO_GO: '+failures.join(' | '));
  process.exitCode=1;
}else if(process.argv.includes('--self-test')){
  const slots=[
    {role:'PRIMARY_01',account:'JAVIER_CHATGPT',taskId:'PRIMARY-REAL',permissionsVerified:true,schedulerVerified:true},
    {role:'SECONDARY_02',account:'ALEX_CHATGPT',taskId:'SECONDARY-REAL',permissionsVerified:true,schedulerVerified:true},
  ];
  const base={repo:'solqaryn/Solqaryn',branch:'dev',environment:'dev',
    pilotMax:6,primaryMax:3,secondaryMax:3,slots,
    mode:'ACTIVE_DEV',authorizationVerified:true,
    actorRole:'PRIMARY_01',actorAccount:'JAVIER_CHATGPT',
    permissionVerified:true,exactHeadVerified:true,
    operation:'WRITE',ownerAuthorized:true,writePermissionVerified:true,
    scopeId:'P0-012.A',exclusiveScopeVerified:true,createdExclusively:true,
    branchReadbackVerified:true,branchName:'work/solqaryn-p0-012-a',
    openWorkBranches:2,idempotencyKey:'P0-012.A:base-sha',conflictingClaim:false,
    runId:'NO_VERIFICADO'};
  const clone=x=>structuredClone(x);
  if(validateProposedRun(base).length)throw Error('DEV without native RUN_ID must be allowed');
  const variants=[
    ['wrong repo',p=>p.repo='external/Other'],
    ['wrong branch',p=>p.branch='qa'],
    ['over-cap',p=>p.slots=[...p.slots,...p.slots,...p.slots,...p.slots]],
    ['same role',p=>p.slots[1].role='PRIMARY_01'],
    ['duplicate id',p=>p.slots[1].taskId='PRIMARY-REAL'],
    ['wrong account',p=>p.slots[0].account='ALEX_CHATGPT'],
    ['missing provider right',p=>p.writePermissionVerified=false],
    ['scope collision',p=>p.conflictingClaim=true],
    ['claim not atomic',p=>p.createdExclusively=false],
    ['branch not verified',p=>p.branchReadbackVerified=false],
    ['wrong scope',p=>p.scopeId=''],
    ['stale head',p=>p.exactHeadVerified=false],
    ['too many branches',p=>p.openWorkBranches=3],
    ['forbidden prod',p=>p.environment='prod'],
    ['shared credentials',p=>p.sharedCredentials=true],
    ['unknown actor',p=>p.actorRole='VAEP_SUPERVISOR'],
    ['legacy dispatcher',p=>p.legacyReactivate=true],
  ];
  for(const [name,mutate] of variants){
    const t=clone(base);mutate(t);
    if(!validateProposedRun(t).length)throw Error('negative test bypass: '+name);
  }
  const alexWrite=clone(base);
  alexWrite.actorRole='SECONDARY_02';alexWrite.actorAccount='ALEX_CHATGPT';
  if(validateProposedRun(alexWrite).length)throw Error('Equal write capability denied to Alex');
  const review={...base,operation:'REVIEW',actorRole:'SECONDARY_02',
    actorAccount:'ALEX_CHATGPT',authorAccount:'JAVIER_CHATGPT',
    reviewedPrRef:'https://github.com/solqaryn/Solqaryn/pull/3734',
    independentReviewerVerified:true};
  if(validateProposedRun(review).length)throw Error('Alex review denied');
  const javierReview={...review,actorRole:'PRIMARY_01',
    actorAccount:'JAVIER_CHATGPT',authorAccount:'ALEX_CHATGPT'};
  if(validateProposedRun(javierReview).length)
    throw Error('Javier cannot review Alex PR: asymmetric authorization');
  if(!validateProposedRun({...javierReview,authorAccount:'JAVIER_CHATGPT'}).length)
    throw Error('Javier self-review was accepted');
  if(!validateProposedRun({...review,authorAccount:'ALEX_CHATGPT'}).length)
    throw Error('self review was accepted');
  const close={...review,operation:'CLOSE',mergedIntoDev:true,mergeSha:'merge-sha',
    requiredChecksPassed:true,reviewIndependenceSatisfied:true,
    allContributionsReviewed:true,sheetReadbackVerified:true,
    ledgerReadbackVerified:true,dependentsRecalculated:true};
  if(validateProposedRun(close).length)throw Error('Verified merge should close');
  if(!validateProposedRun({...close,allContributionsReviewed:false}).length)
    throw Error('Unreviewed corrective commit bypass');
  if(!validateProposedRun({...close,sheetReadbackVerified:false}).length)
    throw Error('Sheet failure bypass');
  const graph={'P0-012.A':'VALIDANDO'};
  if(dependenciesSatisfied({dependencies:['P0-012.A']},graph))
    throw Error('VALIDANDO incorrectly released dependency');
  if(!dependenciesSatisfied({dependencies:['P0-012.A']},{'P0-012.A':'LISTO'}))
    throw Error('LISTO failed to release dependency');
  const options=[{id:'P0-012.B',state:'PENDIENTE',dependencies:['P0-012.A'],
    priority:'P0',planOrder:24,permissionsVerified:true},
    {id:'P1-100.A',state:'VALIDANDO',authorAccount:'JAVIER_CHATGPT',
      prUrl:'https://github.com/solqaryn/Solqaryn/pull/99'}];
  if(selectWork(options,{'P0-012.A':'LISTO'},'ALEX_CHATGPT').action!=='REVIEW')
    throw Error('Review-first selection regression');
  const alexPR={id:'P0-013.A',state:'VALIDANDO',
    authorAccount:'ALEX_CHATGPT',
    prUrl:'https://github.com/solqaryn/Solqaryn/pull/3736'};
  if(selectWork([alexPR,options[0]],{'P0-012.A':'LISTO'},'JAVIER_CHATGPT').action!=='REVIEW')
    throw Error('Javier must prioritize review of Alex over eligible pending');
  if(selectWork([options[0]],{'P0-012.A':'LISTO'},'ALEX_CHATGPT').action!=='CLAIM')
    throw Error('Alex must be able to develop eligible PENDIENTE');
  if(selectWork([options[0]],{'P0-012.A':'LISTO'},'JAVIER_CHATGPT').action!=='CLAIM')
    throw Error('Javier must be able to develop eligible PENDIENTE');
  if(selectWork([alexPR],{},'ALEX_CHATGPT').action!=='NO_ELIGIBLE')
    throw Error('Alex must not review own VALIDANDO');
  if(selectWork([options[0]],{'P0-012.A':'VALIDANDO'},'ALEX_CHATGPT').action!=='NO_ELIGIBLE')
    throw Error('Unready dependency claimed');
  const mutation={...documents,status:status.replace('PILOT_TASK_LIMIT=6','PILOT_TASK_LIMIT=2')};
  if(!validateDocuments(mutation).length)throw Error('Document mutation bypass');
  const approved={approverPrincipal:'ALEX',identityVerified:true,permissionVerified:true,
    providerRulesSatisfied:true,nativeApprovalVerified:true,exactHeadVerified:true,
    readbackVerified:true,independenceVerified:true,allContributionsReviewed:true,
    authorAccount:'JAVIER_CHATGPT',approverAccount:'ALEX_CHATGPT',environment:'dev'};
  if(validateApprovalDecision(approved).length)throw Error('Independent approval rejected');
  if(!validateApprovalDecision({...approved,allContributionsReviewed:false}).length)
    throw Error('Approval of unreviewed correction accepted');
  console.log('SOLQARYN 3x3 autonomy PASS: Alex and Javier both CLAIM PENDIENTE and REVIEW other-account VALIDANDO, review-first, dependency LISTO, correction independence, 20+ negative safeguards');
}else{
  console.log('SOLQARYN 3x3 authority PASS');
}
