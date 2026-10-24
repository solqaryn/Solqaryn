import { readFileSync } from 'node:fs';
import { join } from 'node:path';

const read = p => readFileSync(join(process.cwd(), p), 'utf8');
const authority = read('docs/AUTOMATION_AUTHORITY.md');
const agents = read('AGENTS.md');
const status = read('docs/AUTOMATION_STATUS.md');
const scope = read('docs/PROJECT_SCOPE_LOCK.md');
const normalize = x => String(x || '').replace(/\r\n/g, '\n');

export function validateAuthority(input) {
  const d = Object.fromEntries(Object.entries(input).map(([k,v]) => [k,normalize(v)]));
  const failures = [];
  function required(source, name, needles) {
    for (const needle of needles) if (!source.includes(needle)) failures.push(name + ': missing ' + needle);
  }
  required(d.authority, 'authority', [
    'PROJECT_SCOPE_LOCK=STRICT','PROJECT_ID=SOLQARYN','REPOSITORY=solqaryn/Solqaryn',
    'BRANCH=dev','AUTOMATION_RUNTIME=ACTIVE_DEV','PILOT_TASK_LIMIT=6',
    'VAEP_DISPATCH_AUTHORIZED=FALSE','JULES_WORKERS_AUTHORIZED=0',
    'PROPIETARIA_INSTITUCIONAL','EJECUTOR_PRINCIPAL_01','EJECUTOR_PRINCIPAL_02',
    '93171442','morales35alex','numeración', 'idéntica autoridad', 
    'ACTOR_HUMANO','CREADOR_TASK','ACTOR_AUTOMATIZADO','TASK_ID','RUN_ID','EVENT_ID',
    'IDEMPOTENCY_KEY','REVISOR','separación de deberes','independiente',
    'RETIRED','PREPARING','READ_ONLY','ACTIVE_DEV','PAUSED','BLOCKED','DISABLED',
    'AUTHORITY_GUARD','PLAN_SELECTOR','DEV_OPERATOR','REVIEW_GATE','AUDIT_REPORTER',
    'PENDIENTE','EN_PROGRESO','VALIDANDO','LISTO','BLOQUEADO','CANCELADO',
    'STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES','1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA',
    '1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU',
    '15.722','1.123','TAREAS_PROGRAMADAS','AUTOMATIZACIONES','NO_VERIFICADO',
    'reconciliación','PROD','GitHub','Google Sheets','permisos efectivos','dependencias existentes y satisfechas',
    'no puede autoaprobarlo','La paridad 01/02 no equivale a aprobación automática',
    'no activa Tasks','No se importa CAS'
  ]);
  required(d.agents, 'AGENTS', ['PROJECT_SCOPE_LOCK=STRICT','AUTOMATION_RUNTIME=ACTIVE_DEV','docs/AUTOMATION_AUTHORITY.md','EJECUTOR_PRINCIPAL_01','EJECUTOR_PRINCIPAL_02']);
  required(d.status, 'AUTOMATION_STATUS', ['AUTOMATION_RUNTIME=ACTIVE_DEV','PILOT_TASK_LIMIT=6','VAEP_DISPATCH_AUTHORIZED=FALSE','JULES_WORKERS_AUTHORIZED=0','docs/AUTOMATION_AUTHORITY.md']);
  required(d.scope, 'PROJECT_SCOPE_LOCK', [
    'PROJECT_SCOPE_LOCK=STRICT',
    'REPOSITORY=solqaryn/Solqaryn',
    'PILOT_TASK_LIMIT=6',
    'docs/AUTOMATION_AUTHORITY.md',
    'EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA',
    'COMMERCIAL_AUTHORITY_MODE=CURRENT_PLAN_ONLY',
    'LEGACY_MASTER_EXECUTABLE=FALSE'
  ]);
  const aCount=Number(d.authority.match(/^SCHEDULED_CHATGPT_TASKS_AUTHORIZED=(\d+)$/m)?.[1]);
  const sCount=Number(d.status.match(/^SCHEDULED_CHATGPT_TASKS_AUTHORIZED=(\d+)$/m)?.[1]);
  if (!Number.isInteger(aCount)||aCount<1||aCount>6||aCount!==sCount)
    failures.push('Authorized count must be verified, identical and within 1..6');
  // Reject values changed in the operational header even when historical prose
  // still contains the expected string (substring checks alone are unsafe).
  const exactKey = (source, key, value) => {
    const matches = source.split(/\r?\n/).filter(line => line.startsWith(key + '='));
    return matches.length === 1 && matches[0] === key + '=' + value;
  };
  for (const [key, value] of [
    ['PROJECT_SCOPE_LOCK', 'STRICT'],
    ['EXECUTION_PLAN_DRIVE_FILE_ID', '1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA'],
    ['COMMERCIAL_AUTHORITY_MODE', 'CURRENT_PLAN_ONLY'],
    ['LEGACY_MASTER_EXECUTABLE', 'FALSE'],
    ['PILOT_TASK_LIMIT', '6']
  ]) if (!exactKey(d.scope, key, value)) failures.push('scope header ' + key);
  if (!/^AUTOMATION_RUNTIME=ACTIVE_DEV$/m.test(d.status)) failures.push('3x3 DEV runtime mismatch');
  if (!d.authority.includes('docs/AUTOMATION_3X3_OPERATING_CONTRACT.md') || !d.agents.includes('TASK_DESIGN=AUTONOMOUS_3_JAVIER_3_ALEX')) failures.push('3x3 contract routing');
  if (/VAEP_DISPATCH_AUTHORIZED\s*=\s*TRUE/.test(d.status)) failures.push('VAEP must remain retired');
  if (/JULES_WORKERS_AUTHORIZED\\s*=\\s*[1-9]/.test(d.status)) failures.push('Jules worker reactivation forbidden');
  // P0-012.B: the operational policy must not route executable work to
  // legacy SQ identifiers or to a second Google Doc/Sheet. Historical
  // sections remain available for audit but have no execution authority.
  const approvedGoogleIds = new Set([
    '1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA',
    '1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU',
  ]);
  const activeSection = text => text.split(/^#{2,3}\\s+Archivo hist[oó]rico\\b/m)[0];
  for (const [name, text] of [
    ['authority', d.authority], ['AGENTS', d.agents],
    ['AUTOMATION_STATUS', d.status], ['PROJECT_SCOPE_LOCK', d.scope],
  ]) {
    const active = activeSection(text);
    if (/\\bSQ-\\d{3}\\b/.test(active))
      failures.push(name + ': historical SQ microtasks in active policy');
    for (const match of active.matchAll(/https:\\/\\/docs\\.google\\.com\\/(?:document|spreadsheets)\\/d\\/([A-Za-z0-9_-]+)/g)) {
      if (!approvedGoogleIds.has(match[1]))
        failures.push(name + ': unauthorized Google authority document ' + match[1]);
    }
  }
  return failures;
}
const source = { authority, agents, status, scope };
const errors = validateAuthority(source);
if (errors.length) { console.error('AUTOMATION AUTHORITY NO_GO:\n' + errors.join('\n')); process.exitCode = 1; }
else if (process.argv.includes('--self-test')) {
  const tests = [
    ['repository', {authority:authority.replaceAll('REPOSITORY=solqaryn/Solqaryn', 'REPOSITORY=other/Other')}],
    ['branch', {authority:authority.replaceAll('BRANCH=dev', 'BRANCH=main')}],
    ['role', {authority:authority.replaceAll('EJECUTOR_PRINCIPAL_02', 'OBSERVER_02')}],
    ['confused task', {authority:authority.replaceAll('ACTOR_AUTOMATIZADO', 'ACTOR_HUMANO')}],
    ['self approval', {authority:authority.replaceAll('no puede autoaprobarlo','puede autoaprobarlo')}],
    ['collision', {authority:authority.replaceAll('No se importa CAS', 'Se importa CAS')}],
    ['dependency', {authority:authority.replaceAll('dependencias existentes y satisfechas','dependencias ignoradas')}],
    ['writing', {authority:authority.replaceAll('AUTOMATION_RUNTIME=ACTIVE_DEV','AUTOMATION_RUNTIME=RETIRED')}],
    ['duplicate idempotency', {authority:authority.replaceAll('IDEMPOTENCY_KEY','DUPLICATE_KEY')}],
    ['missing evidence', {authority:authority.replaceAll('NO_VERIFICADO','APROBADO_SIN_EVIDENCIA')}],
    ['wrong contract status', {status:status.replace('docs/AUTOMATION_AUTHORITY.md','legacy/VAEP_AUTHORITY.md')}],
    ['revoked privilege', {authority:authority.replaceAll('permisos efectivos','permisos supuestos')}],
    ['prod', {authority:authority.replaceAll('PROD','UNRESTRICTED')}],
    ['reactivate vaep', {status:status.replace('VAEP_DISPATCH_AUTHORIZED=FALSE','VAEP_DISPATCH_AUTHORIZED=TRUE')}],
    ['reactivate workers', {status:status.replace('JULES_WORKERS_AUTHORIZED=0','JULES_WORKERS_AUTHORIZED=5')}],
    ['missing authorized Task', {status:status.replace('SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1','SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0')}],
    ['wrong 3x3 cap', {scope:scope.replace('PILOT_TASK_LIMIT=6','PILOT_TASK_LIMIT=2')}],
    ['no 3x3 agent routing', {agents:agents.replace('TASK_DESIGN=AUTONOMOUS_3_JAVIER_3_ALEX','TASK_DESIGN=DUAL_TASK')}],
    ['legacy Master executable', {scope:scope.replace('LEGACY_MASTER_EXECUTABLE=FALSE','LEGACY_MASTER_EXECUTABLE=TRUE')}],
    ['commercial authority substituted', {scope:scope.replace('COMMERCIAL_AUTHORITY_MODE=CURRENT_PLAN_ONLY','COMMERCIAL_AUTHORITY_MODE=LEGACY_MASTER')}],
    ['commercial plan switched', {scope:scope.replace('1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA','HISTORICAL_PLAN_ID')}],
    ['legacy SQ command in live AGENTS', {agents:agents.replace('TASK_DESIGN=AUTONOMOUS_3_JAVIER_3_ALEX','TASK_DESIGN=AUTONOMOUS_3_JAVIER_3_ALEX\\nEXECUTE=SQ-123')}],
    ['historical Word made live in Scope Lock', {scope:scope.replace('LEGACY_MASTER_EXECUTABLE=FALSE','LEGACY_MASTER_EXECUTABLE=FALSE\\nSOURCE=https://docs.google.com/document/d/1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j/edit')}],
    ['unapproved Sheet made live in Status', {status:status.replace('SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1','SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1\\nSOURCE=https://docs.google.com/spreadsheets/d/HISTORICAL_SHEET_123/edit')}]
  ];
  for (const [label, override] of tests) {
    const errs = validateAuthority({...source,...override});
    if (errs.length === 0) throw Error('Negative contract test not rejected: ' + label);
  }
  console.log('SOLQARYN automation authority PASS: canonical identities, parity, flags and ' + tests.length + ' fail-closed negative mutations');
} else if (!errors.length) console.log('SOLQARYN automation authority PASS');
