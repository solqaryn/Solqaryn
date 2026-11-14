# N5.4.A — Backend/data preflight for profitability

Authority: `docs/VAEP_AUTHORITY.md`.

## Source-backed authorities

- `VentaDetalle.CostoUnitarioSnapshot`, `VentaDetalle.Subtotal` and `VentaDetalle.UtilidadBruta` persist line-level historical monetary inputs/results.
- `Venta.CostoTotal` and `Venta.UtilidadBruta` persist sale-level cost and gross-profit totals.
- `VentaService` computes line gross profit from line subtotal minus line cost and later rolls up `CostoTotal`; sale-level `UtilidadBruta` subtracts the sale discount from the sum of line gross profit.
- `ReporteVentasService` already aggregates persisted `CostoTotal` and `UtilidadBruta` and projects report data server-side.
- Existing product descriptive snapshots in `VentaDetalle` preserve historical display values independently of later master-data edits.

## Gaps and risks carried into N5.4.B+

1. The repository does not yet establish a source-backed profitability-percentage denominator for N5.4. A future percentage contract must explicitly choose its denominator and zero-denominator behavior rather than infer one here.
2. Sale-level discounts are applied at header gross-profit roll-up. Product/category profitability therefore needs an explicit source-backed allocation rule if grouped line results are expected to reconcile exactly with the header total.
3. Seller/customer dimensions exist at sale/report level; product/category grouped profitability needs the existing detail-dimension query path and must preserve row-scope authorization.
4. Existing persisted monetary fields mean no new profitability fact columns are proven necessary by this preflight. Index or other persistence changes remain contingent on measured query plans/performance; this document does not certify that no future DDL can ever be needed.
5. Shipping-cost treatment is not resolved by the inspected gross-profit roll-up and must not be invented as part of N5.4.A.

## REVIEW_FIRST disposition

The foundational historical cost and absolute gross-profit data required to begin N5.4.B design is present. This facet is accepted as a factual preflight only; it is not a parent-level PASS/LISTO_REAL and it does not resolve the percentage, discount-allocation, shipping-cost, authorization or performance contracts above.
