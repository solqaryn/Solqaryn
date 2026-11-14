# N3.11.B — POS — Dominio y contratos — QA takeover

## Dictamen

`N/A_PRODUCT_DELTA / DOMAIN_BOUNDARY_CERTIFIED`.

N3.11.B no justifica crear un segundo agregado POS. La autoridad comercial existente sigue siendo `Venta` y POS debe actuar como una superficie/orquestación de venta rápida sobre contratos existentes, sin duplicar Venta, devoluciones, notas de crédito ni caja.

## CURRENT_CONFIRMED_FACT

- `Venta` ya es el agregado comercial y conserva estado `Borrador`, confirmación/anulación mediante `ConfirmableEntity`, cliente opcional y snapshot de cliente final.
- `Venta` ya concentra importes, descuentos, impuestos, envío, total, costo y utilidad y mantiene detalles y Factura relacionados.
- El preflight N3.11.A certificó resolución existente de producto por código/barcode y estableció que no existe hoy un agregado POS independiente.
- `FacturaPago` ya representa pagos de factura con `Monto`, `MontoRecibido`, `Cambio`, método de pago y referencia; N3.11.B no debe crear un segundo ledger de pagos.
- DevolucionCliente y NotaCreditoCliente son capacidades separadas y no deben duplicarse dentro de POS.

## DOMAIN_BOUNDARY

1. `Venta` permanece como única autoridad transaccional/comercial de la operación vendida.
2. El escaneo/código de barras es un mecanismo de resolución de producto/variante para la capa de aplicación/UI; no crea identidad de dominio POS nueva.
3. POS no introduce un aggregate root, lifecycle, tabla, estado o enumeración adicional en N3.11.B.
4. La semántica existente de cliente opcional / `ClienteNombre = "Cliente final"` es suficiente como frontera de dominio para mostrador; cualquier política comercial adicional queda fuera de este child.
5. Los pagos y el cambio deben reutilizar las autoridades de Facturación/Pago cuando la capa de aplicación defina el flujo; N3.11.B no inventa split-tender ni una nueva contabilidad.
6. Stock/Kardex, facturación, auditoría, devoluciones y notas de crédito conservan sus autoridades existentes y no pueden ser bypassed por POS.

## DECISION_PENDING — NO AUTORIZADO EN ESTE CHILD

- Contrato transaccional exacto para múltiples/combinados y cálculo de cambio cuando exista más de un pago.
- Semántica de suspensión/reanudación de ticket.
- Formato/canal de impresión o reimpresión POS/ESC-POS.
- Sesión/cierre de caja, cajero/terminal o política de caja física.
- Idempotencia específica POS más allá de la que ya exijan las autoridades reutilizadas.
- Nuevos permisos POS o bypass administrativo.

Estas decisiones deberán resolverse únicamente en el child posterior que tenga autoridad de aplicación/API/UX o en la capacidad de Caja correspondiente, y siempre sin alterar la autoridad de `Venta` por inferencia.

## Validación / rollback

- No existe product delta de dominio en este cierre; no se modifican entidades, enums, migraciones, API ni frontend.
- Rollback de N3.11.B es documental: retirar esta decisión si una autoridad posterior aprobada demuestra que el modelo existente es insuficiente.
- Los hijos N3.11.C+ deben tratar esta frontera como fail-closed: no materializar schema POS si el contrato de dominio no lo requiere.

## DoD N3.11.B

- Dependencia N3.11.A satisfecha por `c53bfc04e238799c6d267da4bc7547afd043dfd9`.
- Reutilización y fronteras de dominio establecidas con evidencia directa.
- No se crea segundo agregado comercial ni segundo ledger.
- Decisiones aún no autorizadas aisladas como `DECISION_PENDING`.
- Product delta de dominio: N/A.
- P0 atribuible conocido: 0.
- P1 atribuible conocido: 0.

Selector permitido después del cierre: N3.11.C. N3.11.D+ permanecen `WORK_CAN_PIPELINE__PROMOTION_CANNOT` hasta satisfacer dependencias.
