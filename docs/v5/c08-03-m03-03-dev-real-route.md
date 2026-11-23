# C08-03.M03.03 — DEV opt-in JWT/RBAC tenant read-only endpoint

PROJECT_SCOPE_LOCK=STRICT; repository=solqaryn/Solqaryn; branch=dev; risk=R2.

The development API now maps the same endpoint implementation used by the
isolated GitHub-hosted HTTP TestServer, only when the environment is Development
and GymAuthorization:EnableReadOnlyProbe=true. The flag defaults to false and
no endpoint is mapped in qa/production. Route:
/__dev/gym-auth-readonly/{tenantId:int}/{branchId:int}.

Authentication uses the actual JwtBearer scheme, current relational tenant,
role, permission, membership and branch state; mismatched tenant, fake JWT,
revoked permission, disabled role or branch produce 401/403 without exposing
records. A valid current read authorization returns READONLY_NO_GYM_GRANT,
never a subscription, payment, door/hardware or business access permission.
The user-supplied headers X-Empresa-Id and X-Rol-Id are not trusted.

Integration is scoped to Oracle EF Core and ephemeral MySQL 8.4 in GitHub Actions:
one test exercises tenant A/B, forged JWT, current SQL changes, revocation
and rollback, another proves that a missing opt-in never maps the route.
This is not a public gym endpoint nor certification of gym entitlements,
physical devices, actual membership reconciliation, historical approvals or
QA/main/PROD. Retain VALIDANDO until new exact-SHA CI A0-A4 are complete and
then seek independent A5.

Rollback: normal non-destructive git revert of the new commit; disabling the
flag also removes the route on next app startup. Never change prod secrets.
