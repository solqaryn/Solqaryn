# N5.4.B — Domain contract: profitability grouping identity

Authority: `docs/VAEP_AUTHORITY.md`.

## Source-backed grouping identities

The live sales-report query path in `ReporteVentasService` establishes the current grouping/filter identities that N5.4 profitability may reuse without inventing a second identity model.

1. **Seller / vendedor**
   - Source: `Venta.CreadoPorUsuarioId` and `Venta.CreadoPorNombreUsuario`.
   - The existing report projects these fields as `VendedorId` / `VendedorNombre`, and the row-scope query uses `CreadoPorUsuarioId` for seller filtering and non-admin scope.
   - This is therefore the source-backed seller identity for the current report contract; it must not be broadened to another user relation without a later approved contract.

2. **Customer / cliente**
   - Source: `Venta.ClienteId` and `Venta.ClienteNombre`.
   - This is a header-level sale identity and is already projected/filterable by the sales-report path.

3. **Product / producto**
   - Source: `VentaDetalle.ProductoId`; variant identity remains separately available as `ProductoVarianteId`.
   - Product profitability must aggregate detail rows rather than infer a product from sale-header totals.
   - Historical display names come from the persisted product snapshots already projected by the report; identity remains the normalized product ID.

4. **Category / categoría**
   - Source: the current relation `VentaDetalle.Producto.CategoriaId` used by `ReporteVentasService.AplicarDimensionesDetalle`.
   - No historical category snapshot is persisted on `VentaDetalle` by the inspected N5.4 source. Therefore category grouping follows the product's **current master-data category association** unless a later approved migration introduces a historical dimension.

## Constraints

- Do not invent a historical category snapshot in N5.4.B.
- Seller/customer are sale-header identities; product/category require the detail dimension.
- Filters are not authorization grants. The existing report first applies authenticated row scope and then intersects request filters.
- Grouped profitability implementation remains dependency-gated behind the unresolved monetary allocation/percentage facets of N5.4.B.

## REVIEW_FIRST disposition

Accepted after controller review of the ATTEMPT1 documentation artifact. The missing `TESTS_EXECUTED` marker is an evidence-only gap for this documentation/source-inspection scope; direct inspection of the live query contract verifies the identities above, so no content R2 is warranted. This facet is not a parent-level PASS/LISTO_REAL.
