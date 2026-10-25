import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

// DEV project only. This script never addresses the SOLQARYN QA or PROD projects.
const REPO = 'solqaryn/Solqaryn';
const TEAM = 'team_owJ2SudSPWiEzeiDthSVV063';
const PROJECT = 'prj_1Anhx5mWyXEBX89lWC24Py6JXe7A';
const CHECKS = ['verify-solqaryn-project-scope', 'SOLQARYN - Seguridad multiempresa', 'SOLQARYN - Build y pruebas backend'];
const shaOk = s => /^[a-f0-9]{40}$/.test(s || '');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function policy() {
  const json = JSON.parse(readFileSync('frontend/vercel.json', 'utf8'));
  assert.equal(json.git?.deploymentEnabled?.dev, false);
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
async function requestJson(url, token, opts = {}) {
  const resp = await fetch(url, { ...opts, headers: {
    Accept: 'application/json', Authorization: 'Bearer ' + token,
    ...(opts.body ? {'Content-Type':'application/json'} : {}), ...opts.headers
  }});
  if (!resp.ok) throw Error('PROVIDER_HTTP_' + resp.status + ':' + (await resp.text()).slice(0,250));
  return resp.json();
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
