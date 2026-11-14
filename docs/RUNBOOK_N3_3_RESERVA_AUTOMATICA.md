# Runbook N3.3 — Reserva automática desde PedidoVenta

## Propósito

Validar y operar en Desarrollo el vínculo `PedidoVenta` → `ReservaInventario` sin crear una segunda autoridad de stock ni tocar Producción.

## Precondiciones

- Pedido persistido y en estado permitido por el contrato vigente.
- Asignaciones físicas explícitas por `ProductoVarianteId + AlmacenId + UbicacionAlmacenId`.
- Cantidades asignadas exactamente consistentes con las cantidades requeridas.
- Permisos/RBAC y auditoría vigentes.

## Validación funcional

1. Confirmar que el pedido no tenga una reserva activa inconsistente.
2. Validar disponibilidad usando la autoridad `ExistenciaVariante`.
3. Ejecutar la confirmación dentro de la unidad transaccional vigente.
4. Verificar que exista una única `ReservaInventario` asociada al pedido.
5. Verificar que `StockReservado` refleje el compromiso y que `StockFisico` no cambie por reservar.
6. Repetir la operación/replay sólo bajo la semántica idempotente vigente; nunca duplicar la reserva o el reservado.
7. Confirmar auditoría/correlación sin exponer secretos ni payloads sensibles.

## Diagnóstico

Ante inconsistencia, inspeccionar en este orden:

1. estado/lifecycle de `PedidoVenta`;
2. relación `PedidoVentaId` de la reserva;
3. estado de `ReservaInventario`;
4. detalles y clave física Variante/Almacén/Ubicación;
5. `StockFisico`, `StockReservado` y disponibilidad derivada;
6. auditoría/correlation id;
7. pruebas y logs causales del HEAD vigente.

No atribuir fallos de MySQL, CI o red al parent sin evidencia causal concreta.

## Rollback / recuperación

N3.3 usa recuperación forward-only. No ejecutar DDL/DML destructivo ni restauraciones contra Producción.

- Preservar datos/evidencia.
- Bloquear promoción del parent afectado.
- Corregir la causa en Desarrollo.
- Revalidar pruebas dirigidas y gates causales.
- Si existe daño de datos en Desarrollo, usar únicamente mecanismos de backup/restore ya certificados y con alcance explícito; no improvisar borrados.

## Evidencia mínima de cierre

- N3.3.D/E/F/G en LISTO.
- Certificación `docs/CERTIFICACION_N3_3_RESERVA_AUTOMATICA.md`.
- `TASKS.md` y `CHANGELOG_AI.md` reconciliados.
- P0/P1 conocidos = 0.
- Issue de control H cerrado únicamente después de validar el commit atómico.
