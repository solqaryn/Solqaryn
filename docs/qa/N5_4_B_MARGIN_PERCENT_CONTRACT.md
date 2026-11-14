# N5.4.B — Margin percentage contract for profitability reporting

Authority: `docs/VAEP_AUTHORITY.md`.

## REVIEW_FIRST source inspection

The current repository has a percentage convention in `FinanzasService.GetResumenAsync`: `MargenUtilidadBruta` is calculated as `decimal.Round(utilidadBruta / ingresosTotales * 100m, 2)` only when `ingresosTotales > 0`; otherwise it is `0m`. The same service calculates `MargenInventarioPotencial` with value-at-sale as its denominator and the same two-decimal rounding convention.

That evidence is useful precedent, but it does **not** establish that N5.4 sales profitability must use `Venta.Total`, `Venta.Subtotal`, `ImporteBruto`, or any other sale field as the denominator. In `FinanzasService`, `ingresosTotales` comes from non-cancelled `MovimientoFinanciero` income movements, not directly from the N5.4 sales-report projection. `ReporteVentasService` currently exposes absolute `ImporteBruto`, `Subtotal`, `Descuento`, `Impuesto`, `Total`, `CostoTotal` and `UtilidadBruta` aggregates but does not expose or compute a profitability percentage.

## Source-backed decisions

1. **Percentage shape:** repository precedent expresses a margin as `(profit / positive denominator) * 100`.
2. **Zero/non-positive denominator precedent:** existing finance margins return `0m` when their chosen denominator is not positive.
3. **Rounding precedent:** existing finance margins use `decimal.Round(..., 2)`.

## Contract still unresolved

The **sales-profitability denominator itself is not source-backed yet**. N5.4.B must not silently map `ingresosTotales` to `Venta.Total`, `Subtotal`, `ImporteBruto`, or another sale amount. Until an approved source establishes that mapping, implementation of `MargenPorcentaje` remains dependency-gated.

Discount allocation and shipping-cost treatment remain separate N5.4.B contract facets and cannot be inferred here.

## REVIEW_FIRST disposition

Accepted after controller correction of the ATTEMPT1 evidence gap and overclaim. The repository proves calculation/rounding/zero-denominator precedents, but not the N5.4 sales denominator. Documentation-only scope; no runtime tests are required for this factual source inspection. This facet is not a parent-level PASS/LISTO_REAL.
