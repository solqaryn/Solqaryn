# V22 C04-02.M03.06 - structured contract-preflight audit boundary (R2)

Plan D631: Implementar auditoria, logs estructurados y manejo de errores seguro. Authority: V5 institutional PLAN_MAESTRO; SOLQARYN dev and PROJECT_SCOPE_LOCK STRICT. PLAN E631 describes accumulated phase M03, not this atomic unit.

## Delivered scope

Real .NET 10 Application-layer C0402ContractAuditedPreflight decorates C0402ContractApplicationService.PrepareAsync. An injected IC0402ContractAuditSink must confirm sanitized event before preview; a structured ILogger must be enabled. Event contains only fixed name, server-controlled outcome code, and read-only candidate flag. No caller cause, tenant, user, resource, token, exception or payload is logged. A missing/failing sink or logger fails closed; unexpected source exception produces a generic denial, while cancellation propagates. No payment or contract mutation is ever authorized.

## Tests and known limitations

C0402M0306StructuredAuditTests asserts positive read-only preview, cross-tenant, revoked membership, payment flag denial, sink rejects/errors, logger disabled/errors, provider exception redaction and post-audit cancellation. CI must actually run tests in GitHub-hosted .NET 10 Release. Actor, contract and sink in these tests are synthetic, not real JWT HTTP or Oracle EF MySQL 8.4. No durable production audit sink, DI registration, HTTP endpoint, hosted worker or legal D01-D08 approval has been installed. Such live integration is NOT certified here; phase completion and production permissions remain separate. Require exact-SHA Scope Lock, V5 provenance, backend Release/security/authorization checks SUCCESS, and independent A5 after A0-A4. No QA/main/PROD. Normal git revert rollback, preserve BITACORA and calculated DAG.

## V23 isolated durable MySQL 8.4 increment (2026-11-21)

An Infrastructure IC0402ContractAuditSink uses parameterized SQL in an isolated InnoDB schema, explicit transaction COMMIT, sanitized event/outcome without caller secrets, and fail-closed behavior. The new Category=Integration test checks a second physical connection, cross-tenant denial, unsafe event/outcome, wrong schema, cancellation and missing table. A GitHub-hosted physical mysql:8.4 job must pass its unique TRX 1/1 and scope lock exact-SHA before this counts toward A3/A4. The Application actor/contract sources in this test are synthetic. This increment does NOT certify signed JWT/HTTP, production tenant-RBAC, real commercial writes, live database or DI. PLAN E631 is cumulative, PLAN D631 audit scope atomic; A5 requires an independent Javier Task. SKREF 03/04/05/06/10/11/12 NO_APLICA: no UI/motion/copy/threat-model/perf scope. No QA/main/PROD. Rollback normal git revert.
