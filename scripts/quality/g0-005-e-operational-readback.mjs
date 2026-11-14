/**
 * G0-005.E: deterministic, offline classification of DEV operational evidence.
 * EVIDENCE_CONSISTENT is NOT proof of a live deployment, tenant auth or LISTO.
 * Do not log response bodies, credentials, account identifiers or PII.
 */
const SHA = /^[0-9a-f]{40}$/;
const REPO = 'solqaryn/Solqaryn';
const PROJECT_ID = 'prj_1Anhx5mWyXEBX89lWC24Py6JXe7A';
const PUBLIC_ALIAS = 'solqaryn-dev.vercel.app';
const SSO_ALIAS = 'solqaryn-dev-solqaryn.vercel.app';

export function assessDevOperationalEvidence(proof) {
  const reasons = [];
  const vercel = proof?.vercel;
  const sso = proof?.sso;
  const api = proof?.api;
  if (proof?.repository !== REPO || proof?.branch !== 'dev') {
    reasons.push('UNTRUSTED_REPOSITORY_OR_BRANCH');
  }
  if (!SHA.test(proof?.head_sha ?? '')) reasons.push('INVALID_DEV_HEAD');
  if (vercel?.project_id !== PROJECT_ID || vercel?.project_name !== 'solqaryn-dev') {
    reasons.push('VERCEL_PROJECT_MISMATCH');
  }
  if (vercel?.state !== 'READY' || vercel?.branch !== 'dev' ||
      vercel?.target !== 'production' ||
      !Array.isArray(vercel?.aliases) || !vercel.aliases.includes(PUBLIC_ALIAS)) {
    reasons.push('VERCEL_READY_NOT_VERIFIED');
  }
  if (!SHA.test(vercel?.sha ?? '') || vercel?.sha !== proof?.head_sha) {
    reasons.push('VERCEL_SHA_MISMATCH');
  }
  if (sso?.hostname !== SSO_ALIAS || ![401, 403].includes(sso?.status)) {
    reasons.push('SSO_ALIAS_NOT_VERIFIED');
  }
  if (api?.environment !== 'DEV' || api?.status !== 200) {
    reasons.push('DEV_API_NOT_HEALTHY');
  }
  if (api?.upstream_status !== 200) {
    reasons.push('DEV_UPSTREAM_UNAVAILABLE');
  }
  return {
    decision: reasons.length ? 'NO_GO' : 'EVIDENCE_CONSISTENT_NOT_LIVE_ATTESTATION',
    reasons
  };
}
