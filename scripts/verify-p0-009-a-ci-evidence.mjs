#!/usr/bin/env node
// P0-009.A: pure, offline negative gate fixtures. Not a provider API client.
import assert from 'node:assert/strict';

const REQUIRED = [
  'verify-solqaryn-project-scope',
  'SOLQARYN - Seguridad multiempresa',
  'SOLQARYN - Build y pruebas backend',
];
const SHA40 = /^[a-f0-9]{40}$/;

export function evaluateCiEvidence(evidence) {
  const sha = evidence?.devSha;
  const checks = evidence?.checks;
  const ciValid = SHA40.test(sha ?? '') && Array.isArray(checks) && REQUIRED.every((name) =>
    checks.some((c) => c?.name === name && c?.sha === sha && c?.conclusion === 'success'));
  const deployment = evidence?.vercelDev;
  const devParity = Boolean(ciValid && deployment?.project === 'solqaryn-dev' &&
    deployment?.gitRef === 'dev' && deployment?.state === 'READY' &&
    deployment?.gitSha === sha);
  const review = evidence?.review;
  const independentReview = Boolean(SHA40.test(evidence?.prSha ?? '') &&
    review?.state === 'APPROVED' && review?.commitSha === evidence?.prSha &&
    review?.login && evidence?.authorLogin && evidence?.lastContributorLogin &&
    review.login !== evidence.authorLogin && review.login !== evidence.lastContributorLogin);
  return { ciValid: Boolean(ciValid), devParity, independentReview };
}

if (process.argv.includes('--self-test')) {
  const sha = 'a'.repeat(40);
  const prSha = 'b'.repeat(40);
  const base = {
    devSha: sha,
    checks: REQUIRED.map((name) => ({ name, sha, conclusion: 'success' })),
    vercelDev: { project: 'solqaryn-dev', gitRef: 'dev', state: 'READY', gitSha: sha },
    prSha, authorLogin: 'dev-author', lastContributorLogin: 'fixer',
    review: { login: 'independent-reviewer', state: 'APPROVED', commitSha: prSha },
  };
  const copy = () => structuredClone(base);
  assert.deepEqual(evaluateCiEvidence(base), {
    ciValid: true, devParity: true, independentReview: true,
  });
  const missing = copy(); missing.checks.pop();
  assert.equal(evaluateCiEvidence(missing).ciValid, false);
  const staleCheck = copy(); staleCheck.checks[0].sha = 'c'.repeat(40);
  assert.equal(evaluateCiEvidence(staleCheck).ciValid, false);
  const canceled = copy(); canceled.vercelDev.state = 'CANCELED';
  assert.equal(evaluateCiEvidence(canceled).devParity, false);
  const staleReady = copy(); staleReady.vercelDev.gitSha = 'd'.repeat(40);
  assert.equal(evaluateCiEvidence(staleReady).devParity, false);
  const selfReview = copy(); selfReview.review.login = base.authorLogin;
  assert.equal(evaluateCiEvidence(selfReview).independentReview, false);
  const lastContributorReview = copy(); lastContributorReview.review.login = base.lastContributorLogin;
  assert.equal(evaluateCiEvidence(lastContributorReview).independentReview, false);
  const staleApproval = copy(); staleApproval.review.commitSha = 'e'.repeat(40);
  assert.equal(evaluateCiEvidence(staleApproval).independentReview, false);
  const missingPrSha = copy(); delete missingPrSha.prSha;
  assert.equal(evaluateCiEvidence(missingPrSha).independentReview, false);
  console.log('P0-009.A SELF-TEST PASS: 1 positive fixture and 8 fail-closed negative fixtures.');
  console.log('LIMIT: synthetic assertions do not verify live CI, branch protection, Vercel, PROD or native PR review.');
} else {
  console.log('Run with --self-test. This script never calls providers or accesses credentials.');
}
