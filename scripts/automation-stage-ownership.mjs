// SOLQARYN: immutable author, transferable stage custody and fail-closed release.
// The global open-scope count INCLUDES VALIDANDO; release only frees the author TASK_ID.
// GitHub and Sheets are distinct systems: an observed two-phase/readback receipt is
// mandatory. Never claim an atomic cross-provider transaction.
const TASK_ID=/^[a-f0-9]{32}$/i;
const scopeId=x=>typeof x==='string'&&/^P\d+-\d+\.[A-Z]$/.test(x);
const taskId=x=>typeof x==='string'&&TASK_ID.test(x);

export function validValidationRelease(t){
  if(!t||t.state!=='VALIDANDO'||t.stageReleaseRequested!==true)return false;
  return scopeId(t.id)&&taskId(t.authorTaskId)&&
    t.originalAuthorTaskId===t.authorTaskId&&
    t.ownerTaskId==null&&
    (t.stageOwnerTaskId==null
      ? t.reviewReservationTaskId==null
      : taskId(t.stageOwnerTaskId)&&t.stageOwnerTaskId!==t.authorTaskId&&
        t.reviewReservationTaskId===t.stageOwnerTaskId)&&
    t.stageReleaseReceiptVerified===true&&
    typeof t.releaseEventId==='string'&&t.releaseEventId.length>8&&
    t.provenancePreserved===true&&t.prHeadReadbackVerified===true&&
    t.deliveryTestsVerified===true&&t.previousLeaseReleased===true&&
    t.releaseActiveEditorAbsentVerified===true&&t.githubReadbackVerified===true&&
    t.planReadbackVerified===true&&t.bitacoraReadbackVerified===true&&
    t.releaseVersionVerified===true&&t.noConflictingClaim===true;
}

export function isStageOwnedBy(t,id){
  if(!taskId(id)||!t)return false;
  if(t.state==='EN_PROGRESO')return t.ownerTaskId===id;
  if(t.state==='VALIDANDO'){
    if(t.stageReleaseRequested===true)
      return validValidationRelease(t)
        ? t.stageOwnerTaskId===id
        : t.ownerTaskId===id||t.stageOwnerTaskId===id||t.authorTaskId===id;
    return t.ownerTaskId===id;
  }
  return false;
}

export function reviewReleaseEligible(t,actorTaskId,activeReviewIds=new Set()){
  return validValidationRelease(t)&&taskId(actorTaskId)&&actorTaskId!==t.authorTaskId&&
    !activeReviewIds.has(t.id)&&t.reviewerAllowed===true&&
    t.reviewPermissionVerified===true&&t.reviewerLeaseAvailable===true&&
    t.reviewReservationTaskId==null&&t.stageOwnerTaskId==null&&
    t.conflictingClaim!==true;
}

export function validateReleaseTransition(p){
  const errors=[];
  if(!p||!scopeId(p.scopeId)||!taskId(p.authorTaskId)||
    p.requesterTaskId!==p.authorTaskId||p.fromState!=='EN_PROGRESO'||
    p.toState!=='VALIDANDO')errors.push('release_identity');
  if(p.workDeliveredVerified!==true||p.deliveryTestsVerified!==true||
    p.exactHeadVerified!==true||p.branchReadbackVerified!==true||
    p.provenancePreserved!==true)errors.push('release_delivery');
  if(p.activeEditorVerifiedAbsent!==true||p.previousLeaseReleased!==true||
    p.releaseLockAcquired!==true||p.noConflictingClaim!==true)
      errors.push('release_lease');
  if(p.githubReadbackVerified!==true||p.planReadbackVerified!==true||
    p.bitacoraReadbackVerified!==true||p.releaseVersionVerified!==true||
    !p.releaseEventId||p.stageOwnerTaskId!==null||p.reviewReservationTaskId!=null)
      errors.push('release_readback');
  // If the release receipt is incomplete, the former owner's scope stays LOCKED.
  return errors;
}

export function validateReviewReservation(p){
  const errors=[];
  if(!p||!scopeId(p.scopeId)||!taskId(p.authorTaskId)||
    !taskId(p.reviewerTaskId)||p.reviewerTaskId===p.authorTaskId)
      errors.push('self_review_or_identity');
  if(p.releaseReadbackVerified!==true||p.provenancePreserved!==true||
    p.reviewLeaseAcquiredExclusively!==true||p.reviewVersionVerified!==true||
    p.noConflictingReviewer!==true||p.previousLeaseReleased!==true||
    p.ownerTransferReadbackVerified!==true||p.planReadbackVerified!==true||
    p.bitacoraReadbackVerified!==true)
      errors.push('review_reservation_readback');
  if(p.reviewerOwnershipReadbackVerified!==true||
    p.reviewerOwnedScopeCount!==0||p.reviewerActiveWriteCount!==0||
    p.globalOpenScopesReadbackVerified!==true||
    !Number.isInteger(p.globalOpenScopeCount)||p.globalOpenScopeCount<1||
    p.globalOpenScopeCount>6) errors.push('reviewer_busy_or_wip');
  if(p.exactHeadVerified!==true||p.reviewPermissionVerified!==true)
      errors.push('review_permissions');
  return errors;
}

