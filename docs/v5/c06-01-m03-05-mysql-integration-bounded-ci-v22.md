# SOLQARYN V22 — C06-01.M03.05: bounded real MySQL integration CI

Repo: solqaryn/Solqaryn; branch: dev; PROJECT_SCOPE_LOCK=STRICT.
Historical owner in PLAN_MAESTRO I882: DEV01. Repairer: DEV04.
Risk R2, CI-only. No business runtime, API/DTO, dependency package, persistent schema, JWT, production connection or permission changes.

## Reproducible causal failure

GitHub Actions run 44918951130, job 136890636871, commit efb636108419e950664c8a5f5ce7c0c285e745d7.
MySQL container reported physical 8.4.13 and migration/bootstrap/preflight steps SUCCESS. The complete, unfiltered Category=Integration xUnit test step started around 2026-11-21T15:30:39Z, never emitted final test results, and the 35-minute job cap ended it as CANCELLED at 2026-11-21T15:59:22Z. This is NOT an integration PASS; a missing TRX remains a failed gate.

## Minimal repair

Raise only the ephemeral migration CI job deadline from 35 to 65 minutes, preserving all tests. Add a 12-minute per-test hang diagnostic (no memory dump), physical MySQL integration TRX and fail-closed validation requiring at least 144 executed successes with zero failed and zero unexecuted. The job still fails on timeout, hang, failed/omitted tests or missing/malformed TRX. No job disabled, no skipped Category=Integration, no mocked MySQL, no Aiven/QA/main/PROD. GitHub-hosted SHA-bound concurrency remains unchanged.

A0: Scope restricted to dev-ci migration job, documentary evidence and code diff; risk R2.
A1-A3: Must be evidenced by the new GitHub-hosted job, actual MySQL 8.4 and TRX, not by this document.
A4: Requires new exact-SHA Scope Lock/V5/backend/security checks and full migration job SUCCESS, with no required SKIPPED/CANCELLED.
A5: Independent Javier Task other than original DEV01 and repairer DEV04, after A0-A4.
State remains VALIDANDO until all causal requirements are satisfied. Rollback via an ordinary revert of the repair commit; no destructive history rewrite.

Next: inspect exact new-SHA migrations job and TRX; if hang diagnostic identifies a named test, fix that actual test or integration under its owned scope rather than skipping/filtering it.
