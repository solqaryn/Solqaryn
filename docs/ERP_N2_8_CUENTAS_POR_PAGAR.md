# ERP-N2.8 — Cuentas por pagar

## Estado y propósito

ERP-N2.8 materializa la obligación financiera derivada de una `FacturaProveedor` registrada. La cuenta por pagar es un subledger financiero: no recibe mercancía, no modifica stock/Kardex y no sustituye a FacturaProveedor como documento comercial.

## Autoridad funcional

- Origen: `FacturaProveedor` en estado `Registrada`.
- Obligación: `CuentaPorPagar`, vinculada a `FacturaProveedor` y `Proveedor`.
- Aplicaciones: `AplicacionCuentaPorPagar`, asociada a una cuenta por pagar.
- Idempotencia de alta: clave lógica `cxp.factura:{facturaProveedorId}:registrar` dentro de transacción.
- Idempotencia de aplicaciones: `ReferenciaIdempotencia` única por cuenta.
- Estados, condición de pago y tipos de aplicación se validan mediante enums de dominio; valores fuera de contrato fallan cerrado.
- Reversiones se representan explícitamente y deben conservar coherencia entre marca de reversión, fecha y motivo.

## Persistencia

La migración canónica es `20260822161500_N28_CuentasPorPagar`.

Crea `CuentasPorPagar` y `AplicacionesCuentaPorPagar`, con FKs restrictivas hacia `FacturasProveedor`/`Proveedores`, cascade únicamente cuenta→aplicaciones, checks de importes/estados/tipos y unicidad de idempotencia. El `Down()` es destructivo por definición y se bloquea si cualquiera de las tablas contiene filas; el rollback operativo exige exportación/backup, quiescence y reconciliación antes de ejecutar la reversión.

## Application/API

`CuentaPorPagarService` concentra registrar desde factura, aplicar movimiento, anular el último pago y recalcular vencimiento dentro de transacciones cuando hay mutación.

`CuentasPorPagarController` exige autenticación y RBAC relacional:

- `GET /api/cuentasporpagar/abiertas` — `Finanzas/Ver`.
- `GET /api/cuentasporpagar/proveedor/{proveedorId}` — `Finanzas/Ver`.
- `GET /api/cuentasporpagar/{id}` — `Finanzas/Ver`.
- `POST /api/cuentasporpagar/registrar-factura/{facturaProveedorId}` — `Finanzas/Crear`.
- `POST /api/cuentasporpagar/{id}/aplicar` — `Finanzas/Editar`.
- `POST /api/cuentasporpagar/{id}/anular-ultimo-pago` — `Finanzas/Editar`.

La aplicación de movimientos exige `ReferenciaIdempotencia` no vacía y conserva el contrato de tipo, monto, fecha, referencia/comentario y confirmación administrativa.

## Frontend y UX

La superficie Angular de Cuentas por Pagar consume exclusivamente los contratos API certificados, protege rutas/acciones mediante permisos runtime y mantiene estados loading/error/vacío. La UI no inventa efectos de inventario ni contabilidad automática fuera del alcance N2.8.

## Seguridad, auditoría y QA

N2.8.G congeló por regresión el contrato RBAC `Finanzas/Ver`, `Finanzas/Crear`, `Finanzas/Editar` y `Finanzas/Anular` donde corresponda, y verifica que el controller permanezca bajo `[Authorize]` sin bypass anónimo. Los gates causales Development, Acceptance, Fase8 y M13 del baseline de cierre previo a este paquete documental terminaron SUCCESS y no se conocen P0/P1 atribuibles a N2.8.

## Riesgos residuales

- Rollback con datos no es una operación rutinaria: exige backup/export y reconciliación.
- N2.8 no introduce automáticamente asiento contable, conciliación bancaria ni tesorería; esos efectos pertenecen a módulos posteriores.
- Cualquier ampliación de lifecycle, moneda/FX o integración contable requiere contrato explícito posterior.

## Fuentes de cierre

- `docs/ADR_N2_8_CUENTAS_POR_PAGAR_AUTORIDAD_FINANCIERA.md`
- `docs/OPENAPI_N2_8_CUENTAS_POR_PAGAR.md`
- `docs/RUNBOOK_N2_8_CUENTAS_POR_PAGAR.md`
- `docs/ROLLBACK_N2_8_CUENTAS_POR_PAGAR.md`
- `docs/CERTIFICACION_N2_8_CUENTAS_POR_PAGAR.md`
