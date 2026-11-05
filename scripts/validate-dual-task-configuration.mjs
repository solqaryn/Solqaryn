import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { reviewerFor, developerFor, selfTestReviewRouting } from './automation-review-routing.mjs';
import { assertStageReleaseNegativeTests, validateReleaseTransition, validateReviewReservation } from './automation-stage-ownership.mjs';
const read=path=>readFileSync(join(process.cwd(),path),'utf8');
const authority=read('docs/AUTOMATION_AUTHORITY.md');
const status=read('docs/AUTOMATION_STATUS.md');
const agents=read('AGENTS.md');
const scope=read('docs/PROJECT_SCOPE_LOCK.md');
const TASK=/^[a-f0-9]{32}$/i;
const SCOPE=/^P\d+-\d+\.[A-Z]$/;
const PR=/^https:\/\/github\.com\/solqaryn\/Solqaryn\/pull\/[1-9]\d*$/;
const key=(s,k)=>(s.match(new RegExp('^'+k+'=([^\\r\\n]+)$','m'))||[])[1]||null;
const roster=key(authority,'AUTHORIZED_COMMERCIAL_TASK_IDS')?.split(',')||[];
export function validateDocuments(src) {
  const errors=[];
  for (const [name,body] of Object.entries({authority:src.authority,status:src.status})) {
    if(key(body,'PROJECT_SCOPE_LOCK')!=='STRICT' ||
       key(body,'AUTOMATION_RUNTIME')!=='ACTIVE_DEV' ||
       key(body,'MAX_CONCURRENT_SCOPES')!=='15' ||
       key(body,'MAX_OWNED_SCOPES_PER_TASK')!=='15' ||
       key(body,'MAX_ACTIVE_WRITES_PER_TASK')!=='15')
      errors.push(name+' policy');
    if(key(body,'REVIEW_ROUTE_MODE')!=='DETERMINISTIC_AUTHOR_EXCLUSION')
      errors.push(name+' reviewer routing');
    if(!body.includes('SIN BLOQUEO EN SHEETS')) errors.push(name+' no-row-lock policy');
  }
  const auth=key(src.authority,'AUTHORIZED_COMMERCIAL_TASK_IDS')?.split(',')||[];
  const state=key(src.status,'AUTHORIZED_COMMERCIAL_TASK_IDS')?.split(',')||[];
  const registered=key(src.status,'REGISTERED_TASK_IDS')?.split(',')||[];
  if(auth.length!==6 || auth.some(x=>!TASK.test(x)) ||
     new Set(auth).size!==6 || auth.join(',')!==state.join(',') ||
     auth.some(x=>!registered.includes(x))) errors.push('task identity allowlist');
  if(key(src.scope,'PROJECT_SCOPE_LOCK')!=='STRICT' ||
     !src.scope.includes('SIN BLOQUEO EN SHEETS') ||
     !src.agents.includes('SIN BLOQUEO EN SHEETS'))
      errors.push('governance lock');
  if(!['6'].includes(key(src.status,'SCHEDULED_CHATGPT_TASKS_AUTHORIZED')) ||
     key(src.status,'VAEP_DISPATCH_AUTHORIZED')!=='FALSE' ||
     key(src.status,'JULES_WORKERS_AUTHORIZED')!=='0')
      errors.push('registered Task runtime');
  if(!src.authority.includes('REVIEW_INDEPENDENCE_REQUIRED=TRUE'))
    errors.push('independent review');
  return errors;
}
export function dependenciesSatisfied(task, stateById) {
  return Array.isArray(task?.dependencies) &&
    task.dependencies.every(id=>stateById[id]==='LISTO');
}
export function selectWork(tasks,stateById,account,activeReviewIds=new Set(),taskId=null,ownershipReadbackVerified=false){
  if(!['JAVIER_CHATGPT','ALEX_CHATGPT'].includes(account) ||
     !Array.isArray(tasks)) return {action:'NO_ELIGIBLE',reason:'ACCOUNT_OR_ROSTER'};
  const eligible=TASK.test(taskId||'') && roster.includes(taskId) &&
    ownershipReadbackVerified === true;
  if(!eligible) return {action:'NO_ELIGIBLE',reason:'NATIVE_ID_OR_READBACK_REQUIRED'};
  const owned=tasks.filter(t=>t.state==='EN_PROGRESO'&&t.ownerTaskId===taskId).length;
  if(owned>=15) return {action:'NO_ELIGIBLE',reason:'TASK_CAPACITY_15'};
  const review=tasks.find(t=>t.state==='VALIDANDO' && SCOPE.test(t.id||'') &&
    TASK.test(t.authorTaskId||'') && t.authorTaskId!==taskId &&
    reviewerFor(t.id,t.authorTaskId,roster)===taskId &&
    !activeReviewIds.has(t.id) && t.activeEditorConflict!==true &&
    t.activeReviewerConflict!==true && t.reviewerAllowed===true &&
    t.reviewPermissionVerified===true && PR.test(t.prUrl||'') &&
    (t.ownerTaskId==null || t.ownerTaskId===taskId || t.authorDeliveryVerified===true));
  if(review) return {action:'REVIEW',task:review,requiresNativeCurrentReadback:true};
  const own=tasks.find(t=>t.state==='EN_PROGRESO'&&t.ownerTaskId===taskId&&
    t.activeEditorConflict!==true&&t.branchReadbackVerified===true);
  if(own) return {action:'RESUME',task:own};
  if(tasks.filter(t=>['EN_PROGRESO','VALIDANDO'].includes(t.state)).length>=15)
    return {action:'NO_ELIGIBLE',reason:'GLOBAL_WIP_15'};
  const work=tasks.filter(t=>t.state==='PENDIENTE'&&SCOPE.test(t.id||'')&&
    developerFor(t.id,roster)===taskId&&dependenciesSatisfied(t,stateById)&&
    t.permissionsVerified===true&&t.activeEditorConflict!==true&&!t.conflictingClaim)
    .sort((a,b)=>(a.planOrder??0)-(b.planOrder??0));
  return work.length?{action:'CLAIM',task:work[0],requiresNativeCurrentReadback:true}:
    {action:'NO_ELIGIBLE',reason:'NO_SCOPE_ON_THIS_ROUTE'};
}
export function selectNextScheduledFreeTask(candidates, nowIso, account, previousOwnerTaskId=null) {
  const instant=x=>typeof x==='string' &&
    /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:Z|[+-]\d{2}:\d{2})$/.test(x) &&
    Number.isFinite(Date.parse(x));
  const taskId=x=>typeof x==='string' && /^[a-f0-9]{32}$/i.test(x);
  if(!instant(nowIso)||!['JAVIER_CHATGPT','ALEX_CHATGPT'].includes(account))
    return {action:'NO_GO',reason:'UNVERIFIED_CLOCK_OR_ACCOUNT'};
  if(!Array.isArray(candidates))
    return {action:'NO_GO',reason:'ROSTER_NOT_VERIFIED'};
  const now=Date.parse(nowIso);
  const verified=candidates.filter(c=>c&&taskId(c.taskId)&&
    c.taskId!==previousOwnerTaskId&&c.account===account&&
    c.registeredTask===true&&c.authorizedTask===true&&
    c.isEnabled===true&&c.schedulerVerified===true&&
    c.rruleVerified===true&&c.timezone==='America/Tegucigalpa'&&
    c.identityVerified===true&&c.permissionsVerified===true&&
    c.ownershipReadbackVerified===true&&Number.isInteger(c.ownedOpenScopes)&&
    c.ownedOpenScopes>=0&&c.ownedOpenScopes<15&&
    c.activeRun===false&&c.activeEditor===false&&c.activeLease===false&&
    ['PROVIDER_NEXT_RUN','DERIVED_VERIFIED_RRULE'].includes(c.nextRunSource)&&
    instant(c.nextRunAt)&&Date.parse(c.nextRunAt)>now&&
    Date.parse(c.nextRunAt)-now<=60*60*1000);
  // Free Tasks before busy Tasks; nearest real scheduled occurrence within equal load tiers.
  verified.sort((a,b)=>Number(a.ownedOpenScopes>0)-Number(b.ownedOpenScopes>0)||
    Date.parse(a.nextRunAt)-Date.parse(b.nextRunAt)||a.taskId.localeCompare(b.taskId));
  const first=verified[0];
  return first
    ? {action:'HANDOFF_CANDIDATE',taskId:first.taskId,nextRunAt:first.nextRunAt,
       nextRunSource:first.nextRunSource,requiresAuthorizationAndLeaseReadback:true}
    : {action:'NO_GO',reason:'NO_VERIFIED_FREE_FUTURE_RUN'};
}
// Historical read-only evidence is optional metadata and NEVER a gate for DEV.
export function verifiedScheduledReadOnlyProof(proof,slot) {
  return Boolean(slot && proof && proof.taskId === slot.taskId &&
    proof.account === slot.account && proof.completed === true &&
    proof.ownerReadbackVerified === true && proof.independentReviewerVerified === true);
}

