# G0-006.A — cierre dirigido de trazabilidad V13 (DEV)

**Fuente:** Plan Comercial Inicial Unificado, microtarea G0-006.A; estado operativo de PLAN_MAESTRO/COLA. Este documento no crea un segundo plan ni certifica por sí solo LISTO.

## Matriz requisito → consumidor → código → prueba → evidencia

| Requisito | Consumidor técnico / código | Prueba causal | Evidencia y limitación |
| --- | --- | --- | --- |
| G0-006.C | scripts/automation-ci-throughput.mjs | scripts/tests/g0-006-c-trazabilidad.test.mjs | PLAN/COLA LISTO, prueba negativa de recibos PR incompletos; la certificación histórica debe verificarse por ledger, no inferirse desde este documento. |
| G0-006.D | scripts/quality/direct-dev-traceability.mjs | scripts/tests/g0-006-d-trazabilidad-direct-dev.test.mjs | PLAN/COLA LISTO; commit directo 033bb6e9b57cf37af623dbf387d71049ba15a2b9; recibos sintéticos no sustituyen readback real. |
| G0-006.E | scripts/quality/direct-dev-traceability.mjs | scripts/tests/g0-006-e-historical-phase-negative.test.mjs | PLAN/COLA LISTO; commit directo 67f3b0122d9df38322fcf10ea40aedf723c51285; fases 8/13 no reabren el roadmap. |
| G0-006.A | scripts/quality/g0-006-a-closure.mjs | scripts/tests/g0-006-a-closure.test.mjs | Control positivo y 23 controles negativos/sintéticos del contrato A0–A5 y de las tres predecesoras; necesita readbacks vivos para cerrar. |

## Decisión y alcance

**INCLUDE**: agrega una puerta asesora y tests deterministas. Solo un recibo de predecesoras C/D/E con estados LISTO, rutas de prueba canónicas, publicación directa de dev con fast-forward, auditorías A0–A4 vinculadas a SHA, A5_PENDIENTE y readbacks institucionales completos puede considerarse formalmente íntegro. Un recibo que diga A5_PASS sin evidencia independiente no se acepta. Las fases históricas o pruebas SKIPPED no se traducen a PASS.

**No se altera** Angular, backend, EF/MySQL, API, JWT, RBAC, PII, secretos, reglas de ramas, despliegues ni QA/main/PROD. Los fixtures son datos sintéticos, no prueba de servicios vivos ni certificación de Sheets. Este verificador no concede permisos, no escribe en GitHub/Drive y no reemplaza revisiones/CI causales del proveedor.

## Auditoría y rollback

Comando reproducible: node --check scripts/quality/g0-006-a-closure.mjs && node --test scripts/tests/g0-006-a-closure.test.mjs con Node 22.16.0. Revisar el diff, SHA candidato, posibles secretos, dependencia Node estándar y los riesgos por datos faltantes antes de publicar. Después del commit directo fast-forward, reejecutar pruebas sobre el contenido final, inspeccionar HEAD, PLAN/COLA/G y BITACORA, y registrar A5 pendiente para otro TASK_ID. Si una prueba o lectura no puede acreditarse, conservar VALIDANDO y registrar bloqueo.

Rollback autorizado en DEV: crear revert aditivo y fast-forward del commit afectado, preservando los cambios de otros Scopes; nunca force-push ni reset destructivo.
