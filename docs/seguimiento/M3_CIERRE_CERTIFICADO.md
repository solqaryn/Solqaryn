# M3 — Checkpoint final de cierre certificado

Fecha: 2026-08-09  
Rama: `Desarrollo`  
PR oficial: `#2 Desarrollo -> main` — debe permanecer abierto y Draft.  
Producción: sin cambios.

## Base funcional certificada

HEAD funcional verificado: `9ea747acd110914d6445f687caabf4cf42a1fefe`.

Evidencia GitHub Actions:

- `31330348378` — Desarrollo - Compilación y pruebas — success.
- `31330348374` — Desarrollo - aceptación funcional integral — success.
- `31330348396` — Fase 2 - Auditoría de configuración y dependencias — success.
- `31330348421` — Bloque 2C.1 - Variante técnica y migración — success.
- `31330348369` — Fase 8 - Validación completa automatizada — success.
- `31330348386` — Solqaryn CI — skipped; no contabilizado como verde.

## Correcciones M3 certificadas

- seed fiscal idempotente incluso ante registros eliminados lógicamente;
- decisiones administrativas preservadas después de reinicio;
- `Codigo` fiscal inmutable después de creación;
- UI bloquea edición del código estable;
- `VentaImpuesto.IncluidoEnPrecioSnapshot` se persiste correctamente;
- DTO histórico devuelve el snapshot en lugar del valor actual del maestro;
- Compra ya conservaba correctamente el mismo snapshot;
- Factura consume snapshots de Venta y no recalcula con la tasa vigente;
- pruebas unitarias y MySQL 8.4 añadidas para estas invariantes;
- sin migración adicional de esquema ni cambios en Producción.

## Cierre documental

`docs/FASE_M3_CONFIGURACION_FISCAL_ISV_ISC.md` quedó marcado como **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE** y el Plan Maestro registra M3 como completada.

El commit documental previo `4f45f949c261b2d5ba5d5d64123418d29c90f9f7` no modifica lógica funcional ni esquema. Este checkpoint produce un HEAD normal de `Desarrollo` para ejecutar nuevamente todos los gates oficiales sobre el estado definitivo.

## Siguiente fase

M4 — Estado persistente de filtros y navegación. No se inicia sin autorización posterior al cierre de M3.
