# N5.4.A — API/test preflight for profitability reporting

Authority: `docs/VAEP_AUTHORITY.md`.

## Existing source-backed test patterns

- API authorization is expressed with the repository's `Authorize`/`RequierePermiso` patterns and is covered by controller/contract tests.
- Query validation uses DTO validators and standard API error handling. `EstadoFinancieroFiltroValidator` is a concrete warning example: its current boolean condition allows `PeriodoContableId` together with exactly one date endpoint, so a future profitability validator must test every mutually-exclusive-filter combination explicitly rather than copy an unverified rule.
- The API uses shared pagination/result patterns and standardized `ProblemDetails` error responses.
- Existing sales-report contract tests provide fixtures and assertions for report authorization, query behavior and projections.

## Causal test matrix for N5.4.B+

1. **Authorization/disclosure:** authenticated access, required source-backed permission, explicit negative cases for cost/profit/margin disclosure, and no authorization widening through filters.
2. **Row scope:** seller/customer/branch/product/category queries and aggregates must never include rows outside the caller's authorized scope.
3. **Contract validation:** date/range/period rules, grouping value, sorting and pagination must be tested against the actual N5.4.B contract. Mutually exclusive filter combinations require both positive and negative cases.
4. **Profitability math:** absolute revenue/cost/gross-profit aggregation, zero-denominator behavior for any percentage, discount allocation and reconciliation to persisted sale totals must be tested after those semantics are explicitly defined.
5. **Grouping:** product/category/seller/customer aggregation must verify identity semantics and the current-vs-historical category caveat.
6. **Pagination/sorting:** bounds and aggregate ordering must match the implemented contract; this preflight does not invent whether an oversized page is clamped or rejected.
7. **Errors/observability:** standardized validation/authorization/server error shapes and non-leakage of internals, plus audit/correlation assertions where required by the chosen security contract.

## REVIEW_FIRST disposition

This facet is accepted after removing implementation assumptions that were not yet source-backed. It establishes the reusable API/test harnesses and the causal cases N5.4.B must satisfy once its contract is defined. It is not a parent-level PASS/LISTO_REAL.
