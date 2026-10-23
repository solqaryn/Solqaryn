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
  AUTOMATION_RUNTIME: 'RETIRED',
  SCHEDULED_CHATGPT_TASKS_AUTHORIZED: '0',
  VAEP_DISPATCH_AUTHORIZED: 'FALSE',
  JULES_WORKERS_AUTHORIZED: '0',
};

const expectedApproval = {
  APPROVAL_ROUTING: 'FIRST_AVAILABLE_ELIGIBLE',
  APPROVAL_CANDIDATES: 'JAVIER,ALEX,SOLQARYN_PLATFORM',
  APPROVAL_INDEPENDENCE_REQUIRED: 'TRUE',
  APPROVAL_SELF_REVIEW: 'DENY',
  APPROVAL_PLATFORM_OPERATOR_VERIFICATION: 'REQUIRED',
  APPROVAL_TASK_REVIEW_COUNTS_AS_GITHUB_APPROVAL: 'FALSE',
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
  for (const [key, value] of Object.entries(expectedApproval)) {
    if (readKey(a, key) !== value) errors.push('authority approval ' + key);
  }
  if (!g.includes('APPROVAL_ROUTING=FIRST_AVAILABLE_ELIGIBLE')) errors.push('agents approval routing');
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


const approvedReviewerRoles = new Set(['JAVIER', 'ALEX', 'SOLQARYN_PLATFORM']);

// This is a pure eligibility check, not a real-time presence detector,
// automatic GitHub approval, or authorization to use another account.
export function firstAvailableIndependentApprover(request) {
  if (!request || !approvedReviewerRoles.has(request.authorActor) ||
      typeof request.authorPersonId !== 'string' || !request.authorPersonId.trim()) return null;
  const candidates = Array.isArray(request.candidates) ? request.candidates : [];
  for (const candidate of candidates) {
    if (!candidate || !approvedReviewerRoles.has(candidate.actor)) continue;
    if (candidate.actor === request.authorActor) continue;
    if (typeof candidate.personId !== 'string' || !candidate.personId.trim()) continue;
    if (candidate.personId === request.authorPersonId) continue;
    if (candidate.available !== true || candidate.identityVerified !== true ||
        candidate.permissionVerified !== true || candidate.independenceVerified !== true ||
        candidate.scopeAuthorized !== true || candidate.sharedCredentials === true) continue;
    if (candidate.actor === 'SOLQARYN_PLATFORM' &&
        candidate.institutionalOperatorVerified !== true) continue;
    return candidate.actor;
  }
  return null;
}

const ownerByRole = { PRIMARY_01: 'JAVIER_CHATGPT', SECONDARY_02: 'ALEX_CHATGPT' };
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
    if (!p.runId || !p.evidence) errors.push('missing_evidence');
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
      if (!p.reviewedRunId) errors.push('review_without_run');
    }
    if (!['READ','WRITE','REVIEW'].includes(p.operation)) errors.push('unknown_operation');
  }
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
    authorizationVerified: true, runId: 'RUN-READ-01', evidence: 'verified:readback',
    actorRole: 'SECONDARY_02', actorAccount: 'ALEX_CHATGPT', authorAccount: 'JAVIER_CHATGPT',
    reviewedRunId: 'RUN-PRIMARY-01', independentReviewerVerified: true,
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
  if (validateProposedRun({...base, stage:'PREPARING', mode:'RETIRED', operation:'NONE', slots:[]}).length) throw Error('Safe zero-Task preparing state must pass');
  for (const [name, mutate] of mutants) {
    const test = clone(base); mutate(test);
    if (!validateProposedRun(test).length) throw Error('Fail-closed test did not reject: ' + name);
  }
  const eligible = (actor, personId, institutionalOperatorVerified = false) => ({
    actor, personId, institutionalOperatorVerified,
    available: true, identityVerified: true, permissionVerified: true,
    independenceVerified: true, scopeAuthorized: true, sharedCredentials: false,
  });
  const selection = {
    authorActor: 'JAVIER',
    authorPersonId: 'human-javier',
    candidates: [
      eligible('ALEX', 'human-alex'),
      eligible('SOLQARYN_PLATFORM', 'human-platform-reviewer', true),
    ],
  };
  if (firstAvailableIndependentApprover(selection) !== 'ALEX') throw Error('First available independent reviewer selection fails');
  if (firstAvailableIndependentApprover({
    ...selection, candidates: [{...selection.candidates[0], available:false}, selection.candidates[1]]
  }) !== 'SOLQARYN_PLATFORM') throw Error('Independent institutional fallback selection fails');
  if (firstAvailableIndependentApprover({
    authorActor:'ALEX', authorPersonId:'human-alex', candidates:[eligible('JAVIER','human-javier')]
  }) !== 'JAVIER') throw Error('Javier must be eligible for review of Alex');
  if (firstAvailableIndependentApprover({
    authorActor:'SOLQARYN_PLATFORM', authorPersonId:'human-platform-reviewer',
    candidates:[eligible('ALEX','human-alex')]
  }) !== 'ALEX') throw Error('Alex must be eligible for institutional changes');

  const approvalMutants = [
    ['self approval by author', p => p.candidates=[eligible('JAVIER','human-javier')]],
    ['institutional shared human', p => p.candidates=[eligible('SOLQARYN_PLATFORM','human-javier',true)]],
    ['institutional unidentified operator', p => p.candidates=[eligible('SOLQARYN_PLATFORM','human-platform-reviewer',false)]],
    ['reviewer unavailable', p => p.candidates[0].available=false],
    ['permission missing', p => p.candidates[0].permissionVerified=false],
    ['identity missing', p => p.candidates[0].identityVerified=false],
    ['independence not proved', p => p.candidates[0].independenceVerified=false],
    ['scope unauthorized', p => p.candidates[0].scopeAuthorized=false],
    ['shared credentials', p => p.candidates[0].sharedCredentials=true],
    ['unknown reviewer', p => p.candidates[0].actor='UNAUTHORIZED'],
    ['missing author human identity', p => p.authorPersonId=''],
  ];
  for (const [name, mutate] of approvalMutants) {
    const test = structuredClone(selection);
    test.candidates = [structuredClone(selection.candidates[0])];
    mutate(test);
    if (firstAvailableIndependentApprover(test) !== null) {
      throw Error('Availability policy must reject: ' + name);
    }
  }

  const documentMutants = [
    ['cap', {authority:authority.replace('PILOT_TASK_LIMIT=2','PILOT_TASK_LIMIT=3')}],
    ['role', {authority:authority.replace('PRIMARY_TASK_ROLE=PRIMARY_01','PRIMARY_TASK_ROLE=SECONDARY_02')}],
    ['owner', {authority:authority.replace('SECONDARY_TASK_ACCOUNT=ALEX_CHATGPT','SECONDARY_TASK_ACCOUNT=JAVIER_CHATGPT')}],
    ['runtime', {status:status.replace('AUTOMATION_RUNTIME=RETIRED','AUTOMATION_RUNTIME=ACTIVE_DEV')}],
    ['authorized task', {status:status.replace('SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0','SCHEDULED_CHATGPT_TASKS_AUTHORIZED=2')}],
    ['human parity', {authority:authority.replace('HUMAN_ROLE_PARITY=TRUE','HUMAN_ROLE_PARITY=FALSE')}],
    ['revived previous dispatch', {status:status.replace('VAEP_DISPATCH_AUTHORIZED=FALSE','VAEP_DISPATCH_AUTHORIZED=TRUE')}],
    ['approval routing disabled', {authority:authority.replace('APPROVAL_ROUTING=FIRST_AVAILABLE_ELIGIBLE','APPROVAL_ROUTING=FIXED_ALICE')}],
    ['institutional operator ignored', {authority:authority.replace('APPROVAL_PLATFORM_OPERATOR_VERIFICATION=REQUIRED','APPROVAL_PLATFORM_OPERATOR_VERIFICATION=NOT_REQUIRED')}],
    ['self approval authorized', {authority:authority.replace('APPROVAL_SELF_REVIEW=DENY','APPROVAL_SELF_REVIEW=ALLOW')}],
    ['task approvals substituted', {authority:authority.replace('APPROVAL_TASK_REVIEW_COUNTS_AS_GITHUB_APPROVAL=FALSE','APPROVAL_TASK_REVIEW_COUNTS_AS_GITHUB_APPROVAL=TRUE')}],
  ];
  for (const [name, fragment] of documentMutants) {
    if (!validateDocuments({...sources,...fragment}).length) throw Error('Document negative test did not reject: ' + name);
  }
  console.log(`Dual account authority PASS: 1 Primary Javier + 1 Secondary Alex, zero authorized Tasks, ${mutants.length + documentMutants.length + approvalMutants.length} negative tests`);
} else {
  console.log('Dual account authority PASS (preparation only, zero authorized Tasks)');
}
