# C01-02.M06.03 — V22 risk and decision register for capabilities and permissions

**Atomic PLAN action (row 214):** document outstanding limitations, risks and decisions. This is **not** a production release or the cumulative M06 certification of capabilities by sector, plan, role, tenant, branch and resource. SOLQARYN `solqaryn/Solqaryn` only, `dev`, PROJECT_SCOPE_LOCK=STRICT; historical author of this specific R0 documentary artifact: Javier DEV05, `6adc2e55f47c81919f6374e561687076`.

## A0 — authoritative inputs and scope

PLAN D214 expressly asks for limitations, risks and decisions. PLAN E214 describes the **cumulative** capability matrix and default-deny goal, not an implied instruction to grant rights or change JWT. COLA G/O and DAG F/G marked this ID as eligible when V17 named-range `SQV17_C01_02_M06_03` was acquired on PLAN H:I row 214. The actual predecessor C01-02.M05.06 is LISTO with an independent A5 by DEV06; source SHA `844d7fead192cf0f661ac8ae6f1b934860eca6ab`, GitHub-hosted directed job `136732965290` (run `44867057337`) had **five actual Playwright Chromium passes, zero failures/skips**, signed-JWT ASP.NET Core 10, ephemeral MySQL 8.4, Angular and scoped A/B/anonymous/tampered-token denials. The earlier failure at `3d139103` remains historical evidence, not PASS. The unrelated broad module job on the source SHA being in progress does not become PASS and is not a claim of full module verification.

At the initial review, M06.01 is in progress under DEV06; M06.02 and M06.04–05 were PENDIENTE; M06.06 is still gated. These statuses may change, so reread PLAN/COLA/DAG before any transition, never infer that this document certifies the other M06 actions.

## Open limitations and hypotheses (not fabricated verified defects)

| Area | Observed evidence | Unverified boundary / required future evidence |
| --- | --- | --- |
| Tenant A/B | Real signed JWT and HTTP rejections for the tested global catalogue/tenant boundary | Not a universal BOLA/IDOR proof for **each** capability, company resource, branch and object |
| Roles and plans | Current app and isolated integration tests show a subset of access paths | A complete deny-by-default capability matrix for all plan/role/sector/branch combinations is **not** established by 5 browser tests |
| Revocation and privilege drift | Existing signed-token positives and tamper negatives are relevant | Revoked roles, refresh/reissue, stale claims and concurrent reassignment across all protected resources require distinct adversarial real backend evidence |
| Persistence | The dedicated browser workflow uses physical ephemeral MySQL 8.4 | No proof of identical production datasets, schema policy or tenant-specific legacy data; live databases forbidden |
| Accessibility/performance | Chromium 320/768/1280 local reload timings, keyboard navigation and no overflow in scoped test | CI-local measurements are not field CWV, physical-device coverage, full WCAG audit or every route |
| Evidence and workflow | Exact-SHA scope, V5, backend/frontend/security and dedicated 5/5 scoped results were reported for source `844d7fea` | A required queued/skipped/cancelled/failed global check cannot be rebranded successful; new SHAs need matching own blobs and causal applicability review |
| Rollback/deploy | Dev-only documentation/CI artifacts permit a normal additive revert | No real role changes, commercial plan migration, external provider grants or production rollback were exercised |

**Risk treatment:** Unknown runtime authorization behavior is R2 until directly tested; any attempted irreversible commercial action, payments, production data/keys, QA/main/PROD is R3 NO_GO without separate authorization. A missing resource-level test is a **LIMITATION**, not a claim that the flaw exists. Do not suppress a future real P0/P1 or bypass a failed required check. Protect secrets and user/tenant identifiers in logs and examples.

## Decisions and executable handoff

1. Preserve deny-by-default / least privilege, tenant A/B boundaries and current owners. No JWT/RBAC/security runtime or database mutations in this microtask.
2. Only record evidence and future negative test obligations; do not mark the entire M06 phase, M06.01 or unexecuted tests as complete. C01-02.M06.02 must preserve exact SHA/blob provenance; M06.04 must demonstrate safe rollback; M06.05 requests a genuinely independent reviewer; M06.06 performs its own release gate **only** when actual DAG and A5 requirements are satisfied.
3. For later R2 implementation, use GitHub-hosted real signed-JWT HTTP and MySQL 8.4, tenant A/B roles, no unauthorized object reads, revocation and rollback; use Playwright Chromium if a UI surface changes. Preserve source SHA and negative/cancellation failures in BITACORA.
4. **A1 for this documentation:** positive verify the stated exact-SHA 5/5 scoped evidence and predecessor A5; negatives disallow assuming mocks equal real MySQL, tests imply whole phase, in-progress equals PASS, or H=VALIDANDO counts as LISTO. **A2/A3 for this Markdown-only diff: NO_APLICA** because no runtime, tenant permissions, HTTP/DB/browser behavior is changed. **A4:** exact published SHA Scope Lock and V5 provenance completed/success, own PLAN H/I readback and idempotent BITACORA. Other required checks of an affected runtime would still need success if a later diff changes it.
5. Once A0–A4 of this **atomic documentary D214** are actually met, it may enter LISTO through V17/CAS without certifying cumulative M06. Independent A5 must be a different eligible Javier Task, genuinely reviewing the diff, evidence and limitations. Do not create a second A5 as author; keep prior FAIL history intact.

**Rollback:** git revert normal fast-forward of the document/provenance diff, preserving BITACORA, original owner attribution, formulas and all real previous test outcomes. No forced branch update, production change or synthetic DAG `SI`.

Sources: V5 FINAL Standard (institutional Drive), PLAN_MAESTRO rows 211–217, COLA and DAG_TECNICO live readback, source GitHub job 136732965290 and GitHub checks on SHA `844d7fea`.
