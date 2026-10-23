import { readFileSync } from 'node:fs';
import { join } from 'node:path';

const read = path => readFileSync(join(process.cwd(), path), 'utf8');
const authority = read('docs/AUTOMATION_AUTHORITY.md');
const status = read('docs/AUTOMATION_STATUS.md');
const agents = read('AGENTS.md');
const scope = read('docs/PROJECT_SCOPE_LOCK.md');

const expected = {
  CONTRACT_VERSION: '1.2',
  PROJECT_SCOPE_LOCK: 'STRICT',
  PROJECT_ID: 'SOLQARYN',
  REPOSITORY: 'solqaryn/Solqaryn',
  BRANCH: 'dev',
  PILOT_TASK_LIMIT: '2',
  PILOT_PRIMARY_TASKS: '1',
  PILOT_SECONDARY_TASKS: '1',
  PRIMARY_TASK_ROLE: 'PRIMARY_01',
  SECONDARY_TASK_ROLE: 'SECONDARY_02',
  PRIMARY_TASK_ACCOUNT: 'JAVIER_CHATGPT',
  SECONDARY_TASK_ACCOUNT: 'ALEX_CHATGPT',
  AUTOMATION_RUNTIME: 'ACTIVE_DEV',
  SCHEDULED_CHATGPT_TASKS_AUTHORIZED: '1',
  VAEP_DISPATCH_AUTHORIZED: 'FALSE',
  JULES_WORKERS_AUTHORIZED: '0',
};

const expectedApprovalPolicy = {
  APPROVAL_POOL_MEMBERS: 'JAVIER,ALEX,SOLQARYN_PLATFORM',
  APPROVAL_SELECTION: 'ANY_AVAILABLE_ELIGIBLE',
  APPROVAL_NO_SELF_REVIEW: 'TRUE',
  APPROVAL_PROVIDER_GATES_MANDATORY: 'TRUE',
  APPROVAL_HUMAN_WHEN_REQUIRED: 'TRUE',
  APPROVAL_AVAILABILITY_MUST_BE_VERIFIED: 'TRUE',
};

function readKey(source, key) {
  const matches = [...source.matchAll(new RegExp('^' + key + '=([^\\r\\n]+)$', 'gm'))];
  return matches.length === 1 ? matches[0][1] : null;
}

export function validateDocuments(sources) {
  const { authority: a, status: s, agents: g, scope: c } = sources;
  const errors = [];
  for (const [key, value] of Object.entries(expected)) {
    if (readKey(a, key) !== value) errors.push('authority ' + key);
  }
  for (const [key, value] of Object.entries(expectedApprovalPolicy)) {
    if (readKey(a, key) !== value) errors.push('authority approval ' + key);
  }
  for (const key of ['PROJECT_SCOPE_LOCK', 'PILOT_TASK_LIMIT', 'PILOT_PRIMARY_TASKS', 'PILOT_SECONDARY_TASKS', 'PRIMARY_TASK_ROLE', 'SECONDARY_TASK_ROLE', 'PRIMARY_TASK_ACCOUNT', 'SECONDARY_TASK_ACCOUNT', 'AUTOMATION_RUNTIME', 'SCHEDULED_CHATGPT_TASKS_AUTHORIZED', 'VAEP_DISPATCH_AUTHORIZED', 'JULES_WORKERS_AUTHORIZED']) {
    if (readKey(s, key) !== expected[key]) errors.push('status ' + key);
  }
  if (!g.includes('DUAL_TASK_DESIGN=PRIMARY_01_JAVIER_SECONDARY_02_ALEX')) errors.push('agents topology');
  if (!c.includes('PILOT_TASK_LIMIT=2')) errors.push('scope task cap');
  for (const marker of ['PROPIETARIA_INSTITUCIONAL', 'EJECUTOR_PRINCIPAL_01', 'EJECUTOR_PRINCIPAL_02', 'HUMAN_ROLE_PARITY=TRUE', 'PRIMARY_ACCOUNT_INDEPENDENT=TRUE', 'SECONDARY_ACCOUNT_INDEPENDENT=TRUE', 'TASKS_NOT_CREATED_BY_AMENDMENT=TRUE', 'REVIEW_INDEPENDENCE_REQUIRED=TRUE', 'NO_CROSS_ACCOUNT_CREDENTIALS=TRUE', 'NO_LEGACY_REACTIVATION=TRUE', 'READ_ONLY', 'ACTIVE_DEV', 'AUTHORITY_GUARD', 'PLAN_SELECTOR', 'DEV_OPERATOR', 'REVIEW_GATE', 'AUDIT_REPORTER', '1.123', '15.722', 'NO_VERIFICADO']) {
    if (!a.includes(marker)) errors.push('authority marker ' + marker);
  }
  if (!a.includes('JAVIER_CHATGPT')) errors.push('primary account label');
  if (!a.includes('ALEX_CHATGPT')) errors.push('secondary account label');
  return errors;
}

