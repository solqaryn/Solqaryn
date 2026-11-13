// G0-006.D: advisory traceability of direct-dev releases. This function never
// authorizes a commit or marks a Sheet row LISTO; it rejects incomplete receipts.
const SCOPE = /^(?:P\d+|I8|G0)-\d{3}\.[A-Z]$/;
const SHA = /^[a-f0-9]{40}$/i;
const SAFE_PATH = /^(?:frontend|backend|scripts)\/[a-zA-Z0-9_.\/-]+$/;
const AUDITS = ['functional', 'security', 'integration', 'postIntegration'];
const validPaths = paths => Array.isArray(paths) && paths.length > 0 &&
  paths.every(path => typeof path === 'string' && SAFE_PATH.test(path) &&
    !path.includes('//') && !path.includes('/../'));

// G0-006.G: deny disguised reopen requests for retired phase 8/13 or the
// completed Angular migration, even when a current commercial scope ID is used.
// Only structured authority/action fields are inspected: archived background
// notes in evidence strings must not accidentally become an authorization.
const RETIRED_AUTHORITY = /(?:^|[^a-z0-9])(?:fase|phase)[\s_-]*(?:8|13)(?:$|[^a-z0-9])|(?:^|[^a-z0-9])m13(?:$|[^a-z0-9])|(?:^|[^a-z0-9])vaep(?:$|[^a-z0-9])|(?:^|[^a-z0-9])jules(?:$|[^a-z0-9])|angular[\s_-]*migration/i;
const LEGACY_FIELDS = ['sourceAuthority', 'phaseId', 'legacyPhase', 'reopenSource', 'migrationTarget', 'requestedAction'];
const legacyReopen = receipt => receipt.reopenCompletedMigration === true ||
  LEGACY_FIELDS.some(key => typeof receipt[key] === 'string' &&
    (RETIRED_AUTHORITY.test(receipt[key]) ||
      /\breopen[\s_-]+(?:completed[\s_-]+)?(?:angular|migration|phase)\b/i.test(receipt[key])));

export function verifyDirectDevTraceability(receipt) {
  const errors = [];
  const requireCheck = (ok, code) => { if (!ok) errors.push(code); };
  const r = receipt && typeof receipt === 'object' && !Array.isArray(receipt) ? receipt : {};
  requireCheck(SCOPE.test(r.scopeId || '') && r.requirementId === r.scopeId &&
    r.state === 'LISTO' && r.integrationMode === 'DIRECT_DEV_FOUR_AUDITS', 'contract');
  requireCheck(!legacyReopen(r), 'legacy_reopen');
  requireCheck(SHA.test(r.publishedSha || '') && r.publishedSha === r.devReadbackSha &&
    r.commitFastForwardVerified === true && r.devBranchVerified === true,
    'dev_publication');
  requireCheck(SHA.test(r.candidateTreeSha || '') &&
    r.candidateTreeSha === r.publishedTreeSha && r.diffReviewed === true,
    'source_equivalence');
  requireCheck(validPaths(r.codePaths) && validPaths(r.testPaths), 'code_and_tests');
  requireCheck(Array.isArray(r.testRuns) && r.testRuns.length > 0 &&
    r.testRuns.every(t => t?.status === 'PASS' &&
      typeof t?.evidence === 'string' && t.evidence.trim().length >= 10 &&
      t?.treeSha === r.candidateTreeSha) && r.negativeCasesVerified === true,
    'causal_tests');
  for (const name of AUDITS) {
    const a = r.audits?.[name];
    const expected = name === 'postIntegration' ? r.publishedSha : r.candidateTreeSha;
    requireCheck(a?.status === 'PASS' && a?.sha === expected &&
      typeof a?.evidence === 'string' && a.evidence.trim().length >= 10,
      'audit_' + name);
  }
  requireCheck(r.planReadbackVerified === true && r.colaReadbackVerified === true &&
    r.dependencyFormulaVerified === true && r.bitacoraReadbackVerified === true &&
    r.eventUniqueVerified === true && typeof r.eventId === 'string' &&
    r.eventId.trim().length >= 10, 'institutional_readback');
  return { verified: errors.length === 0, errors };
}
