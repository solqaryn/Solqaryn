import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import test from 'node:test';

const hex = data => createHash('sha256').update(data).digest('hex');
function git(root, ...args) {
  const p = spawnSync('git', args, { cwd: root, encoding: 'utf8', env: { ...process.env, GIT_TERMINAL_PROMPT: '0' } });
  assert.equal(p.status, 0, 'git ' + args.join(' ') + ': ' + p.stderr);
  return p.stdout.trim();
}
function put(root, path, contents) {
  const full = join(root, path);
  mkdirSync(dirname(full), { recursive: true });
  writeFileSync(full, contents, 'utf8');
}
const read = (root, path) => readFileSync(join(root, path), 'utf8');
const ids = Array.from({ length: 2340 }, (_, index) => ({
  id: 'SYNTH-' + String(index + 1).padStart(4, '0'),
  state: 'LISTO', tenant: index % 2 ? 'B' : 'A',
}));
function setup(t) {
  const root = mkdtempSync(join(tmpdir(), 'sq-c15-m06-rollback-'));
  t.after(() => rmSync(root, { recursive: true, force: true }));
  git(root, 'init', '-q', '-b', 'dev');
  git(root, 'config', 'user.name', 'Solqaryn ephemeral');
  git(root, 'config', 'user.email', 'test@example.invalid');
  put(root, 'plan/ids.json', JSON.stringify(ids) + '\n');
  put(root, 'evidence/bitacora.jsonl', '{"event":"baseline"}\n');
  put(root, 'evidence/mapping.json', '{"count":2340,"scope":"synthetic"}\n');
  put(root, 'features/coverage.json', '{"enabled":true,"scope":"synthetic"}\n');
  git(root, 'add', '--', 'plan', 'evidence', 'features');
  git(root, 'commit', '-q', '-m', 'baseline synthetic coverage ledger');
  put(root, 'features/coverage.json', '{"enabled":false,"scope":"synthetic"}\n');
  git(root, 'add', '--', 'features/coverage.json');
  git(root, 'commit', '-q', '-m', 'disable synthetic coverage');
  const disable = git(root, 'rev-parse', 'HEAD');
  put(root, 'evidence/bitacora.jsonl', '{"event":"baseline"}\n{"event":"independent-change"}\n');
  put(root, 'evidence/mapping.json', '{"count":2340,"scope":"synthetic","independent":true}\n');
  git(root, 'add', '--', 'evidence');
  git(root, 'commit', '-q', '-m', 'concurrent evidence preservation');
  return { root, disable, head: git(root, 'rev-parse', 'HEAD') };
}
function safeRevert(root, sha, expectedHead) {
  assert.match(sha, /^[0-9a-f]{40}$/, 'invalid source SHA');
  assert.equal(git(root, 'rev-parse', 'HEAD'), expectedHead, 'stale lease');
  assert.equal(git(root, 'status', '--porcelain'), '', 'dirty worktree');
  git(root, 'merge-base', '--is-ancestor', sha, 'HEAD');
  const changed = git(root, 'diff-tree', '--no-commit-id', '--name-only', '-r', sha).split('\n').filter(Boolean).sort();
  assert.deepEqual(changed, ['features/coverage.json'], 'unsafe changed-file set');
  git(root, 'revert', '--no-edit', sha);
}
test('real Git rollback preserves all 2340 synthetic IDs and latest evidence SHA256', t => {
  const f = setup(t);
  const paths = ['plan/ids.json', 'evidence/bitacora.jsonl', 'evidence/mapping.json'];
  const before = paths.map(p => hex(read(f.root, p)));
  assert.equal(JSON.parse(read(f.root, 'plan/ids.json')).length, 2340);
  safeRevert(f.root, f.disable, f.head);
  assert.equal(JSON.parse(read(f.root, 'features/coverage.json')).enabled, true);
  assert.deepEqual(paths.map(p => hex(read(f.root, p))), before);
  assert.match(read(f.root, 'evidence/bitacora.jsonl'), /independent-change/);
  assert.equal(git(f.root, 'rev-list', '--count', 'HEAD'), '4');
  assert.equal(git(f.root, 'status', '--porcelain'), '');
  console.log('REAL_GIT_REVERT_PASS;SYNTHETIC_IDS=2340;EVIDENCE_SHA256=PRESERVED');
});
test('negative: refuse reverting ledger and evidence commit', t => {
  const f = setup(t);
  assert.throws(() => safeRevert(f.root, f.head, f.head), /unsafe changed-file set/);
  assert.equal(git(f.root, 'rev-parse', 'HEAD'), f.head);
});
test('negative: stale branch HEAD refuses rollback', t => {
  const f = setup(t);
  assert.throws(() => safeRevert(f.root, f.disable, f.disable), /stale lease/);
  assert.equal(git(f.root, 'rev-parse', 'HEAD'), f.head);
  assert.equal(JSON.parse(read(f.root, 'features/coverage.json')).enabled, false);
});
test('negative: malformed SHA or dirty worktree rejects before mutation', t => {
  const f = setup(t);
  assert.throws(() => safeRevert(f.root, '../HEAD', f.head), /invalid source SHA/);
  put(f.root, 'evidence/untracked.txt', 'unsaved synthetic note\n');
  assert.throws(() => safeRevert(f.root, f.disable, f.head), /dirty worktree/);
  assert.equal(git(f.root, 'rev-parse', 'HEAD'), f.head);
});
