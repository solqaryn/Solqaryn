import assert from 'node:assert/strict';

// Stable partition of work, no row locks, claim service, or persistent reviewer ownership.
// Advisory result: each native Task must re-read the live scope and GitHub before work.
export const PRIORITY_REVIEWERS = Object.freeze({
  'P1-007.B': '6adcc98a80a48191b71a5156624c21df', // Alex :50; author Javier :20
  'P1-011.C': '6adc2e55f47c81919f6374e561687076', // Javier :40; author Alex :10
  'P1-013.A': '6adc2e4bd8a48191ac7d8641ed95a1a3', // Javier :20; independent of author Alex :10 and J00 integration contributor
  'P1-015.C': '6adc2e4bd8a48191ac7d8641ed95a1a3', // Javier :20; author Alex :10
  'P1-016.C': '6aeacb7820ac8191b7d768a6f33dfa09', // Javier :30; author Alex :10
});
const TASK = /^[a-f0-9]{32}$/i;
const SCOPE = /^P\d+-\d+\.[A-Z]$/;
const sortedEligible = (taskIds, author) =>
  [...new Set(Array.isArray(taskIds) ? taskIds.filter(x => TASK.test(x)) : [])]
    .filter(x => x !== author).sort();
export function reviewerFor(scopeId, authorTaskId, taskIds) {
  if (!SCOPE.test(scopeId || '') || !TASK.test(authorTaskId || '')) return null;
  const candidates = sortedEligible(taskIds, authorTaskId);
  if (!candidates.length) return null;
  const preferred = PRIORITY_REVIEWERS[scopeId];
  if (preferred && candidates.includes(preferred)) return preferred;
  let hash = 2166136261;
  for (const c of scopeId) hash = Math.imul(hash ^ c.charCodeAt(0), 16777619) >>> 0;
  return candidates[hash % candidates.length];
}
export function developerFor(scopeId, taskIds) {
  if (!SCOPE.test(scopeId || '')) return null;
  const candidates = sortedEligible(taskIds, null);
  if (!candidates.length) return null;
  const m = /^P(\d+)-(\d+)\.([A-Z])$/.exec(scopeId);
  return candidates[(Number(m[1]) * 131 + Number(m[2]) * 29 + m[3].charCodeAt(0)) % candidates.length];
}
// Read-only preflight. A PENDIENTE successor can START when predecessors are
// VALIDANDO or LISTO, but cannot be merged or certified until all are LISTO.
// COLA!O is the advisory start gate; COLA!G remains the strict completion gate.
export function canBeginDevelopmentInNativeRun(data) {
  if (!data || !/^P1-\d+\.[A-Z]$/.test(data.scopeId || '') ||
      data.state !== 'PENDIENTE' || data.developmentEnabled !== 'SI' ||
      !Array.isArray(data.dependencyStatuses) || data.dependencyStatuses.length > 2 ||
      !data.dependencyStatuses.every(s => s === 'LISTO' || s === 'VALIDANDO') ||
      Boolean(data.ownerTaskId) || data.activeEditorConflict !== false ||
      data.codexActiveConflict === true || data.branchAlreadyExists !== false ||
      data.prAlreadyExists !== false || data.wipWithinLimit !== true ||
      data.nativeRunVerified !== true || data.githubCurrentVerified !== true ||
      data.planCurrentVerified !== true || data.permissionsVerified !== true ||
      !sortedEligible(data.authorizedTaskIds, null).includes(data.actorTaskId)) return false;
  return true;
}
export function dependenciesReadyForIntegration(statuses) {
  return Array.isArray(statuses) && statuses.every(s => s === 'LISTO');
}
// This is a ranking suggestion, not a claim or lock. Native exclusivity applies later.
export function developmentCandidatesFor(actorTaskId, rows, taskIds) {
  const authorized = sortedEligible(taskIds, null);
  if (!authorized.includes(actorTaskId) || !Array.isArray(rows)) return [];
  return rows.filter(r => r && /^P1-\d+\.[A-Z]$/.test(r.scopeId || '') &&
      r.state === 'PENDIENTE' && r.developmentEnabled === 'SI' && !r.ownerTaskId)
    .sort((a, b) => {
      const preferenceA = developerFor(a.scopeId, authorized) === actorTaskId ? 0 : 1;
      const preferenceB = developerFor(b.scopeId, authorized) === actorTaskId ? 0 : 1;
      return preferenceA - preferenceB ||
        (Number.isSafeInteger(a.queueRow) ? a.queueRow : 999999) -
        (Number.isSafeInteger(b.queueRow) ? b.queueRow : 999999) ||
        a.scopeId.localeCompare(b.scopeId);
    }).map(r => r.scopeId);
}
export function canReviewInNativeRun(data) {
  if (!data || data.nativeRunVerified !== true ||
      data.githubCurrentVerified !== true || data.planCurrentVerified !== true ||
      data.authorTaskId === data.actorTaskId ||
      data.activeEditorConflict === true || data.activeReviewerConflict === true ||
      data.codexActiveConflict === true || data.permissionsVerified !== true ||
      data.state !== 'VALIDANDO' || (data.prMerged !== true && data.prExists !== true)) return false;
  return reviewerFor(data.scopeId, data.authorTaskId, data.authorizedTaskIds) === data.actorTaskId;
}
export function selfTestReviewRouting() {
  const roster=[
    '6adbb9fff1548191a1bd5b30d9e9984a',
    '6adc2e4bd8a48191ac7d8641ed95a1a3',
    '6adc2e55f47c81919f6374e561687076',
    '6adbbc9ad9e88191bea8eb69104afb25',
    '6adcc98a80a48191b71a5156624c21df',
    '6aeacb7820ac8191b7d768a6f33dfa09',
  ];
  const author=roster[3];
  for(const scope of ['P1-011.C','P1-013.A','P1-015.C','P1-016.C','P1-123.D']) {
    const a=reviewerFor(scope,author,roster);
    assert(a && a!==author);
    assert.equal(a,reviewerFor(scope,author,[...roster].reverse()));
    assert.equal(new Set(roster.filter(x=>x===a)).size,1);
    const live={scopeId:scope,authorTaskId:author,actorTaskId:a,authorizedTaskIds:roster,
      nativeRunVerified:true,githubCurrentVerified:true,planCurrentVerified:true,
      permissionsVerified:true,state:'VALIDANDO',prMerged:true};
    assert.equal(canReviewInNativeRun(live),true);
    assert.equal(canReviewInNativeRun({...live,actorTaskId:author}),false);
    assert.equal(canReviewInNativeRun({...live,codexActiveConflict:true}),false);
    assert.equal(canReviewInNativeRun({...live,activeReviewerConflict:true}),false);
    assert.equal(canReviewInNativeRun({...live,permissionsVerified:false}),false);
  }
  assert.equal(reviewerFor('P1-013.A',author,roster),roster[1]); // skip J00 contributor to PR #3853
  assert.equal(reviewerFor('P1-007.B',roster[1],roster),roster[4]);
  assert.equal(developerFor('P1-014.D',roster),developerFor('P1-014.D',[...roster].reverse()));
  const start = {scopeId:'P1-013.D',state:'PENDIENTE',developmentEnabled:'SI',
    dependencyStatuses:['VALIDANDO'],ownerTaskId:null,activeEditorConflict:false,
    codexActiveConflict:false,branchAlreadyExists:false,prAlreadyExists:false,
    wipWithinLimit:true,nativeRunVerified:true,githubCurrentVerified:true,
    planCurrentVerified:true,permissionsVerified:true,authorizedTaskIds:roster,
    actorTaskId:roster[0]};
  assert.equal(canBeginDevelopmentInNativeRun(start),true);
  assert.equal(canBeginDevelopmentInNativeRun({...start,dependencyStatuses:['LISTO']}),true);
  assert.equal(canBeginDevelopmentInNativeRun({...start,dependencyStatuses:['PENDIENTE']}),false);
  assert.equal(canBeginDevelopmentInNativeRun({...start,activeEditorConflict:true}),false);
  assert.equal(canBeginDevelopmentInNativeRun({...start,branchAlreadyExists:true}),false);
  assert.equal(canBeginDevelopmentInNativeRun({...start,permissionsVerified:false}),false);
  assert.equal(canBeginDevelopmentInNativeRun({...start,developmentEnabled:'NO'}),false);
  assert.equal(dependenciesReadyForIntegration(['VALIDANDO']),false);
  assert.equal(dependenciesReadyForIntegration(['LISTO']),true);
  const pending = [
    {scopeId:'P1-013.D',state:'PENDIENTE',developmentEnabled:'SI',queueRow:62},
    {scopeId:'P1-011.B',state:'PENDIENTE',developmentEnabled:'SI',queueRow:69},
    {scopeId:'P1-013.E',state:'PENDIENTE',developmentEnabled:'NO',queueRow:66}
  ];
  assert.equal(developmentCandidatesFor(roster[0],pending,roster).length,2);
  assert.deepEqual(developmentCandidatesFor('invalid',pending,roster),[]);

}
if(process.argv.includes('--self-test')) {selfTestReviewRouting(); console.log('SOLQARYN REVIEW ROUTING SELF_TEST=PASS');}
