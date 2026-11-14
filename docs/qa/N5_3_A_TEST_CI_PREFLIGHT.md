# N5.3.A.5 — TEST / CI preflight for sales reporting

Authority: `docs/VAEP_AUTHORITY.md`.

## Watchdog takeover
J5 R2 exhausted the Jules retry budget (`ATTEMPT2/2`) and remained transport-only for >=10 minutes without a controller-visible result artifact/session handoff. The watchdog therefore classified the lane `TRANSPORT_STALLED`, quarantined the R2 for authoritative integration, and executed this preflight directly as `CHATGPT_VAEP`. No R3 or additional Jules session is authorized. Any late J5 result is forensic unless independently re-reviewed against this accepted takeover.

## Existing test surfaces verified

The repository already contains sales-domain regression material that N5.3 implementation must preserve rather than replace:

- `backend/tests/Solqaryn.Tests/VentaServiceTests.cs` covers core `VentaService` behavior.
- Sales-adjacent API/RBAC contract suites include `N31CotizacionApiContractTests.cs`, `N32PedidoVentaApiContractTests.cs`, `N36DevolucionClienteApplicationContractTests.cs` and `N37NotaCreditoClienteSecurityAuditTests.cs`; these establish a live pattern for explicit `ModuloSistema.Ventas` permission assertions.
- `N04RbacRelacionalTests.cs` covers relational RBAC catalog invariants.
- `InsumosAislamientoVentasTests.cs` and inventario/concurrency suites exercise data interactions that sales-report queries must not destabilize.

These suites are evidence sources and regression anchors. They do not prove a future N5.3 reporting contract until the corresponding N5.3.B–D implementation exists.

## Existing CI surfaces verified

- `.github/workflows/desarrollo-ci.yml` performs backend restore/build and non-integration tests, plus frontend install/lint/production build on the working branch.
- `.github/workflows/ci.yml` contains controlled backend build/test jobs and an isolated MySQL 8.4/browser acceptance environment; its current triggers/phase-specific conditions mean it is **not by itself** a canonical N5.3 gate.
- Existing feature workflows demonstrate a repository pattern of targeted backend tests, frontend unit/component tests, lint/build and Playwright where applicable.

N5.3 must reuse the strongest applicable existing gates and add only targeted coverage that the new reporting behavior actually requires. This preflight does not create a workflow merely to make CI look busy.

## Required N5.3 test matrix after implementation

1. **Backend domain/application:** report aggregation/filter semantics for period, user, client, branch/sucursal, category, product, variant, brand, model, color and size, with deterministic totals.
2. **Row-scope/RBAC negative tests:** unauthorized users cannot widen scope through filters or exports; financial-sensitive fields follow the contract accepted in N5.3.B.
3. **API contract:** pagination/filter validation, ProblemDetails/fail-closed behavior, stable serialization and export contract when implemented.
4. **Data/integration:** MySQL-backed tests for joins/grouping/history required by the accepted backend/data preflight; include representative historical records and avoid tests against Production.
5. **Frontend:** component/unit coverage for filter composition, loading/empty/error states and permission-driven actions after N5.3.E exists.
6. **E2E:** a focused browser flow for a representative sales report, including at least one denied-path case, only after both API and UI exist.
7. **Regression:** existing sales transaction creation/reading, RBAC, returns/credit-note paths and shared report infrastructure remain green.

## Closure gates for N5.3.A

For this preflight facet, inspection evidence is sufficient; no new product code exists to run a new N5.3 functional suite against. N5.3.A parent closure still requires all six preflight facets REVIEW_FIRST-accepted plus the exact-head causal gates required by the MASTER. A green workflow alone must never be treated as `PASS` or `LISTO_REAL`.

## Facet disposition

`N5.3.A.5.TEST_CI_PREFLIGHT` is accepted by CHATGPT_VAEP takeover with P0/P1=0 for this documentation scope. The concrete future test plan is source-backed, dependency-gated and non-filler. This facet does not close N5.3.A and does not authorize N5.3.B+ implementation.
