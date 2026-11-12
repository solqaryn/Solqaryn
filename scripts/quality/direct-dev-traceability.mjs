// G0-006.D: advisory traceability of direct-dev releases. This function never
// authorizes a commit or marks a Sheet row LISTO; it rejects incomplete receipts.
const SCOPE = /^(?:P\d+|I8|G0)-\d{3}\.[A-Z]$/;
const SHA = /^[a-f0-9]{40}$/i;
const SAFE_PATH = /^(?:frontend|backend|scripts)\/[a-zA-Z0-9_.\/-]+$/;
const AUDITS = ['functional', 'security', 'integration', 'postIntegration'];
const validPaths = paths => Array.isArray(paths) && paths.length > 0 &&
  paths.every(path => typeof path === 'string' && SAFE_PATH.test(path) &&
    !path.includes('//') && !path.includes('/../'));

export function verifyDirectDevTraceability(receipt) {
  const errors = [];
  const requireCheck = (ok, code) => { if (!ok) errors.push(code); };
  const r = receipt && typeof receipt === 'object' && !Array.isArray(receipt) ? receipt : {};
  requireCheck(SCOPE.test(r.scopeId || '') && r.requirementId === r.scopeId &&
    r.state === 'LISTO' && r.integrationMode === 'DIRECT_DEV_FOUR_AUDITS', 'contract');
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
