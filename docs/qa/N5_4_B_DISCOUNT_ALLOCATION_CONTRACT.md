# N5.4.B — Discount allocation contract

Authority: `docs/VAEP_AUTHORITY.md`.

## Source-backed state

Current sale logic persists the document discount at `Venta.Descuento` and computes sale-level gross profit from line gross profit less that header discount. Line `VentaDetalle.UtilidadBruta` is calculated from line subtotal minus historical line cost and does not persist a share of the header discount.

`CalculoService` contains `descuentoProrrateado` logic for tax-base calculation. That proration is evidence for tax computation only; it is not a source-backed profitability-allocation rule for product/category groups.

## Contract

- The repository currently has **no source-backed rule** that allocates a sale-header discount across products or categories for profitability grouping.
- N5.4 must not reuse tax-base proration as a profitability rule by analogy.
- Until an explicit approved rule exists, product/category grouped profitability cannot claim exact reconciliation with sale-header `UtilidadBruta` after discount by inventing an allocation.
- Sale-level profitability may continue to use the persisted header result; grouped implementation remains dependency-gated on a real allocation/reconciliation decision.

## REVIEW_FIRST disposition

Accepted after controller source review of the ATTEMPT1 terminal artifact. The artifact correctly identified the missing allocation contract and did not require a content R2. This documentation facet is not a parent-level PASS/LISTO_REAL.
