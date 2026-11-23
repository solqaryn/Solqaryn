# SOLQARYN V5 — C03-01.M03.02 Application login 0/1/N
Scope: PLAN D447 + UX_LOGIN_INTELIGENTE_V1, risk R2, project scope STRICT, dev only.

Changed: Application.Security.C0301LoginInteligenteApplicationService and xUnit tests.
The service checks BCrypt credentials once using the existing backend user repository,
re-reads the current user, active company memberships and enterprise-owned roles
through existing repository interfaces, and obtains the last tenant from a strictly
server-bound preference source. It then calls the approved Domain 0/1/N rules.
Zero valid memberships denies. One yields a candidate only. N automatically suggests
a last preference only if it remains present in the current eligible tenant set;
otherwise a deliberate tenant choice is required. No JWT, sessions or role grants
are issued; platform access is never inferred from an enterprise role.

Security: wrong password or blocked user cannot query memberships, duplicate and
foreign evidence fails closed, unavailable data returns a safe code, cancellation
propagates. This is an incremental pre-issuance Application contract, NOT an
authenticated live endpoint. The preference source needs a real trusted persistence
adapter, and the login bootstrap still needs API/DI, signed tenant-scoped JWT,
transactional revocation/replay protections and an isolated MySQL 8.4 HTTP integration
test before the full login flow can be certified. This test-only Application change
does NOT claim those absent integrations or grant tenant access; a domain candidate
must be revalidated by an authoritative backend at issuance.

A0: code+test diff scoped to application case orchestration.
A1/A2: xUnit positive/negative/boundary/error tests, pending Github-hosted CI.
A3: MySQL/HTTP/JWT/Chromium not certified by this changeset; real repo interfaces
are consumed, but mocks in unit tests do not prove live integration.
A4: GitHub exact-SHA checks, V5/ScopeLock and readback pending.
A5: independent Javier Task not author DEV05 or any repairer.
Rollback: normal git revert on dev, preserve historical BITACORA, no prod/QA/main.
SKREF-03/04/05/06/10: NOT_APPLICABLE (no UI, motion or copy).
SKREF-11: NOT_APPLICABLE (no formal threat-model scope); SKREF-12: no profiling.
