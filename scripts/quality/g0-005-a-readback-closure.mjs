/**
 * G0-005.A: offline acceptance of institutional receipts and operational signals.
 * Structural validation never certifies a live deployment, A5, or QA/PROD.
 * Input is externally collected evidence; this module does no network calls.
 */
import { assessDevOperationalEvidence } from './g0-005-e-operational-readback.mjs';

const SHA = /^[a-f0-9]{40}$/;
const REQUIRED_RECEIPTS = ['G0-005.C', 'G0-005.D', 'G0-005.E'];
const ENVIRONMENTS = ['DEV', 'QA', 'PROD'];
const OFFICIAL_DEV = 'solqaryn-dev.vercel.app';
const SSO_DEV = 'solqaryn-dev-solqaryn.vercel.app';

function validOrigin(value) {
  if (typeof value !== 'string' || value.length > 200) return false;
  try {
    const url = new URL(value);
    return url.protocol === 'https:' && !url.username && !url.password &&
      url.pathname === '/' && !url.search && !url.hash &&
      /^[a-z0-9.-]+$/.test(url.hostname);
  } catch { return false; }
}

/** No response bodies, tokens, user identifiers, or secrets are emitted. */
export function assessOperationalReadbackClosure(proof) {
  const reasons = [];
  const receipts = Array.isArray(proof?.receipts) ? proof.receipts : [];
  const observations = Array.isArray(proof?.observations) ? proof.observations : [];
  if (!Array.isArray(receipts) || receipts.length !== REQUIRED_RECEIPTS.length ||
      new Set(receipts.map(r => r?.id)).size !== REQUIRED_RECEIPTS.length) {
    reasons.push('RECEIPT_CHAIN_INVALID');
  }
  for (const id of REQUIRED_RECEIPTS) {
    const r = receipts?.find?.(item => item?.id === id);
    if (!r || !SHA.test(r.sha ?? '') || r.state !== 'LISTO' ||
        r.repository !== 'solqaryn/Solqaryn' || r.branch !== 'dev' ||
        r.source !== `https://github.com/solqaryn/Solqaryn/commit/${r.sha}`) {
      reasons.push(`${id.replaceAll('-', '_').replaceAll('.', '_')}_RECEIPT_UNVERIFIED`);
    }
  }
  if (!Array.isArray(observations) || observations.length !== ENVIRONMENTS.length ||
      new Set(observations.map(o => o?.environment)).size !== ENVIRONMENTS.length) {
    reasons.push('ENVIRONMENT_MATRIX_INVALID');
  }
  const table = [];
  for (const environment of ENVIRONMENTS) {
    const o = observations?.find?.(item => item?.environment === environment);
    if (!o || !validOrigin(o.origin) || !SHA.test(o.github_sha ?? '') ||
        !SHA.test(o.deployment_sha ?? '') || !/^[a-zA-Z0-9_-]{1,64}$/.test(o.project ?? '') ||
        !['OBSERVED','NOT_OBSERVED'].includes(o.evidence_status)) {
      reasons.push(`${environment}_EVIDENCE_INVALID`);
    }
    const isDev = environment === 'DEV';
    let category = 'NOT_OBSERVED';
    if (o?.evidence_status === 'OBSERVED') {
      if (o.http_surface === 'SSO_ALIAS' && [401,403].includes(o.http_status)) {
        category = 'AUTH_GATED_EXPECTED';
      } else if (o.http_surface === 'API_UPSTREAM' && o.http_status === 200) {
        category = 'HEALTHY';
      } else {
        category = 'HTTP_ANOMALOUS';
        reasons.push(`${environment}_HTTP_ANOMALY`);
      }
    } else reasons.push(`${environment}_NOT_OBSERVED`);
    if (isDev && o && validOrigin(o.origin) &&
        ![OFFICIAL_DEV, SSO_DEV].includes(new URL(o.origin).hostname)) {
      reasons.push('DEV_ALIAS_UNTRUSTED');
    }
    if (o?.evidence_status === 'OBSERVED' && o?.deployment_sha !== o?.github_sha) {
      reasons.push(`${environment}_SHA_MISMATCH`);
    }
    table.push(Object.freeze({
      environment, origin: validOrigin(o?.origin) ? new URL(o.origin).origin : null,
      project: typeof o?.project === 'string' && /^[a-zA-Z0-9_-]{1,64}$/.test(o.project) ? o.project : null,
      github_sha: SHA.test(o?.github_sha ?? '') ? o.github_sha : null,
      deployment_sha: SHA.test(o?.deployment_sha ?? '') ? o.deployment_sha : null,
      http_category: category
    }));
  }
  const dev = assessDevOperationalEvidence(proof?.dev_readback);
  if (dev.decision !== 'EVIDENCE_CONSISTENT_NOT_LIVE_ATTESTATION') reasons.push('DEV_READBACK_NO_GO');
  return Object.freeze({
    decision: reasons.length ? 'NO_GO' : 'STRUCTURAL_EVIDENCE_CONSISTENT_NOT_LIVE_ATTESTATION',
    reasons: [...new Set(reasons)], table
  });
}
