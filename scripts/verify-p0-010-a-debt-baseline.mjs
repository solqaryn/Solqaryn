import { readFileSync } from 'node:fs';

const DEFAULT_SNAPSHOT_URL = new URL('../docs/baselines/P0-010-A-DEV-20261026.json', import.meta.url);
const FORBIDDEN_GOOD_NEWS = ['READY', 'CERTIFIED', 'YES', 'TRUE'];

export function validateSnapshot(x) {
  const failures = [];
  const req = (ok, message) => { if (!ok) failures.push(message); };
  req(x && typeof x === 'object' && !Array.isArray(x), 'snapshot must be object');
  if (!x || typeof x !== 'object') return failures;
  req(x.snapshotType === 'AS_OF_ONLY_NOT_LIVE', 'snapshot must be dated, not live');
  req(x.project === 'SOLQARYN' && x.repository === 'solqaryn/Solqaryn' && x.branch === 'dev', 'scope identity');
  req(/^[0-9a-f]{40}$/.test(x.devHead ?? ''), '40-character dev head');
  req(/^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\dZ$/.test(x.observedUtc ?? '') && Number.isFinite(Date.parse(x.observedUtc)), 'UTC observation');
  const source = x.source ?? {};
  req(source.sheetId === '1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU', 'single official sheet');
  req(source.sheetRange === 'PLAN_MAESTRO!A2:I100', 'source range');
  req(source.issuesQuery === 'repo:solqaryn/Solqaryn is:issue is:open', 'open issues scope');
  req(source.openGithubIssueCount === 1 && source.openGithubIssueClassification === 'LEGACY_COORDINATION_NOT_PRODUCT_DEFECT', 'issue classification');
  req(Array.isArray(source.openCommercialPrNumbers) && source.openCommercialPrNumbers.includes(3758) && source.openCommercialPrNumbers.includes(3761), 'observed open commercial PRs');
  const priorities = x.byPriority ?? {};
  const p0 = priorities.P0 ?? {};
  const p1 = priorities.P1 ?? {};
  const p0States = p0.states ?? {};
  const p1States = p1.states ?? {};
  const nonnegative = (s) => Object.values(s).every(v => Number.isSafeInteger(v) && v >= 0);
  const sum = (s) => Object.values(s).reduce((a, v) => a + v, 0);
  req(nonnegative(p0States) && nonnegative(p1States), 'nonnegative integer counts');
  req(p0.total === 24 && p1.total === 32 && x.totalPlanMicrotasks === 56, 'dated totals');
  req(p0States.LISTO === 14 && p0States.PENDIENTE === 6 && p0States.EN_PROGRESO === 3 && p0States.VALIDANDO === 1, 'P0 state readback');
  req(p1States.PENDIENTE === 32 && Object.keys(p1States).length === 1, 'P1 state readback');
  req(sum(p0States) === p0.total && sum(p1States) === p1.total && p0.total + p1.total === x.totalPlanMicrotasks, 'state counts reconcile');
  const missing = x.incompleteP0Ids;
  req(Array.isArray(missing) && missing.length === 10 && new Set(missing).size === 10, '10 distinct incomplete P0 rows');
  req(Array.isArray(missing) && missing.every(s => /^P0-\d{3}\.[AB]$/.test(s)) && missing.includes('P0-001.B') && missing.includes('P0-010.A'), 'incomplete ID domain');
  req(Array.isArray(missing) && missing.length === p0.total - p0States.LISTO, 'incomplete P0 agrees with states');
  const claims = x.claims ?? {};
  req(claims.noKnownDefects === false, 'no clean defect claim');
  req(claims.untrackedIncidents === 'NOT_VERIFIED', 'untracked incidents unknown');
  req(claims.p0p1PlanRowsAreDefects === false, 'backlog is not bug list');
  req(claims.allP0P1Closed === false, 'backlog not completed');
  req(claims.devProviderParity === 'NO_ACREDITADA' && claims.p0_001_bProviderParity === 'NO_GO', 'provider parity fail closed');
  req(!FORBIDDEN_GOOD_NEWS.includes(claims.devProviderParity) && !FORBIDDEN_GOOD_NEWS.includes(claims.p0_001_bProviderParity), 'no artificial READY');
  const limitations = x.limitations;
  req(Array.isArray(limitations) && limitations.includes('DATED_SNAPSHOT_ONLY_RECHECK_SHEET_AND_GITHUB_BEFORE_ACTION') && limitations.includes('UNTRACKED_INCIDENTS_NOT_VERIFIED'), 'scope limitations declared');
  return failures;
}

function loadSnapshot() {
  return JSON.parse(readFileSync(DEFAULT_SNAPSHOT_URL, 'utf8'));
}

const good = loadSnapshot();
const fail = (msg) => { console.error(msg); process.exitCode = 1; };
const initial = validateSnapshot(good);
if (initial.length > 0) fail(`P0-010.A positive FAIL: ${initial.join('; ')}`);
else console.log('P0-010.A snapshot positive PASS (as-of only; no live parity claim)');

if (process.argv.includes('--self-test')) {
  const changes = [
    x => { x.snapshotType = 'LIVE_CERTIFIED'; },
    x => { x.byPriority.P0.states.LISTO = 24; },
    x => { x.byPriority.P0.states.EN_PROGRESO = -1; },
    x => { x.byPriority.P1.total = 0; },
    x => { x.claims.noKnownDefects = true; },
    x => { x.claims.p0p1PlanRowsAreDefects = true; },
    x => { x.claims.untrackedIncidents = 'VERIFIED_CLEAR'; },
    x => { x.claims.devProviderParity = 'READY'; },
    x => { x.claims.p0_001_bProviderParity = 'READY'; },
    x => { x.incompleteP0Ids.push('P0-010.A'); },
    x => { x.source.openGithubIssueClassification = 'REPRODUCED_PRODUCT_DEFECT'; },
    x => { x.limitations = []; },
  ];
  let negativePass = 0;
  for (const mutate of changes) {
    const altered = structuredClone(good);
    mutate(altered);
    if (validateSnapshot(altered).length > 0) negativePass++;
  }
  if (negativePass !== changes.length) fail(`P0-010.A negative regressions FAIL: ${negativePass}/${changes.length}`);
  else console.log(`P0-010.A negative regressions PASS ${negativePass}/${changes.length}`);
}
