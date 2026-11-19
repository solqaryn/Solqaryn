# C04-04.M03.02 - provider-neutral application service | V22

Owner DEV05 6adc2e55f47c81919f6374e561687076. solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK=STRICT. Risk R2 (financial). Authority: PLAN D699 "Implementar caso de uso, servicio o aplicación con límites claros"; E699 is a cumulative M03 phase objective, not this individual unit's certification of PSP settlement.

## Implemented scope (real C# code)
- Application service C0404PaymentLinkApplicationService consumes the pure Domain M03.01 verified context and plans link initialization, exact idempotency replay, webhook reconciliation and compensation without any PSP dependency.
- Prevents payment effects from an in-memory lock or browser redirect. An unknown PSP timeout demands an authorized provider query, not blind retry. A callback requires trusted backend HMAC/timestamp/ownership/reference/amount before even proposing atomic ledger reconciliation.
- A stable key with different digest rejects conflict 409. Settled state is immutable; identical replay is read-only. Refund demands a separate explicit permission, active membership, verified receipt and ledger version; no method executes refund or charge.
- xUnit adversarial cases in GitHub-hosted .NET10, directed TRX min 13, Scope Lock, V5, backend and security checks exact SHA required.

## A0-A5 and limitations
A0: isolated Application decision service, xUnit, workflow, documentation, CHANGELOG, no changed API/DI/DB/tenant runtime. A1: positives/negatives, expired and forged callbacks, cross-tenant, replay, conflict, timeout, non-settled rollback and revoked grant. A2: synthetic trusted-context tenant A/B and role/revocation negatives exercised; real JWT integration NOT certified. A3: HTTP/EF/MySQL/PSP NO_APLICA to this non-wired, pure application decision diff; full phase M03.03-M03.06 must prove JWT/RBAC, MySQL8.4 unique concurrent transactions, HMAC and real provider only with explicit commercial authority. A4: exact-SHA GitHub Actions plus V5/ScopeLock and sheet BITACORA readback; pending until checks pass. A5: independent Task Javier distinct from DEV05 after A0-A4.

No real cards, charges, webhooks, monetary effects, refunds, payment providers, production data, or branch promotion. This isolated class NEVER authorizes effects; consumer MUST implement durable UNIQUE and permission checks, not assume exactly-once. Rollback through ordinary git revert on dev with CI and history preserved. No QA/main/PROD.
