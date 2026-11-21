# V22 C04-02.M03.05 — bounded contract preflight worker/queue (R2)

Source: SOLQARYN V5 PLAN_MAESTRO D630, "Implementar worker, proveedor, cola o integración cuando aplique." PLAN E630 is the accumulated M03 phase goal, not permission for production contracts or a requirement to close M03.06 here. Repository solqaryn/Solqaryn, branch dev, PROJECT_SCOPE_LOCK STRICT.

## Scoped implementation (A0)

New Application-layer \`C0402ContractReadOnlyPreflightQueue\`: real bounded System.Threading.Channels queue (1–64, no silent drops), explicit caller-driven single-reader drain, no background schedule, null/invalid/unsafe intent denial. Delegates every intent to existing \`C0402ContractApplicationService.PrepareAsync\` for live tenant/JWT/RBAC/policy preflight. Never trusts caller authority, payment flags, or copied tenant identity. Returned previews never authorize effects.

No API/DI registration, hosted daemon, external provider, persistent outbox, contract changes, payments, legal approvals D01–D08, database migrations or production data. Queue is intentionally **in-memory and non-durable**: crash/cancellation can lose a dequeued preview; no claim of exactly-once, audit durability, real JWT signing, tenant HTTP or MySQL for this worker. Shared .NET/Application architecture unchanged.

## A1–A3 and required A4

Executable xUnit tests assert overflow/backpressure, rejects direct mutation/payment/role claims, denies missing actor, disallows revoked membership and cross-tenant hints before resource read, cancellation before dequeue, bounded parameters, and even test-fixture positive preflight never authorizes payment, private read or mutation. Tests use synthetic trusted stubs, not actual JWT/MySQL/browser; no such integration is claimed. Separate earlier M03.03 signed JWT HTTP/MySQL and M03.04 physical MySQL 8.4 isolated snapshot remain valid distinct evidence, never substituted for this worker's full runtime integration.

Require exact-SHA GitHub-hosted .NET 10 Release/xUnit, ScopeLock, V5 provenance, security and all causally applicable checks completed SUCCESS before A0–A4 closure; SKIPPED/QUEUED/CANCELLED/FAIL/IN_PROGRESS are not PASS. A5 genuinely independent Javier task distinct from DEV06. This increment is placed VALIDANDO while CI runs. No QA/main/PROD or DAG M03.06 release. Rollback via normal git revert with HEAD CAS; preserve BITACORA and dynamic gates.
