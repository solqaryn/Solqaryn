import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import { existsSync, mkdtempSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import test from 'node:test';

// C14-02.M06.04: real git revert in a disposable Git repo; data is synthetic.
// Never runs against the checkout of SOLQARYN, a tenant database or a provider.
function git(cwd, ...args) {
  const p = spawnSync('git', args, {
    cwd, encoding: 'utf8',
    env: { ...process.env, GIT_TERMINAL_PROMPT: '0', GIT_CONFIG_NOSYSTEM: '1' },
  });
  if (p.status !== 0) throw new Error('git ' + args[0] + ' failed: ' + p.stderr.trim());
  return p.stdout.trim();
}
function put(root, name, content) {
  const dest = join(root, name);
  mkdirSync(dirname(dest), { recursive: true });
  writeFileSync(dest, content, 'utf8');
}
function digest(file) {
  return createHash('sha256').update(readFileSync(file)).digest('hex');
}
const protectedPaths = [
  'audit/bitacora.jsonl', 'backups/manifest.json', 'cache/tenant-b.json',
  'files/A/private.txt', 'files/B/private.txt', 'jobs/B/queue.jsonl',
  'sessions/A/session.json', 'tenants/A/object.json', 'tenants/B/object.json',
];
function fixture(t) {
  const root = mkdtempSync(join(tmpdir(), 'solqaryn-c14-02-rollback-'));
  t.after(() => rmSync(root, { recursive: true, force: true }));
  git(root, 'init', '-q', '-b', 'dev');
  git(root, 'config', 'user.name', 'SOLQARYN disposable rollback CI');
  git(root, 'config', 'user.email', 'isolated-ci@example.invalid');
  const seed = {
    'flags/security-audit.json': '{"enabled":true,"mode":"enforce","origin":"synthetic"}\n',
    'tenants/A/object.json': '{"tenant":"A","owner":"alice","version":1}\n',
    'tenants/B/object.json': '{"tenant":"B","owner":"bob","version":1}\n',
    'files/A/private.txt': 'A: synthetic private file\n',
    'files/B/private.txt': 'B: synthetic private file\n',
    'jobs/B/queue.jsonl': '{"tenant":"B","job":"B-1","status":"ready"}\n',
    'cache/tenant-b.json': '{"tenant":"B","cacheVersion":3}\n',
    'sessions/A/session.json': '{"tenant":"A","session":"old-synthetic"}\n',
    'backups/manifest.json': '{"snapshot":"test-only","checksum":"synthetic"}\n',
    'audit/bitacora.jsonl': '{"event_id":"E0","operation":"seed"}\n',
  };
  for (const [path, body] of Object.entries(seed)) put(root, path, body);
  git(root, 'add', '--', '.');
  git(root, 'commit', '-q', '-m', 'synthetic baseline: data and audit remain legitimate');

  put(root, 'flags/security-audit.json', '{"enabled":false,"mode":"quarantine","origin":"synthetic"}\n');
  put(root, 'flags/disabled-note.md', '# Synthetic security-audit deactivation\n');
  git(root, 'add', '--', 'flags');
  git(root, 'commit', '-q', '-m', 'disable only synthetic security-audit flag');
  const disableSha = git(root, 'rev-parse', 'HEAD');

  // Concurrent *legitimate* work occurs after the deactivation.
  put(root, 'tenants/A/object.json', '{"tenant":"A","owner":"alice","version":2}\n');
  put(root, 'jobs/B/queue.jsonl',
    '{"tenant":"B","job":"B-1","status":"ready"}\n' +
    '{"tenant":"B","job":"B-2","status":"queued"}\n');
  put(root, 'sessions/A/session.json', '{"tenant":"A","session":"rotated-synthetic"}\n');
  put(root, 'audit/bitacora.jsonl',
    '{"event_id":"E0","operation":"seed"}\n' +
    '{"event_id":"E1","operation":"legitimate_update"}\n');
  git(root, 'add', '--', 'tenants', 'jobs', 'sessions', 'audit');
  git(root, 'commit', '-q', '-m', 'legitimate later tenant and audit updates');
  return { root, disableSha, latestSha: git(root, 'rev-parse', 'HEAD') };
}
function revertGuarded(root, sourceSha, expectedHead) {
  assert.match(sourceSha, /^[a-f0-9]{40}$/, 'invalid exact commit SHA');
  assert.equal(git(root, 'rev-parse', 'HEAD'), expectedHead,
    'stale HEAD: another writer changed the disposable branch');
  assert.equal(git(root, 'status', '--porcelain'), '',
    'dirty worktree: refuse unsafe revert');
  git(root, 'merge-base', '--is-ancestor', sourceSha, 'HEAD');
  const changed = git(root, 'diff-tree', '--no-commit-id', '--name-only', '-r', sourceSha)
    .split('\n').filter(Boolean).sort();
  assert.deepEqual(changed, ['flags/disabled-note.md', 'flags/security-audit.json'],
    'revert must not touch tenant data, files, jobs, cache, sessions, backups or audit');
  // If another actor also edited these operational files, fail before git revert.
  for (const path of changed) {
    assert.equal(git(root, 'show', sourceSha + ':' + path), git(root, 'show', 'HEAD:' + path),
      'changed operational flag since deactivation: require separate reconciliation');
  }
  git(root, 'revert', '--no-edit', sourceSha);
}
test('real Git revert reactivates only security flag; all legitimate data survive byte-identical', t => {
  const f = fixture(t);
  const previous = protectedPaths.map(p => digest(join(f.root, p)));
  revertGuarded(f.root, f.disableSha, f.latestSha);
  assert.equal(readFileSync(join(f.root, 'flags/security-audit.json'), 'utf8'),
    '{"enabled":true,"mode":"enforce","origin":"synthetic"}\n');
  assert.equal(existsSync(join(f.root, 'flags/disabled-note.md')), false);
  assert.deepEqual(protectedPaths.map(p => digest(join(f.root, p))), previous);
  assert.match(readFileSync(join(f.root, 'audit/bitacora.jsonl'), 'utf8'), /"event_id":"E1"/);
  assert.equal(git(f.root, 'status', '--porcelain'), '');
  assert.equal(git(f.root, 'rev-list', '--count', 'HEAD'), '4');
  console.log('GIT_REVERT_REAL=PASS;TENANTS_FILES_JOBS_CACHE_SESSIONS_BACKUP_BITACORA_SHA256_PRESERVED=PASS');
});
test('negative: commit changing tenant data or history cannot be selected for rollback', t => {
  const f = fixture(t);
  const head = git(f.root, 'rev-parse', 'HEAD');
  assert.throws(() => revertGuarded(f.root, f.latestSha, head), /revert must not touch/);
  assert.equal(git(f.root, 'rev-parse', 'HEAD'), head);
  assert.match(readFileSync(join(f.root, 'audit/bitacora.jsonl'), 'utf8'), /"event_id":"E1"/);
});
test('negative: stale expected HEAD prevents rollback before any mutation', t => {
  const f = fixture(t);
  assert.throws(() => revertGuarded(f.root, f.disableSha, f.disableSha), /stale HEAD/);
  assert.equal(git(f.root, 'rev-parse', 'HEAD'), f.latestSha);
});
test('negative: concurrent security-flag edit requires reconciliation, not blind revert', t => {
  const f = fixture(t);
  put(f.root, 'flags/security-audit.json',
    '{"enabled":false,"mode":"incident-hold","origin":"synthetic"}\n');
  git(f.root, 'add', '--', 'flags/security-audit.json');
  git(f.root, 'commit', '-q', '-m', 'later independent incident hold');
  const current = git(f.root, 'rev-parse', 'HEAD');
  assert.throws(() => revertGuarded(f.root, f.disableSha, current),
    /changed operational flag/);
  assert.equal(git(f.root, 'rev-parse', 'HEAD'), current);
  assert.match(readFileSync(join(f.root, 'flags/security-audit.json'), 'utf8'),
    /incident-hold/);
});
test('negative and error: malformed SHA and dirty worktree are rejected', t => {
  const f = fixture(t);
  assert.throws(() => revertGuarded(f.root, '../HEAD', f.latestSha), /invalid exact/);
  put(f.root, 'scratch/untracked.txt', 'uncommitted synthetic note\n');
  assert.throws(() => revertGuarded(f.root, f.disableSha, f.latestSha),
    /dirty worktree/);
  assert.equal(git(f.root, 'rev-parse', 'HEAD'), f.latestSha);
});
