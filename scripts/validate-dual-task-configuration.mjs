import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { validValidationRelease, isStageOwnedBy, reviewReleaseEligible, validateReleaseTransition, validateReviewReservation, assertStageReleaseNegativeTests } from './automation-stage-ownership.mjs';

const read = path => readFileSync(join(process.cwd(), path), 'utf8');
const authority = read('docs/AUTOMATION_AUTHORITY.md');
const status = read('docs/AUTOMATION_STATUS.md');
const agents = read('AGENTS.md');
const scope = read('docs/PROJECT_SCOPE_LOCK.md');

const supportedAccounts = new Set(['JAVIER_CHATGPT','ALEX_CHATGPT']);
const expected = {
  PROJECT_SCOPE_LOCK: 'STRICT',
  PROJECT_ID: 'SOLQARYN',
  REPOSITORY: 'solqaryn/Solqaryn',
  BRANCH: 'dev',
  CONTRACT_VERSION: '2.0',
  TASK_COUNT_LIMIT: 'PROVIDER_EFFECTIVE_CAPACITY',
  TASK_ASSIGNMENT_MODEL: 'OWNER_CONFIGURABLE',
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
  if (!Number.isInteger(countA) || countA < 1 || countA !== countS) errors.push('authorized counter');
  const readIds=(source,keyName)=>{const value=key(source,keyName);if(value===null)return null;const ids=value.split(',').map(x=>x.trim());return ids.length&&ids.every(x=>/^[a-f0-9]{32}$/i.test(x))&&new Set(ids).size===ids.length?ids:null;};
  const registeredA=readIds(src.authority,'REGISTERED_TASK_IDS'), registeredS=readIds(src.status,'REGISTERED_TASK_IDS');
  const authorizedA=readIds(src.authority,'AUTHORIZED_COMMERCIAL_TASK_IDS'), authorizedS=readIds(src.status,'AUTHORIZED_COMMERCIAL_TASK_IDS');
  if(!registeredA||!registeredS||registeredA.join(',')!==registeredS.join(',')||
     Number(key(src.authority,'PHYSICAL_TASKS_REGISTERED'))!==registeredA.length||
     Number(key(src.status,'PHYSICAL_TASKS_REGISTERED'))!==registeredS.length)errors.push('registered Task set');
  if(!authorizedA||!authorizedS||authorizedA.join(',')!==authorizedS.join(',')||
     authorizedA.length!==countA||authorizedS.length!==countS||
     authorizedA.some(id=>!new Set(registeredA||[]).has(id)))errors.push('commercial Task allowlist');
  // The commercial WIP cap is a dedicated invariant, independent from the 5/6 Task allowlist.
  const capAuthority = Number(key(src.authority,'MAX_CONCURRENT_SCOPES'));
  const capStatus = Number(key(src.status,'MAX_CONCURRENT_SCOPES'));
  const capBranches = Number(key(src.status,'MAX_OPEN_WORK_BRANCHES'));
  if (![capAuthority,capStatus,capBranches].every(x=>Number.isInteger(x)&&x===15))
    errors.push('commercial WIP cap mismatch');
  const ownedCaps = [src.authority, src.status].map(x => Number(key(x,'MAX_OWNED_SCOPES_PER_TASK')));
  const activeWriteCaps = [src.authority, src.status].map(x => Number(key(x,'MAX_ACTIVE_WRITES_PER_TASK')));
  if (!ownedCaps.every(x=>Number.isInteger(x)&&x===15)) errors.push('per-task scope cap mismatch');
  if (!activeWriteCaps.every(x=>Number.isInteger(x)&&x===15)) errors.push('single active editor cap mismatch');
  if (key(src.scope,'TASK_COUNT_LIMIT') !== 'PROVIDER_EFFECTIVE_CAPACITY') errors.push('Task count policy');
  if (key(src.scope,'PROJECT_SCOPE_LOCK') !== 'STRICT') errors.push('strict lock');
  if (!src.agents.includes('TASK_DESIGN=OWNER_CONFIGURABLE_TASK_SET')) errors.push('agents Task set policy');
  for (const [name,value] of Object.entries(approvalPolicy))
    if (key(src.authority,name) !== value) errors.push('approval ' + name);
  for (const phrase of ['TASK_COUNT_LIMIT=PROVIDER_EFFECTIVE_CAPACITY','TASK_ASSIGNMENT_MODEL=OWNER_CONFIGURABLE','MAX_OPEN_WORK_BRANCHES=15','MAX_CONCURRENT_SCOPES=15',
    'MAX_OWNED_SCOPES_PER_TASK=15','MAX_ACTIVE_WRITES_PER_TASK=15',
    'REVIEW_INDEPENDENCE_REQUIRED=TRUE','America/Tegucigalpa',
    'ORDEN VIGENTE — 15/15/15 CON ASIGNACION AL DISPARARSE LA TASK',
    'VALIDANDO','EN_PROGRESO','PENDIENTE','CAS/lease']) {
    if (!src.authority.includes(phrase)) errors.push('authority policy ' + phrase);
  }
  if (!src.authority.includes('ORDEN VIGENTE — 15/15/15 CON ASIGNACION AL DISPARARSE LA TASK') ||
      !src.status.includes('ORDEN VIGENTE — 15/15/15 CON ASIGNACION AL DISPARARSE LA TASK'))
    errors.push('release policy routing');
  return errors;
}
const isUrl = ref => typeof ref === 'string' && /^https:\/\/github\.com\/solqaryn\/Solqaryn\/pull\/[1-9]\d*$/.test(ref);
const validScope = id => typeof id === 'string' && /^P\d+-\d+\.[A-Z]$/.test(id);
export function dependenciesSatisfied(task, stateById) {
  return Array.isArray(task.dependencies) &&
    task.dependencies.every(id => stateById[id] === 'LISTO');
}
export function selectWork(tasks, stateById, account, activeReviewIds = new Set(), taskId = null, ownershipReadbackVerified = false) {
  if (!['JAVIER_CHATGPT','ALEX_CHATGPT'].includes(account))
    return {action:'NO_ELIGIBLE',reason:'UNKNOWN_ACCOUNT'};
  // REVIEW_FIRST: same-login review is allowed ONLY across distinct TASK_IDs
  // with technical evidence; NEVER claim it as native GitHub APPROVED.
  const validTaskId = typeof taskId === 'string' && /^[a-f0-9]{32}$/i.test(taskId);
  // A missing or incomplete stage release receipt cannot free the original author.
  const unreleasedDelivery=validTaskId && tasks.some(t=>t.state==='VALIDANDO'&&
    (t.ownerTaskId===taskId||t.authorTaskId===taskId)&&
    (t.stageReleaseRequested===true
      ? !validValidationRelease(t)
      : !(t.validationHandoffVerified===true&&t.developmentLeaseReleased===true&&t.authorTaskId===taskId)));
  // A Task can own multiple DISTINCT scopes; a released author's provenance is not ownership.
  const assignedIds=new Set(tasks.filter(t=>validTaskId&&((t.state==='EN_PROGRESO'&&t.ownerTaskId===taskId)||
    (t.state==='VALIDANDO'&&(isStageOwnedBy(t,taskId)||t.reviewerTaskId===taskId)))).map(t=>t.id));
  const hasCapacity=!validTaskId||assignedIds.size<15;
  const assignedStage=tasks.find(t=>validTaskId&&ownershipReadbackVerified===true&&
    t.state==='VALIDANDO'&&t.stageReleaseRequested===true&&validValidationRelease(t)&&
    t.stageOwnerTaskId===taskId&&t.reviewReservationTaskId===taskId&&
    t.authorTaskId!==taskId&&t.stageReviewReservationReadbackVerified===true&&
    t.reviewerLeaseReadbackVerified===true&&t.reviewVersionVerified===true&&
    t.exactHeadVerified===true&&isUrl(t.prUrl)&&
    t.reviewPermissionVerified===true&&!activeReviewIds.has(t.id));
  if(assignedStage) return {action:'RESUME_REVIEW',task:assignedStage};
  const stageCandidate=tasks.find(t=>validTaskId&&
    reviewReleaseEligible(t,taskId,activeReviewIds)&&
    ['JAVIER_CHATGPT','ALEX_CHATGPT'].includes(t.authorAccount)&&
    t.exactHeadVerified===true&&isUrl(t.prUrl)&&
    (t.authorAccount!==account||
      (t.technicalReviewAllowed===true&&t.technicalEvidenceVerified===true&&
       t.formalGithubApprovalRequested!==true)));
  if(stageCandidate&&ownershipReadbackVerified===true&&hasCapacity&&!unreleasedDelivery)
    return {action:'REVIEW_RESERVATION_CANDIDATE',task:stageCandidate,
      requiresExclusiveReviewLease:true,requiresOwnerReadback:true};
  const review = tasks.find(t => {
    const technical = t.authorAccount===account && validTaskId &&
      /^[a-f0-9]{32}$/i.test(t.authorTaskId||'') && t.authorTaskId!==taskId &&
      t.technicalReviewAllowed===true && t.technicalEvidenceVerified===true &&
      t.formalGithubApprovalRequested!==true;
    return t.state==='VALIDANDO' && t.stageReleaseRequested!==true &&
      (!validTaskId||/^[a-f0-9]{32}$/i.test(t.authorTaskId||'')) &&
      ['JAVIER_CHATGPT','ALEX_CHATGPT','SOLQARYN_PLATFORM'].includes(t.authorAccount) &&
      (t.authorAccount!==account || technical) && (!validTaskId||t.authorTaskId!==taskId) &&
      (!t.reviewerTaskId||t.reviewerTaskId===taskId) && !activeReviewIds.has(t.id) &&
      isUrl(t.prUrl) && t.reviewerAllowed===true &&
      t.reviewPermissionVerified===true && t.reviewerLeaseAvailable===true;
  });
  if(review && hasCapacity && !unreleasedDelivery) return {action:review.authorAccount===account?'REVIEW_TECHNICAL':'REVIEW',task:review,requiresExclusiveReviewerClaim:true};
  const own = tasks.find(t => t.state === 'EN_PROGRESO' &&
    validTaskId && t.ownerTaskId===taskId && t.ownerAccount === account && t.resumeAuthorized === true &&
    t.exclusiveScopeVerified === true && t.activeEditorVerifiedAbsent === true &&
    t.branchReadbackVerified === true && !t.conflictingClaim);
  if (own) return {action:'RESUME',task:own};
  // No acquisition is allowed if this physical Task already owns any scope,
  // whether EN_PROGRESO or VALIDANDO. Verify the COMPLETE ownership readback.
  if(!validTaskId || ownershipReadbackVerified!==true)
    return {action:'NO_ELIGIBLE',reason:'OWNERSHIP_READBACK_REQUIRED'};
  if(!hasCapacity)return {action:'NO_ELIGIBLE',reason:'TASK_CAPACITY_15'};
  // Foreign IN_PROGRESS: no unverified takeover or lost commits.
  const handoff = tasks.find(t => t.state === 'EN_PROGRESO' &&
    validScope(t.id) && ['JAVIER_CHATGPT','ALEX_CHATGPT'].includes(t.ownerAccount) &&
    (t.ownerAccount!==account || (t.ownerTaskId!==taskId &&
      t.handoffTargetTaskId===taskId)) && t.handoffAuthorized === true &&
    t.previousLeaseReleased === true && t.activeEditorVerifiedAbsent === true &&
    t.exclusiveScopeVerified === true && t.branchReadbackVerified === true &&
    t.lastCommitVerified === true && t.permissionsVerified === true &&
    t.ownerTransferReadbackVerified === true && t.nextRunRecipientSelectionVerified === true &&
    t.handoffTargetTaskId === taskId && !t.conflictingClaim &&
    t.branchName === 'work/solqaryn-' + t.id.toLowerCase().replace('.', '-'));
  if (handoff) return {action:'RECOVER_HANDOFF',task:handoff};
  if(tasks.filter(t=>t.state==='VALIDANDO'||t.state==='EN_PROGRESO').length>=15)
    return {action:'NO_ELIGIBLE',reason:'GLOBAL_WIP_15'};
  const eligible = tasks.filter(t => t.state === 'PENDIENTE' &&
    validScope(t.id) && dependenciesSatisfied(t,stateById) &&
    !t.existingBranch && !t.conflictingClaim && t.permissionsVerified);
  eligible.sort((x,y)=>Number(x.priority.slice(1))-Number(y.priority.slice(1)) ||
    x.planOrder-y.planOrder);
  return eligible.length ? {action:'CLAIM',task:eligible[0]} : {action:'NO_ELIGIBLE'};
}
// Advisory only: never changes an owner or grants permissions.
// Every candidate requires an independently verified, future scheduled occurrence.
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
export function validateProposedRun(p) {
  const errors=[];
  if(p.repo!=='solqaryn/Solqaryn'||p.branch!=='dev')errors.push('identity');
  if(p.environment!=='dev')errors.push('environment');
  if(p.ownerChanged&&(p.configurationChangeAuthorized!==true||p.providerCapacityVerified!==true))errors.push('unverified_configuration_change');
  if(p.sharedCredentials||p.legacyReactivate)errors.push('scope');
  if(!Array.isArray(p.slots))errors.push('slot_registry');
  const seenId=new Set();
  for(const slot of p.slots||[]) {
    if(!supportedAccounts.has(slot.account))errors.push('slot_account_unverified');
    if(seenId.has(slot.taskId))errors.push('slot_duplicate');
    if(!slot.taskId||slot.taskId==='NO_VERIFICADO'||!slot.role||!slot.permissionsVerified||!slot.schedulerVerified)
      errors.push('slot_unverified');
    seenId.add(slot.taskId);
  }
  if(p.operation==='NONE')return errors;
  if(!/^[a-f0-9]{32}$/i.test(p.actorTaskId||'')||
     !(p.slots||[]).some(s=>s.taskId===p.actorTaskId &&
       s.account===p.actorAccount&&s.role===p.actorRole&&s.permissionsVerified===true))
    errors.push('actor_task_identity');
  if(!p.authorizationVerified||p.mode!=='ACTIVE_DEV')errors.push('authorization');
  if(!supportedAccounts.has(p.actorAccount) ||
     !(p.slots||[]).some(s=>s.role===p.actorRole&&s.account===p.actorAccount&&s.permissionsVerified===true))
    errors.push('actor_auth');
  if(!p.permissionVerified||!p.exactHeadVerified)errors.push('permission_or_head');
  // No external scheduler may preassign a scope before the intended physical Task fires.
  if((p.operation==='WRITE'&&!p.resumingExistingScope)||p.operation==='RESERVE_REVIEW'){
    if(p.actorNativeOccurrenceVerified!==true||p.dispatchReadbackVerified!==true)
      errors.push('native_trigger_and_dispatch_readback_required');
  }
  if(p.operation==='DELIVER_VALIDANDO'){
    if(p.stageReleaseRequested===true){
      if(p.authorTaskId!==p.actorTaskId||p.ownershipReadbackVerified!==true||
        !isUrl(p.reviewedPrRef))errors.push('unsafe_validation_release');
      errors.push(...validateReleaseTransition(p));
    }else if(!validScope(p.scopeId)||p.authorTaskId!==p.actorTaskId||
       p.fromState!=='EN_PROGRESO'||p.toState!=='VALIDANDO'||!isUrl(p.reviewedPrRef)||
       p.deliveryCommittedInSheets!==true||p.validationHandoffVerified!==true||
       p.developmentLeaseReleased!==true||p.activeEditorVerifiedAbsent!==true||
       p.ownershipReadbackVerified!==true||p.authorshipPreserved!==true||
       p.reviewUnassignedReadbackVerified!==true||p.ledgerReadbackVerified!==true)
       errors.push('unsafe_validation_release');
  }else if(p.operation==='WRITE'){
    if(!validScope(p.scopeId)||!p.ownerAuthorized||!p.writePermissionVerified||
      !p.exclusiveScopeVerified||!p.createdExclusively||!p.branchReadbackVerified||
      p.conflictingClaim||!p.idempotencyKey||!p.branchName?.startsWith('work/solqaryn-'))
      errors.push('unsafe_write');
    // openWorkBranches is the VERIFIED count of distinct active COMMERCIAL scopes,
    // excluding governance PRs, diagnostic test branches and merged history.
    // Count before a NEW claim; at WIP=15 a seventh claim must be rejected.
    // A read/write RESUME of an already counted scope does not consume another slot.
    if(!Number.isInteger(p.openWorkBranches)||p.openWorkBranches<0||p.openWorkBranches>15||
       (!p.resumingExistingScope&&p.openWorkBranches>=15)) errors.push('branch_cap');
    if(p.ownershipReadbackVerified!==true ||
       !Number.isInteger(p.taskOwnedScopeCount)||p.taskOwnedScopeCount<0||p.taskOwnedScopeCount>15||
       (!p.resumingExistingScope&&p.taskOwnedScopeCount>=15)||
       (p.resumingExistingScope&&(p.taskOwnedScopeCount<1||
         p.ownedScopeId!==p.scopeId||p.scopeOwnerTaskId!==p.actorTaskId)))
      errors.push('task_scope_cap');
    // Only one active editor in the single assigned scope.
    if(!Number.isInteger(p.taskActiveWriteCount)||p.taskActiveWriteCount<0||
       p.taskActiveWriteCount>15||(!p.resumingExistingScope&&p.taskActiveWriteCount>=15)||
       p.activeEditorConflictOnTarget===true) errors.push('task_active_write');
    // No second claim while the first is VALIDANDO or awaiting CI/review.
    if(p.resumingExistingScope && p.recoveringForeignScope===true &&
       (p.handoffAuthorized!==true || p.previousLeaseReleased!==true ||
        p.activeEditorVerifiedAbsent!==true || p.lastCommitVerified!==true ||
        p.ownerTransferReadbackVerified!==true || !p.branchReadbackVerified ||
         p.nextRunRecipientSelectionVerified!==true || p.handoffTargetTaskId!==p.actorTaskId))
      errors.push('unsafe_recovery_handoff');
    if(p.activeEditorAccount&&p.activeEditorAccount!==p.actorAccount)
      errors.push('concurrent_editor');
  }else if(p.operation==='RESERVE_REVIEW'){
    if(p.stageReleaseRequested!==true||p.reviewerTaskId!==p.actorTaskId||
       !isUrl(p.reviewedPrRef)||p.ownershipReadbackVerified!==true)
      errors.push('review_reservation_integrity');
    errors.push(...validateReviewReservation(p));
  }else if(p.operation==='REVIEW'){
    if(p.stageReleaseRequested===true&&
      (p.stageReviewReservationReadbackVerified!==true||
       p.reviewReservationTaskId!==p.actorTaskId||
       p.stageReleaseReceiptVerified!==true))
      errors.push('unverified_stage_review');
    if(p.actorTaskId===p.authorTaskId||p.ownershipReadbackVerified!==true||
       p.reviewerClaimAtomicVerified!==true||p.reviewerTaskId!==p.actorTaskId||
       p.reviewerLeaseReadbackVerified!==true||p.developmentLeaseReleased!==true||
       p.validationHandoffVerified!==true)errors.push('unsafe_reviewer_claim');
    const technical = p.reviewMode==='TECHNICAL_CROSS_TASK';
    if(!isUrl(p.reviewedPrRef)||p.activeReviewerConflict)
      errors.push('review_integrity');
    if(technical){
      // Technical review by another TASK_ID is valid even on the same GitHub
      // login, but it cannot claim formal independent provider approval.
      if(!/^[a-f0-9]{32}$/i.test(p.authorTaskId||'')||
        p.authorTaskId===p.actorTaskId||p.technicalEvidenceVerified!==true||
        p.reviewPermissionVerified!==true||p.exactHeadVerified!==true||
        p.formalGithubApprovalRequested===true)
        errors.push('technical_review_integrity');
    }else if(p.authorAccount===p.actorAccount||!p.independentReviewerVerified)
      errors.push('review_integrity');
  }else if(p.operation==='CLOSE'){
    if(!isUrl(p.reviewedPrRef)||!p.mergedIntoDev||!p.mergeSha||
      !p.requiredChecksPassed||!p.reviewIndependenceSatisfied||
      !p.allContributionsReviewed||!p.sheetReadbackVerified||
      !p.ledgerReadbackVerified||!p.dependentsRecalculated)
      errors.push('close_integrity');
    if(p.authorTaskId && p.actorTaskId===p.authorTaskId)
      errors.push('self_signoff');
    if(p.actorAccount===p.authorAccount &&
      (!p.authorTaskId||p.actorTaskId===p.authorTaskId||
       p.technicalReviewPassed!==true||p.technicalEvidenceVerified!==true||
       p.automaticCertificationVerified!==true||p.providerRulesSatisfied!==true||
       p.formalReviewRequired===true))
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
const documents={authority,status,agents,scope};
const failures=validateDocuments(documents);
if(failures.length){
  console.error('SOLQARYN AUTOMATION AUTHORITY NO_GO: '+failures.join(' | '));
  process.exitCode=1;
}else if(process.argv.includes('--self-test')){
  const slots=[
    {role:'PRIMARY_01',account:'JAVIER_CHATGPT',taskId:'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',permissionsVerified:true,schedulerVerified:true},
    {role:'SECONDARY_02',account:'ALEX_CHATGPT',taskId:'cccccccccccccccccccccccccccccccc',permissionsVerified:true,schedulerVerified:true},
  ];
  const base={repo:'solqaryn/Solqaryn',branch:'dev',environment:'dev',
    slots,
    mode:'ACTIVE_DEV',authorizationVerified:true,
    actorRole:'PRIMARY_01',actorAccount:'JAVIER_CHATGPT',
    actorTaskId:'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',ownershipReadbackVerified:true,
    permissionVerified:true,exactHeadVerified:true,
    operation:'WRITE',ownerAuthorized:true,writePermissionVerified:true,
    scopeId:'P0-012.A',exclusiveScopeVerified:true,createdExclusively:true,
    branchReadbackVerified:true,branchName:'work/solqaryn-p0-012-a',
    openWorkBranches:5,taskOwnedScopeCount:0,taskActiveWriteCount:0,
    existingScopeState:null,existingScopeActiveEditor:false,reviewFirstChecked:true,
    hasEligibleExternalReview:false,resumingExistingScope:false,
    idempotencyKey:'P0-012.A:base-sha',conflictingClaim:false,
    runId:'NO_VERIFICADO',actorNativeOccurrenceVerified:true,dispatchReadbackVerified:true};
  const clone=x=>structuredClone(x);
  if(validateProposedRun(base).length)throw Error('DEV without native RUN_ID must be allowed');
  const sameFunction=clone(base);sameFunction.slots[1].role=sameFunction.slots[0].role;
  if(validateProposedRun(sameFunction).length)throw Error('A repeated operational role must not impose a quota');
  const fifteenJavier=clone(base);fifteenJavier.operation='NONE';fifteenJavier.slots=Array.from({length:15},(_,i)=>({role:'JAVIER_TASK_'+String(i+1).padStart(2,'0'),account:'JAVIER_CHATGPT',taskId:'JAVIER-TASK-'+String(i+1).padStart(2,'0'),permissionsVerified:true,schedulerVerified:true}));fifteenJavier.ownerChanged=true;fifteenJavier.configurationChangeAuthorized=true;fifteenJavier.providerCapacityVerified=true;
  if(validateProposedRun(fifteenJavier).length)throw Error('A verified 15-Task single-account roster must not hit an internal quota');
  const unverifiedResize=clone(fifteenJavier);unverifiedResize.providerCapacityVerified=false;
  if(!validateProposedRun(unverifiedResize).length)throw Error('Unverified provider capacity change accepted');
  const variants=[
    ['wrong repo',p=>p.repo='external/Other'],
    ['wrong branch',p=>p.branch='qa'],
    ['duplicate Task ID',p=>p.slots=[...p.slots,...p.slots]],
    ['duplicate id',p=>p.slots[1].taskId=p.slots[0].taskId],
    ['wrong account',p=>p.slots[0].account='ALEX_CHATGPT'],
    ['missing provider right',p=>p.writePermissionVerified=false],
    ['scope collision',p=>p.conflictingClaim=true],
    ['claim not atomic',p=>p.createdExclusively=false],
    ['branch not verified',p=>p.branchReadbackVerified=false],
    ['wrong scope',p=>p.scopeId=''],
    ['stale head',p=>p.exactHeadVerified=false],
    ['too many branches',p=>p.openWorkBranches=16],
    ['global fifteenth slot occupied',p=>p.openWorkBranches=15],
    ['Task capacity full',p=>p.taskOwnedScopeCount=15],
    ['missing ownership readback readback',p=>p.ownershipReadbackVerified=false],
    ['missing task scope count',p=>delete p.taskOwnedScopeCount],
    ['writer capacity full',p=>p.taskActiveWriteCount=15],
    ['negative per task scope count',p=>p.taskOwnedScopeCount=-1],
    ['negative WIP count',p=>p.openWorkBranches=-1],
    ['noninteger WIP count',p=>p.openWorkBranches=2.5],
    ['forbidden prod',p=>p.environment='prod'],
    ['shared credentials',p=>p.sharedCredentials=true],
    ['unknown actor',p=>p.actorRole='VAEP_SUPERVISOR'],
    ['legacy dispatcher',p=>p.legacyReactivate=true],
  ];
  for(const [name,mutate] of variants){
    const t=clone(base);mutate(t);
    if(!validateProposedRun(t).length)throw Error('negative test bypass: '+name);
  }
  // A second independent scope may be claimed on a real run; a sixteenth is forbidden.
  for(const state of ['EN_PROGRESO','VALIDANDO']){
    const second=clone(base);
    Object.assign(second,{taskOwnedScopeCount:1,existingScopeState:state,existingScopeActiveEditor:false,reviewFirstChecked:true});
    if(validateProposedRun(second).length)throw Error('Safe second scope rejected: '+state);
    const fifteenth={...second,taskOwnedScopeCount:14,taskActiveWriteCount:14};
    if(validateProposedRun(fifteenth).length)throw Error('Safe fifteenth scope rejected: '+state);
    if(!validateProposedRun({...fifteenth,taskOwnedScopeCount:15}).includes('task_scope_cap'))throw Error('Sixteenth scope accepted: '+state);
    if(!validateProposedRun({...fifteenth,taskActiveWriteCount:15}).includes('task_active_write'))throw Error('Sixteenth editor accepted: '+state);
  }
  if(!validateProposedRun({...base,actorNativeOccurrenceVerified:false}).includes('native_trigger_and_dispatch_readback_required'))
    throw Error('Preassignment without physical occurrence accepted');
  if(!validateProposedRun({...base,dispatchReadbackVerified:false}).includes('native_trigger_and_dispatch_readback_required'))
    throw Error('Claim without dispatch readback accepted');
  const resumeFull=clone(base);
  Object.assign(resumeFull,{openWorkBranches:15,taskOwnedScopeCount:1,
    taskActiveWriteCount:1,resumingExistingScope:true,
    ownedScopeId:base.scopeId,scopeOwnerTaskId:base.actorTaskId});
  if(validateProposedRun(resumeFull).length)throw Error('Safe resume at WIP=15 denied');
  const handoffWrite=clone(resumeFull);
  Object.assign(handoffWrite,{recoveringForeignScope:true,handoffAuthorized:true,previousLeaseReleased:true,
    activeEditorVerifiedAbsent:true,lastCommitVerified:true,ownerTransferReadbackVerified:true,
    nextRunRecipientSelectionVerified:true,handoffTargetTaskId:base.actorTaskId});
  if(validateProposedRun(handoffWrite).length) throw Error('Verified handoff write rejected');
  for(const key of ['handoffAuthorized','previousLeaseReleased','activeEditorVerifiedAbsent',
    'lastCommitVerified','ownerTransferReadbackVerified','branchReadbackVerified',
     'nextRunRecipientSelectionVerified']) {
    const x=clone(handoffWrite); x[key]=false;
    if(!validateProposedRun(x).length) throw Error('Unsafe handoff WRITE permitted: '+key);
  }
  const release={...base,operation:'DELIVER_VALIDANDO',fromState:'EN_PROGRESO',toState:'VALIDANDO',
    authorTaskId:base.actorTaskId,reviewedPrRef:'https://github.com/solqaryn/Solqaryn/pull/3841',
    deliveryCommittedInSheets:true,validationHandoffVerified:true,developmentLeaseReleased:true,
    activeEditorVerifiedAbsent:true,authorshipPreserved:true,reviewUnassignedReadbackVerified:true,
    ledgerReadbackVerified:true};
  if(validateProposedRun(release).length)throw Error('Verified delivery rejected');
  for(const field of ['deliveryCommittedInSheets','validationHandoffVerified','developmentLeaseReleased',
    'activeEditorVerifiedAbsent','authorshipPreserved','reviewUnassignedReadbackVerified','ledgerReadbackVerified'])
    if(!validateProposedRun({...release,[field]:false}).includes('unsafe_validation_release'))
      throw Error('Unsafe delivery accepted: '+field);
  const alexWrite=clone(base);
  alexWrite.actorRole='SECONDARY_02';alexWrite.actorAccount='ALEX_CHATGPT';alexWrite.actorTaskId='cccccccccccccccccccccccccccccccc';
  if(validateProposedRun(alexWrite).length)throw Error('Equal write capability denied to Alex');
  const review={...base,operation:'REVIEW',actorRole:'SECONDARY_02',
    actorAccount:'ALEX_CHATGPT',actorTaskId:'cccccccccccccccccccccccccccccccc',authorAccount:'JAVIER_CHATGPT',
    reviewedPrRef:'https://github.com/solqaryn/Solqaryn/pull/3734',
    independentReviewerVerified:true,authorTaskId:'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb',reviewerTaskId:'cccccccccccccccccccccccccccccccc',reviewerClaimAtomicVerified:true,reviewerLeaseReadbackVerified:true,developmentLeaseReleased:true,validationHandoffVerified:true};
  if(validateProposedRun(review).length)throw Error('Alex review denied');
  const javierReview={...review,actorRole:'PRIMARY_01',
    actorAccount:'JAVIER_CHATGPT',actorTaskId:'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',reviewerTaskId:'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',authorAccount:'ALEX_CHATGPT'};
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
  const technicalReview={...base,operation:'REVIEW',reviewMode:'TECHNICAL_CROSS_TASK',
    authorAccount:'JAVIER_CHATGPT',authorTaskId:'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb',
    reviewedPrRef:'https://github.com/solqaryn/Solqaryn/pull/3842',
    technicalEvidenceVerified:true,reviewPermissionVerified:true,
    independentReviewerVerified:false,formalGithubApprovalRequested:false,reviewerTaskId:base.actorTaskId,reviewerClaimAtomicVerified:true,reviewerLeaseReadbackVerified:true,developmentLeaseReleased:true,validationHandoffVerified:true};
  if(validateProposedRun(technicalReview).length)
    throw Error('Same-login distinct-TASK_ID technical review refused');
  for(const [name,patch] of [
    ['same TASK_ID',p=>p.authorTaskId=p.actorTaskId],
    ['missing evidence',p=>p.technicalEvidenceVerified=false],
    ['unverified permission',p=>p.reviewPermissionVerified=false],
    ['fake formal APPROVED',p=>p.formalGithubApprovalRequested=true]
  ]){const x=clone(technicalReview);patch(x);
    if(!validateProposedRun(x).length)throw Error('Unsafe technical review accepted: '+name);}
  const crossClose={...close,actorAccount:'JAVIER_CHATGPT',
    actorRole:'PRIMARY_01',actorTaskId:base.actorTaskId,
    authorAccount:'JAVIER_CHATGPT',authorTaskId:'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb',
    technicalReviewPassed:true,technicalEvidenceVerified:true,
    automaticCertificationVerified:true,providerRulesSatisfied:true,
    formalReviewRequired:false};
  if(validateProposedRun(crossClose).length)
    throw Error('Verified cross-Task postmerge closure rejected');
  for(const [name,patch] of [
    ['own Task',p=>p.authorTaskId=p.actorTaskId],
    ['technical review missing',p=>p.technicalReviewPassed=false],
    ['auto cert missing',p=>p.automaticCertificationVerified=false],
    ['provider gate missing',p=>p.providerRulesSatisfied=false],
    ['formal review required',p=>p.formalReviewRequired=true]
  ]){const x=clone(crossClose);patch(x);
    if(!validateProposedRun(x).length)throw Error('Unsafe cross-Task close accepted: '+name);}
  const graph={'P0-012.A':'VALIDANDO'};
  if(dependenciesSatisfied({dependencies:['P0-012.A']},graph))
    throw Error('VALIDANDO incorrectly released dependency');
  if(!dependenciesSatisfied({dependencies:['P0-012.A']},{'P0-012.A':'LISTO'}))
    throw Error('LISTO failed to release dependency');
  const options=[{id:'P0-012.B',state:'PENDIENTE',dependencies:['P0-012.A'],
    priority:'P0',planOrder:24,permissionsVerified:true},
    {id:'P1-100.A',state:'VALIDANDO',authorAccount:'JAVIER_CHATGPT',
      prUrl:'https://github.com/solqaryn/Solqaryn/pull/99',
      reviewerAllowed:true,reviewPermissionVerified:true,reviewerLeaseAvailable:true}];
  if(selectWork(options,{'P0-012.A':'LISTO'},'ALEX_CHATGPT').action!=='REVIEW')
    throw Error('Review-first selection regression');
  const alexPR={id:'P0-013.A',state:'VALIDANDO',
    authorAccount:'ALEX_CHATGPT',authorTaskId:'cccccccccccccccccccccccccccccccc',
    prUrl:'https://github.com/solqaryn/Solqaryn/pull/3736',
    reviewerAllowed:true,reviewPermissionVerified:true,reviewerLeaseAvailable:true};
  if(selectWork([alexPR,options[0]],{'P0-012.A':'LISTO'},'JAVIER_CHATGPT').action!=='REVIEW')
    throw Error('Javier must prioritize review of Alex over eligible pending');
  if(selectWork([options[0]],{'P0-012.A':'LISTO'},'ALEX_CHATGPT',new Set(),'cccccccccccccccccccccccccccccccc',true).action!=='CLAIM')
    throw Error('Alex must be able to develop eligible PENDIENTE');
  if(selectWork([options[0]],{'P0-012.A':'LISTO'},'JAVIER_CHATGPT',new Set(),'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',true).action!=='CLAIM')
    throw Error('Javier must be able to develop eligible PENDIENTE');
  if(selectWork([alexPR],{},'ALEX_CHATGPT').action!=='NO_ELIGIBLE')
    throw Error('Alex must not review own VALIDANDO');
  if(selectWork([{...alexPR,authorAccount:undefined}],{},'JAVIER_CHATGPT').action!=='NO_ELIGIBLE')
    throw Error('A review with unverifiable author cannot be claimed');
  if(selectWork([{...alexPR,reviewPermissionVerified:false}],{},'JAVIER_CHATGPT').action!=='NO_ELIGIBLE')
    throw Error('A review without permission cannot be claimed');
  if(selectWork([alexPR],{},'UNKNOWN_ACCOUNT').action!=='NO_ELIGIBLE')
    throw Error('An unknown account cannot claim a review');
  const own={id:'P0-014.A',state:'EN_PROGRESO',ownerAccount:'JAVIER_CHATGPT',ownerTaskId:'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb',
    resumeAuthorized:true,exclusiveScopeVerified:true,activeEditorVerifiedAbsent:true,branchReadbackVerified:true};
  if(selectWork([own,alexPR],{},'JAVIER_CHATGPT').action!=='REVIEW') throw Error('Advisory review scan missing');
  if(selectWork([own,alexPR],{},'JAVIER_CHATGPT',new Set(),'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb',true).action!=='REVIEW')
    throw Error('Review first blocked by another independent scope');
  if(selectWork([own],{},'JAVIER_CHATGPT',new Set(),'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb').action!=='RESUME') throw Error('Safe own resume denied');
  const idOwnValidating='bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb';
  const ownValidating={id:'P1-009.A',state:'VALIDANDO',authorAccount:'JAVIER_CHATGPT',authorTaskId:idOwnValidating,
    ownerAccount:'JAVIER_CHATGPT',ownerTaskId:idOwnValidating,validationHandoffVerified:true,
    developmentLeaseReleased:true,prUrl:'https://github.com/solqaryn/Solqaryn/pull/3841',
    reviewerAllowed:true,reviewPermissionVerified:true,reviewerLeaseAvailable:true,
    technicalReviewAllowed:true,technicalEvidenceVerified:true};
  if(selectWork([ownValidating,options[0]],{'P0-012.A':'LISTO'},'JAVIER_CHATGPT',new Set(),idOwnValidating,true).action!=='CLAIM')
    throw Error('Released author cannot claim new task');
  if(selectWork([ownValidating],{},'JAVIER_CHATGPT',new Set(),idOwnValidating,true).action!=='NO_ELIGIBLE')
    throw Error('Author self-review accepted');
  if(selectWork([ownValidating],{},'JAVIER_CHATGPT',new Set(),'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',true).action!=='REVIEW_TECHNICAL')
    throw Error('Another Task unable to review');
  for(const prop of ['validationHandoffVerified','developmentLeaseReleased'])
    if(selectWork([{...ownValidating,[prop]:false},options[0]],{'P0-012.A':'LISTO'},'JAVIER_CHATGPT',new Set(),idOwnValidating,true).action==='CLAIM')
      throw Error('Unverified release accepted: '+prop);
  const assigned={...ownValidating,reviewerTaskId:'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'};
  if(selectWork([assigned],{},'JAVIER_CHATGPT',new Set(),'cccccccccccccccccccccccccccccccc',true).action!=='NO_ELIGIBLE')throw Error('Review stolen');
  if(selectWork([assigned],{},'JAVIER_CHATGPT',new Set(),'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',true).action!=='REVIEW_TECHNICAL')throw Error('Reviewer resume denied');
  const institution={...alexPR,authorAccount:'SOLQARYN_PLATFORM'};
  if(selectWork([institution],{},'JAVIER_CHATGPT').action!=='REVIEW') throw Error('Institutional PR incorrectly excluded from review');
  if(selectWork([{...institution,reviewerLeaseAvailable:false}],{},'JAVIER_CHATGPT').action!=='NO_ELIGIBLE') throw Error('Busy review stolen');
  const idle={id:'P0-014.A',state:'EN_PROGRESO',ownerAccount:'ALEX_CHATGPT',handoffAuthorized:true,
    previousLeaseReleased:true,activeEditorVerifiedAbsent:true,exclusiveScopeVerified:true,
    branchReadbackVerified:true,lastCommitVerified:true,permissionsVerified:true,
    ownerTransferReadbackVerified:true,nextRunRecipientSelectionVerified:true,
    handoffTargetTaskId:'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
    conflictingClaim:false,branchName:'work/solqaryn-p0-014-a'};
  if(selectWork([idle],{},'JAVIER_CHATGPT',new Set(),'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',true).action!=='RECOVER_HANDOFF') throw Error('Safe handoff rejected');
  for(const [field,value] of [['handoffAuthorized',false],['previousLeaseReleased',false],
    ['activeEditorVerifiedAbsent',false],['exclusiveScopeVerified',false],
    ['branchReadbackVerified',false],['lastCommitVerified',false],
    ['permissionsVerified',false],['ownerTransferReadbackVerified',false],
     ['nextRunRecipientSelectionVerified',false],['handoffTargetTaskId','dddddddddddddddddddddddddddddddd'],
    ['conflictingClaim',true],['branchName','work/solqaryn-different']]) {
    if(selectWork([{...idle,[field]:value}],{},'JAVIER_CHATGPT',new Set(),'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',true).action!=='NO_ELIGIBLE')
      throw Error('Unverified foreign recovery accepted: '+field);
  }
  if(selectWork([idle,alexPR],{},'JAVIER_CHATGPT',new Set(),'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',true).action!=='REVIEW') throw Error('Foreign handoff bypassed review');
  if(selectWork([options[0]],{'P0-012.A':'LISTO'},
     'JAVIER_CHATGPT',new Set(),idOwnValidating,false).action==='CLAIM')
    throw Error('CLAIM accepted without ownership readback');
  if(selectWork([{...alexPR,authorAccount:'JAVIER_CHATGPT'}],{},'JAVIER_CHATGPT').action!=='NO_ELIGIBLE')
    throw Error('Same-account self review accepted');
  const canonicalAlex=clone(base);
  canonicalAlex.slots[1].role='SECONDARY_REVIEW_01';
  canonicalAlex.actorRole='SECONDARY_REVIEW_01';
  canonicalAlex.actorAccount='ALEX_CHATGPT';
  canonicalAlex.actorTaskId='cccccccccccccccccccccccccccccccc';
  if(validateProposedRun(canonicalAlex).length)
    throw Error('Canonical Alex slot role should be permitted');
  const canonicalJavier=clone(base);
  canonicalJavier.slots[0].role='PRIMARY_DEV_01';
  canonicalJavier.actorRole='PRIMARY_DEV_01';
  if(validateProposedRun(canonicalJavier).length)
    throw Error('Canonical Javier slot role should be permitted');
  if(selectWork([options[0]],{'P0-012.A':'VALIDANDO'},'ALEX_CHATGPT').action!=='NO_ELIGIBLE')
    throw Error('Unready dependency claimed');
  const mutation={...documents,status:status.replace('TASK_COUNT_LIMIT=PROVIDER_EFFECTIVE_CAPACITY','TASK_COUNT_LIMIT=6')};
  if(!validateDocuments(mutation).length)throw Error('Task count policy mutation bypass');
  const expanded={...documents};
  expanded.status=expanded.status.replace(/^REGISTERED_TASK_IDS=(.*)$/m,(_,ids)=>'REGISTERED_TASK_IDS='+ids+',aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa').replace(/^PHYSICAL_TASKS_REGISTERED=(\d+)$/m, (_,n) => 'PHYSICAL_TASKS_REGISTERED=' + (Number(n) + 1));
  expanded.authority=expanded.authority.replace(/^REGISTERED_TASK_IDS=(.*)$/m,(_,ids)=>'REGISTERED_TASK_IDS='+ids+',aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa').replace(/^PHYSICAL_TASKS_REGISTERED=(\d+)$/m, (_,n) => 'PHYSICAL_TASKS_REGISTERED=' + (Number(n) + 1));
  if(validateDocuments(expanded).length)throw Error('Configurable roster must accept expansion without new commercial authority');
  if(!validateDocuments({...documents,status:status.replace('MAX_CONCURRENT_SCOPES=15','MAX_CONCURRENT_SCOPES=16')}).length)
    throw Error('WIP status mutation bypass');
  if(!validateDocuments({...documents,status:status.replace('MAX_OPEN_WORK_BRANCHES=15','MAX_OPEN_WORK_BRANCHES=16')}).length)
    throw Error('WIP branches mutation bypass');
  if(!validateDocuments({...documents,authority:authority.replace('MAX_CONCURRENT_SCOPES=15','MAX_CONCURRENT_SCOPES=16')}).length)
    throw Error('WIP authority mutation bypass');
  for(const [name,mutated] of [
    ['per-task cap', {...documents,authority:authority.replace('MAX_OWNED_SCOPES_PER_TASK=15','MAX_OWNED_SCOPES_PER_TASK=16')}],
    ['active edit cap', {...documents,status:status.replace('MAX_ACTIVE_WRITES_PER_TASK=15','MAX_ACTIVE_WRITES_PER_TASK=16')}],
  ]) if(!validateDocuments(mutated).length)throw Error('WIP mutation bypass: '+name);
  const approved={approverPrincipal:'ALEX',identityVerified:true,permissionVerified:true,
    providerRulesSatisfied:true,nativeApprovalVerified:true,exactHeadVerified:true,
    readbackVerified:true,independenceVerified:true,allContributionsReviewed:true,
    authorAccount:'JAVIER_CHATGPT',approverAccount:'ALEX_CHATGPT',environment:'dev'};
  if(validateApprovalDecision(approved).length)throw Error('Independent approval rejected');
  if(!validateApprovalDecision({...approved,allContributionsReviewed:false}).length)
    throw Error('Approval of unreviewed correction accepted');
  // Selection respects the upcoming execution, not the Task label or list order.
  const when='2026-11-04T11:21:57-06:00';
  const free={account:'JAVIER_CHATGPT',registeredTask:true,authorizedTask:true,
    isEnabled:true,schedulerVerified:true,rruleVerified:true,timezone:'America/Tegucigalpa',
    identityVerified:true,permissionsVerified:true,ownershipReadbackVerified:true,
    ownedOpenScopes:0,activeRun:false,activeEditor:false,activeLease:false,
    nextRunSource:'DERIVED_VERIFIED_RRULE'};
  const candidates=[
    {...free,taskId:'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',nextRunAt:'2026-11-04T12:00:00-06:00'},
    {...free,taskId:'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb',nextRunAt:'2026-11-04T11:30:00-06:00'},
    {...free,taskId:'cccccccccccccccccccccccccccccccc',nextRunAt:'2026-11-04T11:40:00-06:00'}];
  const pick=(xs,acc='JAVIER_CHATGPT')=>selectNextScheduledFreeTask(xs,when,acc);
  if(pick(candidates).taskId!=='bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb')
    throw Error('Closest scheduled free Task not selected');
  if(pick([{...candidates[1],activeRun:true},candidates[0],candidates[2]]).taskId!=='cccccccccccccccccccccccccccccccc')
    throw Error('Active Task chosen for transfer');
  if(pick([{...candidates[1],ownedOpenScopes:1},candidates[0],candidates[2]]).taskId!=='cccccccccccccccccccccccccccccccc')
    throw Error('A completely free Task should be selected over one with scopes');
  if(pick([{...candidates[1],ownedOpenScopes:15},candidates[0],candidates[2]]).taskId!=='cccccccccccccccccccccccccccccccc')
    throw Error('Full Task chosen for handoff');
  for(const [key,value] of [
    ['isEnabled',false],['authorizedTask',false],['registeredTask',false],
    ['permissionsVerified',false],['ownershipReadbackVerified',false],
    ['rruleVerified',false],['schedulerVerified',false],
    ['identityVerified',false],['activeEditor',true],['activeLease',true],
    ['timezone','UTC'],['nextRunSource','UNVERIFIED'],['activeRun',true]]){
    if(pick([{...candidates[1],[key]:value}]).action!=='NO_GO')
      throw Error('Unverified candidate selected: '+key);
  }
  if(pick([{...candidates[1],nextRunAt:'2026-11-04T11:20:00-06:00'}]).action!=='NO_GO')
    throw Error('Past schedule accepted');
  if(pick([{...candidates[1],account:'ALEX_CHATGPT'}]).action!=='NO_GO')
    throw Error('Other account selected');
  if(pick(candidates).requiresAuthorizationAndLeaseReadback!==true)
    throw Error('Ranking improperly grants handoff authorization');
  if(selectNextScheduledFreeTask(candidates,'bad','JAVIER_CHATGPT').action!=='NO_GO')
    throw Error('Invalid clock allowed handoff');
  assertStageReleaseNegativeTests();
  const releasedStage={
    id:'P1-010.B',state:'VALIDANDO',authorAccount:'JAVIER_CHATGPT',
    authorTaskId:'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb',
    originalAuthorTaskId:'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb',
    stageReleaseRequested:true,ownerTaskId:null,stageOwnerTaskId:null,
    reviewReservationTaskId:null,stageReleaseReceiptVerified:true,
    releaseEventId:'stage-release-complete',
    provenancePreserved:true,prHeadReadbackVerified:true,deliveryTestsVerified:true,
    previousLeaseReleased:true,releaseActiveEditorAbsentVerified:true,
    githubReadbackVerified:true,planReadbackVerified:true,bitacoraReadbackVerified:true,
    releaseVersionVerified:true,noConflictingClaim:true,
    reviewerAllowed:true,reviewPermissionVerified:true,reviewerLeaseAvailable:true,
    exactHeadVerified:true,technicalReviewAllowed:true,technicalEvidenceVerified:true,
    prUrl:'https://github.com/solqaryn/Solqaryn/pull/3850'};
  if(selectWork([releasedStage,options[0]],{'P0-012.A':'LISTO'},
    'JAVIER_CHATGPT',new Set(),'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',true).action!=='REVIEW_RESERVATION_CANDIDATE')
    throw Error('Verified stage release not prioritizing exclusive review reservation');
  if(selectWork([releasedStage,options[0]],{'P0-012.A':'LISTO'},
    'JAVIER_CHATGPT',new Set(),releasedStage.authorTaskId,true).action!=='CLAIM')
    throw Error('Original author remained occupied after stage release');
  for(const field of ['planReadbackVerified','previousLeaseReleased','releaseVersionVerified']){
    if(selectWork([{...releasedStage,[field]:false},options[0]],{'P0-012.A':'LISTO'},
      'JAVIER_CHATGPT',new Set(),releasedStage.authorTaskId,true).action==='CLAIM')
      throw Error('Missing stage release receipt freed author: '+field);
  }
  const stageAssigned={...releasedStage,
    stageOwnerTaskId:'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
    reviewReservationTaskId:'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
    stageReviewReservationReadbackVerified:true,
    reviewerLeaseReadbackVerified:true,reviewVersionVerified:true};
  if(selectWork([stageAssigned],{},'JAVIER_CHATGPT',new Set(),
    'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',true).action!=='RESUME_REVIEW')
    throw Error('Reserved reviewer could not continue own stage');
  if(selectWork([stageAssigned],{},'JAVIER_CHATGPT',new Set(),
    'cccccccccccccccccccccccccccccccc',true).action!=='NO_ELIGIBLE')
    throw Error('Reserved review could be stolen');
  if(selectWork([releasedStage],{},'JAVIER_CHATGPT',new Set(),
    releasedStage.authorTaskId,true).action!=='NO_ELIGIBLE')
    throw Error('Author selected own stage review');
  const stageDelivery={...release,stageReleaseRequested:true,
    requesterTaskId:release.actorTaskId,
    workDeliveredVerified:true,deliveryTestsVerified:true,
    branchReadbackVerified:true,provenancePreserved:true,
    previousLeaseReleased:true,releaseLockAcquired:true,
    noConflictingClaim:true,githubReadbackVerified:true,planReadbackVerified:true,
    bitacoraReadbackVerified:true,releaseVersionVerified:true,
    releaseEventId:'stage-delivery-event',stageOwnerTaskId:null,
    reviewReservationTaskId:null};
  if(validateProposedRun(stageDelivery).length)
    throw Error('Verified staged delivery rejected');
  for(const field of ['releaseVersionVerified','previousLeaseReleased','planReadbackVerified']){
    if(!validateProposedRun({...stageDelivery,[field]:false}).length)
      throw Error('Unsafe staged delivery allowed: '+field);
  }
  const stageReservation={...base,operation:'RESERVE_REVIEW',
    stageReleaseRequested:true,reviewedPrRef:release.reviewedPrRef,
    authorTaskId:'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb',
    reviewerTaskId:base.actorTaskId,releaseReadbackVerified:true,
    provenancePreserved:true,reviewLeaseAcquiredExclusively:true,
    reviewVersionVerified:true,noConflictingReviewer:true,
    previousLeaseReleased:true,ownerTransferReadbackVerified:true,
    planReadbackVerified:true,bitacoraReadbackVerified:true,
    reviewerOwnershipReadbackVerified:true,reviewerOwnedScopeCount:0,
    reviewerActiveWriteCount:0,globalOpenScopesReadbackVerified:true,
    globalOpenScopeCount:6,reviewPermissionVerified:true};
  if(validateProposedRun(stageReservation).length)
    throw Error('Verified exclusive reviewer reservation rejected');
  for(const field of ['reviewLeaseAcquiredExclusively','reviewVersionVerified','reviewerOwnershipReadbackVerified']){
    if(!validateProposedRun({...stageReservation,[field]:false}).length)
      throw Error('Unverified reviewer reservation allowed: '+field);
  }
  if(!validateProposedRun({...stageReservation,authorTaskId:base.actorTaskId}).length)
    throw Error('Self-review reservation allowed');
  console.log('SOLQARYN WIP 15/15/15 + STAGE_RELEASE PASS: verified handoff, exclusive reviewer and author freed with readback');
}else{
  console.log('SOLQARYN automation authority PASS');
}
