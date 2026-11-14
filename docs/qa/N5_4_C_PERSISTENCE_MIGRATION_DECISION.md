# N5.4.C — Persistence, migration and data decision

Authority: `docs/VAEP_AUTHORITY.md`.

## REVIEW_FIRST integration context

ATTEMPT1 `N5.4.C.1.PERSISTENCE_MIGRATION_DECISION` completed with a valid terminal contract on Jules session `sessions/3724127607689832244`, workflow run `34440850049`, patch present, two self-reviews, and terminal classification `READY_FOR_VAEP`. The artifact reports backend non-integration tests and frontend tests passing.

A controller-side transport-stall marker was written before that terminal became visible because no correlated session/activity was observable through the controller surfaces being polled at that moment. The completed lifecycle artifact subsequently proved useful Jules activity through `2026-09-10T05:36:03.722838Z` and terminal completion at `2026-09-10T05:36:17Z`. Therefore that earlier stall/takeover assessment is **superseded as an operational conclusion**; Git history is retained as evidence, but the valid terminal result is REVIEW_FIRST authority for this task. No R2 or R3 is warranted.

REVIEW_FIRST accepted the ATTEMPT1 core decision — no schema/migration is required — with bounded controller corrections to avoid four material overclaims in the submitted document: shipping is resolved for the persisted absolute gross-profit contract even though broader contribution-margin semantics are not; category has no historical snapshot; absence of a migration does not justify a blanket “zero risk” claim; and index sufficiency must not be generalized beyond the current source-backed query shapes.

## Decision

**NO_SCHEMA_CHANGE / NO_NEW_MIGRATION / NO_BACKFILL for N5.4.C.**

The source-backed N5.4 profitability contract that is currently safe to implement is already representable by persisted sales data. Adding new profitability columns, historical dimensions, allocation fields, or permission state in this DB/migration stage would encode semantics that N5.4.B explicitly left unresolved.

## Existing persistence that already satisfies the accepted absolute contract

The live `Venta` model persists the sale-level monetary fields required by the accepted absolute-profitability contract: `Subtotal`, `Descuento`, `CostoEnvio`, `Total`, `CostoTotal` and `UtilidadBruta`. `VentaDetalle` persists `Cantidad`, `PrecioUnitario`, `CostoUnitarioSnapshot`, `Subtotal` and `UtilidadBruta`, plus product/variant identity and product display snapshots.

The live EF mappings keep the monetary fields at `decimal(18,2)`. The original `Fase3Fase4VentasFacturacionFinanzas` migration already created `Ventas.CostoTotal`, `Ventas.UtilidadBruta`, `VentaDetalles.CostoUnitarioSnapshot`, `VentaDetalles.Subtotal` and `VentaDetalles.UtilidadBruta`; therefore N5.4.C does not need a new migration merely to store the accepted sale-level absolute cost/gross-profit values.

`ReporteVentasService` already performs read-only server-side projections from persisted `Venta.CostoTotal` / `Venta.UtilidadBruta` for the report summary and from `VentaDetalle.CostoUnitarioSnapshot` / `Subtotal` / `UtilidadBruta` for detail rows. No persistence adapter or shadow copy is required for those existing values.

## Grouping/data identity consequences

N5.4.B established these source-backed identities and they require no new N5.4.C columns:

- seller: `Venta.CreadoPorUsuarioId` / `CreadoPorNombreUsuario`;
- customer: `Venta.ClienteId` / `ClienteNombre`;
- product: `VentaDetalle.ProductoId`, with variant independently available as `ProductoVarianteId`;
- category: the current `VentaDetalle.Producto.CategoriaId` relationship.

There is no persisted historical category snapshot on `VentaDetalle`. N5.4.B explicitly accepted current master-data category association as the current source-backed category dimension and prohibited inventing a historical snapshot. N5.4.C therefore does **not** add or backfill a historical category field.

## Explicit non-persistence gates

The following semantics must **not** be newly persisted or migrated in N5.4.C:

1. **Margin-percentage denominator.** N5.4.B found no source-backed sales denominator. No `MargenPorcentaje` persisted column or precomputed percentage is introduced.
2. **Sale-header discount allocation to product/category.** The sale has a header `Descuento`, while `VentaDetalle` has no approved per-line share. N5.4.C does not add `DescuentoAsignado`, `UtilidadAjustada`, or equivalent fields and does not invent a proration/backfill algorithm.
3. **Financial-disclosure permission state.** N5.4.B preserved the existing sales row-scope contract and deferred any new financial-censorship rule to the security/RBAC stage. Permission state does not belong in this persistence migration.
4. **Shipping as a new profitability cost field.** The accepted absolute contract already establishes that shipping is not subtracted in persisted `CostoTotal` or `UtilidadBruta`. N5.4.C does not rewrite those historical values or invent a second contribution-margin field; any broader contribution-margin meaning remains outside the accepted contract.

## Constraints and indexes

No new N5.4.C constraint or index is justified by the accepted persistence contract alone. The current model already has report-relevant sale indexes on `Ventas.Fecha` and `Ventas.CreadoPorUsuarioId`, plus detail indexing/relationships for product/variant and warehouse dimensions. N5.4.C does not invent a speculative index for a downstream aggregation shape before N5.4.D establishes the exact application/query contract.

This is not a claim that every future profitability query is optimally indexed. If N5.4.D introduces a source-backed query shape that demonstrably requires another index, that need must be justified causally from that exact query and returned through the ordered dependency chain rather than guessed here.

## Migration / operational runbook disposition

Because the decision is `NO_SCHEMA_CHANGE`:

- migration generation: **N/A**;
- logical backup for this change: **N/A** — no DB mutation is authorized or required;
- preflight DDL change: **N/A** beyond verifying the already-live model/migration definitions in source;
- backfill: **N/A**;
- production migration execution: **PROHIBITED / NOT PERFORMED**;
- post-migration reconciliation: **N/A** because no migration is applied;
- rollback SQL/migration: **N/A** because no schema/data change is introduced.

Historical integrity is preserved by leaving the existing cost snapshots and already-persisted sale gross-profit values unchanged.

## Proportional verification

The Jules terminal artifact reports:

- `TESTS_EXECUTED: cd backend && dotnet test --filter "Category!=Integration"` — passed;
- `TESTS_EXECUTED: cd frontend && npm run test -- --watch=false` — passed;
- `SELF_REVIEW_PASS_1: true`;
- `SELF_REVIEW_PASS_2: true`;
- terminal changed-files set: only `docs/qa/N5_4_C_PERSISTENCE_MIGRATION_DECISION.md`.

The bounded controller integration after REVIEW_FIRST changes documentation only; it does not modify C#, EF model, migration, SQL, frontend, or production state. The proportional exact-head gate for the correction is therefore source-consistency review against the live branch rather than rerunning unrelated runtime suites solely because control-plane/evidence/doc commits advanced HEAD.

`SELF_REVIEW_1: PASS` — no unresolved N5.4.B semantic was converted into persistence.

`SELF_REVIEW_2: PASS` — no schema, data, Production, main, secrets, or PR #2 merge action is introduced.

## REVIEW_FIRST disposition

**ACCEPTED WITH BOUNDED CONTROLLER CORRECTION.** The valid ATTEMPT1 terminal is the delivery authority; its core `NO_SCHEMA_CHANGE` conclusion is retained, the overclaims above are corrected, and the prior controller-visible stall assessment is superseded by the completed lifecycle evidence. No R2 and no R3 are created. Parent closure still requires a separate causal receipt recording DoD, applicable gates, and `P0=0 / P1=0` on the integrated state.
