# API Contract Preflight - N5.3.A

## Context
As required by task N5.3.A.2.API_CONTRACT_PREFLIGHT, this document analyzes the existing sales and reporting HTTP endpoints, DTOs, filters, and contracts to identify the API surfaces affected by the new reporting dimensions: period, user, client, branch, category, product, variant, brand, model, color, and size. No controllers or contracts are being changed at this time; this is a read-only assessment.

## Findings

### Affected Controllers
Based on the codebase inspection, the following API controllers and their endpoints are likely to be affected or require new endpoints to support the requested sales report dimensions:

1. **VentasController (`backend/src/API/Controllers/VentasController.cs`)**
   * Currently has `[HttpGet]` for retrieving paginated sales data (`GetPagedAsync`) using `PagedRequest`.
   * Currently does not have an endpoint dedicated to comprehensive sales reporting with the requested dimensions.
   * **Implication:** A new dedicated reporting endpoint (e.g., `[HttpGet("reporte")]` or `[HttpGet("analitica")]`) or a significant enhancement to `GetPagedAsync` (via a specialized filter DTO) is needed. Or, a new controller `ReportesVentasController` could be created to align with the existing `ReportesInventario...` and `ReportesAdministrativos...` pattern. Given the module's architecture, creating a new `ReportesVentasController` is the most cohesive approach.

### Affected DTOs and Contracts
The following existing DTO structures provide insight into how the new reporting capabilities should be designed and which existing contracts may need extending:

1. **Report Filter DTOs:**
   * Existing examples: `ReporteAdministrativoFiltroDto`, `ReporteInventarioFiltroBaseDto`, `PedidoVentaFiltroDto`.
   * **Implication:** A new `ReporteVentasFiltroDto` (or similar) will need to be created. It must include fields for:
     * `Period`: `DateTime? Desde`, `DateTime? Hasta`
     * `User`: `int? UsuarioId` (or `int? VendedorId`)
     * `Client`: `int? ClienteId`
     * `Branch`: `int? SucursalId` (or `int? AlmacenId` depending on the domain model mapping for branches)
     * `Category`: `int? CategoriaId`
     * `Product`: `int? ProductoId`
     * `Variant`: `int? ProductoVarianteId`
     * `Brand`: `string? Marca` (or `int? MarcaId` depending on domain)
     * `Model`: `string? Modelo`
     * `Color`: `string? Color`
     * `Size`: `string? Talla`

2. **Report Result/Summary DTOs:**
   * Existing examples: `ResumenAdministrativoDto`, `ReporteInventarioValorizacionResumenDto`, `ReporteInventarioStockHealthDto`.
   * **Implication:** New result DTOs (e.g., `ReporteVentasResumenDto`, `ReporteVentasDetalleDto`) must be defined to return the aggregated sales data, including totals (gross, net, discounts, taxes), margins, and groupings by the requested dimensions.

3. **Export Contracts:**
   * Existing pattern: `[HttpGet("exportar/{tipo}")]` taking `formato` and the filter DTO (e.g., in `ReportesAdministrativosController` and likely `ReportesInventarioController`).
   * **Implication:** The sales reporting solution will need to implement a compatible export endpoint, leveraging the new filter DTO and returning a file result.

4. **Pagination Contracts:**
   * Existing pattern: `PagedRequest` and `PagedResult<T>`.
   * **Implication:** The detailed sales report endpoints (if returning rows rather than just summaries) will need to inherit from or utilize `PagedRequest` and return `PagedResult<T>` to maintain consistency.

### Risks and Limitations
* **Performance:** Complex filtering and aggregation across multiple dimensions (especially text-based dimensions like Brand, Model, Color, Size if they are not normalized) could lead to slow database queries. Proper indexing strategies will need to be evaluated during implementation.
* **Domain Alignment:** The exact mapping of "Branch" (Sucursal) needs to be clarified (e.g., does it map to `AlmacenId` in the context of a sale?).
* **Security/RBAC:** New endpoints will require strict permission checks. The exact module/permission symbol must be derived from the accepted N5.3 contract and existing enum rather than invented in this preflight. The implementation must ensure no sensitive financial data is leaked to unauthorized roles.

## Recommendations for Implementation (N5.3.B+)
1. **New Controller:** Introduce a `ReportesVentasController` specifically for sales analytics, separating it from operational sales logic in `VentasController`.
2. **Dedicated Filter DTO:** Create `ReporteVentasFiltroDto` containing all the requested dimension properties.
3. **Follow Existing Patterns:** Adopt the export and pagination patterns already established in `ReportesAdministrativos` and `ReportesInventario`.

## REVIEW_FIRST note
This preflight is accepted as architectural input, not as a binding implementation contract. In particular, branch/sucursal semantics and historical dimension identity must be reconciled with the backend/data preflight before N5.3.B code is certified. No N5.3.B+ code is implemented here.
