# ADR N3.2 — PedidoVenta como autoridad documental independiente

## Estado

Aceptado para el cierre N3.2; sujeto a la certificación causal del paquete H.

## Contexto

El sistema ya posee una `Venta` legacy y una `Cotizacion`. Reutilizar cualquiera de ellas como PedidoVenta mezclaría responsabilidades. El Plan Maestro N3.2 requiere un documento de pedido propio, originable desde Cotización, mientras N3.3 y puntos posteriores conservan las responsabilidades de reserva, fulfillment y efectos físicos/financieros.

## Decisión

1. `PedidoVenta` es un agregado independiente de `Venta`.
2. Puede originarse desde una `Cotizacion` persistida en estado `Aceptada`.
3. La relación con Cotización es 0..1 desde el Pedido y existe unicidad física sobre `CotizacionId`, evitando pedidos duplicados desde la misma cotización.
4. El Pedido copia snapshots comerciales y se convierte en autoridad de su propio lifecycle `Borrador -> Confirmado -> Anulado`.
5. La creación usa idempotencia durable mediante key + fingerprint SHA-256; Application resuelve replay/conflicto dentro del boundary transaccional.
6. N3.2 no reserva/descuenta stock, no escribe Kardex y no factura/cobra. Esos efectos sólo pueden entrar por puntos posteriores explícitos.
7. Las FKs de referencia son restrictivas; sólo la composición Pedido->Detalles usa cascade.

## Consecuencias

- Cotización y Pedido no compiten como autoridad del mismo documento.
- La historia comercial permanece estable aunque cambien nombres/datos maestros posteriores.
- La API puede reintentar creación sin duplicar PedidoVenta.
- N3.3 puede consumir un Pedido confirmado como entrada explícita para reserva sin retroalimentar responsabilidades dentro de N3.2.

## Alternativas rechazadas

- Renombrar/reutilizar `Venta`: rechazada por mezclar documento histórico de venta/facturación con pedido.
- Mutar inventario al confirmar Pedido: rechazada porque adelanta N3.3 y rompe separación de responsabilidades.
- Depender en lectura de datos maestros sin snapshots: rechazada porque degrada trazabilidad documental histórica.
