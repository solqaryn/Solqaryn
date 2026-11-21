import { spawnSync } from 'node:child_process';
import { mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

// C14-01.M06.04: exact-SHA test-only rollback on isolated CI filesystem.
// Never push, mutate production data or change a shared dev branch.
export const SOURCE = 'fc611e676aa1faa93101419f951baf701262956d';
export const EXPECTED = Object.freeze([
  'CHANGELOG_AI.md',
  'docs/v5/c14-01-m05-06-real-regression-a11y-v22.md',
  'frontend/e2e/c14-01-m05-06-real-regression-a11y.spec.ts'
]);
export function onlyExpectedPaths(paths) {
  return Array.isArray(paths) &&
    paths.length === EXPECTED.length &&
    new Set(paths).size === EXPECTED.length &&
    paths.every(path => typeof path === 'string' && EXPECTED.includes(path));
}
function invariant(condition, reason) { if (!condition) throw new Error(reason); }
function git(args) {
  const p = spawnSync('git', args, { encoding: 'utf8', maxBuffer: 16_000_000 });
  if (p.status !== 0) throw new Error('GIT_ROLLBACK_REHEARSAL_FAILED: ' + args.join(' ') +
    ': ' + (p.stderr || p.error?.message || p.stdout).slice(0, 500));
  return p.stdout.trim();
}
export function runRehearsal() {
  invariant(process.env.CI === 'true' && /^[a-f0-9]{40}$/.test(process.env.GITHUB_SHA || ''),
    'GITHUB_HOSTED_EXACT_SHA_REQUIRED');
  const originalHead = git(['rev-parse', 'HEAD']);
  invariant(originalHead === process.env.GITHUB_SHA, 'SOURCE_RUNNER_HEAD_MISMATCH');
  invariant(git(['status', '--porcelain']) === '', 'SOURCE_CHECKOUT_NOT_CLEAN');
  invariant(git(['cat-file', '-t', SOURCE]) === 'commit', 'ROLLBACK_SOURCE_COMMIT_MISSING');
  const originalParent = git(['rev-parse', SOURCE + '^']);
  const root = mkdtempSync(join(tmpdir(), 'sq-c1401-m0604-'));
  const scratch = join(root, 'detached');
  let attached = false;
  try {
    git(['worktree', 'add', '--detach', scratch, SOURCE]);
    attached = true;
    invariant(git(['-C', scratch, 'rev-parse', 'HEAD']) === SOURCE,
      'ROLLBACK_WORKTREE_WRONG_SOURCE');
    git(['-C', scratch, 'revert', '--no-commit', SOURCE]);
    const staged = git(['-C', scratch, 'diff', '--cached', '--name-only'])
      .split('\n').map(v => v.trim()).filter(Boolean);
    invariant(onlyExpectedPaths(staged), 'ROLLBACK_UNEXPECTED_PATHS:' + staged.join(','));
    for (const filename of EXPECTED) {
      const actual = git(['-C', scratch, 'rev-parse', ':' + filename]);
      const expected = git(['-C', scratch, 'rev-parse', originalParent + ':' + filename]);
      invariant(actual === expected, 'ROLLBACK_PARENT_BLOB_MISMATCH:' + filename);
    }
    console.log('C1401_M0604_REAL_GIT_REVERT_PASS source=' + SOURCE +
      ' parent=' + originalParent + ' staged=' + staged.length + '/3' +
      ' original_parent_blobs=3/3 data_write=none');
  } finally {
    if (attached) git(['worktree', 'remove', '--force', scratch]);
    rmSync(root, { recursive: true, force: true });
  }
  invariant(git(['rev-parse', 'HEAD']) === originalHead, 'RUNNER_MAIN_HEAD_CHANGED');
  invariant(git(['status', '--porcelain']) === '', 'RUNNER_MAIN_DIRTY_AFTER_ROLLBACK');
}
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  runRehearsal();
}
