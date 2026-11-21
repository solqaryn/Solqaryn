# SOLQARYN V22 — C04-01.M05.03 MySQL 8.4 causal integration (dev)

## A0 — Requirement, boundary, classification
PLAN_MAESTRO D604: Ejecutar integración de persistencia o proveedor cuando corresponda. The per-unit deliverable is an integration test of physical persistence relevant to the plan and tenant trust boundary. PLAN E604 is the cumulative M05 target, not proof of pricing, quotas, a payment provider or completed M05. Risk R2 (EF/MySQL, tenant and transaction). V17 row 604, PROJECT_SCOPE_LOCK=STRICT, solqaryn/Solqaryn@dev only.

Changed paths: a new test-only xUnit class, SHA-scoped GitHub-hosted MySQL workflow, this document and an additive intact CHANGELOG section. No API route, DTO or production source changes. Existing C04 verified actor adapter remains read-only and default-off. This does not prove authentication/JWT over HTTP.

## A1/A2 — Positive, negative, rollback, isolation and adversarial conditions
Fresh real MySQL database, migrations via existing test initializer, SELECT VERSION() required 8.4.x. Two separate tenants and scoped roles, one user and only tenant A membership. Positive: a fresh EF context finds A membership. Negative: it cannot find B membership. Boundary: insert a B membership inside an explicit SQL transaction; observe it; ROLLBACK; prove B has vanished using a NEW context while A remains. Revocation: disable A, commit with EF, read again in a NEW context and verify neither A nor B has active membership. EF in-memory mocks cannot pass.

## A3 — Physical integration and scope
GitHub-hosted Ubuntu with ephemeral mysql:8.4 on loopback, isolated temporary test credential, Oracle MySql.EntityFrameworkCore provider, TRX total=executed=passed=1 and zero failed/notExecuted. Random test DB deleted in finally. The membership persistence seam is necessary for plan authorization; subscription plan rows are not implemented or proven by this test. No live Aiven, payment PSP, production data or billing.

## A4 — Exact SHA and downstream evidence
To certify test-only M05.03, require dedicated GitHub-hosted job completed/success on source SHA, .NET Release, relevant security/authorization jobs, verify-solqaryn-project-scope, V5 provenance, and Google Sheets PLAN/COLA/BITACORA readback under V17 lock. SKIPPED/CANCELLED/IN_PROGRESS/FAIL required checks are NOT PASS. If a required check is running, keep VALIDANDO and owner unchanged and record run/SHA. No DAG gate bypass.

## A5 / rollback / limitation
Independent Javier Task different from DEV04 must review real requirements, code, tests, and CI. Only a forward git revert plus append-only BITACORA can undo this change; never force-push or erase history. LIMITACION_FASE: physical tenant persistence and rollback only. No paid plans, server-side pricing limits, JWT issuance, public HTTP endpoint, live payment provider, QA/main/PROD, or cumulative M05 certification.
