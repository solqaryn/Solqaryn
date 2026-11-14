# N5.4.A — Security/RBAC preflight for profitability reporting

Authority: `docs/VAEP_AUTHORITY.md`.

## Source-backed security authorities

- Solqaryn authorizes endpoints through relational role/permission grants and the existing `RequierePermiso` / permission-service path.
- Existing sales/report queries apply user data scope independently of request filters; a transactional filter must intersect authorization scope and must never widen it.
- inventario valuation provides a repository precedent for separating access to a report from disclosure of sensitive financial fields: report access and financial-value disclosure can be governed separately, with audit behavior around censored financial data.
- Existing audit services and request correlation patterns provide the implementation precedent for recording access/denial/censoring decisions.

## Mandatory security implications for N5.4.B+

1. Aggregated profitability must be computed only over rows inside the caller's authorized data scope; aggregate queries cannot bypass row-scope enforcement.
2. Cost, gross profit and margin are sensitive financial data. Their disclosure requires an explicit source-backed permission decision and negative authorization tests.
3. This preflight does not create `Rentabilidad:Ver`, `ReportesRentabilidad`, or any other new permission. N5.4.B must select an existing permission or introduce a new one only if a separately approved contract authorizes it.
4. Product/category/seller/customer filters are query criteria, not authorization grants.
5. Aggregates must be tested for cross-branch/cross-user leakage, including cases where an unauthorized slice could be inferred from totals.
6. The implementation must preserve the repository's audit/correlation behavior for financial disclosure, denial or censoring decisions where that behavior is part of the chosen contract.

## REVIEW_FIRST disposition

This facet is accepted as a security preflight. It establishes fail-closed row scope and explicit financial-disclosure authorization as causal implementation gates while deliberately leaving the exact profitability permission contract unresolved. It is not a parent-level PASS/LISTO_REAL.
