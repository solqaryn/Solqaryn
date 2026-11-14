# N5.4.B — Absolute profitability contract

Authority: `docs/VAEP_AUTHORITY.md`.

## REVIEW_FIRST source inspection

The ATTEMPT1 artifact was materially useful but its terminal contract had evidence-only gaps. Controller review compared the patch against the live sale domain and calculation code before integration.

The source-backed absolute values are:

- `VentaDetalle.Subtotal`: built as `Cantidad * PrecioUnitario`.
- `VentaDetalle.CostoUnitarioSnapshot`: historical unit cost captured on the sale line.
- `VentaDetalle.UtilidadBruta`: built as line `Subtotal - (Cantidad * CostoUnitarioSnapshot)`.
- `Venta.CostoTotal`: persisted as `Sum(Detalle.CostoUnitarioSnapshot * Detalle.Cantidad)`.
- `Venta.UtilidadBruta`: persisted as `Sum(Detalle.UtilidadBruta) - Venta.Descuento`.

`Venta.ImporteBruto` is sourced from the rounded sum of sale-line subtotals in `CalculoService`. `Venta.ImporteProductos` must **not** be described as an equivalent raw sum: the current calculation derives it as `max(0, ImporteBruto - CostoEnvio)`. That distinction is preserved here instead of repeating the ATTEMPT1 overclaim.

## Source-backed contract

1. **Line revenue boundary:** for an individual persisted sale line, absolute product revenue is `VentaDetalle.Subtotal`.
2. **Line historical-cost boundary:** line historical cost is `Cantidad * CostoUnitarioSnapshot`.
3. **Line gross-profit boundary:** `VentaDetalle.UtilidadBruta = Subtotal - historical line cost`.
4. **Sale historical-cost boundary:** `Venta.CostoTotal = Sum(line historical cost)`.
5. **Sale absolute gross-profit boundary:** `Venta.UtilidadBruta = Sum(line UtilidadBruta) - Venta.Descuento`.
6. **Shipping boundary:** `CostoEnvio` is not subtracted in the persisted `CostoTotal` or `UtilidadBruta` formulas. Its wider document-calculation behavior is governed by the separate shipping-cost contract.
7. **Discount boundary:** the sale-header discount reduces persisted sale-level `UtilidadBruta`, but the current line entity does not persist a per-line share of that header discount.

## Reconciliation boundaries and dependencies

The repository already has enough persisted fields and service logic for the **sale-level absolute cost and gross-profit values**, so no domain entity change is required for that narrow contract.

This does not resolve every grouped-profitability decision. Exact product/category reconciliation after a sale-header discount remains governed by `N5_4_B_DISCOUNT_ALLOCATION_CONTRACT.md`; margin-percentage denominator semantics remain governed by `N5_4_B_MARGIN_PERCENT_CONTRACT.md`; shipping treatment is governed by `N5_4_B_SHIPPING_COST_CONTRACT.md`. Those separate facets must not be inferred from this absolute-value contract.

## Domain code change required

**N/A for the absolute-value domain contract.** Existing `Venta` / `VentaDetalle` persisted fields and `VentaService` calculations already provide the source-backed absolute boundaries above.

## REVIEW_FIRST disposition

**ACCEPTED after bounded controller correction.** The controller removed the ATTEMPT1 overclaim that `ImporteProductos` is simply the sum of product subtotals and replaced the blanket “no unresolved dependencies” statement with the exact remaining cross-facet boundaries. The Jules activity stream records backend and frontend test execution plus code review; the terminal failure was caused by missing terminal evidence markers, not by a material patch error requiring a content R2. No R2 was consumed for this documentation-only evidence gap.