export function assertStageReleaseNegativeTests(){
  const a='aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa';
  const b='bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb';
  const t={id:'P1-010.B',state:'VALIDANDO',authorTaskId:a,originalAuthorTaskId:a,
    stageReleaseRequested:true,stageOwnerTaskId:null,ownerTaskId:null,
    reviewReservationTaskId:null,stageReleaseReceiptVerified:true,
    releaseEventId:'stage-release-unique',
    provenancePreserved:true,prHeadReadbackVerified:true,deliveryTestsVerified:true,
    previousLeaseReleased:true,releaseActiveEditorAbsentVerified:true,
    githubReadbackVerified:true,planReadbackVerified:true,bitacoraReadbackVerified:true,
    releaseVersionVerified:true,noConflictingClaim:true,
    reviewerAllowed:true,reviewPermissionVerified:true,reviewerLeaseAvailable:true};
  if(!validValidationRelease(t)||!reviewReleaseEligible(t,b)||
      reviewReleaseEligible(t,a)||isStageOwnedBy(t,a))
    throw Error('valid stage release or self-review regression');
  const assigned={...t,stageOwnerTaskId:b,reviewReservationTaskId:b};
  if(!validValidationRelease(assigned)||!isStageOwnedBy(assigned,b)||
    isStageOwnedBy(assigned,a)||reviewReleaseEligible(assigned,b))
      throw Error('review assignee double-claim or author not released');
  for(const [key,value] of [['authorTaskId',b],['planReadbackVerified',false],
    ['previousLeaseReleased',false],['releaseVersionVerified',false],
    ['releaseActiveEditorAbsentVerified',false],['ownerTaskId',a],
    ['stageReleaseReceiptVerified',false],
    ['reviewReservationTaskId',b],['bitacoraReadbackVerified',false],
    ['prHeadReadbackVerified',false]]){
    const bad={...t,[key]:value};
    if(validValidationRelease(bad))throw Error('release bypass: '+key);
  }
  if(reviewReleaseEligible({...t,reviewerLeaseAvailable:false},b)||
    reviewReleaseEligible({...t,conflictingClaim:true},b)||
    reviewReleaseEligible(t,b,new Set([t.id])))
      throw Error('duplicate or busy review accepted');
  if(!isStageOwnedBy({...t,stageReleaseRequested:false,ownerTaskId:a},a))
      throw Error('legacy VALIDANDO should remain locked until a verified receipt');
  const rel={scopeId:'P1-010.B',authorTaskId:a,requesterTaskId:a,
    fromState:'EN_PROGRESO',toState:'VALIDANDO',
    workDeliveredVerified:true,deliveryTestsVerified:true,exactHeadVerified:true,
    branchReadbackVerified:true,provenancePreserved:true,
    activeEditorVerifiedAbsent:true,previousLeaseReleased:true,
    releaseLockAcquired:true,noConflictingClaim:true,githubReadbackVerified:true,
    planReadbackVerified:true,bitacoraReadbackVerified:true,
    releaseVersionVerified:true,releaseEventId:'release-event',
    stageOwnerTaskId:null,reviewReservationTaskId:null};
  if(validateReleaseTransition(rel).length)throw Error('valid stage transition denied');
  for(const key of ['previousLeaseReleased','planReadbackVerified',
    'githubReadbackVerified','releaseLockAcquired','deliveryTestsVerified']){
    if(!validateReleaseTransition({...rel,[key]:false}).length)
      throw Error('unsafe release transition accepted: '+key);
  }
  const rev={scopeId:'P1-010.B',authorTaskId:a,reviewerTaskId:b,
    releaseReadbackVerified:true,provenancePreserved:true,
    reviewLeaseAcquiredExclusively:true,reviewVersionVerified:true,
    reviewerOwnershipReadbackVerified:true,reviewerOwnedScopeCount:0,
    reviewerActiveWriteCount:0,globalOpenScopesReadbackVerified:true,
    globalOpenScopeCount:6,
    noConflictingReviewer:true,previousLeaseReleased:true,
    ownerTransferReadbackVerified:true,planReadbackVerified:true,
    bitacoraReadbackVerified:true,exactHeadVerified:true,reviewPermissionVerified:true};
  if(validateReviewReservation(rev).length)throw Error('valid review reservation denied');
  if(!validateReviewReservation({...rev,reviewerTaskId:a}).length ||
    !validateReviewReservation({...rev,noConflictingReviewer:false}).length ||
    !validateReviewReservation({...rev,reviewerOwnedScopeCount:1}).length ||
    !validateReviewReservation({...rev,globalOpenScopeCount:7}).length)
      throw Error('unsafe reviewer accepted');
}
