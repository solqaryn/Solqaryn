# N3.9.A — Cuentas por cobrar — Preflight canónico VAEP v3.25.1

## Disposición

**PREFLIGHT CERTIFIED / QA_TAKEOVER / EVIDENCE-ONLY.** Este documento no crea ni autoriza todavía un aggregate `CuentaPorCobrar`, schema, API, lifecycle o política contable. Su propósito es fijar la autoridad actual y los gaps que N3.9.B+ deben resolver explícitamente.

## CURRENT_CONFIRMED_FACT

- No existe en el repositorio una entidad `CuentaPorCobrar` con autoridad propia.
- `Factura` ya materializa autoridad receivable-adjacent por factura: `FechaVencimiento`, `Moneda`, `CondicionPago`, `Total`, `TotalPagado`, `SaldoPendiente` y colección de `FacturaPago`.
- `FacturaPago` es un registro auditable por factura con `FechaPago`, monto aplicado, monto recibido, cambio, método de pago, referencia y datos de anulación.
- `NotaCreditoCliente` está ligada a `FacturaId` y `VentaId`, conserva moneda y monto de crédito, y declara expresamente que lifecycle fiscal, numeración y aplicación contable/saldo permanecen fuera de su contrato actual.
- La evidencia actual demuestra seguimiento de deuda/pagos a nivel Factura/FacturaPago; no demuestra un subledger CxC independiente.

## OBSERVED_PATTERN

- El saldo por cobrar vigente se representa en `Factura.SaldoPendiente` y sus pagos asociados, no en un ledger separado.
- Los documentos financieros existentes mantienen responsabilidades separadas y snapshots auditables; cualquier nueva autoridad CxC debe evitar duplicar o contradecir Factura/FacturaPago/NotaCreditoCliente.

## DECISION_PENDING / RISK

N3.9.A no autoriza todavía ninguna de estas decisiones:

- aggregate nuevo `CuentaPorCobrar` versus extensión/proyección de `Factura`;
- cardinalidad por factura, cliente u otra dimensión;
- aging buckets, mora/late-fee, anticipos o saldos a favor;
- distribución de pagos entre múltiples facturas;
- semántica contable de `NotaCreditoCliente` sobre saldo;
- idempotencia/concurrencia de aplicaciones de pago;
- schema/FKs/índices/precisiones/migración;
- endpoints, permisos/RBAC, filtros, paginación o ProblemDetails.

Resolver cualquiera de estas decisiones sin autoridad explícita sería inventar contrato.

## REVIEW_DRAIN

- Jules A ATTEMPT1: evidence útil, **REJECTED para integración por base divergence**; ATTEMPT1 consumido.
- Jules B ATTEMPT1: evidence útil, **REJECTED para integración por base divergence**; ATTEMPT1 consumido.
- Jules D ATTEMPT1: **REJECTED / EMPTY_CHANGESET / base divergence**; ATTEMPT1 consumido.
- Jules C: bootstrap anterior sin job/session >5m; recovery SAME logical task / SAME ATTEMPT1 emitido bajo ManifestGuard; ausencia de bootstrap no consume intento.
- Ningún patch Jules de esos resultados forma parte de esta certificación.

## Handoff obligatorio

### N3.9.B — Dominio y contratos

Debe decidir, con autoridad explícita, si CxC requiere un nuevo aggregate o si la autoridad seguirá/proyectará desde Factura. Solo después puede definir invariantes, cardinalidad, lifecycle, saldo, vencimiento, crédito e idempotencia.

### N3.9.C — Persistencia, migración y datos

Solo después del contrato B puede definir mapping, FKs, unicidad, precisión, migración, backfill, pre/postchecks y rollback/data-safety. No debe inferir schema desde este preflight.

### N3.9.D/F — Application/API y seguridad

Solo después de B/C puede definir servicios/endpoints/RBAC/auditoría/observabilidad. No existe autorización en N3.9.A para inventarlos.

## DoD N3.9.A

- autoridad actual inspeccionada y documentada;
- ausencia de CuentaPorCobrar dedicada confirmada;
- límites entre CURRENT_CONFIRMED_FACT y DECISION_PENDING fijados;
- reviews Jules terminales reconciliados sin integración insegura;
- handoff B/C/D/F explícito y fail-closed;
- P0/P1 funcional introducido por este preflight: **0 conocidos**.

N3.9.A puede cerrarse como preflight únicamente con esta evidencia; no implica que N3.9.B+ estén implementados ni aprobados.
