# ERP-N3.2 — Pedidos de venta

## Estado

**N3.2.H — VALIDANDO.** Este documento describe el estado implementado de N3.2; no sustituye la certificación causal final.

## Alcance materializado

`PedidoVenta` es un documento comercial propio e independiente de `Venta` legacy. Puede originarse desde una `Cotizacion` persistida y `Aceptada`, conserva snapshots de cliente y producto y no introduce por sí mismo reserva/descuento de inventario, Kardex, facturación, cobro, finanzas, despacho ni entrega.

### Dominio

- Lifecycle: `Borrador -> Confirmado -> Anulado`.
- Sólo `Borrador` es editable.
- Confirmación y anulación exigen usuario válido y fecha UTC; la anulación exige motivo.
- Creación desde Cotización exige Cotización persistida, aceptada y documento válido.
- Idempotencia durable: `IdempotencyKey` <= 128 + fingerprint SHA-256 hexadecimal de 64 caracteres, persistidos de forma atómica.
- El pedido conserva snapshots comerciales del cliente y de los detalles.

### Persistencia

Migración canónica: `20260824080000_N3_2_PedidoVentaPersistencia`.

- Tablas: `PedidosVenta`, `PedidoVentaDetalles`.
- Cardinalidad 0..1 PedidoVenta por `CotizacionId` mediante índice único.
- `IdempotencyKey` única y fingerprint con check SHA-256.
- FKs `Restrict` a Cotización, Cliente, Producto y ProductoVariante.
- Cascade únicamente `PedidoVenta -> PedidoVentaDetalles`.
- Cantidad y precio unitario `decimal(18,4)` con checks fail-closed.
- `Down()` contiene guard que impide eliminar las tablas mientras existan pedidos o detalles persistidos.

### Application/API

API canónica: `/pedidos-venta` bajo `[Authorize]`.

- `GET /pedidos-venta` — `Ventas:Ver`.
- `GET /pedidos-venta/{id}` — `Ventas:Ver`.
- `POST /pedidos-venta` — `Ventas:Crear`, requiere `Idempotency-Key`.
- `PUT /pedidos-venta/{id}` — `Ventas:Editar`.
- `POST /pedidos-venta/{id}/confirmar` — `Ventas:Confirmar`.
- `POST /pedidos-venta/{id}/anular` — `Ventas:Anular`.

La aplicación mantiene transacciones, locking en mutaciones sensibles, resolución fail-closed de idempotencia y auditoría conforme al patrón empresarial vigente.

### Frontend/UX

Feature Angular canónico `pedidos-venta`:

- listado, filtros y paginación;
- creación/edición;
- detalle;
- confirmación/anulación según lifecycle y RBAC;
- rutas protegidas por `authGuard` + `permisoGuard`;
- navegación de Ventas a `/pedidos-venta`;
- cobertura E2E de acceso/RBAC.

## Fronteras de responsabilidad

N3.2 no materializa reserva automática de stock. Esa responsabilidad pertenece al siguiente punto rector dependency-valid (`N3.3`). Tampoco crea movimientos de Kardex, facturas, cobros o movimientos financieros.

## Evidencia de certificación

Baseline funcional acumulado N3.2.A-G: `58a6550094a043556367b79c89e4ac963bd34a4a`, con Development `#32731508498`, Acceptance `#32731508548`, Fase 8 `#32731508654`, M13 `#32731508646` y Recovery MySQL `#32731508448` en `SUCCESS`.

HEAD de control/cierre antes de este paquete: `a923c715b0d3490b3e4c8d0dedafbf3da8df9ada`, con Development `#32739427111`, Acceptance `#32739427514`, Fase 8 `#32739427393`, M13 `#32739427454` y Recovery MySQL `#32739427189` en `SUCCESS`.

## Criterio de cierre H

N3.2.H sólo pasa a `LISTO` después de publicar este paquete canónico, certificar su HEAD causal y reconciliar `TASKS.md`, `CHANGELOG_AI.md`, COLA/CONFIG/BITACORA con P0=0 y P1=0.
