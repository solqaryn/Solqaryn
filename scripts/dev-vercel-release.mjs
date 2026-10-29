import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { assessDevSourceEquivalence } from './vercel-dev-runtime-equivalence.mjs';

// DEV project only. This script never addresses the SOLQARYN QA or PROD projects.
const REPO = 'solqaryn/Solqaryn';
const TEAM = 'team_owJ2SudSPWiEzeiDthSVV063';
const PROJECT = 'prj_1Anhx5mWyXEBX89lWC24Py6JXe7A';
const CHECKS = ['verify-solqaryn-project-scope', 'SOLQARYN - Seguridad multiempresa', 'SOLQARYN - Build y pruebas backend'];
const shaOk = s => /^[a-f0-9]{40}$/.test(s || '');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function policy() {
  const json = JSON.parse(readFileSync('frontend/vercel.json', 'utf8'));
  assert.equal(json.git?.deploymentEnabled?.dev, true);
  assert.deepEqual(Object.keys(json.git.deploymentEnabled), ['dev']);
  assert.equal(json.ignoreCommand, 'node scripts/vercel-ignore-build.mjs');
}
function validateChecks(runs, sha) {
  if (!shaOk(sha)) throw Error('INVALID_SHA');
  for (const name of CHECKS) {
    const run = runs.filter(x => x.name === name && x.head_sha === sha)
      .sort((a,b) => Date.parse(b.completed_at || b.started_at || 0) -
                     Date.parse(a.completed_at || a.started_at || 0))[0];
    if (!run || run.status !== 'completed' || run.conclusion !== 'success') {
      throw Error('CI_NOT_SUCCESS:' + name);
    }
  }
}
function actualSha(d) { return d.gitSource?.sha || d.meta?.githubCommitSha || null; }
function matchesReady(d, sha) {
  return (d.state === 'READY' || d.readyState === 'READY') &&
    actualSha(d) === sha && (!d.projectId || d.projectId === PROJECT);
}
function recentRelease(deployments, now) {
  return deployments.some(d => d.meta?.solqarynConsolidatedDev === 'true' &&
    Number.isFinite(Number(d.created ?? d.createdAt)) &&
    now - Number(d.created ?? d.createdAt) >= 0 &&
    now - Number(d.created ?? d.createdAt) < 3600000);
}
// Vercel Hobby daily quota is shared by DEV, QA and PROD. Read-only guard.
function teamQuotaHeadroom(all, now) {
  const count = all.filter(d => Number.isFinite(Number(d.created ?? d.createdAt)) &&
    now - Number(d.created ?? d.createdAt) >= 0 &&
    now - Number(d.created ?? d.createdAt) < 86400000).length;
  return count < 95;
}
class ProviderHttpError extends Error {
  constructor(status, detail) {
    super('PROVIDER_HTTP_' + status + ':' + detail.slice(0, 250));
    this.status = status;
  }
}
// A historical READY record may be visible in Vercel's project-scoped list
// but inaccessible through the GitHub Actions token's detail lookup.
// A 404 here is NOT evidence of source equivalence or of a missing current SHA.
// Only this optional historical lookup may continue; all other provider errors
// (including 401/403, rate limits and deployment-create failures) remain fatal.
async function optionalHistoricalDeployment(fetcher) {
  try {
    return await fetcher();
  } catch (error) {
    if (error instanceof ProviderHttpError && error.status === 404) return null;
    throw error;
  }
}

