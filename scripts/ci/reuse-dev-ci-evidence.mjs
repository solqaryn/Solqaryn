import assert from 'node:assert/strict';
import { appendFileSync } from 'node:fs';
import { setTimeout as sleep } from 'node:timers/promises';

// Reuse evidence only from the real DEV workflow for this immutable commit.
// Never manufacture PASS: missing, transient or unverified data uses local CI.
const SOURCE_PATH = '.github/workflows/dev-ci.yml';
const REQUIRED_JOBS = Object.freeze({
  backend: 'Backend Release y pruebas',
  frontend: 'Frontend producción'
});

function validSourceRun(run, sha, repo, event) {
  return Boolean(run &&
    run.head_sha === sha &&
    run.path === SOURCE_PATH &&
    run.event === event &&
    run.head_repository?.full_name === repo &&
    (event !== 'push' || run.head_branch === 'dev'));
}

function selectRun(runs, sha, repo, event) {
  return (runs || [])
    .filter(run => validSourceRun(run, sha, repo, event))
    .sort((a, b) => (b.id ?? 0) - (a.id ?? 0))[0] || null;
}

function classifyJob(job) {
  if (!job || job.status !== 'completed') return 'pending';
  return job.conclusion === 'success' ? 'success' : 'failure';
}

function selfTest() {
  const base = {
    id: 15, head_sha: 'a'.repeat(40), event: 'push',
    head_branch: 'dev', path: SOURCE_PATH,
    head_repository: { full_name: 'solqaryn/Solqaryn' }
  };
  assert.equal(validSourceRun(base, base.head_sha, 'solqaryn/Solqaryn', 'push'), true);
  assert.equal(validSourceRun({ ...base, head_sha: 'b'.repeat(40) }, base.head_sha, 'solqaryn/Solqaryn', 'push'), false);
  assert.equal(validSourceRun({ ...base, path: '.github/workflows/fake.yml' }, base.head_sha, 'solqaryn/Solqaryn', 'push'), false);
  assert.equal(validSourceRun({ ...base, event: 'workflow_dispatch' }, base.head_sha, 'solqaryn/Solqaryn', 'push'), false);
  assert.equal(selectRun([{ ...base, id: 13 }, base], base.head_sha, 'solqaryn/Solqaryn', 'push').id, 15);
  assert.equal(classifyJob({ status: 'completed', conclusion: 'success' }), 'success');
  for (const result of ['failure', 'cancelled', 'skipped', 'timed_out']) {
    assert.equal(classifyJob({ status: 'completed', conclusion: result }), 'failure');
  }
  assert.equal(classifyJob({ status: 'in_progress', conclusion: null }), 'pending');
  assert.equal(classifyJob(null), 'pending');
  console.log('EXACT_SHA_REUSE_SELF_TEST=PASS');
}

async function github(path, token) {
  const response = await fetch('https://api.github.com' + path, {
    headers: {
      Accept: 'application/vnd.github+json',
      Authorization: 'Bearer ' + token,
      'X-GitHub-Api-Version': '2022-11-28'
    },
    signal: AbortSignal.timeout(15000)
  });
  if (!response.ok) throw new Error('GitHub API HTTP ' + response.status + ' on authorized endpoint');
  return await response.json();
}

function writeOutputs(result) {
  const output = Object.entries(result)
    .map(([key, value]) => key + '=' + String(value).replace(/[\r\n]/g, ' ')).join('\n') + '\n';
  if (process.env.GITHUB_OUTPUT) appendFileSync(process.env.GITHUB_OUTPUT, output);
  console.log('EXACT_SHA_REUSE backend=' + result.backend +
    ' frontend=' + result.frontend + ' source_run=' + result.source_run);
}

async function main() {
  if (process.argv.includes('--self-test')) return selfTest();
  const sha = process.env.GITHUB_SHA ?? '';
  const repo = process.env.GITHUB_REPOSITORY ?? '';
  const event = process.env.GITHUB_EVENT_NAME ?? '';
  const token = process.env.GH_TOKEN ?? '';
  const fallback = { backend: 'fallback', frontend: 'fallback', source_run: 'none' };
  if (!/^[a-f0-9]{40}$/.test(sha) ||
      repo !== 'solqaryn/Solqaryn' ||
      !['push', 'pull_request'].includes(event) || !token) {
    console.warn('Evidence unavailable or invalid metadata; run original required tests');
    return writeOutputs(fallback);
  }

  const latest = { ...fallback };
  // Wait for the corresponding DEV workflow to appear; docs-only changes
  // legitimately have no DEV run and must execute the local tests.
  const start = Date.now();
  const lookupEnd = start + 40000;
  const finishEnd = start + 14 * 60 * 1000;
  try {
    let run = null;
    while (Date.now() < finishEnd) {
      const list = await github('/repos/' + repo + '/actions/runs?head_sha=' + sha + '&per_page=100', token);
      const found = selectRun(list.workflow_runs, sha, repo, event);
      if (found) run = found;
      if (!run) {
        if (Date.now() >= lookupEnd) break;
        await sleep(7000);
        continue;
      }
      latest.source_run = String(run.id);
      const jobs = await github('/repos/' + repo + '/actions/runs/' + run.id + '/jobs?per_page=100', token);
      for (const [key, name] of Object.entries(REQUIRED_JOBS)) {
        const job = (jobs.jobs || []).find(j => j.name === name);
        const outcome = classifyJob(job);
        if (outcome !== 'pending') latest[key] = outcome;
        else if (run.status === 'completed') latest[key] = 'fallback';
      }
      if (latest.backend !== 'fallback' && latest.frontend !== 'fallback') {
        return writeOutputs(latest);
      }
      if (run.status === 'completed') break;
      await sleep(8000);
    }
  } catch (error) {
    console.warn('API evidence query failed; refusing to reuse: ' + String(error.message).slice(0, 170));
  }
  return writeOutputs(latest);
}

main().catch(error => {
  console.error('Evidence script failed-closed: ' + String(error.message));
  process.exitCode = 1;
});
