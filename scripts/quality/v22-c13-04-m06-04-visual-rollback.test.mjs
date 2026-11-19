import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { existsSync, mkdtempSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import test from 'node:test';

// Actual Git rollback of synthetic visual-feature toggle in disposable repo.
function git(cwd, ...args) {
  const p = spawnSync('git', args, {
    cwd, encoding: 'utf8',
    env: { ...process.env, GIT_TERMINAL_PROMPT: '0' },
  });
  if (p.status !== 0) throw new Error('git ' + args[0] + ': ' + p.stderr.trim());
  return p.stdout.trim();
}
function put(root, name, contents) {
  const path = join(root, name);
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, contents, 'utf8');
}
const digest = bytes => createHash('sha256').update(bytes).digest('hex');
function setup(t) {
  const root = mkdtempSync(join(tmpdir(), 'sq-c13-visual-rollback-'));
  t.after(() => rmSync(root, { recursive: true, force: true }));
  git(root, 'init', '-q', '-b', 'dev');
  git(root, 'config', 'user.name', 'SOLQARYN visual rollback isolated');
  git(root, 'config', 'user.email', 'isolated@example.invalid');
  put(root, 'site/visual-flags.json', '{"catalogEnabled":true,"source":"synthetic"}\n');
  put(root, 'tenants/A/theme.css', ':root{--brand-tone:blue}\n');
  put(root, 'tenants/B/theme.css', ':root{--brand-tone:green}\n');
  put(root, 'evidence/screens.jsonl', '{"screen":"catalog","sha":"synthetic"}\n');
  put(root, 'evidence/bitacora.jsonl', '{"event_id":"V0","result":"baseline"}\n');
  git(root, 'add', '--', 'site', 'tenants', 'evidence');
  git(root, 'commit', '-q', '-m', 'baseline visual evidence and synthetic tenant state');
  put(root, 'site/visual-flags.json', '{"catalogEnabled":false,"source":"synthetic"}\n');
  put(root, 'site/disable-note.md', 'Ephemeral visual disable test only\n');
  git(root, 'add', '--', 'site');
  git(root, 'commit', '-q', '-m', 'synthetic visual feature deactivate');
  const disableSha = git(root, 'rev-parse', 'HEAD');
  put(root, 'tenants/A/theme.css', ':root{--brand-tone:blue;--spacing:compact}\n');
  put(root, 'evidence/bitacora.jsonl',
    '{"event_id":"V0","result":"baseline"}\n' +
    '{"event_id":"V1","result":"independent_change"}\n');
  git(root, 'add', '--', 'tenants/A/theme.css', 'evidence/bitacora.jsonl');
  git(root, 'commit', '-q', '-m', 'legitimate visual improvement');
  return { root, disableSha, finalSha: git(root, 'rev-parse', 'HEAD') };
}
function safeRevert(root, sha, expectedHead) {
  assert.match(sha, /^[0-9a-f]{40}$/, 'invalid source SHA');
  assert.equal(git(root, 'rev-parse', 'HEAD'), expectedHead,
    'stale lease: concurrent HEAD changes must not be ignored');
  assert.equal(git(root, 'status', '--porcelain'), '', 'refuse dirty worktree');
  git(root, 'merge-base', '--is-ancestor', sha, 'HEAD');
  const changed = git(root, 'diff-tree', '--no-commit-id', '--name-only', '-r', sha)
    .split('\n').filter(Boolean).sort();
  assert.deepEqual(changed, ['site/disable-note.md', 'site/visual-flags.json'],
    'unsafe changed-file set refused');
  git(root, 'revert', '--no-edit', sha);
}
test('real revert reactivates visual catalog and preserves tenants/evidence', t => {
  const f = setup(t);
  const paths = ['tenants/A/theme.css', 'tenants/B/theme.css',
    'evidence/screens.jsonl', 'evidence/bitacora.jsonl'];
  const before = paths.map(p => digest(readFileSync(join(f.root, p))));
  safeRevert(f.root, f.disableSha, f.finalSha);
  assert.equal(readFileSync(join(f.root, 'site/visual-flags.json'), 'utf8'),
    '{"catalogEnabled":true,"source":"synthetic"}\n');
  assert.equal(existsSync(join(f.root, 'site/disable-note.md')), false);
  assert.deepEqual(paths.map(p => digest(readFileSync(join(f.root, p)))), before);
  assert.match(readFileSync(join(f.root, 'evidence/bitacora.jsonl'), 'utf8'),
    /"event_id":"V1"/);
  assert.equal(git(f.root, 'status', '--porcelain'), '');
  assert.equal(git(f.root, 'rev-list', '--count', 'HEAD'), '4');
  console.log('REAL_VISUAL_REVERT=PASS;TENANTS_A_B=UNCHANGED;EVIDENCE_LOGS=PRESERVED');
});
test('negative: reverting tenant and evidence edit is forbidden', t => {
  const f = setup(t);
  assert.throws(() => safeRevert(f.root, f.finalSha, f.finalSha),
    /unsafe changed-file set refused/);
  assert.equal(git(f.root, 'rev-parse', 'HEAD'), f.finalSha);
});
test('negative: stale branch lease fails before revert', t => {
  const f = setup(t);
  assert.throws(() => safeRevert(f.root, f.disableSha, f.disableSha),
    /stale lease/);
  assert.equal(git(f.root, 'rev-parse', 'HEAD'), f.finalSha);
  assert.match(readFileSync(join(f.root, 'site/visual-flags.json'), 'utf8'),
    /"catalogEnabled":false/);
});
test('negative: malformed SHA and unsaved changes reject rollback', t => {
  const f = setup(t);
  assert.throws(() => safeRevert(f.root, '../HEAD', f.finalSha), /invalid source SHA/);
  put(f.root, 'unsaved-note.txt', 'synthetic unsaved evidence\n');
  assert.throws(() => safeRevert(f.root, f.disableSha, f.finalSha),
    /refuse dirty worktree/);
  assert.equal(git(f.root, 'rev-parse', 'HEAD'), f.finalSha);
});
