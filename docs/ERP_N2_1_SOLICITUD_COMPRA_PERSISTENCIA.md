# ERP-N2.1.C — Solicitud de compra — Persistencia, migración y datos

Fecha: 2026-08-18.

## Alcance

Esta etapa materializa en MySQL el modelo documental `SolicitudCompra` / `SolicitudCompraDetalle` aprobado en N2.1.B. Es una capacidad aditiva: no convierte compras históricas, no crea backfill y no toca stock, Kardex, costeo ni movimientos financieros.

## Artefacto canónico

Migración forward-only:

- `backend/src/Infrastructure/Persistence/Migrations/20260818144500_N2_1_SolicitudCompraPersistencia.cs`

El repositorio no mantiene `AppDbContextModelSnapshot` como autoridad manual para estas migraciones recientes; la autoridad operativa es la historia de migraciones versionada y la generación/aplicación de SQL que ejecuta CI sobre MySQL.

## Preflight fail-closed

La migración aborta si:

1. `SolicitudesCompra` o `SolicitudCompraDetalles` ya existen, evitando colisión o doble autoridad.
2. Falta cualquiera de las autoridades relacionales requeridas: `Proveedores`, `Productos` o `ProductoVariantes`.

No se lee ni transforma `Compras` porque N2.1 no debe inferir solicitudes históricas inexistentes.

## Esquema

### SolicitudesCompra

- número único `NumeroSolicitud`;
- estado dedicado persistido como entero (`Borrador=1`, `Solicitada=2`, `Aprobada=3`, `Rechazada=4`);
- proveedor opcional con FK `RESTRICT`;
- fechas/usuarios/snapshots de solicitud y decisión;
- rechazo con motivo;
- auditoría técnica estándar;
- índices por número, estado/fecha y proveedor.

Los CHECK físicos exigen estado válido, evidencia de envío cuando el estado deja Borrador y consistencia de decisión. En Rechazada el motivo debe ser no vacío; en Aprobada no puede quedar motivo de rechazo.

### SolicitudCompraDetalles

- FK a cabecera con `CASCADE`;
- FK a Producto y ProductoVariante con `RESTRICT`;
- cantidad `decimal(18,4)` estrictamente positiva;
- costo estimado `decimal(18,4)` nullable y no negativo;
- snapshots comerciales con longitudes acotadas;
- índices por solicitud y producto/variante.

Dos triggers fail-closed verifican en INSERT/UPDATE que una variante opcional pertenezca realmente al producto indicado y no esté eliminada. Esto evita combinaciones físicamente incoherentes sin crear una nueva autoridad de producto.

## Postcheck

La propia migración valida antes de finalizar:

- presencia de las dos tablas;
- presencia de los dos triggers;
- presencia de cinco CHECK de integridad;
- presencia de cuatro FKs esperadas;
- presencia de cinco índices explícitos;
- cero filas creadas por la migración.

Cualquier divergencia hace fallar la migración.

## Backfill y reconciliación

Backfill: **no aplica**. El preflight de N2.1 estableció que no existía `SolicitudCompra` legacy y que las `Compras` actuales representan una autoridad transaccional distinta.

Reconciliación esperada tras upgrade:

- 0 solicitudes inventadas desde compras históricas;
- 0 detalles inventados;
- 0 mutaciones sobre `Compras`, `MovimientosInventario`, `ExistenciasVariante`, lotes, series, capas de costo o finanzas;
- FKs e índices íntegros según postcheck.

## Rollback

`Down` es fail-closed: sólo elimina triggers y tablas cuando ambas tablas están vacías. Si ya existe cualquier solicitud o detalle, el rollback aborta para preservar evidencia documental y obliga a una corrección forward.

## Criterio de cierre

N2.1.C puede marcarse `LISTO` cuando el commit que contiene esta migración complete exitosamente los gates causales de backend/MySQL/migraciones y los gates agregados exigidos por el runner. Hasta entonces permanece `VALIDANDO`.
