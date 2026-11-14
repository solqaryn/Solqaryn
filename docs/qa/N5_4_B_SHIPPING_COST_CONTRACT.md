# N5.4.B — Shipping cost contract

Authority: `docs/VAEP_AUTHORITY.md`.

## REVIEW_FIRST source inspection

The ATTEMPT1 artifact correctly identified the persisted profitability formulas but overclaimed that shipping affects only the final document total. Controller review corrected that statement against the live `VentaService` and `CalculoService` implementation.

Current source behavior is:

1. `Venta.CostoTotal = venta.Detalles.Sum(d => d.CostoUnitarioSnapshot * d.Cantidad)`; shipping is not included in this persisted cost field.
2. `Venta.UtilidadBruta = venta.Detalles.Sum(d => d.UtilidadBruta) - venta.Descuento`; shipping is not subtracted from this persisted gross-profit field.
3. `CalculoService` includes `envio.Monto` in the document `Total` calculation.
4. `CalculoService` also derives `ImporteProductos` as `max(0, ImporteBruto - envio.Monto)`. Therefore shipping is **not** isolated from every document calculation, even though it is excluded from the persisted `CostoTotal` and `UtilidadBruta` formulas.

## Source-backed profitability contract

For the current N5.4 **persisted absolute gross-profit contract**, shipping cost does **not** participate in `Venta.CostoTotal` or `Venta.UtilidadBruta`. Implementations that claim to reproduce those persisted fields must not subtract `CostoEnvio` again, because that would diverge from the source-backed formulas.

This decision is intentionally narrow. It does not establish a new business rule for a future margin-percentage denominator, product/category allocation, accounting contribution margin, or any other metric. `N5_4_B_MARGIN_PERCENT_CONTRACT.md` already records that the sales-profitability percentage denominator is unresolved and must not be invented.

## Document-calculation caveat

Shipping still participates in other sale calculations: it is included in the final document total and affects the current `ImporteProductos` derivation. Consumers must not generalize the persisted gross-profit exclusion into a claim that shipping is absent from all monetary calculations.

## REVIEW_FIRST disposition

**ACCEPTED after bounded controller correction.** The final Jules patch was reduced to the single requested documentation file after its own review detected and removed temporary C# investigation files. The controller additionally corrected the “only final total” overclaim. The terminal contract failed only because self-review/test markers were missing from the terminal envelope; the artifact activity contains review work, and no material content R2 is required for this documentation-only evidence gap.
