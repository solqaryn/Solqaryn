/** G0-006.A: advisory closure evidence gate. Does not write GitHub/Sheets or certify a live scope. */
const SHA = /^[0-9a-f]{40}$/;
const TASK = /^[0-9a-f]{32}$/;
const EXPECTED_TESTS = {
  'G0-006.C': 'scripts/tests/g0-006-c-trazabilidad.test.mjs',
  'G0-006.D': 'scripts/tests/g0-006-d-trazabilidad-direct-dev.test.mjs',
  'G0-006.E': 'scripts/tests/g0-006-e-historical-phase-negative.test.mjs',
};
const REQUIRED = Object.keys(EXPECTED_TESTS);
const PATH = /^scripts\/(?:quality|tests)\/[a-z0-9][a-z0-9.-]*\.mjs$/;
const AUDITS = ['static', 'functional', 'security', 'integration', 'postIntegration'];
const evidence = s => typeof s === 'string' && s.trim().length >= 20;

/** All values are supplied by independently authenticated readbacks; never infer PASS. */
export function assessG0006Closure(receipt) {
  const errors = [];
  const need = (condition, code) => { if (!condition) errors.push(code); };
  const p = receipt && typeof receipt === 'object' && !Array.isArray(receipt) ? receipt : {};
  need(p.scopeId === 'G0-006.A' && p.requirementId === p.scopeId &&
    p.qualityProtocol === 'V13_A0_A5' && p.integrationMode === 'DIRECT_DEV_FOUR_AUDITS' &&
    p.state === 'LISTO' && ['INCLUDE', 'REUSE'].includes(p.decision), 'scope_contract');
  need(TASK.test(p.authorTaskId ?? '') && SHA.test(p.candidateTreeSha ?? ''), 'candidate_identity');
  need(SHA.test(p.publishedSha ?? '') && p.devReadbackSha === p.publishedSha &&
    p.fastForwardVerified === true && p.diffReviewed === true &&
    p.rollback?.startsWith('git revert '), 'publication');
  need(Array.isArray(p.predecessors) && p.predecessors.length === REQUIRED.length &&
    REQUIRED.every(id => p.predecessors.filter(x => x?.id === id && x.state === 'LISTO' &&
      SHA.test(x.sha ?? '') && x.sheetReadback === true && x.testPath === EXPECTED_TESTS[id]).length === 1),
    'predecessor_receipts');
  need(Array.isArray(p.matrix) && p.matrix.length >= 1 && p.matrix.every(x =>
    x?.requirementId === p.scopeId && evidence(x.consumer) && PATH.test(x.codePath ?? '') &&
    PATH.test(x.testPath ?? '') && ['INCLUDE', 'REUSE'].includes(x.decision) &&
    SHA.test(x.sha ?? '')), 'trace_matrix');
  for (const name of AUDITS) {
    const a = p.audits?.[name];
    need(a?.status === 'PASS' && a.sha === (name === 'postIntegration' ? p.publishedSha : p.candidateTreeSha) &&
      evidence(a.evidence) && (name !== 'static' || (Array.isArray(a.tools) && a.tools.length > 0 &&
        a.tools.every(t => typeof t?.name === 'string' && t.name.length > 1 &&
        typeof t?.version === 'string' && t.version.length > 1 &&
        evidence(t.command) && t.result === 'PASS'))),
      'audit_' + name);
  }
  need(p.a5?.status === 'A5_PENDIENTE' && p.a5.sha === p.publishedSha &&
    p.a5.reviewerTaskId !== p.authorTaskId && evidence(p.a5.nextAction), 'a5_independent_pending');
  need(p.negativeLegacyPhaseTested === true && p.negativeMissingEvidenceTested === true,
    'mandatory_negatives');
  need(p.planReadback === true && p.colaReadback === true && p.dependencyFormulaReadback === true &&
    p.bitacoraReadback === true && p.eventUnique === true && evidence(p.eventId),
    'institutional_readback');
  return { verified: errors.length === 0, errors };
}
