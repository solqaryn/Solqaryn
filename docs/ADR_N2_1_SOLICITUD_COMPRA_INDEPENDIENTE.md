# ADR N2.1 — SolicitudCompra como documento independiente

## Estado

Aceptado para ERP-N2.1.

## Contexto

`Compra` es un documento transaccional cuyo ciclo de confirmación materializa efectos de inventario/Kardex/finanzas. ERP-N2.1 necesita representar una necesidad de compra que pueda ser enviada, aprobada o rechazada antes de existir una orden/compra ejecutable.

Reutilizar `Compra` o `EstadoDocumento` introduciría acoplamiento semántico y riesgo de disparar efectos económicos prematuros.

## Decisión

Crear `SolicitudCompra` como agregado independiente, derivado de `AuditableEntity`, con enum propio `EstadoSolicitudCompra` y lifecycle:

`Borrador -> Solicitada -> Aprobada/Rechazada`.

La solicitud no modifica stock, Kardex, costeo ni finanzas. La aprobación de la solicitud tampoco crea implícitamente una `Compra`; la materialización posterior pertenece a ERP-N2.2 y siguientes.

Las transiciones de lifecycle se serializan mediante unidad transaccional y lock pesimista cuando operan sobre una solicitud persistida.

## Consecuencias

Positivas:

- separación clara entre intención, aprobación y ejecución de compra;
- auditabilidad del proceso previo;
- evita efectos laterales antes de la orden/recepción;
- permite RBAC específico sobre enviar/aprobar/rechazar;
- facilita evolución hacia OrdenCompra sin romper Compra existente.

Costos:

- nuevo agregado, persistencia, API y UI;
- mapeo explícito futuro SolicitudCompra -> OrdenCompra;
- estados y reglas no deben duplicarse accidentalmente en otros documentos.

## Alternativas rechazadas

1. Reutilizar `Compra` en estado Borrador/Solicitada: rechazado porque Compra ya concentra semántica transaccional y confirmación con efectos materiales.
2. Extender `EstadoDocumento`: rechazado porque convertiría un enum compartido en autoridad de procesos heterogéneos.
3. Solicitud sin persistencia propia: rechazado por pérdida de auditoría, concurrencia y trazabilidad.

## Verificación

`N21SolicitudCompraContractTests` protege la independencia de `EstadoSolicitudCompra`, la ausencia de herencia `ConfirmableEntity` y la ausencia de campos de stock/Kardex/costeo en el agregado.

Baseline certificado: `a1a6f699cbad0186d0e0d7d7ac7f366c51009f7c`; CI `32172981351` SUCCESS.
