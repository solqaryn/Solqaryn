# V23 cross-rescue C04-02.M03.06 — real JWT tenant denial audit assertion

## Exact causal defect
- Failed GitHub-hosted job: https://github.com/solqaryn/Solqaryn/actions/runs/44973408685/job/137057881058 on source SHA a8a01bc9b3001b7abb558814f4361aa8720df90e.
- The real .NET 10 test host with signed JWT, actual HTTP, Oracle EF MySQL 8.4 and committed isolated audit records executed 12 tests: 11 passed and one failed at assertion line 326. The test required the second audit outcome to include CLIENT_TENANT_HINT_MISMATCH.
- In C0402ContractVerifiedActorSource.ReadCurrentAsync, a request for company B by an actor whose relational membership belongs only to company A is rejected before the application layer checks the client-provided tenant hint. Consequently, the safe server-controlled denial is LIVE_SERVER_JWT_RBAC_MEMBERSHIP_REQUIRED, not necessarily CLIENT_TENANT_HINT_MISMATCH. The HTTP foreign-tenant response was already asserted 403; revoked membership was also asserted 403.
- This is a mismatch in the test's demanded *denial reason*, not evidence that foreign access succeeded. Do not weaken production authentication or silently convert a denied operation into an allowed one.

## Narrow test-only repair
- Verify exactly three durable audit events: one positive domain candidate plus two explicit authorization/tenant-denial codes, with no unsafe cause persisted.
- Preserve the real HTTP 401 for anonymous and forged JWT; 200 for the authenticated scoped read-only domain candidate; 403 for company B and for grant revocation.
- Accept only two server-controlled rejection codes for the two negative audit events: CLIENT_TENANT_HINT_MISMATCH or LIVE_SERVER_JWT_RBAC_MEMBERSHIP_REQUIRED. An unknown outcome, missing event, duplicate positive, unsafe cause or skipped test must fail.
- No runtime, DI, endpoint, database schema, payment, QA, main or production change. No claim of a real contract repository: the owned contract snapshot in the test remains synthetic. Production-owned contract persistence and transactional contract authorization remain unimplemented and prevent global completion.
- Governance read; V23 external UI/Angular/motion/copy/threat/perf references NO_APLICA for this existing backend integration test-only repair; original project architecture and Scope Lock preserved.

## Acceptance and handoff
- Run GitHub-hosted C04-02.M03.06 - durable MySQL 8.4 positive and negatives on this exact new dev commit: require both Category=Integration classes executed with 2 passed, 0 failed, 0 skipped, backed by an actual disposable MySQL 8.4.
- Verify exact SHA Scope Lock, V5 changelog provenance, backend Release, multiempresa security and all causally required CI completed SUCCESS; queued/in-progress/skipped/failed are never PASS.
- Preserve PLAN_MAESTRO H=VALIDANDO and historical I=DEV04; record DEV03 as repair actor only under exclusive V17 lock and BITACORA event. A5 later by a Javier Task distinct from the author and repairers; no premature LISTO or phase-wide authorization.
- Rollback using ordinary git revert after rechecking the live branch and writer lease; do not force push.
