import test from 'node:test';
import assert from 'node:assert/strict';
import { assessG0006Closure } from '../quality/g0-006-a-closure.mjs';

const tree = 'a'.repeat(40), sha = 'b'.repeat(40), task = 'c'.repeat(32);
const proof = () => ({
  scopeId: 'G0-006.A', requirementId: 'G0-006.A', qualityProtocol: 'V13_A0_A5',
  integrationMode: 'DIRECT_DEV_FOUR_AUDITS', state: 'LISTO', decision: 'INCLUDE',
  authorTaskId: task, candidateTreeSha: tree, publishedSha: sha, devReadbackSha: sha,
  fastForwardVerified: true, diffReviewed: true, rollback: 'git revert ' + sha,
  predecessors: ['C', 'D', 'E'].map(letter => ({ id: 'G0-006.' + letter, state: 'LISTO',
    sha: letter.toLowerCase().repeat(40), sheetReadback: true,
    testPath: ({ C: 'scripts/tests/g0-006-c-trazabilidad.test.mjs',
      D: 'scripts/tests/g0-006-d-trazabilidad-direct-dev.test.mjs',
      E: 'scripts/tests/g0-006-e-historical-phase-negative.test.mjs' })[letter] })),
  matrix: [{ requirementId: 'G0-006.A', consumer: 'Validation gate for requirement to source to tests',
    codePath: 'scripts/quality/g0-006-a-closure.mjs',
    testPath: 'scripts/tests/g0-006-a-closure.test.mjs', sha, decision: 'INCLUDE' }],
  audits: Object.fromEntries(['static', 'functional', 'security', 'integration', 'postIntegration']
    .map(name => [name, { status: 'PASS', sha: name === 'postIntegration' ? sha : tree,
      evidence: 'Executed directed ' + name + ' checks on exact candidate source',
      ...(name === 'static' ? { tools: [{ name: 'Node.js', version: '22.16.0',
        command: 'node --check scripts/quality/g0-006-a-closure.mjs', result: 'PASS' }] } : {}) }])),
  a5: { status: 'A5_PENDIENTE', sha, reviewerTaskId: null,
    nextAction: 'Different TASK_ID must independently audit exact published SHA' },
  negativeLegacyPhaseTested: true, negativeMissingEvidenceTested: true,
  planReadback: true, colaReadback: true, dependencyFormulaReadback: true,
  bitacoraReadback: true, eventUnique: true, eventId: 'SQ-G0-006-A-J20-DIRECT-BBBBBBBB-20261112',
});

// Synthetic controls only. These cannot certify real GitHub commits or Sheet updates.
test('G0-006.A: complete A0-A4, A5 pending and C/D/E receipts accepted', () => {
  assert.deepEqual(assessG0006Closure(proof()), { verified: true, errors: [] });
});

test('G0-006.A: rejects obsolete, incomplete and self-approved receipts', () => {
  const mutations = [
    p => { p.scopeId = 'FASE8-001.A'; },
    p => { p.qualityProtocol = 'V13_OLD'; },
    p => { p.state = 'VALIDANDO'; },
    p => { p.decision = 'DEFER'; },
    p => { p.devReadbackSha = 'd'.repeat(40); },
    p => { p.fastForwardVerified = false; },
    p => { p.predecessors[0].sheetReadback = false; },
    p => { p.predecessors[1].sha = 'bad'; },
    p => { p.predecessors.push(p.predecessors[0]); },
    p => { p.matrix[0].codePath = '../legacy/secret'; },
    p => { p.matrix[0].consumer = ''; },
    p => { p.audits.static.status = 'FAIL'; },
    p => { p.audits.static.tools = []; },
    p => { p.audits.static.tools = [{ name: undefined, version: undefined, command: undefined, result: 'PASS' }]; },
    p => { p.predecessors[0].testPath = 'scripts/tests/g0-006-fake.test.mjs'; },
    p => { p.audits.security.sha = 'd'.repeat(40); },
    p => { p.audits.integration.evidence = ''; },
    p => { p.audits.postIntegration.sha = tree; },
    p => { p.a5.status = 'A5_PASS'; },
    p => { p.a5.reviewerTaskId = task; },
    p => { p.negativeLegacyPhaseTested = false; },
    p => { p.bitacoraReadback = false; },
    p => { p.eventUnique = false; },
  ];
  for (const [index, mutate] of mutations.entries()) {
    const fixture = proof(); mutate(fixture);
    assert.equal(assessG0006Closure(fixture).verified, false, 'negative case ' + (index + 1));
  }
});

test('G0-006.A: missing or malformed receipt never passes', () => {
  for (const fixture of [null, {}, [], { scopeId: 'G0-006.A' }]) {
    assert.equal(assessG0006Closure(fixture).verified, false);
  }
});
