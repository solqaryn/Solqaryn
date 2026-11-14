# ERP-N2.4 — Factura de proveedor — Preflight

## Objetivo
Separar formalmente **OrdenCompra**, **RecepcionCompra** y **FacturaProveedor**, controlando comprado vs recibido vs facturado sin reutilizar `Compra` legacy como autoridad del nuevo flujo ERP.

## Estado real inspeccionado
- N2.3 quedó certificado y documenta explícitamente que la recepción física no debe generar saldos financieros.
- No existe una entidad `FacturaProveedor` localizada en el dominio actual.
- `Compra` legacy mezcla documento de compra, proveedor, estado de pago, método de pago, subtotal/impuesto/total y detalles; por ello no debe convertirse silenciosamente en FacturaProveedor.
- La factura nueva deberá relacionarse con proveedor y con una OrdenCompra; la conciliación estricta Orden→Recepción→Factura pertenece a N2.5 (three-way match), no debe adelantarse aquí.

## Alcance N2.4
1. Dominio y contratos de `FacturaProveedor` + detalle, estados e invariantes.
2. Persistencia/migración y constraints independientes.
3. Application/API para borrador, edición, emisión/registro y anulación según lifecycle definido.
4. Frontend mínimo empresarial con listado, formulario y detalle.
5. QA/RBAC/auditoría/idempotencia/concurrencia aplicables.
6. Documentación, runbook y certificación.

## Fuera de alcance
- No modificar `main` ni Producción.
- No retirar `Compra` legacy dentro de N2.4.
- No contabilizar ni pagar automáticamente la factura si el Plan no lo exige en este punto.
- No implementar N2.5 ni cerrar diferencias de three-way match por adelantado.
- No alterar stock/Kardex al registrar una factura: la autoridad física sigue siendo RecepcionCompra.

## Riesgos e invariantes
- Evitar doble autoridad financiera entre `Compra` legacy y `FacturaProveedor`.
- No permitir que una factura incremente stock.
- Mantener importes/impuestos como snapshot documental auditable.
- Diseñar referencias externas/número de factura con unicidad contextual suficiente para evitar duplicados de proveedor.
- Toda mutación sensible debe quedar bajo RBAC y auditoría estricta.

## Estrategia triple lane
- **N2.4.1 ChatGPT A:** arquitectura/preflight + dominio/contratos iniciales, manteniendo el carril integrador.
- **N2.4.2 Jules A:** revisión/QA de contratos, RBAC, seguridad e idempotencia del mismo N2.4; escritura exclusiva en evidencia QA.
- **N2.4.3 Jules C:** revisión de persistencia, integridad, importes/impuestos, rollback y no-afectación de stock; escritura exclusiva en evidencia QA.

Los tres carriles comparten la misma dependencia lógica N2.3.H cerrada y scopes de escritura no solapados. No se abre N2.5.

## Criterios de aceptación del arranque
- Arquitectura explícita y sin reutilización accidental de `Compra`.
- Entidades/contratos de factura independientes y testeables.
- Carriles Jules limitados a review/QA del mismo parent hasta que exista implementación revisable.
- Checkpoint recuperable con HEAD/base/scope y estado por developer.
