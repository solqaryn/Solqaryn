import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { pathToFileURL } from 'node:url';
import { CAPACITY_POLICY_VERSION, MAX_CONCURRENT_SCOPES, MAX_OPEN_WORK_BRANCHES, MAX_OWNED_SCOPES_PER_TASK, MAX_ACTIVE_WRITES_PER_TASK } from './automation-capacity-v13.mjs';

const read = p => readFileSync(join(process.cwd(), p), 'utf8');
const files = {
  authority: 'docs/AUTOMATION_AUTHORITY.md',
  agents: 'AGENTS.md',
  status: 'docs/AUTOMATION_STATUS.md',
  scope: 'docs/PROJECT_SCOPE_LOCK.md',
};
const ID = /^[a-f0-9]{32}$/i;
// The canonical roster is read from the two living authorities, not from an obsolete list.
const keyLines = (body, key) => body.split(/\r?\n/).filter(line => line.startsWith(key + '='));
const key = (body, name) => {
  const lines = keyLines(body, name);
  return lines.length === 1 ? lines[0].slice(name.length + 1) : null;
};
const roster = value => value?.split(',').map(x => x.trim()) || [];
const expectedFour = [
  ['MAX_CONCURRENT_SCOPES', MAX_CONCURRENT_SCOPES],
  ['MAX_OPEN_WORK_BRANCHES', MAX_OPEN_WORK_BRANCHES],
  ['MAX_OWNED_SCOPES_PER_TASK', MAX_OWNED_SCOPES_PER_TASK],
  ['MAX_ACTIVE_WRITES_PER_TASK', MAX_ACTIVE_WRITES_PER_TASK],
];
export function validateAuthority(input) {
  const d = Object.fromEntries(Object.entries(input).map(([k, v]) => [k, String(v || '').replace(/\r\n/g, '\n')]));
  const errors = [];
  const need = (body, name, value, label) => {
    if (key(body || '', name) !== String(value)) errors.push(label + ': invalid ' + name);
  };
  for (const [label, body] of Object.entries(d)) {
    need(body, 'PROJECT_SCOPE_LOCK', 'STRICT', label);
    need(body, 'REPOSITORY', 'solqaryn/Solqaryn', label);
    need(body, 'BRANCH', 'dev', label);
    need(body, 'AUTO_CERTIFICATION_MODE', 'DIRECT_DEV_FOUR_AUDITS', label);
    need(body, 'QUALITY_PROTOCOL_VERSION', 'V13_A0_A5', label);
    need(body, 'GOAL_MAX_LISTO_PER_RUN', 'NONE_WITHIN_PROVIDER_RUN', label);
    need(body, 'TASK_REASONING_PREFERENCE', 'MAX_AVAILABLE_REASONING', label);
    need(body, 'TASK_IDENTITY_EVIDENCE_POLICY', 'ACCOUNT_TASK_CONTEXT_NATIVE_PEEK_AND_SOURCE_LINKS', label);
    need(body, 'TASK_FALSE_SELF_OWNED_POLICY', 'DIAGNOSTIC_ONLY_NOT_ACCOUNT_DENIAL', label);
    need(body, 'TASK_DEFINITION_404_POLICY', 'USE_PEEK_READBACK_NO_AUTO_DENIAL', label);
    need(body, 'TASK_CANDIDATE_TRIAGE', 'SCAN_ALL_ELIGIBLE_WITH_REAL_TOOLCHAIN_PREFLIGHT', label);
    need(body, 'TASK_NATIVE_REASONING_CONTROL', 'OWNER_OR_PLATFORM_SETTING', label);
    need(body, 'JAVIER_PEER_WATCH_SCOPE', 'SIX_EXISTING_TASK_IDS', label);
    need(body, 'JAVIER_PEER_AUTORESUME', 'AUTHORIZED_SINGLE_ATTEMPT_WITH_READBACK', label);
    need(body, 'ALEX_PEER_WATCH_SCOPE', 'NONE', label);
    need(body, 'WORK_BRANCH_CREATION_ALLOWED', 'FALSE', label);
    if (/^AUTO_CERTIFICATION_MODE=REQUIRED_SCOPE_LOCK$/m.test(body)) errors.push(label + ': obsolete CI gate');
  }
  need(d.scope, 'COMMERCIAL_AUTHORITY_MODE', 'CURRENT_PLAN_ONLY', 'scope');
  need(d.scope, 'LEGACY_MASTER_EXECUTABLE', 'FALSE', 'scope');
  need(d.scope, 'LOCAL_SKILL_COUNT', '1', 'scope');
  for (const label of ['authority','status']) {
    const body = d[label];
    need(body, 'AUTOMATION_RUNTIME', 'ACTIVE_DEV', label);
    need(body, 'EXECUTION_PLAN_DRIVE_FILE_ID','1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA',label);
    need(body, 'VAEP_DISPATCH_AUTHORIZED', 'FALSE', label);
    need(body, 'JULES_WORKERS_AUTHORIZED', '0', label);
    need(body, 'SCHEDULED_CHATGPT_TASKS_AUTHORIZED', '6', label);
    need(body, 'PHYSICAL_TASKS_REGISTERED', '6', label);
    need(body, 'CAPACITY_POLICY_VERSION', CAPACITY_POLICY_VERSION, label);
    for (const [k,v] of expectedFour) need(body,k,v,label);
    need(body,'FORMAL_APPROVAL_ROUTINE_COMMERCIAL_REQUIRED','FALSE',label);
    need(body,'FORMAL_APPROVAL_GOVERNANCE_REQUIRED','FALSE',label);
    need(body,'GOAL_MIN_LISTO_PER_RUN','1',label);
    need(body,'GOAL_TARGET_LISTO_PER_RUN','3',label);
    need(body,'CONTINUE_WHILE_ELIGIBLE','TRUE',label);
    const registered = roster(key(body,'REGISTERED_TASK_IDS'));
    const authorized = roster(key(body,'AUTHORIZED_COMMERCIAL_TASK_IDS'));
    if (registered.length!==6 || authorized.length!==6 || new Set(registered).size!==6 ||
        registered.some(x=>!ID.test(x)) || registered.join(',')!==authorized.join(','))
      errors.push(label + ': invalid physical task roster');
  }
  if (key(d.status,'REGISTERED_TASK_IDS')!==key(d.authority,'REGISTERED_TASK_IDS'))
    errors.push('rosters diverge');
  if (!String(d.authority).includes('No se importa un reservador de Sheets'))
    errors.push('no artificial Sheets locks');
  for (const label of ['authority','agents','status','scope']) {
    const body=String(d[label]||'');
    if (/^EXECUTE=SQ-\d+/m.test(body)) errors.push(label + ': obsolete executable plan');
    if (/https:\/\/docs\.google\.com\/(?:document|spreadsheets)\/d\/(?!1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA|1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU)[a-zA-Z0-9_-]+/i.test(body))
      errors.push(label + ': unauthorized plan source');
  }
  return errors;
}
const source=Object.fromEntries(Object.entries(files).map(([k,p])=>[k,read(p)]));
const errors=validateAuthority(source);
if (errors.length) {
  console.error('AUTOMATION AUTHORITY NO_GO:\n' + errors.join('\n'));
  process.exitCode=1;
} else if (process.argv.includes('--self-test')) {
  for (const [description, mutant] of [
    ['retired Alex peer watch',{status:source.status.replace('ALEX_PEER_WATCH_SCOPE=NONE','ALEX_PEER_WATCH_SCOPE=OWNER_ONLY_AFTER_ACTIVATION')}],
    ['seven task roster',{status:source.status.replace(/^REGISTERED_TASK_IDS=(.*)$/m,(_,ids)=>'REGISTERED_TASK_IDS='+ids+',6adbbc9ad9e88191bea8eb69104afb25')}],
    ['duplicate ID',{status:source.status.replace(/^(REGISTERED_TASK_IDS=)(.*)$/m,(_,p,ids)=>p+ids+','+ids.split(',')[0])}],
    ['missing scope',{scope:source.scope.replace('PROJECT_SCOPE_LOCK=STRICT','PROJECT_SCOPE_LOCK=OFF')}],
    ['fake audit',{authority:source.authority.replace('AUTO_CERTIFICATION_MODE=DIRECT_DEV_FOUR_AUDITS','AUTO_CERTIFICATION_MODE=DISABLED')}],
    ['remove A0 A5',{agents:source.agents.replace('QUALITY_PROTOCOL_VERSION=V13_A0_A5','QUALITY_PROTOCOL_VERSION=DISABLED')}],
    ['premature stop after 3',{status:source.status.replace('GOAL_MAX_LISTO_PER_RUN=NONE_WITHIN_PROVIDER_RUN','GOAL_MAX_LISTO_PER_RUN=3')}],
    ['bad peer scope',{authority:source.authority.replace('JAVIER_PEER_AUTORESUME=AUTHORIZED_SINGLE_ATTEMPT_WITH_READBACK','JAVIER_PEER_AUTORESUME=ANY_ACCOUNT')}],
    ['bad thinking',{agents:source.agents.replace('TASK_REASONING_PREFERENCE=MAX_AVAILABLE_REASONING','TASK_REASONING_PREFERENCE=INSTANT')}],
    ['identity false denial regression',{status:source.status.replace('TASK_FALSE_SELF_OWNED_POLICY=DIAGNOSTIC_ONLY_NOT_ACCOUNT_DENIAL','TASK_FALSE_SELF_OWNED_POLICY=AUTO_NO_GO')}],
    ['single-candidate starvation',{authority:source.authority.replace('TASK_CANDIDATE_TRIAGE=SCAN_ALL_ELIGIBLE_WITH_REAL_TOOLCHAIN_PREFLIGHT','TASK_CANDIDATE_TRIAGE=FIRST_ONLY')}],
    ['work branch allowed',{agents:source.agents.replace('WORK_BRANCH_CREATION_ALLOWED=FALSE','WORK_BRANCH_CREATION_ALLOWED=TRUE')}],
    ['legacy engine',{status:source.status.replace('VAEP_DISPATCH_AUTHORIZED=FALSE','VAEP_DISPATCH_AUTHORIZED=TRUE')}],
    ['wrong plan',{status:source.status.replace('EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA','EXECUTION_PLAN_DRIVE_FILE_ID=HISTORICAL')}],
    ['wrong target',{authority:source.authority.replace('REPOSITORY=solqaryn/Solqaryn','REPOSITORY=other/Other')}],
  ]) if (validateAuthority({...source,...mutant}).length===0) throw Error('negative test accepted: '+description);
  console.log('SOLQARYN V13 DIRECT DEV authority PASS; thirteen fail-closed mutations rejected');
} else console.log('SOLQARYN V13 DIRECT DEV authority PASS');
