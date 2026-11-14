# N3.11.C — POS — Persistencia, migración y datos — QA takeover

## Dictamen

`N/A_PRODUCT_DELTA / PERSISTENCE_BOUNDARY_CERTIFIED`.

La autoridad de N3.11.B certifica que POS no introduce un aggregate root ni lifecycle de dominio nuevo. En consecuencia, N3.11.C no tiene un schema POS nuevo que materializar de forma grounded.

## CURRENT_CONFIRMED_FACT

- `Venta`/`VentaDetalle` continúan siendo la persistencia comercial existente.
- `Factura`/`FacturaPago` continúan siendo la autoridad de facturación/pagos; `FacturaPago` ya conserva monto, monto recibido, cambio y método de pago.
- `ReservaInventario`, devoluciones y notas de crédito son capacidades existentes separadas.
- El preflight Jules B #1083 fue revisado como evidence-only: los patrones actuales de migración/snapshot son reutilizables, pero sus propuestas de nuevas tablas/columnas POS permanecen `DECISION_PENDING` y no constituyen contrato.

## PERSISTENCE_BOUNDARY

1. No crear tabla `POS`, `TicketPOS`, `SesionPOS`, `TurnoCajaPOS` ni equivalentes en N3.11.C sin contrato de dominio autorizado.
2. No duplicar `Venta`, `FacturaPago`, reserva, devolución o nota de crédito.
3. No crear índices/FKs para suspensión, split-tender, terminal o caja física mientras esas decisiones sigan pendientes.
4. La resolución por barcode no requiere persistencia POS nueva; consume las identidades existentes de producto/variante.
5. Cualquier futura persistencia de caja/sesión pertenece a la capacidad de Caja correspondiente o a un contrato posterior explícitamente aprobado.

## Migración / snapshot / rollback

- Migración N3.11.C: N/A.
- Snapshot EF N3.11.C: N/A.
- Backfill/reconciliación de datos: N/A.
- Rollback de producto: N/A porque no existe product delta.
- Rollback documental: retirar esta decisión si un contrato posterior aprobado exige persistencia nueva y entonces abrir una migración causal específica.

## Validación

- No se modifica `AppDbContext`, configuraciones EF, migraciones, snapshot, SQL, Domain, Application, API o frontend.
- Los hijos posteriores deben reutilizar la persistencia existente y permanecer fail-closed frente a decisiones aún `DECISION_PENDING`.

## DoD N3.11.C

- Dependencia N3.11.B satisfecha por `7100628f88e3ab1ae66a36c385881c433eb09ad1`.
- Persistencia necesaria para el contrato vigente: N/A, demostrada con la frontera de dominio certificada.
- No se inventan tablas, constraints, migraciones ni rollback.
- P0 atribuible conocido: 0.
- P1 atribuible conocido: 0.

Selector permitido después del cierre: N3.11.D. N3.11.E+ permanece `WORK_CAN_PIPELINE__PROMOTION_CANNOT` hasta satisfacer dependencias.
