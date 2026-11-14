# Certificación N3.3 — Reserva automática de inventario

## Dictamen

ERP-N3.3 queda funcionalmente completado con N3.3.A–G certificados y N3.3.H dedicado exclusivamente al cierre documental y de evidencia. Esta certificación no reabre ni modifica código funcional.

## Alcance certificado

La reserva automática reutiliza `ReservaInventario` como autoridad de compromiso de stock; no crea un segundo agregado de reservas. La confirmación de `PedidoVenta` exige asignaciones físicas explícitas, valida disponibilidad bajo la autoridad de inventario, crea/activa una única reserva asociada al pedido y mantiene `StockReservado` sincronizado sin modificar `StockFisico` por el mero acto de reservar.

No existe selección automática inventada de almacén/ubicación: las asignaciones físicas deben estar determinadas por el contrato vigente antes de confirmar.

## Evidencia funcional

- Baseline funcional de cierre: `960ac07ed1e96d1d2e98a51fdb5dc216fbc8d0f3`.
- N3.3.D Application/API: LISTO por QA takeover y pruebas dirigidas de reserve-on-confirm.
- N3.3.E Frontend/UX: LISTO.
- N3.3.F RBAC/auditoría/seguridad/observabilidad: LISTO.
- N3.3.G QA/regresión/CI: LISTO; la cobertura `reservation-automatic-flow.spec.ts` fue aceptada por el control VAEP.
- P0/P1 bloqueantes conocidos atribuibles a N3.3: 0.

Los fallos de workflows legacy ERP-N0 observados en paralelo no son gates causales de N3.3 sin evidencia directa.

## Autoridad arquitectónica reutilizada

N3.3 no duplica el ADR de reservas. Continúa vigente `docs/ADR_N1_8_RESERVAS_STOCK_RESERVADO_Y_OVERSELLING.md`: `ExistenciaVariante` es la única autoridad cuantitativa, la identidad física completa es Variante + Almacén + Ubicación, las mutaciones usan lock/transacción y reservar no mueve `StockFisico`.

## Rollback y operación

El rollback de N3.3 es forward-only a nivel de aplicación. No se autoriza borrar reservas, pedidos, stock reservado ni históricos para "deshacer" la funcionalidad. Ante una regresión:

1. detener nuevas confirmaciones del flujo afectado en Desarrollo;
2. preservar evidencia y datos existentes;
3. diagnosticar la relación `PedidoVenta` ↔ `ReservaInventario` y los saldos físicos/reservados;
4. aplicar corrección forward;
5. ejecutar regresión dirigida y gates causales aplicables antes de reabrir promoción.

Producción queda fuera de alcance de este runbook.

## DoD de cierre

- N3.3.A–G: LISTO en COLA.
- Documentación/certificación H publicada en Desarrollo.
- `TASKS.md` y `CHANGELOG_AI.md` reconciliados preservando historial.
- Issue de control N3.3.H reconciliado.
- P0=0 y P1=0 conocidos.
- Siguiente parent permitido por dependencias: N3.4.A, sin promover N3.4.B antes de cerrar A.

**Dictamen final:** `N3.3.H = LISTO` una vez publicado este paquete atómico y verificada la preservación documental. ERP-N3.3 queda formalmente cerrado.
