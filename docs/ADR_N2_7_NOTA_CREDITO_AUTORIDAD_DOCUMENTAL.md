# ADR N2.7 — Autoridad documental de NotaCreditoProveedor

## Estado

Aceptado para ERP-N2.7.

## Contexto

Solqaryn ya distingue OrdenCompra, RecepcionCompra, FacturaProveedor y DevolucionProveedor. Una nota de crédito de proveedor debe registrar el crédito documental sin convertir ese documento en autoridad de inventario, Kardex o Cuentas por Pagar completas.

## Decisión

`NotaCreditoProveedor` es la autoridad documental del crédito emitido por el proveedor.

El lifecycle canónico es `Borrador → Registrada → Anulada`.

La nota mantiene vínculos relacionales con el proveedor y la factura de proveedor y puede vincularse a una devolución cuando el caso de negocio lo requiere. El límite de crédito acumulado por factura se valida de forma serializada dentro de la transacción para evitar que dos registros concurrentes excedan el máximo permitido.

La API y la UI usan el mismo lifecycle. La autorización se expresa mediante permisos del módulo `Compras` (`Ver`, `Crear`, `Editar`, `Confirmar`, `Anular`).

## Consecuencias

- La nota de crédito no modifica inventario ni Kardex por inferencia.
- N2.8 conserva la responsabilidad de Cuentas por Pagar.
- La anulación es documental y sólo parte de `Registrada`.
- Las consultas y mutaciones fallan cerrado cuando proveedor, factura, devolución, usuario o invariantes no son válidos.
- El rollback técnico de la migración no se interpreta como estrategia universal de recuperación de datos.

## Alternativas descartadas

- Reutilizar `FacturaProveedor` como nota de crédito: mezcla documentos con semánticas/lifecycle distintos.
- Hacer que la nota de crédito sea autoridad de stock: rompe la separación entre documento financiero y movimiento físico.
- Resolver el crédito acumulado sin lock transaccional: permite carreras concurrentes.
