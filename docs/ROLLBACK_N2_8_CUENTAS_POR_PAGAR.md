# Rollback y data-safety — ERP-N2.8 Cuentas por Pagar

## Principio

La reversión de esquema de N2.8 puede destruir datos. No se considera una operación rutinaria ni se autoriza sin evidencia previa de backup/export y reconciliación.

## Migración

Target canónico: `20260822161500_N28_CuentasPorPagar`.

El `Down()` elimina primero `AplicacionesCuentaPorPagar` y luego `CuentasPorPagar`, pero falla cerrado si detecta filas en cualquiera de las dos tablas. Esta protección evita que una reversión normal borre obligaciones o movimientos existentes silenciosamente.

## Procedimiento antes de rollback

1. Declarar ventana de mantenimiento y detener escrituras de CxP (quiescence).
2. Verificar backup recuperable y registrar su identificador/evidencia sin exponer secretos.
3. Exportar/reconciliar `CuentasPorPagar` y `AplicacionesCuentaPorPagar`, incluyendo IDs, factura/proveedor, saldos, referencias idempotentes y estado de reversión.
4. Verificar que el target de migración sea exactamente el esperado y que no existan migraciones posteriores dependientes que quedarían inconsistentes.
5. Si las tablas contienen datos, **no ejecutar `Down()`**. Restaurar desde backup en entorno controlado o ejecutar una corrección forward diseñada y revisada.
6. Solo si las tablas están vacías y las dependencias lo permiten, ejecutar rollback y comprobar postcondición de ausencia de ambas tablas.

## Recuperación

Ante fallo durante una actualización, preferir corrección forward. Si se requiere restauración, restaurar el conjunto coherente de base de datos y aplicación correspondiente; no restaurar únicamente dos tablas sin evaluar FKs, migraciones y consumidores.

## Riesgo explícito

Eliminar las tablas implica pérdida de historia financiera de N2.8. El guard de `Down()` reduce el riesgo operativo, pero no convierte el rollback destructivo en una operación sin pérdida de datos.
