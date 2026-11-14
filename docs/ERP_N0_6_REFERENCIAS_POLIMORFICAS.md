# ERP-N0.6 — Referencias polimórficas críticas

## Dictamen

**Estado final:** LISTO / CERRADO.

ERP-N0.6 retira `ReferenciaTipo + ReferenciaId` y equivalentes como autoridad decisoria para documentos origen críticos. Las FKs tipadas son la autoridad relacional; los campos legacy permanecen sólo como snapshot/bridge de compatibilidad mientras se completa el saneamiento posterior.

**SHA funcional certificado:** `0e35a9f75c49b6ddfbd5ef21d426521e2b559c40`.

El preflight histórico se conserva en `docs/ERP_N0_6_REFERENCIAS_POLIMORFICAS_PREFLIGHT.md`; este documento es la fuente canónica final del punto.

## Contrato canónico

Los orígenes documentales admitidos para `MovimientoInventario` son `Compra`, `Venta` y `ConsumoInsumo`. Para un movimiento documental debe existir exactamente un origen tipado válido: `CompraId`, `VentaId` o `ConsumoInsumoId`.

La operación concreta (entrada, salida, anulación o reversión) se expresa mediante `TipoMovimientoInventario`/`CausaMovimientoInventario`, no mediante strings de origen.

En finanzas, `CompraId`, `VentaId` y `FacturaId` son la autoridad relacional. `ModuloOrigen`/`ReferenciaId` se conservan como snapshot de auditoría/correlación.

## Persistencia y transición

Migraciones principales:

- `20260812083000_N0_6_OrigenTipadoMovimientoInventario.cs`: FKs tipadas, backfill determinista y preservación del snapshot legacy.
- `20260812084900_N0_6_C3_IntegridadOrigenTipadoMovimientoInventario.cs`: postcheck, exclusividad, integridad y bridge transitorio.
- `20260812101500_N0_6_D2A_OrigenTipadoTypedFirst.cs`: boundary de escritura typed-first.

Backfill certificado:

- `Compra` / `CompraAnulada` → `CompraId`.
- `Venta` / `VentaAnulada` → `VentaId`.
- `ConsumoInsumo` → `ConsumoInsumoId`.

El preflight falla cerrado ante tipos desconocidos, IDs inválidos o documentos inexistentes. Una discrepancia entre FK tipada y snapshot no se corrige silenciosamente desde el string legacy.

La recertificación N0.5.14 endureció además el snapshot temporal de C2 para MySQL administrado con `sql_require_primary_key=ON`, sin cambiar la semántica de N0.6.

## Aplicación y API

- `MovimientoInventarioRepository` usa las FKs tipadas para decisiones y consultas.
- Compra, Venta y ConsumoInsumo escriben mediante origen typed-first.
- `MovimientoInventarioDto` expone `OrigenTipo`, `OrigenId`, `CompraId`, `VentaId` y `ConsumoInsumoId`.
- `MovimientoInventarioService` deriva esos campos desde el origen persistido tipado; `ReferenciaTipo/ReferenciaId` quedan separados como snapshot.
- `GET /inventario/movimientos` devuelve el contrato tipado bajo la autorización existente.
- En finanzas, `MovimientoFinancieroConfiguration` mantiene FKs tipadas como autoridad y snapshots legacy sólo para auditoría/correlación.

## Frontend y seguridad

N0.6.E quedó N/A verificado: no existe consumidor Angular del contrato legacy que requiera modificación para este punto.

N0.6.F quedó N/A verificado para cambios nuevos de RBAC/seguridad/observabilidad: no se añadió una nueva ruta ni una nueva superficie de autorización.

## QA y certificación

Cobertura crítica existente y ejecutada:

- `MovimientoInventarioServiceTests.GetFilteredAsync_Incluye_Imagen_Principal_Y_Origen_Tipado` valida el mapping DTO tipado.
- `MovimientoInventarioOrigenTipadoIntegrationTests.ConsultasDeCompra_UsanCompraId_AunqueSnapshotLegacyNoCoincida` demuestra en MySQL real que `CompraId` prevalece sobre un snapshot conflictivo.
- `MovimientoInventarioOrigenTipadoIntegrationTests.EscrituraTipada_EsAutoridad_YBridgeSoloCubreLegacySinFk` valida typed-first, bridge legacy y rechazo fail-closed de mismatch.

Evidencia final sobre `0e35a9f75c49b6ddfbd5ef21d426521e2b559c40`:

- ERP-N0.6 `31754907625` — SUCCESS.
- Desarrollo build/tests `31754907682` — SUCCESS.
- Recovery MySQL `31754907598` — SUCCESS.
- M11 backup/restore `31754907601` — SUCCESS.
- Fase 8 `31754907626` — SUCCESS.
- Aceptación integral `31754907600` — SUCCESS.
- M13 `31754907614` — SUCCESS.

No quedan P0/P1 conocidos atribuibles a ERP-N0.6.

## Trazabilidad de cierre

- N0.6.A: preflight/documentación de deuda, riesgos y rollback.
- N0.6.B: dominio tipado (`5fe605cc93470a4f4b90f73185016b9e15bc622e`).
- N0.6.C: persistencia/backfill/constraints; C2 `7375a61165b7e9e32feb6054e843937963472e67`, C3 final `01c1116e6db4e839b56176333251e3992fa09d77`.
- N0.6.D1: consultas tipadas `2a2e093f66899b9c02c18026ecd3f270b6a730c1`.
- N0.6.D2A: typed-first `6eadf19a27a0c7c90b0cec54262070f896209738`.
- N0.6.D2B1 Compra `e62b0667f4faace2d8d6520f753547b3e2624a1d`.
- N0.6.D2B2 Venta `bac4d61b34813168b087fd7e9caf740a518c354a`.
- N0.6.D2B3 ConsumoInsumo `8648cc61f29a878d213ff2ddcce4e3731a81ff43`.
- N0.6.D3: DTO/API y autoridad tipada en finanzas reconciliados contra el código actual.
- N0.6.E/F: N/A verificados.
- N0.6.G: QA/regresión/CI cerrado con suites existentes suficientes.
- N0.6.H: documentación y certificación final.

## Riesgo residual y continuidad

Los campos legacy continúan físicamente como snapshots de transición; no representan doble autoridad mientras las decisiones usen las FKs tipadas. Su eliminación física sólo procede tras respaldo, postcheck y confirmación de consumidores en la fase de saneamiento correspondiente, especialmente N0.8.

El siguiente punto del Plan Maestro es **N0.7 — AjusteInventario formal**, comenzando por N0.7.A preflight.

## Cierre

ERP-N0.6 queda formalmente cerrado sobre evidencia reproducible. No se modificó `main`, Producción, secretos, infraestructura productiva, merge/auto-merge del PR #2, force-push ni se crearon ramas nuevas.
