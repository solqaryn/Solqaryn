# SOLQARYN V17 — C11-04.M03.04 isolated immutable observability snapshot adapter

**R2**: tenancy, JWT, RBAC, access to telemetry and private data. **ID**: C11-04.M03.04, fila PLAN 1709, author TASK_ID 6adc2e55f47c81919f6374e561687076 (DEV05), branch dev, PROJECT_SCOPE_LOCK STRICT, named range lock 2000001709.

## A0 / technical design

Implements a real Infrastructure .NET10 class C1104ObservabilityIsolatedSnapshotAdapter that captures trusted **synthetic** principal and aggregate evidence, validating exact SHA and tenant by ordinal comparisons. It deep-copies label dictionaries to prevent mutable test snapshot poisoning. It composes C1104ObservabilityAuthorizationGate (Application M03.03), C1104ObservabilityApplicationService (M03.02) and C1104ObservabilitySignalDomainRules (M03.01). The principal and evidence read ports are in-memory deterministic fakes explicitly **not registered in production DI**.

No MySQL/EF mapping, migrations, HTTP endpoint, JWT token verification, persistent tenant query, exporters, alerting, metrics provider, SLO calculation or production access. A nominal verdict requires a separate authenticated API adapter, and every result always denies tenant read, telemetry exposure, database write, metric export, SLO/cost certification.

## A1/A2 risk-proportionate executable tests

11 xUnit fact methods test nominal no-grant, tenant B, stale SHA, foreign principal, revoked JWT/membership/role/RBAC, forged resource owner, NO_MEDIDO denominator, raw/PII signal labels, immutable label copy against injected PII, unsafe signal/correlation, missing aggregate, canceled operation and invalid constructor. Required GitHub-hosted .NET10 Backend Release, Security multiempresa, Authorization/files/secrets, Scope Lock and V5 provenance must all complete SUCCESS for exact causal SHA; no count of queued/in-progress as PASS.

## A3 / A4 / A5

Real MySQL 8.4 integration and HTTP/Playwright are **NO_APLICA** to this isolated Infrastructure adapter; integration remains mandatory when authentic provider/EF/HTTP implementation is separately authorized. Four-file direct fast-forward CAS publication, CHANGELOG section contiguous, BITACORA event and PLAN/COLA/DAG readback. If required CI pending, leave VALIDANDO with owner DEV05; independent A5 needs distinct Javier Task. Rollback normal git revert, no QA/main/PROD, secrets, paid infrastructure or data migration.