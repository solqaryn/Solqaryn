# V22 — C04-01.M05.02 API/DTO real HTTP and safe error contract (dev)

## A0 scope, requirement, risk
PLAN_MAESTRO D603: Ejecutar contrato API/DTO y validar errores seguros. PLAN E603 (Completa M05) is cumulative, not a full-phase prerequisite or certification by this test-only unit. R2 because the surrounding plan is SaaS finance/auth. This unit modifies only xUnit tests, a dedicated SHA-scoped GitHub Actions workflow, this documentation and a historically intact CHANGELOG section. It does not add a production controller or enable paid plans.

## A1 positive, negative, boundary and error
A real ASP.NET Core 10 TestServer invokes the existing C0401PlanApplicationService and existing production ExceptionHandlingMiddleware. HTTP 200 serializes actual shared ApiResponse<T> JSON envelope and Preview DTO: requires backend gate and explicitly denies writes, quota reservations and payments. Bad plan or tenant-zero yields HTTP 400; tenant B vs a trusted synthetic actor A yields HTTP 403 without disclosing plan details; unavailable snapshot provider HTTP 503 does not reflect a private SQL diagnostic; unexpected exception passes through real production middleware and returns safe 500. Signed request JWT is NOT exercised here: the fixture uses synthetic test-only sources for actor/plan, not real MySQL. A separate M05.03 proves physical MySQL tenant rollback; M05.05 tests real signed JWT HTTP negatives. No mock is described as real JWT/DB isolation.

## A2/A3 applicable gates
A2 tests no secret/tenant data leaks in HTTP error bodies; tenant B conflict denies by existing application case-use service. A3 is actual ASP.NET Core TestServer and production middleware/DTO serialization, not a mock transport. JWT live signing, MySQL, PSP and browser are NO_APLICA to this test-only HTTP/DTO change; do not claim those integrations from these fixtures. No live endpoints or production runtime/transaction touched.

## A4 exact SHA, rollback
GitHub-hosted .NET10 Release xUnit targeted job TRX >=6, 0 fail/skip/not-executed plus backend and security checks, Scope Lock strict, V5 changelog provenance exact SHA, full BITACORA/PLAN/COLA readback under namedRange V17. If a required job is queued/in_progress/fail/skipped/cancelled, keep VALIDANDO owner unchanged and hand off precise SHA/job. No QA/main/PROD promotion. Rollback only normal git revert plus new history.

## A5 independence and LIMITACION_FASE
A5 only after LISTO and genuine code/test review by another Javier Task, never DEV04 self. This is an HTTP/API/DTO contract test of existing code and exception middleware, not an actual priced plan contract, real authenticated tenant-B isolation, payment provider, quota enforcement, JWT token validation, deployed API or cumulative M05 proof.
