# N3.8.B — Nota de débito de cliente — Dominio y contratos

## Disposición

**N/A CON EVIDENCIA / LISTO PARA ESTE ALCANCE ACTUAL.**

N3.8.A certificó que el repositorio no contiene hoy `NotaDebitoCliente` y, más importante, que no existe todavía una necesidad legal/operativa autoritativa que permita fijar su lifecycle, efectos fiscales/contables, documento origen, cardinalidad, idempotencia o vínculos obligatorios. El alcance maestro de N3.8 es condicional: añadir nota de débito **cuando legislación/operación lo requiera**.

Implementar ahora un aggregate implicaría convertir decisiones pendientes en contrato. Por PARENT_CLOSE_FIRST y fail-closed, N3.8.B se resuelve como N/A hasta que exista un requisito funcional/legal autoritativo.

## Evidencia

- Preflight N3.8.A: `docs/qa/N3_8_A_PREFLIGHT_V3251.md`.
- Búsqueda dirigida actual: no existe entidad/servicio/controller/DTO/ruta/migración `NotaDebitoCliente`.
- `NotaCreditoCliente` es únicamente patrón adyacente observado; no es autoridad para invertir semánticas o copiar lifecycle.
- No existe evidencia autoritativa actual que obligue `FacturaId`, `VentaId`, `DevolucionClienteId`, numeración fiscal, impacto CxC/caja, stock/Kardex, idempotencia o cardinalidad específica.

## Contrato de no-invención

Hasta que exista requisito autoritativo, permanecen `DECISION_PENDING`:

- identidad y documento origen;
- lifecycle/estados/transiciones;
- reglas monetarias/fiscales;
- efectos contables y de saldo;
- idempotencia/cardinalidad;
- relaciones y ownership;
- persistencia, API/RBAC y UI.

## Cambios de producto

Ninguno. No se crea domain model, enum, DTO, migración, API ni frontend.

## Validación

- scope exclusivamente documental;
- no se adelantan hijos N3.8.C+;
- P0/P1 atribuibles a N3.8.B bajo el requisito actual: 0 conocidos;
- si posteriormente la legislación/operación exige NotaDebitoCliente, N3.8.B deberá reabrirse mediante requisito explícito antes de implementar dominio.

## Criterio de cierre

El criterio de la microtarea permite documentar N/A con evidencia cuando la implementación no aplica. Con el requisito actual no demostrado, **N3.8.B puede cerrarse LISTO_REAL/N_A_CERTIFIED sin código especulativo**.
