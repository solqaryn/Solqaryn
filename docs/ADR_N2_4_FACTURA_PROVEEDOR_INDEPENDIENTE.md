# ADR-N2.4 — Factura de Proveedor Independiente

## Contexto
El sistema requiere separar la factura del proveedor del documento de recepción y de la orden de compra. Anteriormente, la entidad `Compra` agrupaba estos conceptos, lo que generaba problemas financieros y de inventario.

## Decisión
- **Entidad Independiente:** Se crea `FacturaProveedor` como una entidad separada.
- **Relaciones:** Se asocia directamente a `ProveedorId` y `OrdenCompraId`. Los detalles de la factura se enlazan con `OrdenCompraDetalleId`.
- **Sin Dependencia Directa a Recepción:** No existe una FK directa a `RecepcionCompra`. La conciliación contra recepciones no forma parte del ciclo de vida de N2.4; corresponde al trabajo posterior de three-way match (N2.5).
- **Ciclo de Vida:**
  - `Borrador` -> `Registrada`.
  - `Registrada` -> `Anulada`.
  - **Restricciones:** No se puede pasar de `Borrador` a `Anulada`. La anulación es un proceso documental y **no** revierte el stock, Kardex, costeo ni cantidades, ya que esto lo controla la Recepción de Compra.
- **Sin Eliminación:** No existe operación DELETE, solo transición a estado `Anulada`.

## Consecuencias
- Mejora la trazabilidad y auditoría de documentos financieros.
- Separa el movimiento de inventario del registro documental de la factura de proveedor; N2.4 no modifica Kardex ni stock.
- Requiere controles estrictos de anulación, asegurando que solo facturas registradas puedan anularse y dejando evidencia del motivo.
