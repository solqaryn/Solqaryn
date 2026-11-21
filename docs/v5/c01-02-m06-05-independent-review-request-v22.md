# C01-02.M06.05 — V22 independent review request, not an A5 approval

**PLAN D216:** `Solicitar revisión independiente del lote cuando sea un gate.` Project: SOLQARYN `solqaryn/Solqaryn`, `dev`, PROJECT_SCOPE_LOCK=STRICT. Requesting Task DEV05 `6adc2e55f47c81919f6374e561687076`, R0 documentary/request action only. PLAN E216 is the cumulative target of default-deny sector/plan/role/tenant/branch/resource capabilities; this request **does not** assert the target is fully implemented, tested, audited or released.

## A0 — request and verified context

The independent reviewer is **one authorized Javier Task** with a physical TASK_ID distinct from the author of the affected code and any repairer; the reviewer must independently read code, tests, CI and diff, not inherit a previous approval. Author DEV05 is disqualified from self-review of D216. Possible recipient Tasks are DEV01 `6adbb9fff1548191a1bd5b30d9e9984a`, DEV02 `6af16bb9492c8191b8e8c7d606f11507`, DEV03 `6adc2e4bd8a48191ac7d8641ed95a1a3`, DEV04 `6aeacb7820ac8191b7d768a6f33dfa09`, DEV06 `6af16bbad26881918c283abe06e1f32a`, subject to **actual** per-scope author/repairer independence, V17 lock and no active writer. This document and a matching idempotent BITACORA request are the handoff; neither schedules, reactivates nor impersonates a reviewer.

Source scoped evidence: C01-02.M05.06 `844d7fead192cf0f661ac8ae6f1b934860eca6ab` had a real GitHub-hosted isolated ASP.NET10/Angular22/MySQL 8.4 signed JWT Chromium suite `136732965290`, 5/5 pass, 0 fail/skip. Independent DEV06 A5 is recorded for that *specific M05.06 test only*. Historical failed SHA `3d139103` must remain visible. Scope Lock/V5 and backend/security frontend checks on the actual causal SHA must be verified; a green build is not global capability authority. C01-02.M06.03 risk decision register is its own isolated D214 scope, not substitute for the entire phase.

Snapshot at initial request: C01-02.M06.01 was worked by DEV06 under its own scope; M06.02 and M06.04 were still outstanding; M06.03 had a scoped documentation delivery by DEV05, and M06.06 remained dependent on live DAG formulas. These observations are **not a lock**, and the reviewer must reread actual H/I/J/K/L/M, latest BITACORA, CURRENT HEAD, releases and named ranges before any write.

## Independent review acceptance contract (future review only)

- Verify each affected PLAN D microtask, true diff/blobs/source SHA, actual positive/negative/error/boundary assertions, A2 tenant A/B signed-JWT/RBAC/role/resource denials when pertinent, A3 real MySQL 8.4/HTTP/Chromium when pertinent, and A4 `verify-solqaryn-project-scope` and V5 provenance **completed SUCCESS on that SHA** or rigorously established blob-equivalent descendant.
- Check that required `SKIPPED`, `CANCELLED`, `IN_PROGRESS`, `QUEUED`, `FAIL` are **never treated as PASS**. A5_FAIL history must stay visible; an independent A5_PASS cannot be inferred from another row's A5 or from this request.
- Inspect capability matrix coverage for sector, plan, role, tenant, branch, resource, revoked roles, stale claims, replay, and rollback; distinguish tested real endpoints from mocks and remaining limitations. Any unresolved P0/P1 must block certification of the affected technical scope.
- Respect V20 exclusion by **active writer ID rather than owner I**; obtain exact V17 named range first, re-read state, perform CAS and append idempotent BITACORA evidence, preserve author owner I, and release only own lock. A reviewer cannot self-approve changed code and cannot write over an active scope.
- An approved A5 for the scoped review would be recorded by a distinct Task with reviewer SHA, logs and READBACK; a FAIL should identify specific regression and leave gate closed. Unanswered request stays **A5_PENDING**; do not mark M06 or technical successors complete on the request alone.
- QA/main/PROD, commercial payment actions, secrets, real privilege grants and production migrations require independent authorization and are not permitted by this request.

## A1–A4 documentary gate and rollback

A1 for **D216 request only** verifies that this file states a concrete independent-review target, eligible reviewer selection constraint, expected evidence, negative FAIL handling and a future handoff; it must reject the propositions “request = A5_PASS”, “M05.06 5/5 = all capabilities” and “VALIDANDO = LISTO”. A2 and A3 are **NO_APLICA** for Markdown-only request/change provenance: no JWT/RBAC, UI, HTTP, SQL, or payments runtime is altered. A4 requires a published Git SHA with Scope Lock/V5 completed/success plus PLAN/COLA/DAG/BITACORA readback; only then is D216 **request task** eligible for `LISTO` via CAS, with its **own** independent A5 still pending. No synthetic status or formula overrides.

Rollback is normal fast-forward `git revert` for the documentation commit, retaining historical events and review decisions. No new automation/PR/branch/provider/paid service is created.

Sources: institutional V5 FINAL STANDARD, PLAN_MAESTRO rows 211–217, COLA/DAG_TECNICO, BITACORA A:Z and GitHub-hosted job 136732965290.
