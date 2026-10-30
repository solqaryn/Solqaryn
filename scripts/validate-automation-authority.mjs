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
    'BRANCH=dev','AUTOMATION_RUNTIME=ACTIVE_DEV','TASK_COUNT_LIMIT=PROVIDER_EFFECTIVE_CAPACITY','TASK_ASSIGNMENT_MODEL=OWNER_CONFIGURABLE',
    'AUTO_CERTIFICATION_MODE=REQUIRED_SCOPE_LOCK', 'FORMAL_APPROVAL_ROUTINE_COMMERCIAL_REQUIRED=FALSE', 'FORMAL_APPROVAL_GOVERNANCE_REQUIRED=TRUE',
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
  required(d.status, 'AUTOMATION_STATUS', ['AUTOMATION_RUNTIME=ACTIVE_DEV','TASK_COUNT_LIMIT=PROVIDER_EFFECTIVE_CAPACITY','TASK_ASSIGNMENT_MODEL=OWNER_CONFIGURABLE','VAEP_DISPATCH_AUTHORIZED=FALSE','JULES_WORKERS_AUTHORIZED=0','docs/AUTOMATION_AUTHORITY.md']);
  required(d.scope, 'PROJECT_SCOPE_LOCK', [
    'PROJECT_SCOPE_LOCK=STRICT',
    'REPOSITORY=solqaryn/Solqaryn',
    'TASK_COUNT_LIMIT=PROVIDER_EFFECTIVE_CAPACITY',
    'docs/AUTOMATION_AUTHORITY.md',
    'EXECUTION_PLAN_DRIVE_FILE_ID=1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA',
    'COMMERCIAL_AUTHORITY_MODE=CURRENT_PLAN_ONLY',
    'LEGACY_MASTER_EXECUTABLE=FALSE'
  ]);
  // Enforce the current automatic CI certification as a unique live property,
  // while preserving formal independent review for governance modifications.
  for (const [label, body] of [
    ['authority', d.authority], ['AGENTS', d.agents],
    ['AUTOMATION_STATUS', d.status], ['PROJECT_SCOPE_LOCK', d.scope],
  ]) {
    const hits = [...body.matchAll(/^AUTO_CERTIFICATION_MODE=(.+)$/gm)];
    if (hits.length !== 1 || hits[0][1] !== 'REQUIRED_SCOPE_LOCK')
      failures.push(label + ': automatic certification gate missing or duplicated');
  }
  for (const [label, body] of [
    ['authority', d.authority], ['AGENTS', d.agents], ['AUTOMATION_STATUS', d.status],
  ]) {
    for (const [key, wanted] of [
      ['FORMAL_APPROVAL_ROUTINE_COMMERCIAL_REQUIRED', 'FALSE'],
      ['FORMAL_APPROVAL_GOVERNANCE_REQUIRED', 'TRUE'],
    ]) {
      const matches = [...body.matchAll(new RegExp('^' + key + '=(.+)
  const counterMatches = text => [...text.matchAll(/^SCHEDULED_CHATGPT_TASKS_AUTHORIZED=(\d+)$/gm)];
  const aMatches = counterMatches(d.authority), sMatches = counterMatches(d.status);
  const aCount = Number(aMatches[0]?.[1]), sCount = Number(sMatches[0]?.[1]);
  if (aMatches.length !== 1 || sMatches.length !== 1 ||
      !Number.isInteger(aCount) || aCount < 1 || aCount !== sCount)
    failures.push('Authorized count must be unique, positive, verified and identical');

  // A global Sheet flag or a raw count cannot authorize a Task.
  // Each commercial executor must be listed in both live authorities.
  const parseIds = (source, key) => {
    const lines = [...source.matchAll(new RegExp('^' + key + '=([^\\r\\n]*)$', 'gm'))];
    if (lines.length !== 1) return null;
    const ids = lines[0][1].split(',').map(v => v.trim());
    if (ids.length === 0 || ids.some(id => !/^[a-f0-9]{32}$/i.test(id)) ||
        new Set(ids).size !== ids.length) return null;
    return ids;
  };
  const aRegistered = parseIds(d.authority, 'REGISTERED_TASK_IDS');
  const sRegistered = parseIds(d.status, 'REGISTERED_TASK_IDS');
  const aIds = parseIds(d.authority, 'AUTHORIZED_COMMERCIAL_TASK_IDS');
  const sIds = parseIds(d.status, 'AUTHORIZED_COMMERCIAL_TASK_IDS');
  const registered = new Set(aRegistered ?? []);
  const physicalCounts = text => [...text.matchAll(/^PHYSICAL_TASKS_REGISTERED=(\d+)$/gm)];
  const aPhysical = physicalCounts(d.authority), sPhysical = physicalCounts(d.status);
  if (!aRegistered || !sRegistered || aRegistered.join(',') !== sRegistered.join(',') ||
      aPhysical.length !== 1 || sPhysical.length !== 1 ||
      Number(aPhysical[0]?.[1]) !== aRegistered.length ||
      Number(sPhysical[0]?.[1]) !== sRegistered.length)
    failures.push('Registered Task roster must be unique and identical in live authorities');
  if (!aIds || !sIds || aIds.length !== aCount || sIds.length !== sCount ||
      aIds.join(',') !== sIds.join(','))
    failures.push('Commercial Task allowlist must be identical, unique and exactly match count');
  if (aIds && aIds.some(id => !registered.has(id)))
    failures.push('Commercial Task allowlist must be a subset of the registered Task roster');
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
    ['TASK_COUNT_LIMIT', 'PROVIDER_EFFECTIVE_CAPACITY']
  ]) if (!exactKey(d.scope, key, value)) failures.push('scope header ' + key);
  if (!/^AUTOMATION_RUNTIME=ACTIVE_DEV$/m.test(d.status)) failures.push('3x3 DEV runtime mismatch');
  if (!d.authority.includes('docs/AUTOMATION_3X3_OPERATING_CONTRACT.md') || !d.agents.includes('TASK_DESIGN=OWNER_CONFIGURABLE_TASK_SET')) failures.push('configurable Task contract routing');
  if (/VAEP_DISPATCH_AUTHORIZED\s*=\s*TRUE/.test(d.status)) failures.push('VAEP must remain retired');
  if (/JULES_WORKERS_AUTHORIZED\s*=\s*[1-9]/.test(d.status)) failures.push('Jules worker reactivation forbidden');
  // P0-012.B: the operational policy must not route executable work to
  // legacy SQ identifiers or to a second Google Doc/Sheet. Historical
  // sections remain available for audit but have no execution authority.
  const approvedGoogleIds = new Set([
    '1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA',
    '1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU',
  ]);
  // Keep the required markers in the original live header, but scan all
  // other live sections: a historical block must not hide later policy.
  const activeHeader = text => text.split(/^#{2,3}\s+Archivo hist[oó]rico\b/m)[0];
  const operationalSections = text => {
    const liveLines = [];
    let archiveHeadingLevel = 0;
    for (const line of text.split(/\r?\n/)) {
      const heading = line.match(/^(#{1,6})\s+(.+)$/);
      if (heading) {
        const level = heading[1].length;
        if (archiveHeadingLevel && level <= archiveHeadingLevel) archiveHeadingLevel = 0;
        if (/^(?:\d+(?:\.\d+)*\.?\s+)?Archivo hist[oó]rico\b/i.test(heading[2]))
          archiveHeadingLevel = level;
      }
      if (!archiveHeadingLevel) liveLines.push(line);
    }
    return liveLines.join('\n');
  };
  // Do not permit a forged "Archivo histórico" heading to hide the live
  // operating header while substring-only checks still pass on the archive.
  const liveMarkers = {
    authority: ['CONTRACT_VERSION=2.0', 'AUTOMATION_RUNTIME=ACTIVE_DEV',
      authorizationKey + '=' + aCount,
      'AUTHORIZED_COMMERCIAL_TASK_IDS=' + (aIds?.join(',') ?? 'INVALID'),
      'REGISTERED_TASK_IDS=' + (aRegistered?.join(',') ?? 'INVALID')],
    AGENTS: ['AUTOMATION_RUNTIME=ACTIVE_DEV', 'TASK_DESIGN=OWNER_CONFIGURABLE_TASK_SET'],
    AUTOMATION_STATUS: ['AUTOMATION_RUNTIME=ACTIVE_DEV',
      authorizationKey + '=' + sCount,
      'AUTHORIZED_COMMERCIAL_TASK_IDS=' + (sIds?.join(',') ?? 'INVALID'),
      'REGISTERED_TASK_IDS=' + (sRegistered?.join(',') ?? 'INVALID')],
    PROJECT_SCOPE_LOCK: ['COMMERCIAL_AUTHORITY_MODE=CURRENT_PLAN_ONLY', 'LEGACY_MASTER_EXECUTABLE=FALSE'],
  };
  for (const [name, text] of [
    ['authority', d.authority], ['AGENTS', d.agents],
    ['AUTOMATION_STATUS', d.status], ['PROJECT_SCOPE_LOCK', d.scope],
  ]) {
    const header = activeHeader(text);
    const operational = operationalSections(text);
    for (const marker of liveMarkers[name]) {
      if (!header.split(/\r?\n/).includes(marker)) failures.push(name + ': live authority marker missing ' + marker);
    }
    if (/\bSQ-\d{3}\b/.test(operational))
      failures.push(name + ': historical SQ microtasks in active policy');
    for (const line of operational.split(/\r?\n/)) {
      for (const match of line.matchAll(/https:\/\/docs\.google\.com\/(?:document|spreadsheets)\/d\/([A-Za-z0-9_-]+)/g)) {
        // Explicitly documented as non-executable history, not a plan source.
        const historicalCitation = name === 'AUTOMATION_STATUS'
          && match[1] === '1A525NGt09mBiZez_r6hFcdCYAdoOAwOZM--N0uXPBOQ'
          && line.includes('[STANDARD AUTORIDAD OPERATIVA VAEP]')
          && /es archivo hist[oó]rico no ejecutable/i.test(line);
        if (!approvedGoogleIds.has(match[1]) && !historicalCitation)
          failures.push(name + ': unauthorized Google authority document ' + match[1]);
      }
    }
  }
  return failures;
}
const source = { authority, agents, status, scope };
const errors = validateAuthority(source);
if (errors.length) { console.error('AUTOMATION AUTHORITY NO_GO:\n' + errors.join('\n')); process.exitCode = 1; }
else if (process.argv.includes('--self-test')) {
  const expandedRoster = {...source};
  const additionalTaskId = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa';
  for (const key of ['authority','status']) {
    expandedRoster[key] = expandedRoster[key]
      .replace(/^REGISTERED_TASK_IDS=(.*)$/m, (_, ids) => 'REGISTERED_TASK_IDS=' + ids + ',' + additionalTaskId)
      .replace(/^PHYSICAL_TASKS_REGISTERED=6$/m, 'PHYSICAL_TASKS_REGISTERED=7');
  }
  if (validateAuthority(expandedRoster).length)
    throw Error('Positive configurability test failed for a seven-Task roster');
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
    ['missing authorized Task', {status:status.replace(/^SCHEDULED_CHATGPT_TASKS_AUTHORIZED=\d+$/m,'SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0')}],
    ['mismatched commercial count', {status:status.replace(/^SCHEDULED_CHATGPT_TASKS_AUTHORIZED=\d+$/m,'SCHEDULED_CHATGPT_TASKS_AUTHORIZED=6')}],
    ['unapproved commercial Task', {authority:authority.replace(/^AUTHORIZED_COMMERCIAL_TASK_IDS=.*$/m, line => line + ',deadbeefdeadbeefdeadbeefdeadbeef')}],
    ['duplicate commercial Task', {status:status.replace(/^AUTHORIZED_COMMERCIAL_TASK_IDS=(.*)$/m, (_, ids) => 'AUTHORIZED_COMMERCIAL_TASK_IDS=' + ids + ',' + ids.split(',')[0])}],
    ['commercial list length mismatch', {authority:authority.replace(/^AUTHORIZED_COMMERCIAL_TASK_IDS=(.*)$/m, (_, ids) => 'AUTHORIZED_COMMERCIAL_TASK_IDS=' + ids.split(',')[0])}],
    ['unregistered but syntactically valid commercial Task', {status:status.replace(/^AUTHORIZED_COMMERCIAL_TASK_IDS=(.*)$/m, (_, ids) => 'AUTHORIZED_COMMERCIAL_TASK_IDS=' + ids + ',aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa').replace(/^SCHEDULED_CHATGPT_TASKS_AUTHORIZED=5$/m, 'SCHEDULED_CHATGPT_TASKS_AUTHORIZED=6')}],
    ['automatic CI gate disabled', {status:status.replace('AUTO_CERTIFICATION_MODE=REQUIRED_SCOPE_LOCK','AUTO_CERTIFICATION_MODE=DISABLED')}],
    ['old mandatory human approval restored', {authority:authority.replace('FORMAL_APPROVAL_ROUTINE_COMMERCIAL_REQUIRED=FALSE','FORMAL_APPROVAL_ROUTINE_COMMERCIAL_REQUIRED=TRUE')}],
    ['governance independent review weakened', {authority:authority.replace('FORMAL_APPROVAL_GOVERNANCE_REQUIRED=TRUE','FORMAL_APPROVAL_GOVERNANCE_REQUIRED=FALSE')}],
    ['registered roster count mismatch', {status:status.replace(/^PHYSICAL_TASKS_REGISTERED=6$/m, 'PHYSICAL_TASKS_REGISTERED=7')}],
    ['reintroduced fixed task ceiling', {scope:scope.replace('TASK_COUNT_LIMIT=PROVIDER_EFFECTIVE_CAPACITY','PILOT_TASK_LIMIT=6')}],
    ['no configurable agent routing', {agents:agents.replace('TASK_DESIGN=OWNER_CONFIGURABLE_TASK_SET','TASK_DESIGN=DUAL_TASK')}],
    ['legacy Master executable', {scope:scope.replace('LEGACY_MASTER_EXECUTABLE=FALSE','LEGACY_MASTER_EXECUTABLE=TRUE')}],
    ['commercial authority substituted', {scope:scope.replace('COMMERCIAL_AUTHORITY_MODE=CURRENT_PLAN_ONLY','COMMERCIAL_AUTHORITY_MODE=LEGACY_MASTER')}],
    ['commercial plan switched', {scope:scope.replace('1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA','HISTORICAL_PLAN_ID')}],
    ['legacy SQ command in live AGENTS', {agents:agents.replace('TASK_DESIGN=OWNER_CONFIGURABLE_TASK_SET','TASK_DESIGN=OWNER_CONFIGURABLE_TASK_SET\nEXECUTE=SQ-123')}],
    ['historical Word made live in Scope Lock', {scope:scope.replace('LEGACY_MASTER_EXECUTABLE=FALSE','LEGACY_MASTER_EXECUTABLE=FALSE\nSOURCE=https://docs.google.com/document/d/1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j/edit')}],
    ['unapproved Sheet made live in Status', {status:status.replace(/^SCHEDULED_CHATGPT_TASKS_AUTHORIZED=\d+$/m, line => line + '\nSOURCE=https://docs.google.com/spreadsheets/d/HISTORICAL_SHEET_123/edit')}],
    ['archive heading cannot hide live runtime', {status:status.replace('AUTOMATION_RUNTIME=ACTIVE_DEV','## Archivo histórico\nAUTOMATION_RUNTIME=ACTIVE_DEV')}],
    ['legacy SQ hidden after historical section', {agents:agents.replace('## Trabajo de desarrollo','## Trabajo de desarrollo\nEXECUTE=SQ-123')}],
    ['foreign plan after historical section', {status:status.replace('## Fuentes nativas vigentes','## Fuentes nativas vigentes\nSOURCE=https://docs.google.com/document/d/1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j/edit')}]
  ];
  for (const [label, override] of tests) {
    const errs = validateAuthority({...source,...override});
    if (errs.length === 0) throw Error('Negative contract test not rejected: ' + label);
  }
  console.log('SOLQARYN automation authority PASS: configurable Task roster, canonical identities, parity, flags, one positive expansion test and ' + tests.length + ' fail-closed negative mutations');
} else if (!errors.length) console.log('SOLQARYN automation authority PASS');
, 'gm'))];
      if (matches.length !== 1 || matches[0][1] !== wanted) failures.push(label + ': invalid ' + key);
    }
  }

  const authorizationKey = 'SCHEDULED_CHATGPT_TASKS_AUTHORIZED';
  const counterMatches = text => [...text.matchAll(/^SCHEDULED_CHATGPT_TASKS_AUTHORIZED=(\d+)$/gm)];
  const aMatches = counterMatches(d.authority), sMatches = counterMatches(d.status);
  const aCount = Number(aMatches[0]?.[1]), sCount = Number(sMatches[0]?.[1]);
  if (aMatches.length !== 1 || sMatches.length !== 1 ||
      !Number.isInteger(aCount) || aCount < 1 || aCount !== sCount)
    failures.push('Authorized count must be unique, positive, verified and identical');

  // A global Sheet flag or a raw count cannot authorize a Task.
  // Each commercial executor must be listed in both live authorities.
  const parseIds = (source, key) => {
    const lines = [...source.matchAll(new RegExp('^' + key + '=([^\\r\\n]*)$', 'gm'))];
    if (lines.length !== 1) return null;
    const ids = lines[0][1].split(',').map(v => v.trim());
    if (ids.length === 0 || ids.some(id => !/^[a-f0-9]{32}$/i.test(id)) ||
        new Set(ids).size !== ids.length) return null;
    return ids;
  };
  const aRegistered = parseIds(d.authority, 'REGISTERED_TASK_IDS');
  const sRegistered = parseIds(d.status, 'REGISTERED_TASK_IDS');
  const aIds = parseIds(d.authority, 'AUTHORIZED_COMMERCIAL_TASK_IDS');
  const sIds = parseIds(d.status, 'AUTHORIZED_COMMERCIAL_TASK_IDS');
  const registered = new Set(aRegistered ?? []);
  const physicalCounts = text => [...text.matchAll(/^PHYSICAL_TASKS_REGISTERED=(\d+)$/gm)];
  const aPhysical = physicalCounts(d.authority), sPhysical = physicalCounts(d.status);
  if (!aRegistered || !sRegistered || aRegistered.join(',') !== sRegistered.join(',') ||
      aPhysical.length !== 1 || sPhysical.length !== 1 ||
      Number(aPhysical[0]?.[1]) !== aRegistered.length ||
      Number(sPhysical[0]?.[1]) !== sRegistered.length)
    failures.push('Registered Task roster must be unique and identical in live authorities');
  if (!aIds || !sIds || aIds.length !== aCount || sIds.length !== sCount ||
      aIds.join(',') !== sIds.join(','))
    failures.push('Commercial Task allowlist must be identical, unique and exactly match count');
  if (aIds && aIds.some(id => !registered.has(id)))
    failures.push('Commercial Task allowlist must be a subset of the registered Task roster');
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
    ['TASK_COUNT_LIMIT', 'PROVIDER_EFFECTIVE_CAPACITY']
  ]) if (!exactKey(d.scope, key, value)) failures.push('scope header ' + key);
  if (!/^AUTOMATION_RUNTIME=ACTIVE_DEV$/m.test(d.status)) failures.push('3x3 DEV runtime mismatch');
  if (!d.authority.includes('docs/AUTOMATION_3X3_OPERATING_CONTRACT.md') || !d.agents.includes('TASK_DESIGN=OWNER_CONFIGURABLE_TASK_SET')) failures.push('configurable Task contract routing');
  if (/VAEP_DISPATCH_AUTHORIZED\s*=\s*TRUE/.test(d.status)) failures.push('VAEP must remain retired');
  if (/JULES_WORKERS_AUTHORIZED\s*=\s*[1-9]/.test(d.status)) failures.push('Jules worker reactivation forbidden');
  // P0-012.B: the operational policy must not route executable work to
  // legacy SQ identifiers or to a second Google Doc/Sheet. Historical
  // sections remain available for audit but have no execution authority.
  const approvedGoogleIds = new Set([
    '1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA',
    '1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU',
  ]);
  // Keep the required markers in the original live header, but scan all
  // other live sections: a historical block must not hide later policy.
  const activeHeader = text => text.split(/^#{2,3}\s+Archivo hist[oó]rico\b/m)[0];
  const operationalSections = text => {
    const liveLines = [];
    let archiveHeadingLevel = 0;
    for (const line of text.split(/\r?\n/)) {
      const heading = line.match(/^(#{1,6})\s+(.+)$/);
      if (heading) {
        const level = heading[1].length;
        if (archiveHeadingLevel && level <= archiveHeadingLevel) archiveHeadingLevel = 0;
        if (/^(?:\d+(?:\.\d+)*\.?\s+)?Archivo hist[oó]rico\b/i.test(heading[2]))
          archiveHeadingLevel = level;
      }
      if (!archiveHeadingLevel) liveLines.push(line);
    }
    return liveLines.join('\n');
  };
  // Do not permit a forged "Archivo histórico" heading to hide the live
  // operating header while substring-only checks still pass on the archive.
  const liveMarkers = {
    authority: ['CONTRACT_VERSION=2.0', 'AUTOMATION_RUNTIME=ACTIVE_DEV',
      authorizationKey + '=' + aCount,
      'AUTHORIZED_COMMERCIAL_TASK_IDS=' + (aIds?.join(',') ?? 'INVALID'),
      'REGISTERED_TASK_IDS=' + (aRegistered?.join(',') ?? 'INVALID')],
    AGENTS: ['AUTOMATION_RUNTIME=ACTIVE_DEV', 'TASK_DESIGN=OWNER_CONFIGURABLE_TASK_SET'],
    AUTOMATION_STATUS: ['AUTOMATION_RUNTIME=ACTIVE_DEV',
      authorizationKey + '=' + sCount,
      'AUTHORIZED_COMMERCIAL_TASK_IDS=' + (sIds?.join(',') ?? 'INVALID'),
      'REGISTERED_TASK_IDS=' + (sRegistered?.join(',') ?? 'INVALID')],
    PROJECT_SCOPE_LOCK: ['COMMERCIAL_AUTHORITY_MODE=CURRENT_PLAN_ONLY', 'LEGACY_MASTER_EXECUTABLE=FALSE'],
  };
  for (const [name, text] of [
    ['authority', d.authority], ['AGENTS', d.agents],
    ['AUTOMATION_STATUS', d.status], ['PROJECT_SCOPE_LOCK', d.scope],
  ]) {
    const header = activeHeader(text);
    const operational = operationalSections(text);
    for (const marker of liveMarkers[name]) {
      if (!header.split(/\r?\n/).includes(marker)) failures.push(name + ': live authority marker missing ' + marker);
    }
    if (/\bSQ-\d{3}\b/.test(operational))
      failures.push(name + ': historical SQ microtasks in active policy');
    for (const line of operational.split(/\r?\n/)) {
      for (const match of line.matchAll(/https:\/\/docs\.google\.com\/(?:document|spreadsheets)\/d\/([A-Za-z0-9_-]+)/g)) {
        // Explicitly documented as non-executable history, not a plan source.
        const historicalCitation = name === 'AUTOMATION_STATUS'
          && match[1] === '1A525NGt09mBiZez_r6hFcdCYAdoOAwOZM--N0uXPBOQ'
          && line.includes('[STANDARD AUTORIDAD OPERATIVA VAEP]')
          && /es archivo hist[oó]rico no ejecutable/i.test(line);
        if (!approvedGoogleIds.has(match[1]) && !historicalCitation)
          failures.push(name + ': unauthorized Google authority document ' + match[1]);
      }
    }
  }
  return failures;
}
const source = { authority, agents, status, scope };
const errors = validateAuthority(source);
if (errors.length) { console.error('AUTOMATION AUTHORITY NO_GO:\n' + errors.join('\n')); process.exitCode = 1; }
else if (process.argv.includes('--self-test')) {
  const expandedRoster = {...source};
  const additionalTaskId = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa';
  for (const key of ['authority','status']) {
    expandedRoster[key] = expandedRoster[key]
      .replace(/^REGISTERED_TASK_IDS=(.*)$/m, (_, ids) => 'REGISTERED_TASK_IDS=' + ids + ',' + additionalTaskId)
      .replace(/^PHYSICAL_TASKS_REGISTERED=6$/m, 'PHYSICAL_TASKS_REGISTERED=7');
  }
  if (validateAuthority(expandedRoster).length)
    throw Error('Positive configurability test failed for a seven-Task roster');
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
    ['missing authorized Task', {status:status.replace(/^SCHEDULED_CHATGPT_TASKS_AUTHORIZED=\d+$/m,'SCHEDULED_CHATGPT_TASKS_AUTHORIZED=0')}],
    ['mismatched commercial count', {status:status.replace(/^SCHEDULED_CHATGPT_TASKS_AUTHORIZED=\d+$/m,'SCHEDULED_CHATGPT_TASKS_AUTHORIZED=6')}],
    ['unapproved commercial Task', {authority:authority.replace(/^AUTHORIZED_COMMERCIAL_TASK_IDS=.*$/m, line => line + ',deadbeefdeadbeefdeadbeefdeadbeef')}],
    ['duplicate commercial Task', {status:status.replace(/^AUTHORIZED_COMMERCIAL_TASK_IDS=(.*)$/m, (_, ids) => 'AUTHORIZED_COMMERCIAL_TASK_IDS=' + ids + ',' + ids.split(',')[0])}],
    ['commercial list length mismatch', {authority:authority.replace(/^AUTHORIZED_COMMERCIAL_TASK_IDS=(.*)$/m, (_, ids) => 'AUTHORIZED_COMMERCIAL_TASK_IDS=' + ids.split(',')[0])}],
    ['unregistered but syntactically valid commercial Task', {status:status.replace(/^AUTHORIZED_COMMERCIAL_TASK_IDS=(.*)$/m, (_, ids) => 'AUTHORIZED_COMMERCIAL_TASK_IDS=' + ids + ',aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa').replace(/^SCHEDULED_CHATGPT_TASKS_AUTHORIZED=5$/m, 'SCHEDULED_CHATGPT_TASKS_AUTHORIZED=6')}],
    ['registered roster count mismatch', {status:status.replace(/^PHYSICAL_TASKS_REGISTERED=6$/m, 'PHYSICAL_TASKS_REGISTERED=7')}],
    ['reintroduced fixed task ceiling', {scope:scope.replace('TASK_COUNT_LIMIT=PROVIDER_EFFECTIVE_CAPACITY','PILOT_TASK_LIMIT=6')}],
    ['no configurable agent routing', {agents:agents.replace('TASK_DESIGN=OWNER_CONFIGURABLE_TASK_SET','TASK_DESIGN=DUAL_TASK')}],
    ['legacy Master executable', {scope:scope.replace('LEGACY_MASTER_EXECUTABLE=FALSE','LEGACY_MASTER_EXECUTABLE=TRUE')}],
    ['commercial authority substituted', {scope:scope.replace('COMMERCIAL_AUTHORITY_MODE=CURRENT_PLAN_ONLY','COMMERCIAL_AUTHORITY_MODE=LEGACY_MASTER')}],
    ['commercial plan switched', {scope:scope.replace('1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA','HISTORICAL_PLAN_ID')}],
    ['legacy SQ command in live AGENTS', {agents:agents.replace('TASK_DESIGN=OWNER_CONFIGURABLE_TASK_SET','TASK_DESIGN=OWNER_CONFIGURABLE_TASK_SET\nEXECUTE=SQ-123')}],
    ['historical Word made live in Scope Lock', {scope:scope.replace('LEGACY_MASTER_EXECUTABLE=FALSE','LEGACY_MASTER_EXECUTABLE=FALSE\nSOURCE=https://docs.google.com/document/d/1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j/edit')}],
    ['unapproved Sheet made live in Status', {status:status.replace(/^SCHEDULED_CHATGPT_TASKS_AUTHORIZED=\d+$/m, line => line + '\nSOURCE=https://docs.google.com/spreadsheets/d/HISTORICAL_SHEET_123/edit')}],
    ['archive heading cannot hide live runtime', {status:status.replace('AUTOMATION_RUNTIME=ACTIVE_DEV','## Archivo histórico\nAUTOMATION_RUNTIME=ACTIVE_DEV')}],
    ['legacy SQ hidden after historical section', {agents:agents.replace('## Trabajo de desarrollo','## Trabajo de desarrollo\nEXECUTE=SQ-123')}],
    ['foreign plan after historical section', {status:status.replace('## Fuentes nativas vigentes','## Fuentes nativas vigentes\nSOURCE=https://docs.google.com/document/d/1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j/edit')}]
  ];
  for (const [label, override] of tests) {
    const errs = validateAuthority({...source,...override});
    if (errs.length === 0) throw Error('Negative contract test not rejected: ' + label);
  }
  console.log('SOLQARYN automation authority PASS: configurable Task roster, canonical identities, parity, flags, one positive expansion test and ' + tests.length + ' fail-closed negative mutations');
} else if (!errors.length) console.log('SOLQARYN automation authority PASS');
