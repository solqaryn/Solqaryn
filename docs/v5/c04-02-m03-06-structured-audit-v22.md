# V22 C04-02.M03.06 - structured contract-preflight audit boundary (R2)

Plan D631: Implementar auditoria, logs estructurados y manejo de errores seguro. Authority: V5 institutional PLAN_MAESTRO; SOLQARYN dev and PROJECT_SCOPE_LOCK STRICT. PLAN E631 describes accumulated phase M03, not this atomic unit.

## Delivered scope

Real .NET 10 Application-layer C0402ContractAuditedPreflight decorates C0402ContractApplicationService.PrepareAsync. An injected IC0402ContractAuditSink must confirm sanitized event before preview; a structured ILogger must be enabled. Event contains only fixed name, server-controlled outcome code, and read-only candidate flag. No caller cause, tenant, user, resource, token, exception or payload is logged. A missing/failing sink or logger fails closed; unexpected source exception produces a generic denial, while cancellation propagates. No payment or contract mutation is ever authorized.

## Tests and known limitations

C0402M0306StructuredAuditTests asserts positive read-only preview, cross-tenant, revoked membership, payment flag denial, sink rejects/errors, logger disabled/errors, provider exception redaction and post-audit cancellation. CI must actually run tests in GitHub-hosted .NET 10 Release. Actor, contract and sink in these tests are synthetic, not real JWT HTTP or Oracle EF MySQL 8.4. No durable production audit sink, DI registration, HTTP endpoint, hosted worker or legal D01-D08 approval has been installed. Such live integration is NOT certified here; phase completion and production permissions remain separate. Require exact-SHA Scope Lock, V5 provenance, backend Release/security/authorization checks SUCCESS, and independent A5 after A0-A4. No QA/main/PROD. Normal git revert rollback, preserve BITACORA and calculated DAG.
