# N5.4.A — Frontend/UX preflight for profitability reporting

Authority: `docs/VAEP_AUTHORITY.md`.

## Reusable source-backed patterns

- `CentroReportesComponent` is the existing report hub and already uses runtime permission checks for report visibility.
- `ReportesVentasComponent` provides the current report orchestration pattern for filters, summary, detail, loading and error states.
- `ReporteVentasFiltrosComponent` already carries date-range and sales-report dimensions including seller, customer, branch, category and product filters, plus selector loading/error and range validation states.
- The existing sales-report models use the shared paged-result/API response patterns.
- `ReporteVentasResumenComponent` already renders `Costo Total` and `Utilidad Bruta` as HNL monetary KPIs.
- `ReporteVentasDetalleComponent` provides the existing responsive/paginated tabular pattern for sales-report detail.

## Gaps for N5.4.B+

1. The current sales-report detail is flat; no generic grouped-profitability view is established for product/category/seller/customer aggregation.
2. A future grouped response/model and its column semantics must be defined by the backend/application contract before the UI depends on them.
3. Sorting semantics for aggregated groups are not established by the current flat sales-report default and must be explicit in the future contract.
4. Profitability permission semantics must reuse source-backed authorization decisions from the backend/security contract; this preflight does not invent a new permission or report module.
5. Loading, empty, error, accessibility and responsive behavior should follow the existing report components unless a later approved contract requires a deliberate deviation.

## REVIEW_FIRST disposition

The existing Centro de Reportes and sales-report UX provide reusable filters, summary/detail structure and state-management patterns. The missing material contract is grouped profitability representation and its authorization/sorting semantics. This facet is accepted as preflight evidence only and is not a parent-level PASS/LISTO_REAL.
