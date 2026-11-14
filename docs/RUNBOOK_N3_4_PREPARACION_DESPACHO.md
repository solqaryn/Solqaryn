# Runbook N3.4 — Preparación y despacho

## Propósito

Validar y operar en Desarrollo el flujo `PedidoVenta → ReservaInventario → PreparacionPedidoVenta` sin crear una segunda autoridad de stock ni tocar Producción.

## Precondiciones

- Pedido de venta existente y en estado permitido por el contrato vigente.
- Reserva automática asociada al mismo pedido.
- Detalles físicos coherentes por `ProductoVarianteId + AlmacenId + UbicacionAlmacenId`.
- Usuario autenticado y permisos Ventas adecuados.
- Auditoría/correlation id disponibles.

## Validación funcional

1. Consultar el pedido y comprobar que exista una reserva válida del mismo `PedidoVentaId`.
2. Iniciar la preparación y verificar idempotencia: no debe crearse una segunda preparación para el mismo pedido.
3. Validar el lifecycle en orden: `PendientePicking → PickingCompletado → PackingCompletado → Despachado → Entregado`.
4. Verificar que la cancelación solo sea posible antes del despacho y exija motivo.
5. Confirmar que cada transición persista auditoría estricta con estado anterior/nuevo y usuario autenticado.
6. Confirmar que la preparación no modifica por sí sola la autoridad cuantitativa de stock ni duplica la reserva.
7. Verificar que la UI respete permisos, rutas protegidas, loading/error y el estado actual del lifecycle.
8. Ejecutar regresión backend/frontend y gates causales aplicables al HEAD funcional.

## RBAC HTTP

- `GET /preparaciones-pedido-venta/{id}` → `Ventas/Ver`.
- `GET /preparaciones-pedido-venta/pedido/{pedidoVentaId}` → `Ventas/Ver`.
- `POST /preparaciones-pedido-venta/pedido/{pedidoVentaId}` → `Ventas/Crear`.
- `POST /preparaciones-pedido-venta/{id}/picking` → `Ventas/Editar`.
- `POST /preparaciones-pedido-venta/{id}/packing` → `Ventas/Editar`.
- `POST /preparaciones-pedido-venta/{id}/despachar` → `Ventas/Confirmar`.
- `POST /preparaciones-pedido-venta/{id}/entregar` → `Ventas/Confirmar`.
- `POST /preparaciones-pedido-venta/{id}/cancelar` → `Ventas/Anular`.

## Diagnóstico

Ante inconsistencia, inspeccionar en este orden:

1. `PedidoVenta` y su estado;
2. `ReservaInventario` del pedido;
3. `PreparacionPedidoVenta` y su estado;
4. detalles físicos Variante/Almacén/Ubicación;
5. auditoría y correlation id;
6. frontend/routing/permisos;
7. pruebas y logs causales del HEAD vigente.

No atribuir fallos de MySQL, CI, red o workflows legacy al parent sin evidencia causal concreta.

## Rollback / recuperación

N3.4 usa recuperación forward-only. No ejecutar DDL/DML destructivo ni restauraciones contra Producción.

- Preservar datos y evidencia.
- Bloquear promoción del parent afectado.
- Corregir la causa en Desarrollo.
- Revalidar pruebas dirigidas y gates causales.
- Si existe daño de datos en Desarrollo, usar únicamente mecanismos de backup/restore ya certificados y con alcance explícito.

## Evidencia mínima de cierre

- N3.4.C/D/E/F/G en LISTO con evidencia causal vigente.
- Certificación `docs/CERTIFICACION_N3_4_PREPARACION_DESPACHO.md`.
- `TASKS.md` reconciliado.
- P0/P1 conocidos atribuibles a N3.4 = 0.
- COLA H cerrada únicamente después de validar el paquete documental.