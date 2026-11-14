# N5.5.C — Reportes de compras · persistencia, migración y datos

Autoridad operativa: `docs/VAEP_AUTHORITY.md`.

## Decisión

`N5.5.C = N/A_PERSISTENCE_CHANGE`: el contrato aceptado en N5.5.B puede resolverse con autoridades ERP ya persistidas. No se crea tabla de reporting, snapshot redundante, backfill ni migración artificial.

## Evidencia de persistencia existente

- `OrdenCompra` persiste proveedor, estado, moneda, fecha esperada y snapshots; `OrdenCompraDetalle` persiste cantidad y precio ordenado.
- `RecepcionCompra` persiste orden, estado y fecha de recepción; `RecepcionCompraDetalle` persiste cantidades físicas/aceptadas/dañadas/faltantes/sobrantes y `CostoUnitarioSnapshot`.
- `FacturaProveedor` persiste proveedor, orden, moneda, estado y fechas; `FacturaProveedorDetalle` persiste `OrdenCompraDetalleId`, cantidad facturada y `PrecioUnitarioSnapshot` con FK restrictiva e índice/único por factura+línea.
- `DevolucionProveedor` persiste proveedor, orden, recepción, factura y lifecycle; sus FKs/índices conservan linaje explícito. Los detalles preservan línea de recepción/orden y snapshots monetarios.
- `EvaluacionProveedor` persiste proveedor, orden, recepción, fechas esperada/real y cantidades observables; posee índices por recepción, orden y proveedor+fecha de recepción.

## Suficiencia frente al contrato B

1. Proveedor: resoluble desde IDs/snapshots existentes.
2. Precio ordenado/facturado: resoluble por línea de orden y línea de factura enlazada mediante `OrdenCompraDetalleId`.
3. Variación absoluta autorizada: derivable en lectura; no requiere columna persistida.
4. Recepción/cumplimiento factual: derivable desde recepciones/evaluaciones persistidas.
5. Devoluciones reales: derivables desde `DevolucionProveedor`/detalles, sin reconstrucción heurística.
6. Ausencia de dato: se representa como `null`/cero según el contrato B; no requiere sentinelas persistidos.
7. Moneda: ya está persistida en orden/factura/devolución; no se autoriza FX ni tabla adicional.

## Índices y rendimiento

Los índices existentes cubren los joins primarios y filtros fundamentales: proveedor/estado/fecha esperada en órdenes, orden+estado y fecha en recepciones, línea de orden en factura, proveedor/estado y FKs documentales en devoluciones, y proveedor+fecha en evaluaciones. No existe evidencia de un cuello de botella que justifique una migración preventiva. N5.5.D debe usar proyección server-side, filtros acotados y paginación; cualquier índice adicional sólo se materializa ante evidencia de consulta real/gate de rendimiento, nunca como busywork.

## Preflight/migración/backfill/rollback

- Migración EF: **NO APLICA**.
- SQL forward/backfill: **NO APLICA**.
- Backup lógico por cambio de esquema/datos: **NO APLICA**.
- Producción: **NO TOCADA**.
- Rollback: revertir únicamente este documento/evidencia en `Desarrollo`; no hay datos ni esquema que revertir.

## Gates para N5.5.D

N5.5.D puede implementar una consulta read-only sobre el esquema existente. Debe demostrar mediante pruebas que los joins son explícitos, las comparaciones de precio respetan moneda/linaje, las devoluciones anuladas no contaminan totales efectivos, los filtros fallan cerrados y no se materializa N+1 ni mutación de entidades.

## Resultado

La persistencia actual es suficiente para el contrato de N5.5.B. Crear una nueva tabla/migración ahora sería redundante y violaría `no busywork`.
