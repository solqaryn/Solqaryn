// Read-only validation of author provenance and stage transition.
// No persistent reviewer row reservation; one reviewer is selected by deterministic routing.
const TASK = /^[a-f0-9]{32}$/i;
const SCOPE = /^P\d+-\d+\.[A-Z]$/;
const validTask = id => typeof id === 'string' && TASK.test(id);
export function validValidationRelease(t) {
  return Boolean(t && SCOPE.test(t.id || '') && t.state === 'VALIDANDO' &&
    validTask(t.authorTaskId) &&
    (t.originalAuthorTaskId == null || t.originalAuthorTaskId === t.authorTaskId) &&
    t.stageReleaseRequested === true &&
    t.ownerTaskId == null &&
    t.provenancePreserved === true && t.prHeadReadbackVerified === true &&
    t.deliveryTestsVerified === true && t.githubReadbackVerified === true &&
    t.planReadbackVerified === true);
}
export function isStageOwnedBy(t, id) {
  if (!validTask(id) || !t) return false;
  if(t.state === 'EN_PROGRESO') return t.ownerTaskId === id;
  if(t.state === 'VALIDANDO') return t.stageOwnerTaskId === id && t.authorTaskId !== id;
  return false;
}
export function reviewReleaseEligible(t, actorTaskId, activeReviewIds = new Set()) {
  return validValidationRelease(t) && validTask(actorTaskId) &&
    actorTaskId !== t.authorTaskId && t.reviewerAllowed === true &&
    t.reviewPermissionVerified === true &&
    t.activeEditorConflict !== true && t.activeReviewerConflict !== true &&
    !activeReviewIds.has(t.id);
}
export function validateReleaseTransition(p) {
  const errors = [];
  if(!p || !SCOPE.test(p.scopeId || '') || !validTask(p.authorTaskId) ||
     p.requesterTaskId !== p.authorTaskId ||
     p.fromState !== 'EN_PROGRESO' || p.toState !== 'VALIDANDO')
    errors.push('release_identity');
  if(p?.workDeliveredVerified !== true || p?.deliveryTestsVerified !== true ||
     p?.exactHeadVerified !== true || p?.branchReadbackVerified !== true ||
     p?.provenancePreserved !== true) errors.push('release_delivery');
  if(p?.activeEditorConflict === true || p?.githubReadbackVerified !== true ||
     p?.planReadbackVerified !== true || p?.bitacoraReadbackVerified !== true ||
     p?.stageOwnerTaskId != null)
    errors.push('release_readback');
  return errors;
}
export function validateReviewReservation(p) {
  // Kept as a compatibility export; verifies a direct reviewer, not a reservation.
  const errors=[];
  if(!p || !SCOPE.test(p.scopeId || '') || !validTask(p.authorTaskId) ||
      !validTask(p.reviewerTaskId) || p.reviewerTaskId === p.authorTaskId)
    errors.push('self_review_or_identity');
  if(p?.reviewerRouteVerified !== true || p?.githubReadbackVerified !== true ||
     p?.planReadbackVerified !== true || p?.reviewPermissionVerified !== true ||
     p?.activeEditorConflict === true || p?.activeReviewerConflict === true)
    errors.push('review_route_or_conflict');
  if(!Number.isInteger(p?.reviewerOwnedScopeCount) || p.reviewerOwnedScopeCount < 0 ||
     p.reviewerOwnedScopeCount >= 15 || !Number.isInteger(p?.globalOpenScopeCount) ||
     p.globalOpenScopeCount < 1 || p.globalOpenScopeCount > 15)
    errors.push('review_capacity');
  return errors;
}
export function assertStageReleaseNegativeTests(){
  const a='a'.repeat(32),b='b'.repeat(32);
  const delivered={id:'P1-010.B',state:'VALIDANDO',authorTaskId:a,
    stageReleaseRequested:true,ownerTaskId:null,provenancePreserved:true,
    prHeadReadbackVerified:true,deliveryTestsVerified:true,
    githubReadbackVerified:true,planReadbackVerified:true,
    reviewerAllowed:true,reviewPermissionVerified:true};
  if(!validValidationRelease(delivered) || validValidationRelease({...delivered,ownerTaskId:a}) ||
     !reviewReleaseEligible(delivered,b) || reviewReleaseEligible(delivered,a) ||
     reviewReleaseEligible({...delivered,activeReviewerConflict:true},b))
    throw Error('stage provenance or distinct reviewer');
  const release={scopeId:'P1-010.B',authorTaskId:a,requesterTaskId:a,
    fromState:'EN_PROGRESO',toState:'VALIDANDO',workDeliveredVerified:true,
    deliveryTestsVerified:true,exactHeadVerified:true,branchReadbackVerified:true,
    provenancePreserved:true,githubReadbackVerified:true,planReadbackVerified:true,
    bitacoraReadbackVerified:true,stageOwnerTaskId:null};
  if(validateReleaseTransition(release).length ||
    !validateReleaseTransition({...release,workDeliveredVerified:false}).length)
    throw Error('unsafe release');
  const review={scopeId:'P1-010.B',authorTaskId:a,reviewerTaskId:b,
    reviewerRouteVerified:true,githubReadbackVerified:true,planReadbackVerified:true,
    reviewPermissionVerified:true,reviewerOwnedScopeCount:1,globalOpenScopeCount:5};
  if(validateReviewReservation(review).length ||
     !validateReviewReservation({...review,reviewerTaskId:a}).length ||
     !validateReviewReservation({...review,activeEditorConflict:true}).length)
     throw Error('unsafe reviewer');
}
