# SOLQARYN V22 - C04-01.M05.05: security negative JWT/HTTP/MySQL84 (DEV)

PLAN D606 requires a real negative security / tenant / abuse test. PLAN E606 is an accumulated M05 target; this test-only unit does NOT certify billing, prices, entitlements, payments, mutations or production. DEV03, V17 namedRange row 606, R2 security risk, PROJECT_SCOPE_LOCK=STRICT.

## A0 architecture and dependency impact

Extend the existing C04-01.M03.03 TestServer integration test that already constructs tenants A and B, signed HMAC JWT, physical Oracle EF MySQL 8.4 isolated database, current memberships and live role/permission checks. No runtime source or production endpoint changes; no Aiven live, QA, main, PROD, payment provider or secrets. Existing historical tests retained.

## A1/A2 adversarial negative cases

Three extra HTTP assertions in the integration test: (1) authenticated tenant A cannot read B even if query parameters empresaId and tenantId and X-Empresa-Id header claim A; route B must return 403; (2) real bearer requesting tenant 0 must return 403; (3) valid cryptographic JWT with id 999999999 but no actual MySQL membership must return 403. Restore original A bearer to keep existing role revocation and soft-delete tests. These are three new negative checks inside ONE real integration xUnit test, not three separate xUnit tests.

## A3 actual integration

GitHub-hosted .NET 10 and MySQL 8.4 ephemeral service loopback, real EF/DB migrations, TestServer HTTP, JwtBearer token verification, role and membership source from real isolated DB. Require SELECT VERSION 8.4.x already in test; a TRX counters total=executed=passed, zero fail and zero notExecuted. These requests do not imply a live public endpoint, a plan pricing table or server-side billing quotas: the adapter remains read-only and opt-in disabled in production. Those tasks and cumulative M05 phase remain unverified.

## A4/A5 gating and rollback

Require precise SOURCE_SHA GitHub-hosted directed check, .NET Release, real JWT/MySQL negative integration, security/authorization checks, verify-solqaryn-project-scope, V5 provenance and readback SUCCESS. QUEUED/IN_PROGRESS/FAIL/CANCELLED/SKIPPED required checks are not PASS; leave VALIDANDO with SHA/job until CI concludes. A5 independent by another Javier Task distinct from DEV03 author/repairer. No rewrite of huge CHANGELOG_AI.md because connector may truncate its contents, preserving complete history; this document plus append-only BITACORA provide increment provenance. Rollback via normal git revert and new audit event, no force-push.
