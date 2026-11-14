import assert from 'node:assert/strict';
import { verifyDirectDevTraceability } from './quality/direct-dev-traceability.mjs';

// Advisory only: this module NEVER grants a merge, an assignment, or LISTO.
// Classify trusted exact-SHA checks to avoid idle scheduled-Task runs.
export const HOURLY_LISTO_TARGET = 6;
export const THROUGHPUT_WINDOW_MS = 60 * 60 * 1000;
const SCOPE = /^(?:P\d+|I8|G0)-\d{3}\.[A-Z]$/;
const SHA = /^[a-f0-9]{40}$/i;
const TASK_ID = /^[a-f0-9]{32}$/i;
const trusted = c => c?.app?.id === 15368 && typeof c?.name === 'string';
const decision = (action, rest = {}) => ({
  action, canMerge: false, canMarkListo: false, state: 'VALIDANDO', ...rest,
});

export function classifyCiContinuation(data) {
  if (!data || data.state !== 'VALIDANDO' || !SCOPE.test(data.scopeId || '') ||
      !SHA.test(data.headSha || '') || data.githubReadbackVerified !== true ||
      !Array.isArray(data.requiredCheckNames) || data.requiredCheckNames.length === 0 ||
      new Set(data.requiredCheckNames).size !== data.requiredCheckNames.length ||
      !data.requiredCheckNames.every(x => typeof x === 'string' && x.length) ||
      !Array.isArray(data.checks) || data.checks.length === 0)
    return decision('NO_GO_UNVERIFIED_INPUT');
  if (data.authorTaskId != null && !TASK_ID.test(data.authorTaskId))
    return decision('NO_GO_UNVERIFIED_AUTHOR');
  if (data.reviewerTaskId != null && (!TASK_ID.test(data.reviewerTaskId) ||
      data.reviewerTaskId === data.authorTaskId))
    return decision('NO_GO_SELF_REVIEW_OR_INVALID_REVIEWER');
  const required = [];
  for (const name of data.requiredCheckNames) {
    const found = data.checks.filter(x => x?.name === name);
    if (found.length !== 1 || !trusted(found[0]) || found[0].head_sha !== data.headSha)
      return decision('NO_GO_REQUIRED_CHECK_MISSING_OR_UNTRUSTED', { check: name });
    required.push(found[0]);
  }
  const failed = required.filter(x => x.status === 'completed' && x.conclusion !== 'success');
  if (failed.length)
    return decision('CI_FAILED_FIX_SCOPE', { failed: failed.map(x => x.name) });
  const pending = required.filter(x => ['queued','pending','in_progress','waiting','requested'].includes(x.status));
  if (required.some(x => x.status !== 'completed' && !pending.includes(x)))
    return decision('NO_GO_UNKNOWN_CHECK_STATUS');
  if (pending.length) {
    // CI runs on GitHub; Tasks MUST NOT sleep or poll repeatedly while CI runs.
    // Never hold a scheduled Task for a workflow; no row reservation is created.
    return decision('CI_PENDING_ASYNC_CONTINUE', {
      pending: pending.map(x => x.name),
      mayClaimOtherScope: data.reviewComplete === true,
    });
  }
  // Green PR checks do not attest a protected merge, postmerge CI, or sheet readback.
  return decision('CI_GREEN_VERIFY_MERGE_AND_POSTMERGE', { checked: required.map(x => x.name) });
}

export function rollingListoThroughput(events, nowIso, ledgerCoverageVerified = false) {
  const empty = { target: HOURLY_LISTO_TARGET, windowMinutes: 60, certified: null,
    deficit: null, status: 'UNVERIFIED_LEDGER_COVERAGE' };
  if (!Array.isArray(events) || ledgerCoverageVerified !== true ||
      typeof nowIso !== 'string' || !/(Z|[+-]\d\d:\d\d)$/.test(nowIso) ||
      !Number.isFinite(Date.parse(nowIso))) return empty;
  const now = Date.parse(nowIso), counted = new Set();
  for (const x of events) {
    if (!SCOPE.test(x?.scopeId || '') || x.state !== 'LISTO' ||
        typeof x.closedAt !== 'string' || !/(Z|[+-]\d\d:\d\d)$/.test(x.closedAt)) continue;
    const at = Date.parse(x.closedAt);
    if (!Number.isFinite(at) || at > now || now-at >= THROUGHPUT_WINDOW_MS) continue;
    if (x.integrationMode === 'DIRECT_DEV_FOUR_AUDITS') {
      if (verifyDirectDevTraceability(x).verified) counted.add(x.scopeId);
      continue;
    }
    // Legacy receipts remain visible only for historical throughput comparisons.
    if (!SHA.test(x.mergeSha || '') || x.mergeVerified !== true ||
        x.requiredCiVerified !== true || x.independentReviewVerified !== true ||
        x.planReadbackVerified !== true || x.colaReadbackVerified !== true ||
        x.bitacoraReadbackVerified !== true) continue;
    counted.add(x.scopeId); // duplicate status events never count twice
  }
  return {target: HOURLY_LISTO_TARGET, windowMinutes: 60, certified: counted.size,
    deficit: Math.max(0, HOURLY_LISTO_TARGET-counted.size),
    status: counted.size >= HOURLY_LISTO_TARGET ? 'TARGET_MET' : 'BELOW_TARGET'};
}

