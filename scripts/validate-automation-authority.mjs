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
    'BRANCH=dev','AUTOMATION_RUNTIME=ACTIVE_DEV','SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1',
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
  required(d.status, 'AUTOMATION_STATUS', ['AUTOMATION_RUNTIME=ACTIVE_DEV','SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1','VAEP_DISPATCH_AUTHORIZED=FALSE','JULES_WORKERS_AUTHORIZED=0','docs/AUTOMATION_AUTHORITY.md']);
  required(d.scope, 'PROJECT_SCOPE_LOCK', ['PROJECT_SCOPE_LOCK=STRICT','REPOSITORY=solqaryn/Solqaryn','docs/AUTOMATION_AUTHORITY.md']);
  if (!/^SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1$/m.test(d.status)) failures.push('Primary DEV pilot must have exactly one authorized writer');
  if (!/^AUTOMATION_RUNTIME=ACTIVE_DEV$/m.test(d.status)) failures.push('Primary DEV pilot runtime mismatch');
  if (/VAEP_DISPATCH_AUTHORIZED\s*=\s*TRUE/.test(d.status)) failures.push('VAEP must remain retired');
  if (/JULES_WORKERS_AUTHORIZED\s*=\s*[1-9]/.test(d.status)) failures.push('Jules worker reactivation forbidden');
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
    ['activated Task', {status:status.replace('SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1','SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0')}]
  ];
  for (const [label, override] of tests) {
    const errs = validateAuthority({...source,...override});
    if (errs.length === 0) throw Error('Negative contract test not rejected: ' + label);
  }
  console.log('SOLQARYN automation authority PASS: canonical identities, parity, flags and ' + tests.length + ' fail-closed negative mutations');
} else if (!errors.length) console.log('SOLQARYN automation authority PASS');
