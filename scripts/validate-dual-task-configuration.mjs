import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { MAX_CONCURRENT_SCOPES, MAX_OPEN_WORK_BRANCHES, MAX_OWNED_SCOPES_PER_TASK, MAX_ACTIVE_WRITES_PER_TASK, selfTestCapacityV13 } from './automation-capacity-v13.mjs';
import { validateAuthority } from './validate-automation-authority.mjs';

const read = p => readFileSync(join(process.cwd(),p),'utf8');
const sources={authority:read('docs/AUTOMATION_AUTHORITY.md'),agents:read('AGENTS.md'),status:read('docs/AUTOMATION_STATUS.md'),scope:read('docs/PROJECT_SCOPE_LOCK.md')};
const ID=/^[a-f0-9]{32}$/i;
const SCOPE=/^(?:P\d+|I8|G0)-\d+\.[A-Z]$/;
const own = (txt,key) => txt.match(new RegExp('^'+key+'=([^\\r\\n]+)$','m'))?.[1];
const authorized = own(sources.authority,'AUTHORIZED_COMMERCIAL_TASK_IDS')?.split(',')||[];
export function validateDocuments(x) {
  const errors=validateAuthority(x);
  if (!String(x.status||'').includes('PHYSICAL_JAVIER_TASKS_ENABLED=0') &&
      !String(x.status||'').includes('PHYSICAL_JAVIER_TASKS_ENABLED=6')) errors.push('unknown physical status');
  return errors;
}
export function dependenciesSatisfied(task,status) {
  return Array.isArray(task?.dependencies) && task.dependencies.every(id=>status[id]==='LISTO');
}
export function developmentDependenciesSatisfied(task,status) {
  return dependenciesSatisfied(task,status);
}

// Live-read evidence of an authorized task run. A dashboard's self-owned flag is relative
// to its CALLER, not a verdict on who owns the scheduled Task or connected credentials.
// Do not turn a definition lookup 404 into a positive authorization or a denial.
const actorOwner = id => ['6adbbc9ad9e88191bea8eb69104afb25','6adcc98a80a48191b71a5156624c21df'].includes(id) ? 'ALEX_CHATGPT' : 'JAVIER_CHATGPT';
const accountGithub = account => account==='JAVIER_CHATGPT' ? ['jmejia','31'].join('') : 'morales35alex';
export function validateExecutorEvidence(e) {
  const issues=[];
  if(!e || !ID.test(e.taskId||'') || !authorized.includes(e.taskId))return {verified:false,issues:['TASK_ID_NOT_ALLOWLISTED']};
  if(e.account!==actorOwner(e.taskId) || e.promptTaskId!==e.taskId ||
     e.nativePeek?.id!==e.taskId || e.nativePeek?.is_enabled!==true ||
     !String(e.nativePeek?.schedule||'').includes('FREQ=HOURLY') ||
     !String(e.nativePeek?.default_timezone||'').includes('America/Tegucigalpa'))
    issues.push('TASK_CONTEXT_OR_OWNER_MISMATCH');
  if(e.githubActor!==accountGithub(e.account) || e.githubRepository!=='solqaryn/Solqaryn' ||
     e.connectedGithubPermissionVerified!==true || e.connectedSheetPermissionVerified!==true ||
     e.planReadbackVerified!==true || e.exactHeadScopeLockVerified!==true ||
     e.localSkillMatchesCanonical!==true)
    issues.push('EFFECTIVE_SOURCE_AUTHORITY_MISSING');
  if(e.providerPermissionDenied===true || e.credentialMismatch===true)
    issues.push('EFFECTIVE_PERMISSION_DENIED');
  // is_self_owned_automation=false is a neutral diagnostic from another agent.
  // definitionGet404 likewise indicates an unavailable tool action; peek must still succeed.
  return {verified:issues.length===0,issues};
}
export function candidateFeasibility(task,capabilities={}) {
  if(task?.requiresLiveProvider===true && capabilities.liveProviderVerified!==true)
    return {possible:false,reason:'LIVE_PROVIDER_NOT_VERIFIED'};
  const required=Array.isArray(task?.requiredTools)?task.requiredTools:[];
  const available=Array.isArray(capabilities.availableTools)?capabilities.availableTools:[];
  const missing=required.filter(t=>!available.includes(t));
  return missing.length?{possible:false,reason:'MISSING_REQUIRED_TOOLS:'+missing.join(',')}:{possible:true};
}