function selfTest() {
  const sha='a'.repeat(40),author='b'.repeat(32),reviewer='c'.repeat(32);
  const check=(name,status='completed',conclusion='success') =>
    ({name,status,conclusion,head_sha:sha,app:{id:15368}});
  const base={state:'VALIDANDO',scopeId:'P1-013.A',headSha:sha,
    authorTaskId:author,reviewerTaskId:reviewer,githubReadbackVerified:true,
    requiredCheckNames:['Security','Build'],checks:[check('Security'),check('Build')]};
  assert.equal(classifyCiContinuation(base).action,'CI_GREEN_VERIFY_MERGE_AND_POSTMERGE');
  assert.equal(classifyCiContinuation(base).canMarkListo,false);
  for(const id of ['P2-003.C','I8-001.C','G0-006.C'])
    assert.equal(classifyCiContinuation({...base,scopeId:id}).action,'CI_GREEN_VERIFY_MERGE_AND_POSTMERGE');
  assert.equal(classifyCiContinuation({...base,scopeId:'X9-001.C'}).action,'NO_GO_UNVERIFIED_INPUT');
  assert.equal(classifyCiContinuation({...base,githubReadbackVerified:false}).action,'NO_GO_UNVERIFIED_INPUT');
  assert.equal(classifyCiContinuation({...base,checks:[check('Security')]}).action,'NO_GO_REQUIRED_CHECK_MISSING_OR_UNTRUSTED');
  assert.equal(classifyCiContinuation({...base,reviewerTaskId:author}).action,'NO_GO_SELF_REVIEW_OR_INVALID_REVIEWER');
  assert.equal(classifyCiContinuation({...base,checks:[{...check('Security'),head_sha:'d'.repeat(40)},check('Build')]}).action,'NO_GO_REQUIRED_CHECK_MISSING_OR_UNTRUSTED');
  assert.equal(classifyCiContinuation({...base,checks:[{...check('Security'),app:{id:1}},check('Build')]}).action,'NO_GO_REQUIRED_CHECK_MISSING_OR_UNTRUSTED');
  assert.equal(classifyCiContinuation({...base,checks:[check('Security','completed','failure'),check('Build')]}).action,'CI_FAILED_FIX_SCOPE');
  const pending={...base,checks:[check('Security'),check('Build','in_progress',null)]};
  const q=classifyCiContinuation({...pending,reviewComplete:true,reviewerLeaseReleased:false});
  assert.equal(q.action,'CI_PENDING_ASYNC_CONTINUE');
  assert.equal(q.mayClaimOtherScope,true);
  assert.equal(q.canMerge,false);
  assert.equal(classifyCiContinuation({...pending,reviewComplete:false}).mayClaimOtherScope,false);
  assert.equal(classifyCiContinuation({...pending,reviewComplete:true}).mayClaimOtherScope,true);
  assert.equal(classifyCiContinuation({...base,requiredCheckNames:['Security','Security']}).action,'NO_GO_UNVERIFIED_INPUT');
  const now='2026-11-05T18:00:00Z';
  const item=scopeId=>({scopeId,state:'LISTO',closedAt:'2026-11-05T17:30:00Z',mergeSha:sha,
    mergeVerified:true,requiredCiVerified:true,independentReviewVerified:true,
    planReadbackVerified:true,colaReadbackVerified:true,bitacoraReadbackVerified:true});
  assert.equal(rollingListoThroughput([item('P1-001.A')],now).status,'UNVERIFIED_LEDGER_COVERAGE');
  assert.deepEqual(rollingListoThroughput([item('P1-001.A'),item('P1-001.A')],now,true),
    {target:6,windowMinutes:60,certified:1,deficit:5,status:'BELOW_TARGET'});
  assert.equal(rollingListoThroughput([{...item('P1-001.A'),requiredCiVerified:false}],now,true).certified,0);
  assert.equal(rollingListoThroughput([{...item('P1-001.A'),closedAt:'2026-11-05T16:00:00Z'}],now,true).certified,0);
  const six='ABCDEF'.split('').map((x,i)=>item('P1-'+String(i+1).padStart(3,'0')+'.'+x));
  assert.equal(rollingListoThroughput(six,now,true).status,'TARGET_MET');
  console.log('SOLQARYN ASYNC_CI_6_LISTO_PER_HOUR SELF_TEST=PASS');
}
if (process.argv.includes('--self-test')) selfTest();
