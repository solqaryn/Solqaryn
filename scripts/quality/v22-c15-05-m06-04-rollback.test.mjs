import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { existsSync, mkdtempSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import test from 'node:test';

// Actual git revert in a disposable local repository. No remote or live data.
function git(cwd, ...args) {
  const p = spawnSync('git', args, {
    cwd, encoding: 'utf8',
    env: { ...process.env, GIT_TERMINAL_PROMPT: '0' },
  });
  if (p.status !== 0) throw new Error('git ' + args[0] + ': ' + p.stderr.trim());
  return p.stdout.trim();
}
function put(root, name, content) {
  const dest = join(root, name);
  mkdirSync(dirname(dest), { recursive: true });
  writeFileSync(dest, content, 'utf8');
}
function hash(content) {
  return createHash('sha256').update(content).digest('hex');
}
function fixture(t) {
  const root = mkdtempSync(join(tmpdir(), 'sq-v22-rollback-'));
  t.after(() => rmSync(root, { recursive: true, force: true }));
  git(root, 'init', '-q', '-b', 'dev');
  git(root, 'config', 'user.name', 'SOLQARYN isolated rollback test');
  git(root, 'config', 'user.email', 'nonproduction@example.invalid');
  // All data below is synthetic.
  put(root, 'config/automation-state.json', '{"enabled":true,"scope":"synthetic"}\n');
  put(root, 'data/tenant-a.json', '{"tenant":"A","balance":10}\n');
  put(root, 'data/tenant-b.json', '{"tenant":"B","balance":20}\n');
  put(root, 'data/bitacora.jsonl', '{"event_id":"E0","action":"baseline"}\n');
  git(root, 'add', '--', 'config', 'data');
  git(root, 'commit', '-q', '-m', 'baseline synthetic data');
  put(root, 'config/automation-state.json', '{"enabled":false,"scope":"synthetic"}\n');
  put(root, 'docs/ops-note.md', '# Test-only suspension marker\n');
  git(root, 'add', '--', 'config/automation-state.json', 'docs/ops-note.md');
  git(root, 'commit', '-q', '-m', 'disable synthetic automation');
  const disableSha = git(root, 'rev-parse', 'HEAD');
  put(root, 'data/tenant-a.json', '{"tenant":"A","balance":15}\n');
  put(root, 'data/bitacora.jsonl',
    '{"event_id":"E0","action":"baseline"}\n' +
    '{"event_id":"E1","action":"legitimate_update"}\n');
  git(root, 'add', '--', 'data');
  git(root, 'commit', '-q', '-m', 'independent legitimate tenant event');
  return { root, disableSha, legitimateSha: git(root, 'rev-parse', 'HEAD') };
}
function guardedRevert(root, sourceSha, expectedHead) {
  assert.match(sourceSha, /^[a-f0-9]{40}$/, 'refuse absent/non-SHA target');
  assert.equal(git(root, 'rev-parse', 'HEAD'), expectedHead,
    'stale HEAD: refuse to touch concurrent work');
  assert.equal(git(root, 'status', '--porcelain'), '',
    'dirty worktree: refuse rollback');
  git(root, 'merge-base', '--is-ancestor', sourceSha, 'HEAD');
  const actual = git(root, 'diff-tree', '--no-commit-id', '--name-only', '-r', sourceSha)
    .split('\n').filter(Boolean).sort();
  assert.deepEqual(actual, ['config/automation-state.json', 'docs/ops-note.md'],
    'only isolated operational files may be reverted');
  git(root, 'revert', '--no-edit', sourceSha);
}
test('real rollback restores enablement and retains A/B data and events', t => {
  const f = fixture(t);
  const beforeA = hash(readFileSync(join(f.root, 'data/tenant-a.json')));
  const beforeB = hash(readFileSync(join(f.root, 'data/tenant-b.json')));
  const beforeLog = hash(readFileSync(join(f.root, 'data/bitacora.jsonl')));
  guardedRevert(f.root, f.disableSha, f.legitimateSha);
  assert.equal(readFileSync(join(f.root, 'config/automation-state.json'), 'utf8'),
    '{"enabled":true,"scope":"synthetic"}\n');
  assert.equal(existsSync(join(f.root, 'docs/ops-note.md')), false);
  assert.equal(hash(readFileSync(join(f.root, 'data/tenant-a.json'))), beforeA);
  assert.equal(hash(readFileSync(join(f.root, 'data/tenant-b.json'))), beforeB);
  assert.equal(hash(readFileSync(join(f.root, 'data/bitacora.jsonl'))), beforeLog);
  assert.match(readFileSync(join(f.root, 'data/bitacora.jsonl'), 'utf8'),
    /"event_id":"E1"/);
  assert.equal(git(f.root, 'status', '--porcelain'), '');
  assert.equal(git(f.root, 'rev-list', '--count', 'HEAD'), '4');
  console.log('REAL_GIT_REVERT=PASS;TENANT_A_B_HASHES=PASS;BITACORA_E0_E1=PASS');
});
test('negative: protected tenant-data commit is never reverted', t => {
  const f = fixture(t);
  const head = git(f.root, 'rev-parse', 'HEAD');
  assert.throws(() => guardedRevert(f.root, f.legitimateSha, head),
    /only isolated operational files/);
  assert.equal(git(f.root, 'rev-parse', 'HEAD'), head);
  assert.match(readFileSync(join(f.root, 'data/bitacora.jsonl'), 'utf8'),
    /"event_id":"E1"/);
});
test('negative: stale HEAD refuses rollback without modifying rows', t => {
  const f = fixture(t);
  const head = git(f.root, 'rev-parse', 'HEAD');
  assert.throws(() => guardedRevert(f.root, f.disableSha, f.disableSha),
    /stale HEAD/);
  assert.equal(git(f.root, 'rev-parse', 'HEAD'), head);
  assert.equal(readFileSync(join(f.root, 'config/automation-state.json'), 'utf8'),
    '{"enabled":false,"scope":"synthetic"}\n');
});
test('negative: unknown SHA and dirty worktree fail closed', t => {
  const f = fixture(t);
  const head = git(f.root, 'rev-parse', 'HEAD');
  assert.throws(() => guardedRevert(f.root, 'not-a-sha', head), /non-SHA/);
  put(f.root, 'untracked-diagnostic.txt', 'synthetic unsaved change\n');
  assert.throws(() => guardedRevert(f.root, f.disableSha, head), /dirty worktree/);
  assert.equal(git(f.root, 'rev-parse', 'HEAD'), head);
});
