# Certificación N3.4 — Preparación y despacho

## Dictamen

ERP-N3.4 queda funcionalmente completado con N3.4.A–G certificados y N3.4.H dedicado exclusivamente al cierre documental y de evidencia. Esta certificación no reabre ni modifica código funcional.

## Alcance certificado

El flujo de preparación logística se apoya en `PedidoVenta` y en la reserva de inventario previamente creada. La preparación conserva identidad física por variante, almacén y ubicación; no crea una segunda autoridad cuantitativa de stock.

Lifecycle certificado: `PendientePicking → PickingCompletado → PackingCompletado → Despachado → Entregado`, con cancelación controlada antes del despacho. Las transiciones se ejecutan bajo servicio transaccional y auditoría estricta.

La API `/preparaciones-pedido-venta` exige autenticación y permisos relacionales del módulo Ventas: `Ver`, `Crear`, `Editar`, `Confirmar` y `Anular` según operación. El frontend ofrece consulta, inicio, picking, packing, despacho, entrega y cancelación, con rutas protegidas y estados de loading/error.

## Evidencia funcional

- Baseline funcional certificado: `a167434880eab07c3b08ca651ae9309da964c23b`.
- N3.4.C persistencia/migración: LISTO sobre `cb476879203ffb3da40fb7a670c74935c794081d` con M13 `#32803906340` SUCCESS.
- N3.4.D Application/API: LISTO sobre `1fab396541d8ecf33e605703789809ebc1a997ef` con M13 `#32807131468` SUCCESS.
- N3.4.E Frontend/UX: LISTO sobre `a167434880eab07c3b08ca651ae9309da964c23b` con M13 `#32809392404` SUCCESS.
- N3.4.F RBAC/auditoría/seguridad/observabilidad: LISTO por revisión dirigida sobre el mismo HEAD funcional.
- N3.4.G QA/regresión/CI: LISTO; M13 `#32809392404` cubrió backend, MySQL/migraciones/snapshot/upgrade, seguridad HTTP/autorización, Angular, Playwright integral, secretos/dependencias y backup.
- P0/P1 bloqueantes conocidos atribuibles a N3.4: 0.

Los fallos de workflows legacy ERP-N0 observados en paralelo no son gates causales de N3.4 sin evidencia directa.

## Autoridad y límites

- `PedidoVenta` permanece como autoridad comercial del pedido.
- `ReservaInventario` y `ExistenciaVariante` conservan la autoridad de compromiso y cantidad física.
- `PreparacionPedidoVenta` representa el proceso logístico de preparación y entrega; no debe duplicar stock, facturación, cobro ni contabilidad.
- La preparación exige una reserva automática previa del mismo pedido.

## Rollback y recuperación

La recuperación operativa de N3.4 es forward-only. No se autoriza borrar preparaciones, reservas, pedidos, movimientos de inventario ni históricos para deshacer la funcionalidad.

Ante una regresión:

1. detener la promoción del parent afectado en Desarrollo;
2. preservar evidencia y datos existentes;
3. inspeccionar pedido, reserva, preparación, detalles físicos y auditoría/correlation id;
4. corregir la causa forward-only;
5. ejecutar pruebas dirigidas y gates causales aplicables antes de reabrir promoción.

Producción queda fuera de alcance de este runbook.

## DoD de cierre

- N3.4.A–G: LISTO en COLA.
- Documentación/certificación H publicada en Desarrollo.
- `TASKS.md` reconciliado preservando historial.
- P0=0 y P1=0 conocidos atribuibles al parent.
- Siguiente parent permitido por dependencias: `N3.5.A — Venta/factura — Auditoría y preflight`.

**Dictamen final:** `N3.4.H = LISTO` una vez publicado este paquete documental atómico y verificada la preservación documental. ERP-N3.4 queda formalmente cerrado.