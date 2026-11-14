# N5.4.A — Integration/rollback preflight for profitability reporting

Authority: `docs/VAEP_AUTHORITY.md`.

## Persistence findings

- `Venta` already persists sale monetary values including `CostoTotal` and `UtilidadBruta`.
- `VentaDetalle` persists quantity, price/subtotal, `CostoUnitarioSnapshot` and line `UtilidadBruta`, providing the historical cost basis needed for read-side profitability calculations.
- Therefore this preflight finds no source-backed requirement for a new profitability fact table or new cost/profit columns merely to expose existing absolute profitability data.
- That finding does **not** certify that N5.4.B+ can never require DDL: index changes remain contingent on measured query plans/performance, and any new persistence requirement must be justified by the later approved contract.

## Integration and rollback risks

1. Aggregated detail joins must avoid duplicate monetary contribution when traversing product/category or other dimensions.
2. Profitability reads must preserve row-scope authorization and the separately defined financial-disclosure policy.
3. New report queries must not mutate or couple to the transactional sale-creation flow.
4. Performance must be measured on the real query shape before introducing indexes; no speculative migration is authorized by N5.4.A.
5. Rollback should remove only the causal N5.4 implementation delta. Existing persisted historical sale/cost data must remain intact.

## Observability

- Reuse the repository's correlation/audit patterns for report access and sensitive financial disclosure decisions when required by the chosen security contract.
- Capture enough timing/query evidence to decide whether indexes are actually needed rather than pre-authorizing schema work.

## REVIEW_FIRST disposition

This facet is accepted as evidence that the current persisted sale/cost snapshots are sufficient to design a read-side profitability feature without proving new fact-schema requirements. Performance-driven indexes, exact authorization behavior and any later schema change remain dependency-gated decisions. This is not a parent-level PASS/LISTO_REAL.
