# Certificación — ERP-N2.8 Cuentas por Pagar

## Alcance

Certifica el cierre de ERP-N2.8 A–H: preflight, dominio/contratos, persistencia/migración, Application/API, frontend/UX, RBAC/auditoría/seguridad, QA/regresión y documentación.

## Evidencia técnica

Baseline de control previo al paquete documental: `1dd45e4679fcb01ed5052ff648019db1da8f1d53`.

Sobre ese mismo HEAD los cuatro gates causales obligatorios terminaron `SUCCESS`:

- Development — SUCCESS.
- Acceptance — SUCCESS.
- Fase8 — SUCCESS.
- M13 — SUCCESS.

La migración canónica `20260822161500_N28_CuentasPorPagar` implementa checks/FKs/índices e idempotencia; el controller conserva `[Authorize]` y permisos relacionales de Finanzas; el servicio ejecuta las mutaciones dentro de transacciones y preserva idempotencia.

## Review Jules / VAEP

Los resultados Jules de N2.8.H se trataron como evidencia y no como publicación directa. La matriz A1 fue rechazada por self-review no independiente y hechos stale. La evidencia de aceptación C1 confirmó el scope sin cambios de producto y señaló como riesgo residual la ausencia de un E2E UI negativo dedicado; dicho punto no invalida el cierre porque N2.8.G ya fue certificado y Acceptance/Fase8/M13 integrales están verdes. Ningún artifact rechazado fue aplicado directamente a `Desarrollo`.

## Paquete canónico

- `docs/ERP_N2_8_CUENTAS_POR_PAGAR.md`
- `docs/ADR_N2_8_CUENTAS_POR_PAGAR_AUTORIDAD_FINANCIERA.md`
- `docs/OPENAPI_N2_8_CUENTAS_POR_PAGAR.md`
- `docs/RUNBOOK_N2_8_CUENTAS_POR_PAGAR.md`
- `docs/ROLLBACK_N2_8_CUENTAS_POR_PAGAR.md`
- `docs/CERTIFICACION_N2_8_CUENTAS_POR_PAGAR.md`

## Dictamen

El producto N2.8 no presenta P0/P1 conocidos en el baseline certificado. Este paquete documental es el candidato final de N2.8.H y debe pasar Development + Acceptance + Fase8 + M13 sobre su propio HEAD antes de que COLA cambie H a `LISTO`.