const ownerByRole = { PRIMARY_01: 'JAVIER_CHATGPT', SECONDARY_02: 'ALEX_CHATGPT' };

const knownRunId = v => typeof v === 'string' && v.trim() !== '' && v !== 'NO_VERIFICADO';
const validEvidenceRef = v => typeof v === 'string' && /^https:\/\//.test(v);
// Validates provided proof metadata only; a human must verify its native ChatGPT provenance.
export function verifiedScheduledReadOnlyProof(proof, slot) {
  if (!slot || !proof || typeof proof !== 'object') return false;
  return proof.kind === 'CHATGPT_SCHEDULED_TASK_RESULT' &&
    proof.taskId === slot.taskId && proof.account === slot.account &&
    proof.source === 'OWNER_CHATGPT_TASK_HISTORY' &&
    proof.scheduled === true && proof.completed === true &&
    proof.ownerReadbackVerified === true && proof.resultReadbackVerified === true &&
    proof.independentReviewerVerified === true &&
    typeof proof.reviewerAccount === 'string' &&
    proof.reviewerAccount !== slot.account &&
    validEvidenceRef(proof.evidenceRef) &&
    proof.evidenceRef !== proof.taskShareUrl &&
    typeof proof.outputSha256 === 'string' &&
    /^[a-f0-9]{64}$/i.test(proof.outputSha256) &&
    typeof proof.timestampUtc === 'string' &&
    !Number.isNaN(Date.parse(proof.timestampUtc));
}
export function validateProposedRun(p) {
  const errors = [];
  const slots = Array.isArray(p.slots) ? p.slots : [];
  if (p.repo !== 'solqaryn/Solqaryn' || p.branch !== 'dev') errors.push('identity');
  if (p.pilotMax !== 2 || p.primaryMax !== 1 || p.secondaryMax !== 1) errors.push('cap');
  if (slots.length > 2) errors.push('too_many_tasks');
  if (p.stage === 'DUAL_READ_ONLY' && slots.length !== 2) errors.push('missing_task');
  const roles = new Set(), accounts = new Set(), physicalIds = new Set();
  for (const slot of slots) {
    if (!ownerByRole[slot.role]) errors.push('unknown_role');
    if (slot.account !== ownerByRole[slot.role]) errors.push('wrong_account');
    if (roles.has(slot.role)) errors.push('duplicate_role');
    if (accounts.has(slot.account)) errors.push('shared_account');
    if (!slot.taskId || slot.taskId === 'NO_VERIFICADO') errors.push('unknown_task_id');
    if (physicalIds.has(slot.taskId)) errors.push('duplicate_task_id');
    if (!slot.permissionsVerified || !slot.schedulerVerified) errors.push('unverified_task');
    roles.add(slot.role); accounts.add(slot.account); physicalIds.add(slot.taskId);
  }
  if (p.ownerChanged) errors.push('institutional_owner_transfer');
  if (p.sharedCredentials) errors.push('credential_sharing');
  if (p.legacyReactivate) errors.push('legacy_reactivation');
  if (p.environment !== 'dev') errors.push('forbidden_environment');
  if (p.operation && p.operation !== 'NONE') {
    if (p.mode === 'RETIRED' || p.mode === 'PREPARING' || p.mode === 'PAUSED' || p.mode === 'BLOCKED' || p.mode === 'DISABLED') errors.push('disabled_operation');
    if (!p.authorizationVerified) errors.push('authorization');
    const actorSlot = slots.find(s => s.role === p.actorRole && s.account === p.actorAccount);
    const nativeRunVerified = knownRunId(p.runId) && p.nativeRunIdVerified === true;
    // Scoped GitHub work is evidenced by its branch/PR, without provider RUN_ID.
    const artifactRunVerified = p.stage === 'ACTIVE_DEV_PILOT' && p.mode === 'ACTIVE_DEV' &&
      p.actorRole === 'PRIMARY_01' && p.operation === 'WRITE' && p.ownerAuthorized === true &&
      typeof p.scopeId === 'string' && /^P[0-9]+-[0-9]+\.[A-Z]$/.test(p.scopeId) &&
      typeof p.branchName === 'string' && p.branchName.startsWith('work/solqaryn-') &&
      p.exactHeadVerified === true && p.exclusiveScopeVerified === true && !p.conflictingClaim;
    const artifactReadVerified = p.stage === 'ACTIVE_DEV_PILOT' && p.mode === 'ACTIVE_DEV' && p.operation === 'READ';
    const artifactReviewVerified = p.stage === 'ACTIVE_DEV_PILOT' && p.mode === 'ACTIVE_DEV' && p.operation === 'REVIEW' &&
      typeof p.reviewedPrRef === 'string' && /^https:\/\/github\.com\/solqaryn\/Solqaryn\/pull\/[0-9]+$/.test(p.reviewedPrRef);
    const equivalentRunVerified = p.mode === 'READ_ONLY' &&
      p.stage === 'DUAL_READ_ONLY' && (p.operation === 'READ' || p.operation === 'REVIEW') &&
      verifiedScheduledReadOnlyProof(p.runEvidence, actorSlot);
    if ((!nativeRunVerified && !equivalentRunVerified && !artifactRunVerified && !artifactReadVerified && !artifactReviewVerified) ||
        ((p.operation === 'WRITE' || p.stage !== 'ACTIVE_DEV_PILOT') && !p.evidence))
      errors.push('missing_evidence');
    if (!p.actorRole || !ownerByRole[p.actorRole]) errors.push('unknown_actor');
    if (p.actorAccount !== ownerByRole[p.actorRole]) errors.push('actor_account_mismatch');
    if (!slots.some(s => s.role === p.actorRole && s.account === p.actorAccount)) errors.push('actor_not_registered');
    if (p.operation === 'WRITE') {
      if (p.mode !== 'ACTIVE_DEV') errors.push('write_mode');
      if (!p.writePermissionVerified || !p.exclusiveScopeVerified || p.conflictingClaim) errors.push('unsafe_write');
      if (!p.idempotencyKey || !p.exactHeadVerified) errors.push('unsafe_idempotency');
      if (p.actorRole === 'SECONDARY_02' && !p.secondaryWriteExplicitlyApproved) errors.push('secondary_write_not_authorized');
    }
    if (p.operation === 'REVIEW') {
      if (p.authorAccount === p.actorAccount) errors.push('self_review');
      if (!p.independentReviewerVerified) errors.push('reviewer_unverified');
      const authorSlot = slots.find(s => s.account === p.authorAccount);
      const nativeReviewedVerified = knownRunId(p.reviewedRunId) && p.reviewedRunIdVerified === true;
      const equivalentReviewedVerified = p.mode === 'READ_ONLY' &&
        p.stage === 'DUAL_READ_ONLY' &&
        verifiedScheduledReadOnlyProof(p.reviewedEvidence, authorSlot);
      if (!nativeReviewedVerified && !equivalentReviewedVerified && !artifactReviewVerified)
        errors.push('review_without_run');
      if (equivalentRunVerified && equivalentReviewedVerified &&
          p.runEvidence.evidenceRef === p.reviewedEvidence.evidenceRef)
        errors.push('reused_evidence');
    }
    if (!['READ','WRITE','REVIEW'].includes(p.operation)) errors.push('unknown_operation');
  }
  return errors;
}

