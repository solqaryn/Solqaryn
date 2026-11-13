// G0-006.B: structural evidence matrix for the current Commercial Plan only.
// Advisory: this never certifies GitHub, Sheets, A5 or QA/PROD by itself.
const PLAN_ID = '1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA';
const REQUIRED = new Map([
  ['G0-006.F', 'scripts/tests/g0-006-f-traceability-case.test.mjs'],
  ['G0-006.G', 'scripts/tests/g0-006-g-legacy-angular-reopen.test.mjs'],
  ['G0-006.H', 'scripts/tests/g0-006-h-positive-traceability.test.mjs'],
]);
const SHA = /^[0-9a-f]{40}$/;
const CURRENT_PATH = /^scripts\/(?:quality|tests)\/[a-z0-9._\/-]+\.mjs$/;
const LEGACY = /(?:^|[^a-z0-9])(?:fase|phase)[\s_-]*(?:8|13)(?:$|[^a-z0-9])|(?:^|[^a-z0-9])m13(?:$|[^a-z0-9])|angular[\s_-]*migration|\b(?:vaep|jules)\b/i;

export function assessG0006BMatrix(input) {
  const r = input && typeof input === 'object' && !Array.isArray(input) ? input : {};
  const errors = [];
  const requireCheck = (ok, code) => { if (!ok) errors.push(code); };
  requireCheck(r.scopeId === 'G0-006.B' && r.requirementId === r.scopeId &&
    r.integrationMode === 'DIRECT_DEV_FOUR_AUDITS' && r.state === 'LISTO', 'contract');
  requireCheck(r.planId === PLAN_ID && r.planAuthority === 'CURRENT_COMMERCIAL_PLAN_ONLY' &&
    r.futureMasterPlanExecutable === false, 'authority');
  requireCheck(r.reopenCompletedMigration !== true &&
    ![r.legacyPhase, r.requestedAction, r.sourceAuthority].some(x =>
      typeof x === 'string' && LEGACY.test(x)), 'legacy_reopen');
  const entries = Array.isArray(r.receipts) ? r.receipts : [];
  const ids = entries.map(x => x?.id);
  requireCheck(entries.length === REQUIRED.size && new Set(ids).size === REQUIRED.size &&
    [...REQUIRED.keys()].every(id => ids.includes(id)), 'predecessors');
  for (const [id, path] of REQUIRED) {
    const x = entries.find(v => v?.id === id);
    requireCheck(x?.state === 'LISTO' && x?.dependencyReady === 'SI' &&
      x?.planReadback === true && x?.colaReadback === true &&
      x?.bitacoraEventVerified === true, 'receipt_' + id);
    requireCheck(SHA.test(x?.publishedSha || '') &&
      x?.commitReadbackVerified === true && x?.testPath === path &&
      CURRENT_PATH.test(x.testPath) &&
      CURRENT_PATH.test(x?.codePath || '') &&
      typeof x?.evidence === 'string' && x.evidence.trim().length >= 12,
      'code_test_sha_' + id);
    requireCheck(['A5_PASS','A5_PENDIENTE'].includes(x?.a5Status) &&
      x?.promotionAuthorized !== true, 'a5_promotion_' + id);
  }
  requireCheck(r.negativeLegacyPhaseTested === true &&
    r.positiveExactShaTested === true, 'causal_tests');
  requireCheck(r.planReadbackVerified === true && r.colaReadbackVerified === true &&
    r.bitacoraReadbackVerified === true && r.eventUniqueVerified === true,
    'institutional_readback');
  return { verified: errors.length === 0, errors };
}
