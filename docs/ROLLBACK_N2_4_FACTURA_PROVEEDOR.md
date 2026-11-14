# PLAN DE ROLLBACK — N2.4 Factura de Proveedor

## Riesgo Identificado
La migración `20260820082500_N2_4_FacturaProveedorPersistencia` crea las tablas `FacturasProveedor` y `FacturaProveedorDetalles`.
El método `Down()` ejecuta `DropTable` sobre ambas tablas.
**Riesgo crítico:** no existe un `DownGuard`. Si las tablas contienen datos, el downgrade destruye los datos N2.4 almacenados allí; la recuperación depende de un backup/export previamente verificado.

## Target técnico de reversión
El predecesor inmediato de la migración N2.4 es `20260819143000_N2_3_RecepcionCompraOrigenKardex`.

## Protocolo de reversión controlada
1. **Quiescencia:** detener escrituras relacionadas con compras/facturas antes de cualquier downgrade.
2. **Backup/export verificable:** respaldar la base y, si existen filas N2.4, exportar `FacturasProveedor` y `FacturaProveedorDetalles`; validar que el respaldo sea utilizable.
3. **Fail-closed:** abortar el rollback si no puede verificarse el respaldo/export o si no se cumplen las precondiciones operativas.
4. **Ejecución técnica, solo en entorno autorizado:**
   ```bash
   dotnet ef database update 20260819143000_N2_3_RecepcionCompraOrigenKardex
   ```
5. **Postcheck:** comprobar que las tablas N2.4 fueron retiradas y que el baseline N2.3 permanece íntegro.
6. **Restore criteria:** si el downgrade o el postcheck no cumplen la expectativa, restaurar desde el respaldo verificado según el procedimiento operativo autorizado.

> Este documento describe el procedimiento y sus riesgos. VAEP no ejecuta rollback en Producción dentro de este flujo.