// Policy-level eligibility only: no action or provider approval is performed here.
const approvedPrincipals = new Set(['JAVIER', 'ALEX', 'SOLQARYN_PLATFORM']);
export function validateApprovalDecision(p) {
  const errors = [];
  if (!approvedPrincipals.has(p.approverPrincipal)) errors.push('unknown_approver');
  if (!p.provider || !p.objectId || !p.revisionOrHead) errors.push('unscoped_approval');
  if (p.environment !== 'dev' && p.environment !== 'general') errors.push('unauthorized_environment');
  if (!p.identityVerified || !p.accountVerified || !p.permissionVerified) errors.push('identity_or_permission');
  if (!p.availabilityVerified || !p.available) errors.push('unavailable_approver');
  if (!p.providerRulesSatisfied || !p.requiredApprovalsSatisfied || !p.scopeAuthorized) errors.push('provider_gate');
  if (!p.nativeApprovalVerified && p.nativeApprovalRequired) errors.push('native_approval_missing');
  if (p.humanApprovalRequired && !p.humanActorVerified) errors.push('human_required');
  if (p.approverPrincipal === 'SOLQARYN_PLATFORM') {
    if (!p.institutionalAuthorityVerified || !p.responsibleHumanId) errors.push('institutional_representation');
  }
  if (p.independentReviewRequired &&
      (!p.independenceVerified || !p.authorHumanId || !p.responsibleHumanId ||
       p.authorHumanId === p.responsibleHumanId)) errors.push('not_independent');
  if (p.provider === 'github' &&
      (!p.nativeApprovalVerified || !p.exactHeadVerified ||
       !p.authorAccount || p.authorAccount === p.approverAccount)) errors.push('github_review');
  if (p.sharedCredentials || p.impersonated || p.accountBorrowed) errors.push('impersonation');
  if (!p.approvalId || !p.auditEvidence || !p.readbackVerified) errors.push('missing_audit');
  return errors;
}

