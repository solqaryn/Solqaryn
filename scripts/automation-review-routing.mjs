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
const SCOPE = /^(?:P\d+|I8|G0)-\d+\.[A-Z]$/;
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
  const m = /^([A-Z])(\d+)-(\d+)\.([A-Z])$/.exec(scopeId);
  const programBias = m[1] === 'P' ? 0 : m[1].charCodeAt(0) * 17;
  return candidates[(programBias + Number(m[2]) * 131 + Number(m[3]) * 29 + m[4].charCodeAt(0)) % candidates.length];
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
  // STRICT_FIFO_NO_IDLE_V10: never bypass the oldest still-open PR to dev,
  // including governance/technical PRs. Development may proceed in PENDIENTE.
  const first = orderCommercialPRsByCreation(prs).find(p => p.githubState === 'open');
  if (!first) return null;
  const ready = first.stage === 'VALIDANDO' &&
    first.dependenciesOk === true && first.independentTechReview === true &&
    first.requiredChecksSuccess === true && first.mergeable === true &&
    first.currentHeadVerified === true && first.activeEditorConflict !== true &&
    TASK.test(first.authorTaskId || '') && TASK.test(first.reviewerTaskId || '') &&
    first.authorTaskId !== first.reviewerTaskId &&
    /^[a-f0-9]{40}$/i.test(first.headSha || '') &&
    first.reviewedHeadSha === first.headSha && first.checksHeadSha === first.headSha;
  return ready ? first : null;
}

// V13: only an advisory one-at-a-time FIFO decision; re-read GitHub/dev/Sheets after EVERY close.
export function nextV13FifoAction(prs,actorTaskId){
 if(!TASK.test(actorTaskId||'')||!Array.isArray(prs))return {action:'NO_GO_UNVERIFIED_QUEUE'};
 const first=orderCommercialPRsByCreation(prs).find(p=>p.githubState==='open'||(p.githubState==='merged'&&p.institutionalListo!==true));
 if(!first)return {action:'NO_OLDER_PR_DEVELOP_PENDING'};
 if(!TASK.test(first.authorTaskId||''))return {action:'FIFO_BLOCKED_AUTHOR_UNVERIFIED',prNumber:first.number};
 if(first.authorTaskId===actorTaskId)return {action:'STOP_SELF_AUTHOR_DEVELOP_PENDING',prNumber:first.number};
 if(first.githubState==='merged'){
  const passed=first.independentTechReview===true&&TASK.test(first.reviewerTaskId||'')&&first.reviewerTaskId!==first.authorTaskId&&
    /^[a-f0-9]{40}$/i.test(first.mergeSha||'')&&first.mergeShaVerified===true&&first.requiredPostmergeSuccess===true&&
    first.postmergeChecksSha===first.mergeSha&&first.dependenciesOk===true&&first.planCurrentVerified===true&&first.bitacoraAvailable===true;
  return passed?{action:'CERTIFY_ONE_LISTO_AND_RECHECK',prNumber:first.number}:{action:'FIFO_POSTMERGE_BLOCKED_DEVELOP_PENDING',prNumber:first.number};
 }
 return oldestProtectedMergeReady([first])?{action:'MERGE_ONE_PROTECTED_AND_RECHECK',prNumber:first.number}:{action:'FIFO_BLOCKED_DEVELOP_PENDING',prNumber:first.number};
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
  for (const id of ['P2-019.C','I8-001.C','G0-001.C']) {
    assert(developerFor(id,roster));
    assert.equal(developerFor(id,roster),developerFor(id,[...roster].reverse()));
    assert(reviewerFor(id,author,roster) !== author);
  }
  assert.equal(developerFor('X9-001.C',roster),null);
  assert.equal(reviewerFor('X9-001.C',author,roster),null);
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
  assert.equal(oldestProtectedMergeReady([{...oldest,dependenciesOk:false},later]),null);
  assert.equal(oldestProtectedMergeReady([{...oldest,mergeable:false},later]),null);
  assert.equal(oldestProtectedMergeReady([{...oldest,requiredChecksSuccess:false},later]),null);
  assert.equal(oldestProtectedMergeReady([{...oldest,reviewedHeadSha:'b'.repeat(40)},later]),null);
  assert.equal(oldestProtectedMergeReady([{...oldest,reviewerTaskId:roster[0]},later]),null);
  assert.equal(oldestProtectedMergeReady([{...oldest,checksHeadSha:'b'.repeat(40)},later]),null);
  assert.equal(oldestProtectedMergeReady([{...oldest,currentHeadVerified:false}]),null);
  assert.equal(oldestProtectedMergeReady([{...oldest,activeEditorConflict:true}]),null);
  assert.equal(oldestProtectedMergeReady([{...oldest,githubState:'closed'}]),null);
  const technicalOlder = {number:49,created_at:'2026-11-05T00:10:00Z',
    githubState:'open',stage:'GOVERNANCE'};
  assert.equal(oldestProtectedMergeReady([later,technicalOlder]),null);
  assert.equal(oldestProtectedMergeReady([later,{...technicalOlder,githubState:'closed'}]).number,51);
  assert.equal(oldestProtectedMergeReady([later,{...oldest,githubState:'closed'}]).number,51);
  assert.equal(oldestProtectedMergeReady([{},null]),null);
  assert.deepEqual(orderCommercialPRsByCreation([newest,oldest,later]).map(p=>p.number),[50,51,52]);
  const many=Array.from({length:10},(_,i)=>make(100+i,new Date(Date.parse('2026-11-05T03:00:00Z')+60000*i).toISOString()));
  let q=[...many],done=0;
  while(q.length){assert.equal(nextV13FifoAction(q,roster[2]).action,'MERGE_ONE_PROTECTED_AND_RECHECK');q=q.slice(1);done++;}
  assert.equal(done,10);
  const stop=[...many.slice(0,2),{...many[2],authorTaskId:roster[2]},...many.slice(3)];
  assert.equal(nextV13FifoAction(stop,roster[2]).prNumber,100);
  assert.equal(nextV13FifoAction(stop.slice(1),roster[2]).prNumber,101);
  assert.deepEqual(nextV13FifoAction(stop.slice(2),roster[2]),{action:'STOP_SELF_AUTHOR_DEVELOP_PENDING',prNumber:102});
  assert.equal(nextV13FifoAction([{...many[0],requiredChecksSuccess:false},many[1]],roster[2]).action,'FIFO_BLOCKED_DEVELOP_PENDING');
  const merged={...many[0],githubState:'merged',mergeSha:sha,mergeShaVerified:true,postmergeChecksSha:sha,
    requiredPostmergeSuccess:true,planCurrentVerified:true,bitacoraAvailable:true};
  assert.equal(nextV13FifoAction([merged,many[1]],roster[2]).action,'CERTIFY_ONE_LISTO_AND_RECHECK');
  assert.equal(nextV13FifoAction([{...merged,requiredPostmergeSuccess:false},many[1]],roster[2]).action,'FIFO_POSTMERGE_BLOCKED_DEVELOP_PENDING');
  assert.equal(oldestProtectedMergeReady([{...oldest,headSha:'c'.repeat(40),
    reviewedHeadSha:'c'.repeat(40),checksHeadSha:'c'.repeat(40)},later]).number,50);

}
if(process.argv.includes('--self-test')) {selfTestReviewRouting(); console.log('SOLQARYN REVIEW ROUTING SELF_TEST=PASS');}
