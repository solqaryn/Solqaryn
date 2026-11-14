/** G0-005.B: offline acceptance guard. Never attests live QA/PROD, secrets or deployments. */
const SHA = /^[0-9a-f]{40}$/;
const REQUIRED = Object.freeze({
  'G0-005.F': 'scripts/tests/g0-005-f-operational-503-fixture.test.mjs',
  'G0-005.G': 'scripts/tests/g0-005-g-negative-readback.test.mjs',
  'G0-005.H': 'scripts/tests/g0-005-h-positive-readback.test.mjs',
});
const ENV = ['DEV', 'QA', 'PROD'];
const SAFE_REF = /^[a-z][a-z0-9_-]{5,63}$/;
const safeOrigin = raw => {
  if (typeof raw !== 'string' || raw.length > 200) return null;
  try {
    const u = new URL(raw);
    if (u.protocol !== 'https:' || u.username || u.password || u.port ||
      u.pathname !== '/' || u.search || u.hash ||
      !/^[a-z0-9.-]+$/.test(u.hostname) || !u.hostname.includes('.')) return null;
    return u.origin;
  } catch { return null; }
};

/** Inputs are untrusted metadata; outputs never include arbitrary input bodies or credentials. */
export function assessG0005BReadback(proof) {
  const p = proof && typeof proof === 'object' && !Array.isArray(proof) ? proof : {};
  const reasons = [];
  const add = (ok, reason) => { if (!ok) reasons.push(reason); };
  add(p.scope === 'G0-005.B' && p.repository === 'solqaryn/Solqaryn' &&
    p.branch === 'dev' && SHA.test(p.devSha ?? ''), 'SOURCE_IDENTITY_INVALID');
  const receipts = Array.isArray(p.receipts) ? p.receipts : [];
  add(receipts.length === 3 && new Set(receipts.map(r => r?.id)).size === 3 &&
    Object.entries(REQUIRED).every(([id, testPath]) => receipts.filter(r =>
      r?.id === id && r.state === 'LISTO' && SHA.test(r.sha ?? '') &&
      r.testPath === testPath && r.planReadback === true &&
      r.colaReadback === true && r.bitacoraEventVerified === true).length === 1),
    'PREDECESSOR_RECEIPTS_UNVERIFIED');
  const o = Array.isArray(p.environments) ? p.environments : [];
  add(o.length === 3 && new Set(o.map(x => x?.environment)).size === 3 &&
    ENV.every(env => o.filter(x => x?.environment === env).length === 1),
    'ENVIRONMENT_MATRIX_INVALID');
  const table = [];
  const resources = { database: [], cache: [], credentials: [], api: [], project: [] };
  for (const env of ENV) {
    const v = o.find(x => x?.environment === env);
    const origin = safeOrigin(v?.origin);
    const apiOrigin = safeOrigin(v?.apiOrigin);
    add(Boolean(v && origin && apiOrigin && SAFE_REF.test(v?.project ?? '') &&
      SHA.test(v?.githubSha ?? '') && SHA.test(v?.deploymentSha ?? '') &&
      v?.githubSha === v?.deploymentSha && v?.evidenceStatus === 'OBSERVED' &&
      typeof v?.providerEvidenceRef === 'string' &&
      /^[a-zA-Z0-9_-]{12,80}$/.test(v.providerEvidenceRef)), `${env}_OBSERVATION_INVALID`);
    const surface = v?.httpSurface;
    const status = v?.httpStatus;
    const category = surface === 'SSO_ALIAS' && [401, 403].includes(status)
      ? 'AUTH_GATED' : surface === 'API_UPSTREAM' && status === 200
        ? 'HEALTHY' : 'ANOMALOUS';
    add(category !== 'ANOMALOUS', `${env}_HTTP_ANOMALY`);
    if (env === 'DEV') {
      add(origin === 'https://solqaryn-dev.vercel.app' ||
        origin === 'https://solqaryn-dev-solqaryn.vercel.app', 'DEV_ALIAS_MISMATCH');
      add(v?.githubSha === p.devSha, 'DEV_SHA_MISMATCH');
    }
    for (const key of ['database', 'cache', 'credentials']) {
      const ref = v?.resourceRefs?.[key];
      add(SAFE_REF.test(ref ?? ''), `${env}_${key.toUpperCase()}_REF_INVALID`);
      resources[key].push(ref);
    }
    resources.api.push(apiOrigin);
    resources.project.push(v?.project);
    table.push(Object.freeze({
      environment: env, origin, project: SAFE_REF.test(v?.project ?? '') ? v.project : null,
      github_sha: SHA.test(v?.githubSha ?? '') ? v.githubSha : null,
      deployment_sha: SHA.test(v?.deploymentSha ?? '') ? v.deploymentSha : null,
      http_category: category
    }));
  }
  for (const [key, values] of Object.entries(resources)) {
    add(values.length === 3 && values.every(Boolean) && new Set(values).size === 3,
      `${key.toUpperCase()}_NOT_ISOLATED`);
  }
  const dev = p.devReadback;
  add(dev?.sso?.hostname === 'solqaryn-dev-solqaryn.vercel.app' &&
    [401, 403].includes(dev?.sso?.status) && dev?.api?.status === 200 &&
    dev?.api?.upstreamStatus === 200 &&
    dev?.vercel?.project === 'solqaryn-dev' &&
    dev?.vercel?.state === 'READY' && dev?.vercel?.sha === p.devSha,
    'DEV_PROVIDER_SIGNALS_INCONSISTENT');
  // A real provider read with authenticated provenance is outside this offline gate.
  return Object.freeze({
    decision: reasons.length ? 'NO_GO' : 'STRUCTURAL_CONSISTENCY_NOT_LIVE_ATTESTATION',
    reasons: [...new Set(reasons)], table,
    liveVerified: false, qaProdPromotionAuthorized: false
  });
}