const sources = { authority, status, agents, scope };
const failures = validateDocuments(sources);
if (failures.length) {
  console.error('DUAL TASK AUTHORITY NO_GO: ' + failures.join(' | '));
  process.exitCode = 1;
} else if (process.argv.includes('--self-test')) {
  const base = {
    repo: 'solqaryn/Solqaryn', branch: 'dev', pilotMax: 2, primaryMax: 1, secondaryMax: 1,
    stage: 'DUAL_READ_ONLY', environment: 'dev', mode: 'READ_ONLY', operation: 'REVIEW',
    authorizationVerified: true, runId: 'RUN-READ-01', nativeRunIdVerified:true, evidence: 'verified:readback',
    actorRole: 'SECONDARY_02', actorAccount: 'ALEX_CHATGPT', authorAccount: 'JAVIER_CHATGPT',
    reviewedRunId: 'RUN-PRIMARY-01', reviewedRunIdVerified:true, independentReviewerVerified: true,
    slots: [
      { role:'PRIMARY_01', account:'JAVIER_CHATGPT', taskId:'TASK-P1', permissionsVerified:true, schedulerVerified:true },
      { role:'SECONDARY_02', account:'ALEX_CHATGPT', taskId:'TASK-S1', permissionsVerified:true, schedulerVerified:true },
    ],
  };
  const clone = x => structuredClone(x);
  const mutants = [
    ['wrong repo', p => p.repo='other/Repo'],
    ['wrong branch', p => p.branch='main'],
    ['three tasks', p => p.slots.push({...p.slots[0], taskId:'TASK-P2'})],
    ['one task', p => p.slots.pop()],
    ['wrong count', p => p.pilotMax=3],
    ['wrong role', p => p.slots[0].role='SUPERVISOR_OLD'],
    ['same role twice', p => p.slots[1].role='PRIMARY_01'],
    ['wrong creator', p => p.slots[1].account='JAVIER_CHATGPT'],
    ['same account', p => p.slots[0].account='ALEX_CHATGPT'],
    ['fabricated id', p => p.slots[0].taskId='NO_VERIFICADO'],
    ['duplicate ids', p => p.slots[1].taskId='TASK-P1'],
    ['permissions missing', p => p.slots[1].permissionsVerified=false],
    ['scheduler missing', p => p.slots[1].schedulerVerified=false],
    ['owner hijack', p => p.ownerChanged=true],
    ['credentials shared', p => p.sharedCredentials=true],
    ['legacy recovery', p => p.legacyReactivate=true],
    ['prod operation', p => p.environment='prod'],
    ['runtime retired', p => p.mode='RETIRED'],
    ['no authorization', p => p.authorizationVerified=false],
    ['no evidence', p => p.evidence=null],
    ['fake actor', p => p.actorAccount='JAVIER_CHATGPT'],
    ['self review', p => p.authorAccount='ALEX_CHATGPT'],
    ['missing reviewer', p => p.independentReviewerVerified=false],
    ['missing reviewed run', p => p.reviewedRunId=null],
    ['write in read-only', p => p.operation='WRITE'],
    ['write conflicting scope', p => {p.operation='WRITE';p.mode='ACTIVE_DEV';p.writePermissionVerified=true;p.conflictingClaim=true;}],
    ['secondary write without permission', p => {p.operation='WRITE';p.mode='ACTIVE_DEV';p.writePermissionVerified=true;p.exclusiveScopeVerified=true;p.idempotencyKey='KEY';p.exactHeadVerified=true;}],
  ];
  if (validateProposedRun(base).length) throw Error('Safe simulation must pass');

  const positiveProof = (role, taskId, account, reviewerAccount, ref, digest) => ({
    kind: 'CHATGPT_SCHEDULED_TASK_RESULT', taskId, account,
    source: 'OWNER_CHATGPT_TASK_HISTORY', scheduled: true, completed: true,
    ownerReadbackVerified: true, resultReadbackVerified: true,
    independentReviewerVerified: true, reviewerAccount,
    evidenceRef: ref, outputSha256: digest, timestampUtc: '2026-10-23T21:04:34Z',
  });
  const proofPrimary = positiveProof('PRIMARY_01', 'TASK-P1', 'JAVIER_CHATGPT', 'ALEX_CHATGPT', 'https://example.test/primary-run', 'a'.repeat(64));
  const proofSecondary = positiveProof('SECONDARY_02', 'TASK-S1', 'ALEX_CHATGPT', 'JAVIER_CHATGPT', 'https://example.test/secondary-run', 'b'.repeat(64));
  const equivalent = clone(base);
  equivalent.runId = 'NO_VERIFICADO';
  equivalent.reviewedRunId = 'NO_VERIFICADO';
  equivalent.runEvidence = proofSecondary;
  equivalent.reviewedEvidence = proofPrimary;
  if (validateProposedRun(equivalent).length)
    throw Error('Verified individual scheduled read-only proofs must pass when native RUN_ID unavailable');
  const evidenceMutants = [
    ['missing proof', p => p.runEvidence = null],
    ['wrong task', p => p.runEvidence.taskId = 'OTHER-TASK'],
    ['wrong owner', p => p.runEvidence.account = 'JAVIER_CHATGPT'],
    ['manual run', p => p.runEvidence.scheduled = false],
    ['unfinished run', p => p.runEvidence.completed = false],
    ['unauthenticated history', p => p.runEvidence.ownerReadbackVerified = false],
    ['unverified result', p => p.runEvidence.resultReadbackVerified = false],
    ['unverified reviewer', p => p.runEvidence.independentReviewerVerified = false],
    ['self reviewed', p => p.runEvidence.reviewerAccount = 'ALEX_CHATGPT'],
    ['fake share', p => p.runEvidence.evidenceRef = 'not-a-source'],
    ['share task not run', p => p.runEvidence.taskShareUrl = p.runEvidence.evidenceRef],
    ['missing output digest', p => p.runEvidence.outputSha256 = ''],
    ['missing reviewed proof', p => p.reviewedEvidence = null],
    ['reviewed same evidence', p => p.reviewedEvidence.evidenceRef = p.runEvidence.evidenceRef],
    ['equivalent used to write', p => p.operation = 'WRITE'],
    ['equivalent used active dev', p => p.mode = 'ACTIVE_DEV'],
  ];
  for (const [name, mutate] of evidenceMutants) {
    const test = clone(equivalent); mutate(test);
    if (!validateProposedRun(test).length)
      throw Error('Equivalence negative test did not reject: ' + name);
  }

  if (validateProposedRun({...base, stage:'PREPARING', mode:'RETIRED', operation:'NONE', slots:[]}).length) throw Error('Safe zero-Task preparing state must pass');
  for (const [name, mutate] of mutants) {
    const test = clone(base); mutate(test);
    if (!validateProposedRun(test).length) throw Error('Fail-closed test did not reject: ' + name);
  }
  const documentMutants = [
    ['cap', {authority:authority.replace('PILOT_TASK_LIMIT=2','PILOT_TASK_LIMIT=3')}],
    ['role', {authority:authority.replace('PRIMARY_TASK_ROLE=PRIMARY_01','PRIMARY_TASK_ROLE=SECONDARY_02')}],
    ['owner', {authority:authority.replace('SECONDARY_TASK_ACCOUNT=ALEX_CHATGPT','SECONDARY_TASK_ACCOUNT=JAVIER_CHATGPT')}],
    ['runtime', {status:status.replace('AUTOMATION_RUNTIME=ACTIVE_DEV','AUTOMATION_RUNTIME=RETIRED')}],
    ['authorized task', {status:status.replace('SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1','SCHEDULED_CHATGPT_TASKS_AUTHORIZED=2')}],
    ['human parity', {authority:authority.replace('HUMAN_ROLE_PARITY=TRUE','HUMAN_ROLE_PARITY=FALSE')}],
    ['revived previous dispatch', {status:status.replace('VAEP_DISPATCH_AUTHORIZED=FALSE','VAEP_DISPATCH_AUTHORIZED=TRUE')}],
  ];
  for (const [name, fragment] of documentMutants) {
    if (!validateDocuments({...sources,...fragment}).length) throw Error('Document negative test did not reject: ' + name);
  }
  const baseApproval = {
    approverPrincipal: 'ALEX', approverAccount: 'morales35alex',
    responsibleHumanId: 'alex', authorHumanId: 'javier', authorAccount: 'github-javier-93171442',
    provider: 'github', objectId: 'PR-EXAMPLE', revisionOrHead: 'HEAD-EXAMPLE',
    environment: 'dev', identityVerified: true, accountVerified: true,
    permissionVerified: true, availabilityVerified: true, available: true,
    providerRulesSatisfied: true, requiredApprovalsSatisfied: true, scopeAuthorized: true,
    nativeApprovalRequired: true, nativeApprovalVerified: true,
    humanApprovalRequired: true, humanActorVerified: true, independentReviewRequired: true,
    independenceVerified: true, exactHeadVerified: true, approvalId: 'APP-EXAMPLE',
    auditEvidence: 'verified-readback', readbackVerified: true,
  };
  if (validateApprovalDecision(baseApproval).length) throw Error('Eligible Alex must pass');
  if (validateApprovalDecision({...baseApproval,
      approverPrincipal: 'JAVIER', approverAccount:'github-javier-93171442',
      responsibleHumanId:'javier', authorHumanId:'alex', authorAccount:'morales35alex'}).length)
    throw Error('Eligible Javier must pass');
  if (validateApprovalDecision({...baseApproval,
      approverPrincipal: 'SOLQARYN_PLATFORM', approverAccount:'solqaryn-platform',
      responsibleHumanId:'independent-institutional-representative',
      institutionalAuthorityVerified:true}).length)
    throw Error('Eligible independent institutional signatory must pass');
  const approvalMutants = [
    ['unknown approver', p=>p.approverPrincipal='UNKNOWN'],
    ['unverified identity', p=>p.identityVerified=false],
    ['unverified account', p=>p.accountVerified=false],
    ['revoked permission', p=>p.permissionVerified=false],
    ['unverified availability', p=>p.availabilityVerified=false],
    ['approver absent', p=>p.available=false],
    ['provider rules bypass', p=>p.providerRulesSatisfied=false],
    ['multiple approvals missing', p=>p.requiredApprovalsSatisfied=false],
    ['unapproved scope', p=>p.scopeAuthorized=false],
    ['native provider approval absent', p=>p.nativeApprovalVerified=false],
    ['human signatory absent', p=>p.humanActorVerified=false],
    ['self review same human', p=>p.responsibleHumanId='javier'],
    ['independence not verified', p=>p.independenceVerified=false],
    ['self review github account', p=>p.approverAccount='github-javier-93171442'],
    ['stale github head', p=>p.exactHeadVerified=false],
    ['shared credentials', p=>p.sharedCredentials=true],
    ['impersonated', p=>p.impersonated=true],
    ['production environment', p=>p.environment='prod'],
    ['qa environment without authorization', p=>p.environment='qa'],
    ['no object reference', p=>p.objectId=''],
    ['no approval evidence', p=>p.auditEvidence=''],
    ['no readback', p=>p.readbackVerified=false],
    ['no approval id', p=>p.approvalId=''],
    ['institutional account without human', p=>{p.approverPrincipal='SOLQARYN_PLATFORM';p.approverAccount='solqaryn-platform';p.responsibleHumanId='';p.institutionalAuthorityVerified=true;}],
    ['institutional account unverified', p=>{p.approverPrincipal='SOLQARYN_PLATFORM';p.approverAccount='solqaryn-platform';p.responsibleHumanId='independent';p.institutionalAuthorityVerified=false;}],
    ['institutional same human as author', p=>{p.approverPrincipal='SOLQARYN_PLATFORM';p.approverAccount='solqaryn-platform';p.responsibleHumanId='javier';p.institutionalAuthorityVerified=true;}],
  ];
  for (const [name, mutate] of approvalMutants) {
    const test = clone(baseApproval); mutate(test);
    if (!validateApprovalDecision(test).length) throw Error('Approval gate must reject: ' + name);
  }
  const approvalDocMutants = [
    ['approver removed', {authority:authority.replace('APPROVAL_POOL_MEMBERS=JAVIER,ALEX,SOLQARYN_PLATFORM','APPROVAL_POOL_MEMBERS=JAVIER')}],
    ['self review enabled', {authority:authority.replace('APPROVAL_NO_SELF_REVIEW=TRUE','APPROVAL_NO_SELF_REVIEW=FALSE')}],
    ['native provider gates disabled', {authority:authority.replace('APPROVAL_PROVIDER_GATES_MANDATORY=TRUE','APPROVAL_PROVIDER_GATES_MANDATORY=FALSE')}],
    ['availability assumption', {authority:authority.replace('APPROVAL_AVAILABILITY_MUST_BE_VERIFIED=TRUE','APPROVAL_AVAILABILITY_MUST_BE_VERIFIED=FALSE')}],
  ];
  for (const [name, fragment] of approvalDocMutants) {
    if (!validateDocuments({...sources,...fragment}).length) throw Error('Approval document gate must reject: ' + name);
  }
  const pilot = {...base,stage:'ACTIVE_DEV_PILOT',mode:'ACTIVE_DEV',operation:'WRITE',
    actorRole:'PRIMARY_01',actorAccount:'JAVIER_CHATGPT',ownerAuthorized:true,
    scopeId:'P0-012.A',branchName:'work/solqaryn-P0-012-A',
    writePermissionVerified:true,exclusiveScopeVerified:true,exactHeadVerified:true,
    conflictingClaim:false,idempotencyKey:'P0-012.A:exact-head',runId:'NO_VERIFICADO',
    nativeRunIdVerified:false,evidence:'github-branch-readback'};
  if(validateProposedRun(pilot).length)throw Error('ACTIVE_DEV Primary pilot requires only GitHub artifact, not RUN_ID');
  const pilotNegatives=[
    ['wrong writer',p=>{p.actorRole='SECONDARY_02';p.actorAccount='ALEX_CHATGPT';}],
    ['missing owner consent',p=>p.ownerAuthorized=false],
    ['wrong branch',p=>p.branchName='main'],
    ['wrong scope',p=>p.scopeId=''],
    ['scope collision',p=>p.conflictingClaim=true],
    ['stale HEAD',p=>p.exactHeadVerified=false],
    ['lost permission',p=>p.writePermissionVerified=false],
    ['prod',p=>p.environment='prod']];
  for(const [name,mutate] of pilotNegatives){const p=clone(pilot);mutate(p);
    if(!validateProposedRun(p).length)throw Error('Pilot safety not enforced: '+name);}
  const review={...base,stage:'ACTIVE_DEV_PILOT',mode:'ACTIVE_DEV',operation:'REVIEW',
    runId:'NO_VERIFICADO',reviewedRunId:'NO_VERIFICADO',
    reviewedPrRef:'https://github.com/solqaryn/Solqaryn/pull/3733'};
  if(validateProposedRun(review).length)throw Error('Review by PR must not require native run ID');
  console.log('Scoped DEV pilot PASS; native RUN_ID optional; 8 safety negatives');
  console.log('Flexible approval policy PASS: Javier, Alex, SOLQARYN Platform; ' + (approvalMutants.length + approvalDocMutants.length) + ' fail-closed negative tests');
  console.log(`Dual account authority PASS: 1 Primary Javier + 1 Secondary Alex, one authorized writer, ${mutants.length + documentMutants.length} dual-Task negative tests`);
} else {
  console.log('Dual account authority PASS (one Primary authorized for DEV)');
}
