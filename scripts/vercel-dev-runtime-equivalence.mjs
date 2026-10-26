#!/usr/bin/env node
/**
 * SOLQARYN DEV: fail-closed proof that a READY Vercel deployment has the
 * identical frontend source tree as a newer governance-only DEV commit.
 * Never returns EXACT_SHA_READY, commercial LISTO, or a functional test PASS.
 */
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const SHA = /^[a-f0-9]{40}$/;
const REPO = 'solqaryn/Solqaryn';
const PROJECT_ID = 'prj_1Anhx5mWyXEBX89lWC24Py6JXe7A';
const OFFICIAL_ALIAS = 'solqaryn-dev.vercel.app';
const NON_RUNTIME_PREFIXES = ['docs/', '.github/', '.agents/', 'scripts/', 'vaep/'];
const NON_RUNTIME_FILES = new Set([
  'AGENTS.md', 'PROJECT_CONTEXT.md', 'PROJECT_INDEX.md', 'ARCHITECTURE.md',
  'ARCHITECTURE_CHANGELOG.md', 'TASKS.md', 'CHANGELOG_AI.md', 'README.md'
]);
export function isNonRuntimePath(file) {
  return typeof file === 'string' && !file.includes('..') &&
    (NON_RUNTIME_PREFIXES.some(prefix => file.startsWith(prefix)) ||
      NON_RUNTIME_FILES.has(file));
}
function manifest(entries, reasons, label) {
  if (!Array.isArray(entries) || entries.length < 3 || entries.length > 200) {
    reasons.push(label + '_INVALID');
    return null;
  }
  const found = new Set();
  const result = [];
  for (const item of entries) {
    const path = item?.path;
    if (typeof path !== 'string' || !/^frontend\/[^/]+$/.test(path) ||
        found.has(path) || !SHA.test(item?.sha ?? '') ||
        !['file', 'dir'].includes(item?.type)) {
      reasons.push(label + '_ENTRY_INVALID');
      return null;
    }
    found.add(path);
    result.push(path + ':' + item.type + ':' + item.sha);
  }
  return result.sort().join('\n');
}
export function assessDevSourceEquivalence(proof) {
  const reasons = [];
  const head = proof?.head_sha;
  const ready = proof?.ready;
  const compare = proof?.compare;
  if (proof?.repository !== REPO || proof?.branch !== 'dev') reasons.push('REPOSITORY_OR_BRANCH_MISMATCH');
  if (!SHA.test(head ?? '')) reasons.push('HEAD_INVALID');
  if (proof?.project_id !== PROJECT_ID || ready?.project_id !== PROJECT_ID ||
      ready?.project_name !== 'solqaryn-dev') reasons.push('PROJECT_MISMATCH');
  if (ready?.state !== 'READY' || ready?.branch !== 'dev' || ready?.target !== 'production' ||
      !/^dpl_[A-Za-z0-9]+$/.test(ready?.id ?? '') ||
      !Array.isArray(ready?.aliases) || !ready.aliases.includes(OFFICIAL_ALIAS)) {
    reasons.push('READY_DEPLOYMENT_NOT_ATTESTED');
  }
  if (!SHA.test(ready?.sha ?? '') || ready?.sha === head) reasons.push('READY_SHA_INVALID');
  if (compare?.status !== 'ahead' || !Number.isInteger(compare?.ahead_by) ||
      compare.ahead_by < 1 || compare.ahead_by > 50 ||
      compare?.behind_by !== 0 || compare?.total_commits !== compare.ahead_by) {
    reasons.push('NOT_LINEAR_ANCESTOR');
  }
  if (compare?.base !== ready?.sha || compare?.head !== head) reasons.push('COMPARE_REFS_MISMATCH');
  if (!Array.isArray(compare?.files) || compare.files.length < 1 || compare.files.length > 300 ||
      compare.files.some(file => !isNonRuntimePath(file?.filename) ||
        (file.previous_filename && !isNonRuntimePath(file.previous_filename)))) {
    reasons.push('RUNTIME_OR_UNBOUNDED_CHANGE');
  }
  const baseManifest = manifest(proof?.frontend_before, reasons, 'FRONTEND_BASE');
  const headManifest = manifest(proof?.frontend_after, reasons, 'FRONTEND_HEAD');
  if (baseManifest && headManifest && baseManifest !== headManifest) {
    reasons.push('FRONTEND_TREE_CHANGED');
  }
  const decision = reasons.length ? 'NO_GO' : 'SOURCE_EQUIVALENT';
  return {
    decision,
    reasons,
    frontend_manifest_sha256: decision === 'SOURCE_EQUIVALENT'
      ? createHash('sha256').update(baseManifest).digest('hex') : null
  };
}
function selfTest() {
  const old = 'a'.repeat(40), head = 'b'.repeat(40);
  const manifest = ['angular.json', 'src', 'vercel.json'].map((name, i) => ({
    path: 'frontend/' + name, type: i === 1 ? 'dir' : 'file', sha: String(i + 1).repeat(40)
  }));
  const positive = {
    repository: REPO, branch: 'dev', head_sha: head, project_id: PROJECT_ID,
    ready: { id: 'dpl_SafeReady123', sha: old, state: 'READY',
      project_id: PROJECT_ID, project_name: 'solqaryn-dev', branch: 'dev',
      target: 'production', aliases: [OFFICIAL_ALIAS] },
    compare: { base: old, head, status: 'ahead', ahead_by: 1, behind_by: 0,
      total_commits: 1, files: [{filename:'docs/CHANGE.md'}] },
    frontend_before: manifest, frontend_after: structuredClone(manifest)
  };
  assert.equal(assessDevSourceEquivalence(positive).decision, 'SOURCE_EQUIVALENT');
  const cases = [
    ['frontend changed', p => p.frontend_after[1].sha = '4'.repeat(40)],
    ['runtime file', p => p.compare.files = [{filename:'frontend/src/app.ts'}]],
    ['backend changed', p => p.compare.files = [{filename:'backend/src/API/Program.cs'}]],
    ['rename from runtime', p => p.compare.files = [{filename:'docs/new.md', previous_filename:'frontend/app.js'}]],
    ['diverged history', p => p.compare.behind_by = 1],
    ['foreign head', p => p.compare.head = 'f'.repeat(40)],
    ['foreign base', p => p.compare.base = 'e'.repeat(40)],
    ['no ahead', p => p.compare.ahead_by = 0],
    ['oversized history', p => p.compare.ahead_by = 51],
    ['canceled ready', p => p.ready.state = 'CANCELED'],
    ['wrong project', p => p.ready.project_id = 'prj_foreign'],
    ['wrong branch', p => p.ready.branch = 'main'],
    ['wrong target', p => p.ready.target = 'preview'],
    ['missing official alias', p => p.ready.aliases = ['elsewhere.vercel.app']],
    ['no diff files', p => p.compare.files = []],
    ['duplicate tree path', p => p.frontend_after.push(p.frontend_after[0])],
    ['invalid tree sha', p => p.frontend_before[0].sha = 'invalid'],
    ['unknown file', p => p.compare.files = [{filename:'render.yaml'}]],
    ['head equals ready', p => p.head_sha = old],
    ['different repository', p => p.repository = 'foreign/repo']
  ];
  for (const [name, change] of cases) {
    const caseProof = structuredClone(positive);
    change(caseProof);
    assert.equal(assessDevSourceEquivalence(caseProof).decision, 'NO_GO', name);
  }
  console.log('DEV_SOURCE_EQUIVALENCE_SELF_TEST=PASS negatives=' + cases.length);
}
if (process.argv[1] && fileURLToPath(import.meta.url) === resolve(process.argv[1])) {
  if (process.argv.includes('--self-test')) selfTest();
  else {
    console.error('Use this policy through an authenticated verifier; --self-test runs offline negatives.');
    process.exitCode = 2;
  }
}
