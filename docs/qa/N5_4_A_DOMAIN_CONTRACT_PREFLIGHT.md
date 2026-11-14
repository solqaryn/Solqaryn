# N5.4.A — Domain contract preflight for profitability

Authority: `docs/VAEP_AUTHORITY.md`.

## Source-backed semantics

- Sale amounts are persisted on `Venta` (`ImporteBruto`, `Subtotal`, `Descuento`, `Impuesto`, `Total`, `CostoEnvio`) and line amounts on `VentaDetalle` (`Cantidad`, `PrecioUnitario`, `Subtotal`).
- Historical cost is persisted as `Venta.CostoTotal` and `VentaDetalle.CostoUnitarioSnapshot`.
- Absolute gross profit is persisted as `Venta.UtilidadBruta` and `VentaDetalle.UtilidadBruta` and is already exposed by the sales-report DTO/service path.
- Seller/customer grouping can be based on sale-level identities already used by the reporting path. Product/category grouping must use the detail-dimension join rules; category is a current master-data association rather than a historical category snapshot.

## Contracts that are not yet source-backed

1. No persisted or established `MargenPorcentaje` contract was found. N5.4.B must explicitly define the denominator for any percentage metric; N5.4.A must not choose between `Total`, `Subtotal` or another basis.
2. Zero-denominator behavior for a percentage is not established. It must be defined before implementation rather than silently returning an invented value.
3. Persisted money uses decimal storage/monetary precision, but the rounding contract for a new derived percentage or grouped profitability result is not established by the current N5.4 source.
4. Header discount allocation to product/category groups is not defined by the current line model. If grouped totals must reconcile to sale-level `UtilidadBruta`, N5.4.B needs an explicit allocation/reconciliation rule.
5. Shipping-cost treatment in profitability is likewise unresolved by the existing gross-profit fields.

## REVIEW_FIRST disposition

This facet is accepted as a contract-gap preflight. Revenue, historical cost and absolute gross profit are source-backed; percentage denominator, zero-denominator handling, derived rounding, discount allocation and shipping-cost semantics remain explicit dependencies for N5.4.B. This acceptance is not a parent-level PASS/LISTO_REAL.
