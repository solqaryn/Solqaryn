import assert from 'node:assert/strict';

// FAST_CLOSE_V8: reviewerFor is a deterministic preference, not an exclusive authorization.
// No row locks, claim service or persistent reviewer ownership; re-read native run and GitHub.
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
// FIFO-READY_V8_2: prioritize PR created_at, not CI completion or scope suffix.
export function orderCommercialPRsByCreation(prs) {
  if (!Array.isArray(prs)) return [];
  const valid = prs.filter(p => p && Number.isInteger(p.number) &&
    typeof p.created_at === 'string' && Number.isFinite(Date.parse(p.created_at)));
  return [...valid].sort((a,b) =>
    Date.parse(a.created_at) - Date.parse(b.created_at) || a.number - b.number);
}
// Advisory selector only; re-read GitHub, Sheets and branch protection before merge.
export function oldestProtectedMergeReady(prs) {
  return orderCommercialPRsByCreation(prs).find(p =>
    p.stage === 'VALIDANDO' && p.githubState === 'open' &&
    p.dependenciesOk === true && p.independentTechReview === true &&
    p.requiredChecksSuccess === true && p.mergeable === true &&
    p.currentHeadVerified === true && p.activeEditorConflict !== true &&
    TASK.test(p.authorTaskId || '') && TASK.test(p.reviewerTaskId || '') &&
    p.authorTaskId !== p.reviewerTaskId &&
    /^[a-f0-9]{40}$/i.test(p.headSha || '') &&
    p.reviewedHeadSha === p.headSha && p.checksHeadSha === p.headSha) || null;
}

export function canReviewInNativeRun(data) {
  if (!data || data.nativeRunVerified !== true ||
      data.githubCurrentVerified !== true || data.planCurrentVerified !== true ||
      data.authorTaskId === data.actorTaskId ||
      data.activeEditorConflict === true || data.activeReviewerConflict === true ||
      data.codexActiveConflict === true || data.permissionsVerified !== true ||
      data.state !== 'VALIDANDO' || (data.prMerged !== true && data.prExists !== true)) return false;
  // reviewerFor is an advisory preference, never an exclusive queue or reservation.
  // Any authorized independent Task may review an unoccupied VALIDANDO on a native run.
  const preferred = reviewerFor(data.scopeId, data.authorTaskId, data.authorizedTaskIds);
  return preferred !== null && sortedEligible(data.authorizedTaskIds, data.authorTaskId)
    .includes(data.actorTaskId);
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
    // A free, independent non-preferred Task can clear the VALIDANDO backlog.
    const otherReviewer = roster.find(x => x !== author && x !== a);
    assert.equal(canReviewInNativeRun({...live,actorTaskId:otherReviewer}),true);
    assert.equal(canReviewInNativeRun({...live,actorTaskId:'00000000000000000000000000000000'}),false);
    assert.equal(canReviewInNativeRun({...live,actorTaskId:author}),false);
    assert.equal(canReviewInNativeRun({...live,codexActiveConflict:true}),false);
    assert.equal(canReviewInNativeRun({...live,activeReviewerConflict:true}),false);
    assert.equal(canReviewInNativeRun({...live,permissionsVerified:false}),false);
  }
  assert.equal(reviewerFor('P1-013.A',author,roster),roster[1]); // skip J00 contributor to PR #3853
  assert.equal(reviewerFor('P1-007.B',roster[1],roster),roster[4]);
  assert.equal(developerFor('P1-014.D',roster),developerFor('P1-014.D',[...roster].reverse()));
  const sha='a'.repeat(40);
  const make=(n,date)=>({number:n,created_at:date,stage:'VALIDANDO',
    githubState:'open',dependenciesOk:true,independentTechReview:true,
    requiredChecksSuccess:true,mergeable:true,currentHeadVerified:true,
    activeEditorConflict:false,authorTaskId:roster[0],reviewerTaskId:roster[1],
    headSha:sha,reviewedHeadSha:sha,checksHeadSha:sha});
  const oldest=make(50,'2026-11-05T01:00:00Z');
  const later=make(51,'2026-11-05T02:00:00Z');
  const newest=make(52,'2026-11-05T03:00:00Z');
  assert.equal(oldestProtectedMergeReady([later,oldest,newest]).number,50);
  assert.equal(oldestProtectedMergeReady([newest,later,oldest]).number,50);
  assert.equal(oldestProtectedMergeReady([{...oldest,dependenciesOk:false},later]).number,51);
  assert.equal(oldestProtectedMergeReady([{...oldest,mergeable:false},later]).number,51);
  assert.equal(oldestProtectedMergeReady([{...oldest,requiredChecksSuccess:false},later]).number,51);
  assert.equal(oldestProtectedMergeReady([{...oldest,reviewedHeadSha:'b'.repeat(40)},later]).number,51);
  assert.equal(oldestProtectedMergeReady([{...oldest,reviewerTaskId:roster[0]},later]).number,51);
  assert.equal(oldestProtectedMergeReady([{...oldest,checksHeadSha:'b'.repeat(40)},later]).number,51);
  assert.equal(oldestProtectedMergeReady([{...oldest,currentHeadVerified:false}]),null);
  assert.equal(oldestProtectedMergeReady([{...oldest,activeEditorConflict:true}]),null);
  assert.equal(oldestProtectedMergeReady([{...oldest,githubState:'closed'}]),null);
  assert.equal(oldestProtectedMergeReady([{},null]),null);
  assert.deepEqual(orderCommercialPRsByCreation([newest,oldest,later]).map(p=>p.number),[50,51,52]);
  assert.equal(oldestProtectedMergeReady([{...oldest,headSha:'c'.repeat(40),
    reviewedHeadSha:'c'.repeat(40),checksHeadSha:'c'.repeat(40)},later]).number,50);

}
if(process.argv.includes('--self-test')) {selfTestReviewRouting(); console.log('SOLQARYN REVIEW ROUTING SELF_TEST=PASS');}
