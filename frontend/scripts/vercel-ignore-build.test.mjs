import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';

// Offline only: no Vercel API, Git credentials, deployments, network or DB.
const script = fileURLToPath(new URL('./vercel-ignore-build.mjs', import.meta.url));
const DEV = 'prj_1Anhx5mWyXEBX89lWC24Py6JXe7A';
const QA = 'prj_n5STx5F6VboqXd1oLUMR8AvZZtml';
const PROD = 'prj_si3ORH7lBhM4aSAYfYvXsbJT2lHA';

function execute(projectId, branch, expectedCode) {
  const env = {
    ...process.env,
    VERCEL_PROJECT_ID: projectId,
    VERCEL_GIT_COMMIT_REF: branch,
    // Simulate a control-plane-only SHA: a real build must not be canceled
    // just because documentation/CI changed since the prior release.
    VERCEL_GIT_COMMIT_SHA: 'a'.repeat(40),
    VERCEL_GIT_PREVIOUS_SHA: 'b'.repeat(40)
  };
  const result = spawnSync(process.execPath, [script], {
    env, encoding: 'utf8', timeout: 6000
  });
  assert.equal(result.error, undefined);
  assert.equal(result.status, expectedCode,
    `project=${projectId} branch=${branch}, stdout=${result.stdout}, stderr=${result.stderr}`);
  return result.stdout;
}

test('canonical DEV must BUILD even on documentation-only releases', () => {
  const out = execute(DEV, 'dev', 1);
  assert.match(out, /CANONICAL_PROJECT_BUILD_REQUIRED/);
  assert.doesNotMatch(out, /NON_RUNTIME_GOVERNANCE_ONLY_SKIP/);
});

test('canonical QA and PROD branch builds remain allowed', () => {
  assert.match(execute(QA, 'qa', 1), /CANONICAL_PROJECT_BUILD_REQUIRED/);
  assert.match(execute(PROD, 'main', 1), /CANONICAL_PROJECT_BUILD_REQUIRED/);
});

test('every known project continues to reject other environments', () => {
  const cases = [
    [DEV, 'qa'], [DEV, 'main'],
    [QA, 'dev'], [QA, 'main'],
    [PROD, 'dev'], [PROD, 'qa'],
    [DEV, 'dev-fake'], [QA, 'qa-preview'], [PROD, 'main-old']
  ];
  for (const [project, branch] of cases) {
    assert.match(execute(project, branch, 0), /CROSS_ENV_PROJECT_SKIP/,
      `cross-environment build not denied: ${project} ${branch}`);
  }
});

test('missing branch for a bound project fails closed', () => {
  assert.match(execute(DEV, '', 0), /CROSS_ENV_PROJECT_SKIP/);
  assert.match(execute(QA, '', 0), /CROSS_ENV_PROJECT_SKIP/);
  assert.match(execute(PROD, '', 0), /CROSS_ENV_PROJECT_SKIP/);
});

test('unknown project is never impersonated or silently canceled', () => {
  assert.match(execute('prj_unknown', 'dev', 1),
    /UNKNOWN_PROJECT_BUILD_NOT_ATTESTED/);
});
