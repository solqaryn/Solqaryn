# N5.4.B — Disclosure and row-scope contract

Authority: `docs/VAEP_AUTHORITY.md`.

## REVIEW_FIRST source inspection

J6 R2 completed with a valid terminal envelope and useful tests/review, but controller REVIEW_FIRST found one material overclaim: the patch promoted an inventario-report financial-disclosure precedent into an exclusive N5.4 sales-profitability rule. The live sales-report code does not currently establish that mapping, so this ATTEMPT2 scope is corrected by controller takeover; there is no R3.

## Source-backed row-scope contract for sales reports

The current sales-report path is explicit and can be reused without inventing semantics:

1. `ReportesVentasController` requires `ModuloSistema.Ventas + AccionPermiso.Ver` for both summary and detail endpoints.
2. `ReporteVentasService` resolves `UsuarioScopeActual` through `IUsuarioScopeService.ObtenerActualAsync()` before constructing the sales query.
3. If the authenticated database-backed scope is `null`, the query fails closed with `Where(_ => false)`.
4. An administrator may optionally narrow by the requested `VendedorId`; a non-administrator cannot expand row scope with that HTTP parameter and is restricted to `CreadoPorUsuarioId == alcance.UsuarioId`.
5. The restriction is part of the EF query before aggregation/detail projection, so row filtering occurs in the database query path rather than after sensitive rows are materialized.

This row-scope contract is source-backed for the existing sales reports and is the safe boundary for later N5.4 implementation unless a later authorized change deliberately replaces it.

## Sensitive profitability disclosure: precedent vs. current sales contract

The repository also contains a **separate precedent** in `ReportesInventarioValorizacionController`: base access is `Inventario:Ver`, while a runtime `Finanzas:Ver` check controls whether inventario financial values are returned; the censored and authorized paths are audited.

That is evidence that Solqaryn already uses `Finanzas:Ver` as a financial-disclosure pivot in at least one non-Finanzas report. It is **not**, by itself, proof that N5.4 sales-profitability fields must automatically inherit the same rule.

The current `ReportesVentasController` requires `Ventas:Ver` and does not perform a `Finanzas:Ver` censorship check. Its current DTOs already expose `CostoTotal`, `UtilidadBruta`, and line `CostoUnitario`/`UtilidadBruta` under that sales-report path. Therefore the exact future rule for newly expanded profitability disclosure is not source-backed strongly enough to invent here.

## Contract decision

- Do **not** invent a new profitability/cost permission.
- Preserve the existing source-backed sales row scope described above.
- Treat the inventario `Finanzas:Ver` censorship behavior as a candidate precedent for the later security/RBAC implementation, not as an already-established N5.4 sales rule.
- Any change that adds financial censorship to sales-profitability outputs must be decided and tested explicitly in the N5.4 security/RBAC stage; until then, this disclosure sub-contract remains an explicit dependency rather than a fabricated permission rule.
- Existing sales-report audit behavior records report queries; do not claim a censored-vs-full sales audit branch exists until such a branch is actually implemented.

## REVIEW_FIRST disposition

**CONTROLLER TAKEOVER ACCEPTED.** J6 R2 supplied valid evidence and passed its requested backend test suite, but its content overclaimed the applicability of the inventario financial-disclosure precedent. Because this was ATTEMPT2, no R3 is permitted. The controller corrected the scope directly against `ReportesVentasController`, `ReporteVentasService`, `VentaRepository`, and `ReportesInventarioValorizacionController`. The resulting contract is source-backed, fail-closed on row scope, and explicitly preserves the unresolved disclosure dependency without inventing a permission.