export function validateProposedRun(p){
  const errors=[];
  if(!p||p.repo!=='solqaryn/Solqaryn'||p.branch!=='dev'||p.environment!=='dev'||
     p.sharedCredentials===true||p.legacyReactivate===true) errors.push('project_scope');
  if(!p || p.operation==='NONE')return errors;
  if(!TASK.test(p.actorTaskId||'') || !['JAVIER_CHATGPT','ALEX_CHATGPT'].includes(p.actorAccount) ||
     p.mode!=='ACTIVE_DEV' || p.authorizationVerified!==true ||
     p.permissionVerified!==true || p.exactHeadVerified!==true)
    errors.push('actor_permission');
  if(!['READ','WRITE','REVIEW','DELIVER_VALIDANDO','CLOSE','RECOVER_HANDOFF'].includes(p.operation))
    errors.push('unsupported_operation');
  if(p.operation==='READ')return errors;
  if(p.actorNativeOccurrenceVerified!==true || p.dispatchReadbackVerified!==true)
    errors.push('native_task_run_required');
  if(!SCOPE.test(p.scopeId||''))errors.push('scope_id');
  if(p.operation==='WRITE'){
    if(p.ownerAuthorized!==true||p.writePermissionVerified!==true||
      p.branchReadbackVerified!==true||p.conflictingClaim===true||
      p.activeEditorConflictOnTarget===true||!p.branchName?.startsWith('work/solqaryn-'))
      errors.push('unsafe_edit');
    if(!Number.isInteger(p.openWorkBranches)||p.openWorkBranches<0||p.openWorkBranches>15||
       (!p.resumingExistingScope&&p.openWorkBranches>=15))
      errors.push('global_capacity');
    if(!Number.isInteger(p.taskOwnedScopeCount)||p.taskOwnedScopeCount<0||
       p.taskOwnedScopeCount>=15&&!p.resumingExistingScope)
      errors.push('task_capacity');
    if(!Number.isInteger(p.taskActiveWriteCount)||p.taskActiveWriteCount<0||
       p.taskActiveWriteCount>=15&&!p.resumingExistingScope)
      errors.push('writer_capacity');
  }else if(p.operation==='REVIEW'){
    if(!TASK.test(p.authorTaskId||'')||p.authorTaskId===p.actorTaskId||
       reviewerFor(p.scopeId,p.authorTaskId,roster)!==p.actorTaskId||
       !PR.test(p.reviewedPrRef||'')||
       p.githubCurrentVerified!==true||p.planCurrentVerified!==true||
       p.activeReviewerConflict===true||p.activeEditorConflict===true)
      errors.push('independent_review_route');
    if(p.reviewMode==='TECHNICAL_CROSS_TASK'&&
       (p.technicalEvidenceVerified!==true||p.formalGithubApprovalRequested===true))
      errors.push('technical_review_integrity');
  }else if(p.operation==='DELIVER_VALIDANDO'){
    if(p.authorTaskId!==p.actorTaskId || p.workDeliveredVerified!==true ||
       p.deliveryTestsVerified!==true || !PR.test(p.reviewedPrRef||'') ||
       p.githubReadbackVerified!==true || p.planReadbackVerified!==true)
      errors.push('delivery_integrity');
  }else if(p.operation==='RECOVER_HANDOFF'){
    if(p.handoffAuthorized!==true||p.activeEditorConflict===true||
       p.lastCommitVerified!==true||p.branchReadbackVerified!==true||
       p.handoffTargetTaskId!==p.actorTaskId)
      errors.push('handoff_integrity');
  }else if(p.operation==='CLOSE'){
    if(p.authorTaskId===p.actorTaskId || !PR.test(p.reviewedPrRef||'') ||
       p.mergedIntoDev!==true || p.requiredChecksPassed!==true ||
       p.reviewIndependenceSatisfied!==true || p.allContributionsReviewed!==true ||
       p.sheetReadbackVerified!==true || p.ledgerReadbackVerified!==true ||
       p.dependentsRecalculated!==true)
      errors.push('close_integrity');
  }
  return errors;
}
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
const source={authority,status,agents,scope};
const errs=validateDocuments(source);
if(errs.length){console.error('SOLQARYN AUTOMATION AUTHORITY NO_GO '+errs.join(' | '));process.exitCode=1;}
else if(process.argv.includes('--self-test')){
  selfTestReviewRouting();assertStageReleaseNegativeTests();
  const ids=roster,author=ids[3],reviewer=reviewerFor('P1-015.C',author,ids);
  const row={id:'P1-015.C',state:'VALIDANDO',authorTaskId:author,authorAccount:'ALEX_CHATGPT',
    reviewerAllowed:true,reviewPermissionVerified:true,prUrl:'https://github.com/solqaryn/Solqaryn/pull/3861'};
  const pick=selectWork([row],{},'JAVIER_CHATGPT',new Set(),reviewer,true);
  if(pick.action!=='REVIEW')throw Error('ready VALIDANDO wrongly blocked');
  if(selectWork([row],{},'ALEX_CHATGPT',new Set(),author,true).action!=='NO_ELIGIBLE')
    throw Error('author self-review selected');
  if(selectWork([{...row,activeReviewerConflict:true}],{},'JAVIER_CHATGPT',new Set(),reviewer,true).action!=='NO_ELIGIBLE')
    throw Error('active reviewer collision');
  if(selectWork([{...row,activeEditorConflict:true}],{},'JAVIER_CHATGPT',new Set(),reviewer,true).action!=='NO_ELIGIBLE')
    throw Error('active editor collision');
  const pending={id:'P1-014.D',state:'PENDIENTE',dependencies:[],permissionsVerified:true};
  const developer=developerFor(pending.id,ids);
  if(selectWork([pending],{},'JAVIER_CHATGPT',new Set(),developer,true).action!=='CLAIM')
    throw Error('eligible work not routed');
  if(selectWork([{...pending,dependencies:['P1-013.A']}],{},'JAVIER_CHATGPT',new Set(),developer,true).action!=='NO_ELIGIBLE')
    throw Error('unmet dependency allowed');
  const p={repo:'solqaryn/Solqaryn',branch:'dev',environment:'dev',operation:'REVIEW',
    actorTaskId:reviewer,actorAccount:'JAVIER_CHATGPT',authorizationVerified:true,
    mode:'ACTIVE_DEV',permissionVerified:true,exactHeadVerified:true,
    nativeTask:true,actorNativeOccurrenceVerified:true,dispatchReadbackVerified:true,
    scopeId:'P1-015.C',authorTaskId:author,
    reviewedPrRef:'https://github.com/solqaryn/Solqaryn/pull/3861',
    githubCurrentVerified:true,planCurrentVerified:true};
  if(validateProposedRun(p).length)throw Error('valid review rejected');
  if(!validateProposedRun({...p,authorTaskId:reviewer}).length)throw Error('self-review bypass');
  if(!validateProposedRun({...p,actorNativeOccurrenceVerified:false}).length)throw Error('non-native run bypass');
  if(!validateProposedRun({...p,activeEditorConflict:true}).length)throw Error('active collision bypass');
  console.log('SOLQARYN DIRECT REVIEW 15/15/15 SELF_TEST=PASS');
}
