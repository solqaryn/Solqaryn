import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';
import { validateSnapshot as validateHistorical } from './verify-p0-010-a-debt-baseline.mjs';

// This certificate does not call GitHub, read customer rows, or mark issues clean.
const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const record = JSON.parse(readFileSync(join(root, 'docs/baselines/P0-010-B-DEV-20261030.json'), 'utf8'));
const oldRecord = JSON.parse(readFileSync(join(root, 'docs/baselines/P0-010-A-DEV-20261026.json'), 'utf8'));
assert.deepEqual(validateHistorical(oldRecord), [], 'P0-010.A dated reference must remain valid');

function validate(data) {
  const errors = [];
  const expect = (yes, message) => { if (!yes) errors.push(message); };
  expect(data.schema === 'solqaryn-debt-certification-v1', 'strict snapshot schema');
  expect(data.project === 'SOLQARYN' && data.repository === 'solqaryn/Solqaryn' && data.branch === 'dev', 'scope lock');
  expect(data.snapshotType === 'AS_OF_ONLY_NOT_LIVE', 'dated evidence, not live certification');
  expect(/^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\dZ$/.test(data.observedUtc), 'UTC observation');
  expect(/^[0-9a-f]{40}$/.test(data.devHead), 'exact GitHub commit');
  const src = data.source ?? {};
  expect(src.sheetId === '1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU', 'one authorized commercial sheet');
  expect(src.sheetRange === 'COLA!A1:I1124', 'bounded evidence range');
  expect(src.githubOpenIssues === 1 && src.githubIssueId === 3730, 'observed issue count');
  expect(src.issueClassification === 'LEGACY_COORDINATION_NOT_PRODUCT_DEFECT', 'coordination issue not a reproduced product bug');
  const priorities = data.byPriority ?? {};
  const p0 = priorities.P0 ?? {}, p1 = priorities.P1 ?? {};
  const states = ['LISTO','VALIDANDO','EN_PROGRESO','PENDIENTE'];
  for (const p of [p0, p1]) {
    expect(states.every(k => Number.isSafeInteger(p[k]) && p[k] >= 0), 'nonnegative state counts');
    expect(Number.isSafeInteger(p.total) && p.total === states.reduce((a, k) => a + (p[k]||0), 0), 'priority counts reconcile');
  }
  expect(p0.total === 24 && p1.total === 32, 'bounded P0/P1 inventory');
  expect(p0.LISTO === 22 && p0.VALIDANDO === 1 && p0.EN_PROGRESO === 1 && p0.PENDIENTE === 0, 'dated P0 readback');
  expect(p1.LISTO === 1 && p1.PENDIENTE === 31 && p1.VALIDANDO === 0 && p1.EN_PROGRESO === 0, 'dated P1 readback');
  const claims = data.claims ?? {};
  expect(claims.noKnownDefects === false, 'empty issue tracker cannot certify zero defects');
  expect(claims.allP0P1Closed === false, 'open backlog must remain open');
  expect(claims.untrackedIncidents === 'NOT_VERIFIED', 'untracked incidents cannot be inferred absent');
  expect(claims.issueTrackerCoversAllIncidents === false, 'issue tracker is not an exhaustive defect inventory');
  expect(claims.zeroIssuesGuaranteesNoBugs === false, 'zero issues does not guarantee clean application');
  const limitations = data.limitations;
  expect(Array.isArray(limitations) &&
    ['DATED_SNAPSHOT_ONLY','ISSUES_NOT_EXHAUSTIVE','UNTRACKED_INCIDENTS_NOT_VERIFIED','BACKLOG_NOT_EQUAL_PRODUCT_DEFECTS']
      .every(k => limitations.includes(k)), 'limitations preserved');
  return errors;
}

assert.deepEqual(validate(record), [], 'positive dated evidence must pass');

const negatives = [
  x => { x.source.githubOpenIssues = 0; x.claims.noKnownDefects = true; },
  x => { x.claims.noKnownDefects = true; },
  x => { x.claims.untrackedIncidents = 'VERIFIED_CLEAR'; },
  x => { x.claims.issueTrackerCoversAllIncidents = true; },
  x => { x.claims.zeroIssuesGuaranteesNoBugs = true; },
  x => { x.claims.allP0P1Closed = true; },
  x => { x.byPriority.P0.LISTO = 24; },
  x => { x.byPriority.P1.PENDIENTE = 0; },
  x => { x.source.issueClassification = 'REPRODUCED_PRODUCT_DEFECT'; },
  x => { x.snapshotType = 'LIVE_CERTIFIED'; },
  x => { x.limitations = []; },
  x => { x.source.githubOpenIssues = -1; },
];
for (const mutate of negatives) {
  const fixture = structuredClone(record);
  mutate(fixture);
  assert.ok(validate(fixture).length > 0, 'Unsupported good-news inference must fail');
}
console.log('P0-010.B PASS: dated inventory positive + 12/12 negative inference fixtures; zero issue != zero defects');
