# C13-04.M03.06 — Safe structured audit for isolated visual review

- Risk R2: logging, concurrency and tenant identity; only a bounded, in-process synthetic visual review boundary is changed. Domain, Application, HTTP, EF/MySQL and production configuration remain untouched.
- Added `C1304VisualReviewAuditSink` with fixed EventId 130406, typed `LoggerMessage.Define` properties and allowlisted staged/rejected codes from the existing C13-04.M03.05 worker. It cannot accept or print arbitrary caller keys, SHA, JWT, PII or provider exception details.
- Accepted staging is explicitly **not** browser A5 or a role/tenant permission; receipts always deny grants, QA and production. Reject incorrect revision outcomes upstream in the bounded worker, which remains unchanged.
- A1: xUnit success, six safe rejections, invalid tenant, counts 0/>8, unknown status, cancellation and logger throwing a secret-bearing exception.
- A2: independent tenant 11/22 logs under concurrent calls, no mixed tenant records, no auth grants, no replay privilege; no actual JWT/RBAC authority claimed.
- A3: real ILogger structured logging under xUnit on .NET 10, using in-process fake sink; not a durable provider. MySQL 8.4, HTTP, EF and Playwright are NO_APLICA because none is modified or connected.
- A4: require Release xUnit, Scope Lock/V5, backend, security checks SUCCESS on exact published SHA and Sheets/BITACORA readback; external running checks never PASS.
- A5: independent Javier reviewer checks code and adversarial tests. No self-approval, no QA/main/PROD.
- Rollback: normal `git revert` on dev, never force/reset. Source remains independent of DI/hosted worker, no credentials.