async function requestJson(url, token, opts = {}) {
  const resp = await fetch(url, { ...opts, headers: {
    Accept: 'application/json', Authorization: 'Bearer ' + token,
    ...(opts.body ? {'Content-Type':'application/json'} : {}), ...opts.headers
  }});
  if (!resp.ok) throw new ProviderHttpError(resp.status, await resp.text());
  return resp.json();
}
// A skipped control-plane commit must not trigger an artificial Vercel build.
// This is a source-tree equivalence only: it never claims exact-SHA READY,
// runtime configuration equivalence, functional PASS, or commercial LISTO.
async function readySourceEquivalence(deployments, head, ghToken, vercelToken, headers) {
  const candidates = deployments.filter(d =>
    (d.state === 'READY' || d.readyState === 'READY') &&
    d.name === 'solqaryn-dev' && shaOk(actualSha(d)) &&
    d.meta?.githubCommitRef === 'dev' &&
    d.meta?.githubOrg === 'solqaryn' && d.meta?.githubRepo === 'Solqaryn'
  ).sort((a,b) => Number(b.created ?? b.createdAt) - Number(a.created ?? a.createdAt));
  for (const item of candidates.slice(0, 5)) {
    const priorSha = actualSha(item);
    if (priorSha === head) continue;
    const github = 'https://api.github.com/repos/' + REPO;
    const [comparison, before, after, ready] = await Promise.all([
      requestJson(github + '/compare/' + priorSha + '...' + head + '?per_page=100',
        ghToken, {headers}),
      requestJson(github + '/contents/frontend?ref=' + priorSha, ghToken, {headers}),
      requestJson(github + '/contents/frontend?ref=' + head, ghToken, {headers}),
      optionalHistoricalDeployment(() => requestJson(
        'https://api.vercel.com/v13/deployments/' +
        encodeURIComponent(item.id) + '?teamId=' + TEAM, vercelToken))
    ]);
    if (ready === null) {
      console.log('DEV_HISTORICAL_READY_DETAIL_HTTP_404=UNAVAILABLE SOURCE_EQUIVALENCE=NOT_ATTESTED');
      continue;
    }
    if (ready.projectId && ready.projectId !== PROJECT) {
      throw Error('SOURCE_EQUIVALENCE_WRONG_VERCEL_PROJECT');
    }
    const proof = {
      repository: REPO, branch: 'dev', head_sha: head, project_id: PROJECT,
      ready: {
        id: ready.id, sha: actualSha(ready), state: ready.readyState || ready.state,
        project_id: ready.projectId || ready.project?.id || PROJECT,
        project_name: ready.name, branch: ready.meta?.githubCommitRef,
        target: ready.target, aliases: ready.alias
      },
      compare: {
        base: priorSha, head, status: comparison.status,
        ahead_by: comparison.ahead_by, behind_by: comparison.behind_by,
        total_commits: comparison.total_commits, files: comparison.files
      },
      frontend_before: before, frontend_after: after
    };
    const verdict = assessDevSourceEquivalence(proof);
    if (verdict.decision === 'SOURCE_EQUIVALENT') {
      console.log('DEV_FRONTEND_SOURCE_EQUIVALENCE=PASS HEAD=' + head +
        ' READY_SHA=' + priorSha + ' DEPLOYMENT_ID=' + ready.id +
        ' FRONTEND_MANIFEST_SHA256=' + verdict.frontend_manifest_sha256 +
        ' VERCEL_EXACT_SHA_READY=NO FUNCTIONAL_CERTIFICATION=REQUIRED');
      return true;
    }
    console.log('DEV_SOURCE_EQUIVALENCE_NO_GO=' + verdict.reasons.join(','));
  }
  return false;
}
async function main() {
  policy();
  if (process.argv.includes('--self-test')) {
    const sha = 'a'.repeat(40);
    const okay = CHECKS.map(name=>({name,head_sha:sha,status:'completed',conclusion:'success',completed_at:'2026-10-25T16:00:00Z'}));
    validateChecks(okay,sha);
    assert.throws(()=>validateChecks(okay.slice(1),sha),/CI_NOT_SUCCESS/);
    assert.throws(()=>validateChecks(okay,'wrong'),/INVALID_SHA/);
    assert.throws(()=>validateChecks(okay.map(x=>({...x,head_sha:'b'.repeat(40)})),sha),/CI_NOT_SUCCESS/);
    assert.throws(()=>validateChecks(okay.map(x=>({...x,conclusion:'failure'})),sha),/CI_NOT_SUCCESS/);
    assert.equal(matchesReady({state:'CANCELED',gitSource:{sha}},sha),false);
    assert.equal(matchesReady({state:'READY',gitSource:{sha}},sha),true);
    assert.equal(matchesReady({state:'READY',gitSource:{sha:'b'.repeat(40)}},sha),false);
    assert.equal(recentRelease([{created:Date.now()-300000,meta:{solqarynConsolidatedDev:'true'}}],Date.now()),true);
    assert.equal(recentRelease([{created:Date.now()-3900000,meta:{solqarynConsolidatedDev:'true'}}],Date.now()),false);
    assert.equal(teamQuotaHeadroom(Array.from({length:95},()=>({created:Date.now()-1000})),Date.now()),false);
    assert.equal(teamQuotaHeadroom(Array.from({length:94},()=>({created:Date.now()-1000})),Date.now()),true);
    assert.equal(await optionalHistoricalDeployment(async () => {
      throw new ProviderHttpError(404, '{"error":{"code":"not_found"}}');
    }), null);
    for (const status of [401, 402, 403, 429, 500]) {
      await assert.rejects(
        () => optionalHistoricalDeployment(async () => {
          throw new ProviderHttpError(status, 'provider rejected');
        }),
        new RegExp('PROVIDER_HTTP_' + status)
      );
    }
    await assert.rejects(
      () => optionalHistoricalDeployment(async () => {
        throw Error('PROVIDER_HTTP_404');
      }),
      /PROVIDER_HTTP_404/
    );
    console.log('DEV_VERCEL_RELEASE_POLICY_SELF_TEST=PASS');
    return;
  }
  if (process.env.GITHUB_REPOSITORY !== REPO || process.env.GITHUB_REF !== 'refs/heads/dev')
    throw Error('REPOSITORY_OR_BRANCH_NOT_DEV');
  const gh = process.env.GH_TOKEN, vt = process.env.VERCEL_TOKEN;
  if (!gh || !vt) throw Error('MISSING_PROVIDER_TOKEN_IN_ACTIONS_SECRETS');
  const headers = {'X-GitHub-Api-Version':'2022-11-28'};
  const ref = await requestJson('https://api.github.com/repos/' + REPO + '/git/ref/heads/dev',gh,{headers});
  const sha = ref.object?.sha;
  if (!shaOk(sha)) throw Error('INVALID_DEV_HEAD_SHA');
  const chosen = (process.env.REQUESTED_SHA || '').trim();
  if (chosen && chosen !== sha) throw Error('REQUESTED_SHA_NOT_CURRENT_DEV_HEAD');
  const checkRuns = await requestJson('https://api.github.com/repos/' + REPO + '/commits/' + sha + '/check-runs?per_page=100',gh,{headers});
  validateChecks(checkRuns.check_runs || [], sha);
  console.log('DEV_SHA_VALIDATED=' + sha + ' CI=3/3_SUCCESS');
  const url = 'https://api.vercel.com';
  const recent = await requestJson(url + '/v6/deployments?teamId=' + TEAM + '&projectId=' + PROJECT + '&limit=100',vt);
  const deployments = recent.deployments || [];
  if (deployments.some(d => matchesReady(d,sha))) { console.log('ALREADY_READY_SHA=' + sha); return; }
  if (await readySourceEquivalence(deployments, sha, gh, vt, headers)) return;
  const teamList = await requestJson(url+'/v6/deployments?teamId='+TEAM+'&limit=100',vt);
  if (!teamQuotaHeadroom(teamList.deployments || [],Date.now())) {
    console.log('DEV_RELEASE_SKIPPED_TEAM_HOBBY_QUOTA_NEAR_100_PER_DAY; NO_READY_ATTESTED');
    return;
  }
  if (recentRelease(deployments,Date.now())) { console.log('SKIP_ONE_EXPLICIT_RELEASE_PER_HOUR'); return; }
  const body = {
    name:'solqaryn-dev',project:PROJECT,target:'production',
    // 'production' here is an internal target of project solqaryn-dev, not solqaryn-prod.
    gitSource:{type:'github',org:'solqaryn',repo:'Solqaryn',ref:'dev',sha},
    meta:{solqarynConsolidatedDev:'true',solqarynReleaseSha:sha}
  };
  const made = await requestJson(url+'/v13/deployments?teamId='+TEAM,vt,{method:'POST',body:JSON.stringify(body)});
  if (!made.id) throw Error('VERCEL_DEPLOYMENT_ID_NOT_RETURNED');
  console.log('DEPLOYMENT_REQUESTED=' + made.id + ' SHA=' + sha);
  for (let n=0;n<120;n++) {
    const d = await requestJson(url+'/v13/deployments/'+encodeURIComponent(made.id)+'?teamId='+TEAM,vt);
    if (d.projectId && d.projectId!==PROJECT) throw Error('DEPLOYMENT_WRONG_PROJECT');
    const real = actualSha(d);
    if (real && real!==sha) throw Error('DEPLOYMENT_GIT_SHA_MISMATCH');
    const state = d.readyState || d.state;
    if (state==='READY') {
      if (real!==sha) throw Error('DEPLOYMENT_GIT_SHA_NOT_ATTESTED');
      console.log('DEV_DEPLOYMENT_READY=PASS SHA='+sha+' ID='+d.id+' URL='+d.url);
      return;
    }
    if (['ERROR','CANCELED','BLOCKED','SKIPPED'].includes(state)) throw Error('DEV_DEPLOYMENT_NOT_READY:'+state+':'+d.id);
    await sleep(5000);
  }
  throw Error('DEV_DEPLOYMENT_TIMEOUT:'+made.id);
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
