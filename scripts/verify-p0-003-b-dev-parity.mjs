#!/usr/bin/env node
// P0-003.B: pure evidence gate; never contacts providers or changes deployments.
import assert from 'node:assert/strict';

const SHA40 = /^[a-f0-9]{40}$/;
const RENDER_DEV_ID = 'srv-daqvla49v7es738up9vg';
const VERCEL_DEV_ID = 'prj_1Anhx5mWyXEBX89lWC24Py6JXe7A';
const PUBLIC_ORIGIN = 'https://solqaryn-dev.vercel.app';
const API_UPSTREAM = 'https://solqaryn-api-dev-fxx8.onrender.com';

export function evaluateDevParity(evidence) {
  const e = evidence ?? {};
  const sha = e.dev?.sha;
  const failures = [];
  const requireEvidence = (condition, code) => {
    if (!condition) failures.push(code);
  };

  requireEvidence(e.environment === 'DEV' && e.dev?.branch === 'dev' && SHA40.test(sha ?? ''), 'DEV_IDENTITY');
  requireEvidence(e.render?.serviceId === RENDER_DEV_ID && e.render?.status === 'live' &&
    SHA40.test(e.render?.commitSha ?? '') && e.render.commitSha === sha &&
    typeof e.render?.deploymentId === 'string' && e.render.deploymentId.startsWith('dep-'), 'RENDER_SHA_LIVE');
  requireEvidence(e.vercel?.projectId === VERCEL_DEV_ID && e.vercel?.ref === 'dev' &&
    e.vercel?.state === 'READY' && SHA40.test(e.vercel?.commitSha ?? '') &&
    e.vercel.commitSha === sha, 'VERCEL_SHA_READY');
  requireEvidence(typeof e.vercel?.deploymentId === 'string' &&
    e.vercel.deploymentId.startsWith('dpl_') &&
    e.vercel?.aliasDeploymentId === e.vercel.deploymentId &&
    e.vercel?.alias === 'solqaryn-dev.vercel.app', 'ALIAS_IDENTITY');
  requireEvidence(e.frontend?.httpStatus === 200 && e.frontend?.origin === PUBLIC_ORIGIN, 'FRONTEND_HTTP_ORIGIN');
  requireEvidence(e.backend?.httpStatus === 200 && e.backend?.readyStatus === 'ready' &&
    e.backend?.databaseStatus === 'connected', 'BACKEND_READINESS');
  requireEvidence(e.contract?.apiUpstream === API_UPSTREAM &&
    e.contract?.publicOrigin === PUBLIC_ORIGIN && e.contract?.effectivePreflightCors === true,
  'UPSTREAM_CORS_EFFECTIVE');
  requireEvidence(e.smoke?.authenticated === true && e.smoke?.tenantIsolation === true,
    'AUTHENTICATED_TENANCY_SMOKE');
  return { certified: failures.length === 0, failures };
}

if (process.argv.includes('--self-test')) {
  const sha = 'a'.repeat(40);
  const base = {
    environment: 'DEV', dev: { branch: 'dev', sha },
    render: { serviceId: RENDER_DEV_ID, status: 'live', commitSha: sha, deploymentId: 'dep-synthetic' },
    vercel: { projectId: VERCEL_DEV_ID, ref: 'dev', state: 'READY', commitSha: sha,
      deploymentId: 'dpl_synthetic', aliasDeploymentId: 'dpl_synthetic', alias: 'solqaryn-dev.vercel.app' },
    frontend: { httpStatus: 200, origin: PUBLIC_ORIGIN },
    backend: { httpStatus: 200, readyStatus: 'ready', databaseStatus: 'connected' },
    contract: { apiUpstream: API_UPSTREAM, publicOrigin: PUBLIC_ORIGIN, effectivePreflightCors: true },
    smoke: { authenticated: true, tenantIsolation: true },
  };
  assert.deepEqual(evaluateDevParity(base), { certified: true, failures: [] });
  const cases = [
    ['HTTP_200_ALONE', { frontend: { httpStatus: 200, origin: PUBLIC_ORIGIN } }],
    ['NO_SHA', e => { delete e.dev.sha; }],
    ['RENDER_STALE_SHA', e => { e.render.commitSha = 'b'.repeat(40); }],
    ['RENDER_NOT_LIVE', e => { e.render.status = 'deactivated'; }],
    ['VERCEL_STALE_SHA', e => { e.vercel.commitSha = 'b'.repeat(40); }],
    ['VERCEL_CANCELED', e => { e.vercel.state = 'CANCELED'; }],
    ['VERCEL_WRONG_PROJECT', e => { e.vercel.projectId = 'prj_other'; }],
    ['ALIAS_WRONG_DEPLOYMENT', e => { e.vercel.aliasDeploymentId = 'dpl_other'; }],
    ['FRONTEND_200_ORIGIN_WRONG', e => { e.frontend.origin = 'https://unexpected.example'; }],
    ['BACKEND_NOT_READY', e => { e.backend.readyStatus = 'not_ready'; }],
    ['CORS_NOT_OBSERVED', e => { e.contract.effectivePreflightCors = false; }],
    ['NO_AUTH_SMOKE', e => { e.smoke.authenticated = false; }],
    ['NO_TENANT_ISOLATION', e => { e.smoke.tenantIsolation = false; }],
  ];
  for (const [name, mutation] of cases) {
    const fixture = typeof mutation === 'function' ? structuredClone(base) : mutation;
    if (typeof mutation === 'function') mutation(fixture);
    assert.equal(evaluateDevParity(fixture).certified, false, name + ' must fail closed');
  }
  console.log('P0-003.B SELF-TEST PASS: 1 synthetic positive, ' + cases.length + ' negatives; no provider calls');
}
