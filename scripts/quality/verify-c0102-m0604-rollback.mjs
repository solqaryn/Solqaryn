import { spawnSync } from 'node:child_process';
import { mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

// C01-02.M06.04: ephemeral, GitHub-hosted rehearsal ONLY. No shared-branch writes.
export const SOURCE = '844d7fead192cf0f661ac8ae6f1b934860eca6ab';
export const EXPECTED = Object.freeze([
  'CHANGELOG_AI.md',
  'docs/v5/c01-02-m05-06-regression-performance-a11y-v22.md',
  'frontend/e2e/c01-02-m05-06-regression-a11y.spec.ts'
]);
export function onlyExpectedPaths(paths) {
  return Array.isArray(paths) &&
    paths.length === EXPECTED.length &&
    new Set(paths).size === EXPECTED.length &&
    paths.every(path => EXPECTED.includes(path));
}
function git(args) {
  const p = spawnSync('git', args, { encoding: 'utf8', maxBuffer: 16_000_000 });
  if (p.status !== 0) throw new Error('GIT_REHEARSAL_FAILED: ' + args.join(' ') + ': ' +
    (p.stderr || p.error?.message || p.stdout).slice(0, 800));
  return p.stdout.trim();
}
function invariant(condition, reason) { if (!condition) throw new Error(reason); }
export function runRehearsal() {
  invariant(process.env.CI === 'true' && /^[a-f0-9]{40}$/.test(process.env.GITHUB_SHA || ''),
    'GITHUB_HOSTED_EXACT_SHA_REQUIRED');
  const before = git(['rev-parse', 'HEAD']);
  invariant(before === process.env.GITHUB_SHA, 'CHECKOUT_SHA_MISMATCH');
  invariant(git(['status', '--porcelain']) === '', 'BASE_TREE_DIRTY');
  invariant(git(['cat-file', '-t', SOURCE]) === 'commit', 'SOURCE_COMMIT_NOT_PRESENT');
  const parent = git(['rev-parse', SOURCE + '^']);
  const dir = mkdtempSync(join(tmpdir(), 'sqv22-c0102-m0604-'));
  const scratch = join(dir, 'detached');
  let added = false;
  try {
    git(['worktree', 'add', '--detach', scratch, SOURCE]);
    added = true;
    invariant(git(['-C', scratch, 'rev-parse', 'HEAD']) === SOURCE, 'SOURCE_TREE_MISMATCH');
    git(['-C', scratch, 'revert', '--no-commit', SOURCE]);
    const files = git(['-C', scratch, 'diff', '--cached', '--name-only']).split('\n')
      .map(x => x.trim()).filter(Boolean);
    invariant(onlyExpectedPaths(files), 'UNSAFE_ROLLBACK_SCOPE:' + files.join(','));
    for (const path of EXPECTED) {
      const staged = git(['-C', scratch, 'rev-parse', ':' + path]);
      const expectedParent = git(['-C', scratch, 'rev-parse', parent + ':' + path]);
      invariant(staged === expectedParent, 'ROLLBACK_BLOB_MISMATCH:' + path);
    }
    // No database connection, SQL statement, branch, remote or production write occurs.
    console.log('ROLLBACK_DRYRUN_PASS source=' + SOURCE +
      ' parent=' + parent + ' files=' + files.length +
      ' full_parent_blobs_match=3 legitimate_data_unmodified_by_design=true');
  } finally {
    if (added) git(['worktree', 'remove', '--force', scratch]); // Only a temporary scratch worktree.
    rmSync(dir, { recursive: true, force: true });
  }
  invariant(git(['rev-parse', 'HEAD']) === before, 'DEV_SHA_CHANGED');
  invariant(git(['status', '--porcelain']) === '', 'REHEARSAL_LEFT_DIRTY_TREE');
}
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  runRehearsal();
}