export function selectWork(tasks,status,account,activeReviewIds=new Set(),taskId=null,ownershipReadbackVerified=false,capabilities={}) {
  if (!Array.isArray(tasks) || !ID.test(taskId||'') || !authorized.includes(taskId) ||
      !['JAVIER_CHATGPT','ALEX_CHATGPT'].includes(account)||!ownershipReadbackVerified)
    return {action:'NO_ELIGIBLE',reason:'IDENTITY_OR_READBACK_REQUIRED'};
  const isAlex=['6adbbc9ad9e88191bea8eb69104afb25','6adcc98a80a48191b71a5156624c21df'].includes(taskId);
  if ((isAlex?'ALEX_CHATGPT':'JAVIER_CHATGPT')!==account)
    return {action:'NO_ELIGIBLE',reason:'ACCOUNT_MISMATCH'};
  const work=tasks.filter(t=>t && t.state==='PENDIENTE' && SCOPE.test(t.id||'') &&
    (t.developmentEnabled===true||t.developmentEnabled==='SI') &&
    dependenciesSatisfied(t,status) && t.permissionsVerified===true &&
    t.ownerTaskId==null && t.activeEditorConflict!==true && !t.conflictingClaim);
  const triage=work.map(t=>({task:t,feasibility:candidateFeasibility(t,capabilities)}));
  const next=triage.find(x=>x.feasibility.possible)?.task;
  return next?{action:'CLAIM',task:next,skippedUnfeasible:triage.filter(x=>!x.feasibility.possible).map(x=>({id:x.task.id,reason:x.feasibility.reason})),requiresNativeCurrentReadback:true,
    commitMode:'DIRECT_DEV_FAST_FORWARD',auditCount:4,qualityControls:['A0','A1','A2','A3','A4','A5'],targetListoPerRun:3,noFixedClosureCap:true,continueWhileEligible:true}:
    {action:'NO_ELIGIBLE',reason:work.length?'NO_VERIFIABLE_CAPABILITY_IN_ELIGIBLE_SET':'NO_FREE_ELIGIBLE_SCOPE',inspected:work.length,missingCapabilities:triage.map(x=>({id:x.task.id,reason:x.feasibility.reason}))};
}
export function validateProposedRun(p) {
  const issues=[];
  if (!p||p.repo!=='solqaryn/Solqaryn'||p.branch!=='dev'||p.environment!=='dev'||
    p.sharedCredentials===true||p.legacyReactivate===true) return ['PROJECT_SCOPE'];
  if(p.operation==='READ')return [];
  if(!ID.test(p.actorTaskId||'')||!authorized.includes(p.actorTaskId)||p.mode!=='ACTIVE_DEV'||
    p.authorizationVerified!==true||p.permissionVerified!==true||p.exactHeadVerified!==true||
    !((p.actorNativeOccurrenceVerified===true&&p.dispatchReadbackVerified===true) ||
      (p.executorEvidence?.taskId===p.actorTaskId && validateExecutorEvidence(p.executorEvidence).verified)))
    issues.push('IDENTITY_AND_PERMISSIONS');
  if (!['WRITE','CLOSE','REVIEW','RECOVER_HANDOFF','DELIVER_VALIDANDO'].includes(p.operation))
    issues.push('UNSUPPORTED_OPERATION');
  if(p.operation==='WRITE' && (p.ownerAuthorized!==true||p.writePermissionVerified!==true||
    p.activeEditorConflictOnTarget===true||p.conflictingClaim===true||
    p.branchName&&p.branchName!=='dev'||p.forcePush===true))
    issues.push('UNSAFE_DIRECT_DEV_WRITE');
  if(p.operation==='CLOSE' && (p.commitOnDevVerified!==true||p.staticCodeAuditPassed!==true||p.staticCodeEvidenceVerified!==true||p.a5StatusRecorded!==true||p.functionalAuditPassed!==true||
    p.securityAuditPassed!==true||p.integrationAuditPassed!==true||
    p.postIntegrationAuditPassed!==true||p.sheetReadbackVerified!==true||
    p.ledgerReadbackVerified!==true||p.dependentsRecalculated!==true))
    issues.push('A0_A4_A5_PENDING_AND_READBACK_REQUIRED');
  if(p.operation==='REVIEW'&&(p.reviewMode==='TECHNICAL_CROSS_TASK'&&
    (p.technicalEvidenceVerified!==true||p.formalGithubApprovalRequested===true)))
    issues.push('REVIEW_EVIDENCE');
  return issues;
}
// A5 is a separate independent post-dev readiness gate, not a new blocker of dev LISTO.
export function validatePromotionReadiness(p) {
  if (!p || p.repo!=='solqaryn/Solqaryn' || p.sourceBranch!=='dev' ||
      !['qa','main'].includes(p.targetBranch) || p.ownerAuthorized!==true ||
      p.shaPublishedOnDevVerified!==true || p.a0ToA4Passed!==true ||
      p.a5IndependentAuditPassed!==true || p.a5EvidenceVerified!==true ||
      !ID.test(p.authorTaskId||'') || !ID.test(p.reviewerTaskId||'') ||
      p.authorTaskId===p.reviewerTaskId || !authorized.includes(p.authorTaskId) ||
      !authorized.includes(p.reviewerTaskId) || p.highSeverityFindingsOpen===true)
    return ['A5_INDEPENDENT_AND_PROMOTION_AUTHORITY_REQUIRED'];
  return [];
}
const errors=validateDocuments(sources);
if(errors.length){console.error('V13 DIRECT DEV NO_GO: '+errors.join(' | '));process.exitCode=1;}
else if(process.argv.includes('--self-test')) {
  selfTestCapacityV13();
  const task=authorized[0];
  const scope={id:'I8-002.A',state:'PENDIENTE',developmentEnabled:'SI',
    dependencies:[],permissionsVerified:true};
  const pick=selectWork([scope],{},'JAVIER_CHATGPT',new Set(),task,true);
  if(pick.action!=='CLAIM'||pick.auditCount!==4||pick.qualityControls.length!==6||pick.noFixedClosureCap!==true||pick.continueWhileEligible!==true)throw Error('direct DEV claim test failed');
  const blocked={...scope,id:'I8-007.A',requiredTools:['dotnet']};
  const fallback={...scope,id:'I8-009.A',requiredTools:['node']};
  const feasible=selectWork([blocked,fallback],{},'JAVIER_CHATGPT',new Set(),task,true,{availableTools:['node']});
  if(feasible.action!=='CLAIM' || feasible.task.id!=='I8-009.A' || feasible.skippedUnfeasible.length!==1)
    throw Error('capability triage failed');
  if(selectWork([blocked],{},'JAVIER_CHATGPT',new Set(),task,true,{availableTools:['node']}).reason!=='NO_VERIFIABLE_CAPABILITY_IN_ELIGIBLE_SET')
    throw Error('toolchain fail-closed failed');
  if(selectWork([{...scope,permissionsVerified:false}],{},'JAVIER_CHATGPT',new Set(),task,true).action==='CLAIM')
    throw Error('unsafe scope claim');
  const base={repo:'solqaryn/Solqaryn',branch:'dev',environment:'dev',operation:'CLOSE',
    actorTaskId:task,mode:'ACTIVE_DEV',authorizationVerified:true,permissionVerified:true,
    exactHeadVerified:true,actorNativeOccurrenceVerified:true,dispatchReadbackVerified:true,
    commitOnDevVerified:true,staticCodeAuditPassed:true,staticCodeEvidenceVerified:true,a5StatusRecorded:true,functionalAuditPassed:true,securityAuditPassed:true,
    integrationAuditPassed:true,postIntegrationAuditPassed:true,sheetReadbackVerified:true,
    ledgerReadbackVerified:true,dependentsRecalculated:true};
  if(validateProposedRun(base).length)throw Error('valid closure rejected');
  const evidence={taskId:task,promptTaskId:task,account:'JAVIER_CHATGPT',
    nativePeek:{id:task,is_enabled:true,schedule:'FREQ=HOURLY;BYMINUTE=0;BYSECOND=0',default_timezone:'America/Tegucigalpa',is_self_owned_automation:false},
    definitionGet404:true,githubActor:['jmejia','31'].join(''),githubRepository:'solqaryn/Solqaryn',
    connectedGithubPermissionVerified:true,connectedSheetPermissionVerified:true,
    planReadbackVerified:true,exactHeadScopeLockVerified:true,localSkillMatchesCanonical:true};
  if(!validateExecutorEvidence(evidence).verified)throw Error('valid cross verified identity rejected');
  if(validateProposedRun({...base,actorNativeOccurrenceVerified:false,dispatchReadbackVerified:false,executorEvidence:evidence}).length)
    throw Error('false self_owned/404 blocked cross verified identity');
  for(const bad of [{...evidence,account:'ALEX_CHATGPT'},{...evidence,providerPermissionDenied:true},
    {...evidence,nativePeek:{...evidence.nativePeek,id:authorized[1]}},
    {...evidence,connectedGithubPermissionVerified:false}])
    if(validateExecutorEvidence(bad).verified)throw Error('false identity allowed');
  for(const bad of [{...base,securityAuditPassed:false},{...base,staticCodeAuditPassed:false},{...base,a5StatusRecorded:false},{...base,commitOnDevVerified:false},
    {...base,sheetReadbackVerified:false}])
    if(!validateProposedRun(bad).length)throw Error('false LISTO allowed');
  const promotion={repo:'solqaryn/Solqaryn',sourceBranch:'dev',targetBranch:'qa',
    ownerAuthorized:true,shaPublishedOnDevVerified:true,a0ToA4Passed:true,
    a5IndependentAuditPassed:true,a5EvidenceVerified:true,
    authorTaskId:authorized[0],reviewerTaskId:authorized[1],highSeverityFindingsOpen:false};
  if(validatePromotionReadiness(promotion).length)throw Error('valid independent review rejected');
  for(const bad of [{...promotion,a5IndependentAuditPassed:false},
    {...promotion,reviewerTaskId:authorized[0]},{...promotion,ownerAuthorized:false}])
    if(!validatePromotionReadiness(bad).length)throw Error('invalid promotion accepted');
  console.log('V13 DIRECT DEV A0-A5 evidence gates and independent review PASS');
}
