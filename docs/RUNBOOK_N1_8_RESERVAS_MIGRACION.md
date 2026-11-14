# ERP-N1.8.C — Runbook de migración de Reservas de Inventario

## Objetivo

Materializar `ReservasInventario` y `ReservaInventarioDetalles` sin alterar datos productivos ni inventar backfill. N1.8 introduce un documento nuevo: no existe tabla legacy equivalente que deba convertirse.

## Autoridad física

La reserva identifica cada línea por `ProductoVarianteId + AlmacenId + UbicacionAlmacenId`. `ExistenciaVariante` continúa siendo la autoridad de `StockFisico`, `StockReservado` y `StockDisponible`; la persistencia del documento no modifica por sí sola existencias. La mutación transaccional de stock reservado pertenece a N1.8.D.

## Preflight

1. Ejecutar `backend/scripts/preflight-erp-n1-8-reservas.sql` contra el ambiente controlado de Desarrollo.
2. Confirmar que `ReservasInventario` y `ReservaInventarioDetalles` todavía no existen.
3. Confirmar existencia de `Ventas`, `ProductoVariantes`, `Almacenes`, `UbicacionesAlmacen` y `ExistenciasVariante`.
4. Confirmar `AK_UbicacionesAlmacen_AlmacenId_Id`.
5. Exigir cero `ExistenciaVariante` con `StockFisico < 0`, `StockReservado < 0` o `StockReservado > StockFisico`.

Si cualquiera de esos controles falla, no aplicar la migración.

## Backup lógico documentado

Como N1.8.C crea tablas nuevas y no transforma tablas existentes, el backup lógico mínimo consiste en registrar antes de migrar:

- HEAD exacto de `Desarrollo`;
- contenido de `__EFMigrationsHistory`;
- salida completa del preflight;
- conteo de filas e invariantes de `ExistenciasVariante`.

No se requiere copiar datos de reservas porque no existe fuente legacy que convertir.

## Aplicación

Aplicar únicamente la migración:

`20260817064000_N1_8_ReservaInventarioPersistencia`

La migración es fail-closed: valida ausencia de tablas objetivo y dependencias estructurales antes de ejecutar DDL. Las FKs hacia venta, variante, almacén y ubicación usan `Restrict`; únicamente los detalles dependen de su cabecera con `Cascade`.

## Postcheck

Ejecutar `backend/scripts/postcheck-erp-n1-8-reservas.sql` y exigir:

- ambas tablas presentes;
- `UX_ReservasInventario_Numero` único;
- `UX_ReservaDetalles_ClaveFisica` único;
- FK compuesta `FK_ReservaDetalles_Ubicacion_MismoAlmacen` presente;
- cero reservas no borrador sin detalles;
- cero cantidades reservadas/consumidas inválidas;
- cero ubicaciones pertenecientes a otro almacén.

## Rollback

Antes de N1.8.D, si la migración debe revertirse en Desarrollo y no existen reservas válidas que conservar:

1. exportar las tablas N1.8 si contienen alguna fila inesperada;
2. ejecutar el `Down` de la migración, que elimina primero `ReservaInventarioDetalles` y luego `ReservasInventario`;
3. verificar que `Ventas`, `ProductoVariantes`, `Almacenes`, `UbicacionesAlmacen` y `ExistenciasVariante` permanecen intactas;
4. reconciliar `__EFMigrationsHistory` y repetir el preflight.

Después de que N1.8.D empiece a crear reservas reales, no ejecutar rollback destructivo automático. La reversión debe preservar primero los documentos y reconciliar cualquier `StockReservado` materializado.

## Prohibiciones

- No ejecutar este runbook en Producción desde VAEP.
- No alterar `main`.
- No inventar almacén o ubicación para datos inexistentes.
- No modificar `ExistenciaVariante` durante N1.8.C.
