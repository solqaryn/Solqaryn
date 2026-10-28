// The Ignored Build Step protocol uses exit 0 to CANCEL a Vercel deployment
// and exit 1 to BUILD it. This is not the policy that authorizes a deployment.
//
// For solqaryn-dev, frontend/vercel.json disables Git auto-deploy from dev.
// The separate consolidated-release workflow verifies protected dev HEAD,
// exact-SHA CI 3/3, DEV project, quota and release idempotency BEFORE requesting
// a deployment. A documentation-only commit still needs a real READY build.
// Never cancel a canonical DEV release merely because the diff is non-runtime.
const branch = process.env.VERCEL_GIT_COMMIT_REF;
const vercelProjectId = process.env.VERCEL_PROJECT_ID;

const PROJECT_BRANCH_BINDINGS = Object.freeze({
  'prj_1Anhx5mWyXEBX89lWC24Py6JXe7A': 'dev',
  'prj_n5STx5F6VboqXd1oLUMR8AvZZtml': 'qa',
  'prj_si3ORH7lBhM4aSAYfYvXsbJT2lHA': 'main'
});

const expectedBranch = PROJECT_BRANCH_BINDINGS[vercelProjectId];

// Retain the cross-environment denylist: no QA/PROD build from dev and no
// DEV build from qa/main. Only a project's canonical branch can build.
if (expectedBranch && branch !== expectedBranch) {
  console.log(`CROSS_ENV_PROJECT_SKIP branch=${branch} expected=${expectedBranch} project=${vercelProjectId}`);
  process.exit(0);
}

// Unknown projects continue to build rather than impersonate a known project.
// Their deployment/runtime authorization remains a separate fail-closed gate.
if (!expectedBranch) {
  console.log('UNKNOWN_PROJECT_BUILD_NOT_ATTESTED');
  process.exit(1);
}

// Canonical dev/qa/main builds must never be canceled by this heuristic.
// Vercel's control-plane / release workflow already decides if a build
// should have been requested; only READY + exact source SHA certifies it.
console.log(`CANONICAL_PROJECT_BUILD_REQUIRED branch=${branch} project=${vercelProjectId}`);
process.exit(1);
