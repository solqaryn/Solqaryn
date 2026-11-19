#!/usr/bin/env node
// C15-01.M05.01: read-only contract against a verified institutional ID snapshot.
// IMPORTANT: passing this unit rule never claims LIVE Sheets, A5, JWT, DB or PROD.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

const SNAPSHOT = new URL('../../docs/v5/c15-01-m05-01-ids.snapshot.json', import.meta.url);
const ID = /^C\d{2}-\d{2}\.M0[1-6]\.0[1-6]$/;

export function verifyPlanCoverage(candidateIds, canonicalIds) {
  const errors = [];
  if (!Array.isArray(candidateIds) || !Array.isArray(canonicalIds)) {
    return { verified: false, errors: ['INVALID_INPUT'] };
  }
  const actual = candidateIds.map(v => typeof v === 'string' ? v : '');
  const reference = canonicalIds.map(v => typeof v === 'string' ? v : '');
  const expected = new Set(reference);
  const seen = new Set();
  const duplicated = new Set();
  const malformed = [];
  const unexpected = [];
  for (const value of actual) {
    if (!ID.test(value)) malformed.push(value);
    if (seen.has(value)) duplicated.add(value);
    seen.add(value);
    if (!expected.has(value)) unexpected.push(value);
  }
  const missing = reference.filter(value => !seen.has(value));
  const groups = new Map();
  for (const value of reference) {
    const prefix = value.split('.M')[0];
    groups.set(prefix, (groups.get(prefix) ?? 0) + 1);
  }
  if (reference.length !== 2340 || expected.size !== 2340) errors.push('BASELINE_CARDINALITY');
  if (groups.size !== 65 || [...groups.values()].some(n => n !== 36)) errors.push('BASELINE_GROUPS');
  if (reference.some(value => !ID.test(value))) errors.push('BASELINE_FORMAT');
  if (actual.length !== reference.length) errors.push('COUNT_DRIFT');
  if (duplicated.size) errors.push('DUPLICATE');
  if (missing.length) errors.push('MISSING');
  if (unexpected.length) errors.push('UNEXPECTED');
  if (malformed.length) errors.push('MALFORMED');
  if (actual.length === reference.length && actual.some((value, i) => value !== reference[i])) {
    errors.push('ORDER_DRIFT');
  }
  return { verified: errors.length === 0, errors,
    summary: { total: actual.length, unique: seen.size, groups: groups.size,
      missing: missing.length, unexpected: unexpected.length,
      duplicate: duplicated.size, malformed: malformed.length } };
}

function main() {
  const baseline = JSON.parse(readFileSync(SNAPSHOT, 'utf8'));
  assert.equal(baseline.source, 'Google Sheets PLAN_MAESTRO!A2:A2341');
  const reference = baseline.ids;
  const positive = verifyPlanCoverage(reference, reference);
  assert.equal(positive.verified, true, positive.errors.join(','));
  assert.deepEqual(positive.summary, {total:2340,unique:2340,groups:65,
    missing:0,unexpected:0,duplicate:0,malformed:0});
  const expectFailure = (name, changed, code) => {
    const result = verifyPlanCoverage(changed, reference);
    assert.equal(result.verified, false, name + ': false PASS');
    assert.ok(result.errors.includes(code), name + ': expected ' + code);
    console.log('NEGATIVE PASS: ' + name + ' => ' + code);
  };
  if (process.argv.includes('--self-test')) {
    expectFailure('duplicate', [reference[0], ...reference.slice(0, -1)], 'DUPLICATE');
    expectFailure('missing', reference.slice(1), 'MISSING');
    expectFailure('extra', [...reference, 'C99-99.M01.01'], 'UNEXPECTED');
    expectFailure('malformed', [reference[0].replace('M01', 'M99'), ...reference.slice(1)], 'MALFORMED');
    const swapped = reference.slice();
    [swapped[0], swapped[1]] = [swapped[1], swapped[0]];
    expectFailure('order', swapped, 'ORDER_DRIFT');
  }
  const idx = process.argv.indexOf('--input');
  if (idx >= 0) {
    assert.ok(process.argv[idx + 1], '--input requires path to JSON array of plan IDs');
    const parsed = JSON.parse(readFileSync(process.argv[idx + 1], 'utf8'));
    const tested = verifyPlanCoverage(parsed, reference);
    assert.equal(tested.verified, true, 'EXPORTED_PLAN_DRIFT: ' + tested.errors.join(','));
  }
  console.log('C15-01.M05.01 SNAPSHOT RULE PASS: 2340 IDs, 65 groups, 36 microtasks each, zero duplicates/holes in captured dataset.');
  console.log('LIMIT: snapshot-only; live Sheets, requirements-to-code mapping, tenant/JWT/DB, A0-A5 and QA/PROD not certified.');
}
if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) main();
