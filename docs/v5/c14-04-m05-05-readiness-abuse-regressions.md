# C14-04.M05.05 — Regresiones de abuso y seguridad readiness DEV

**Proyecto:** solqaryn/Solqaryn, rama dev, Scope Lock STRICT. **Clasificación:** R2 por revisión release, tenant, autoría y señales CI; NO es ejecución R3.

## Entrega
Suite xUnit de riesgo que ejercita la regla pura Domain `V5ReleaseReadinessDomainRules`: rechazo de QA/PROD, grants no verificados, cruce tenant A/B, cuenta no allowlisted, auto-A5, checks QUEUED/IN_PROGRESS/FAIL/CANCELLED/SKIPPED, SHA diferente, checks requeridos duplicados/ausentes, checks adicionales fallidos, aceptación no ejecutada o duplicada, digest 63 caracteres, manifiesto falso, ausencia stop condition y fuente de autoridad malformada. Éxito sintético significa solo `READY_FOR_EXTERNAL_EVIDENCE_REVIEW`, nunca autorización de go-live.

## Verificación y límites
A0: nuevo archivo de tests, documento y sección CHANGELOG contigua; sin modificar reglas existentes, API, integración, configuración, migraciones ni datos. A1: positivos y rechazos negativos con assertions que fallan ante relajaciones, ejecución real GitHub-hosted .NET10 Release por SHA exacto. A2: tenant A/B, denegación sin grant, reviewer distinto, cancelaciones CI y no certificación de aislamiento real. A3: xUnit en proceso; JWT/HTTP/MySQL 8.4/Playwright/productos de terceros NO_APLICA porque no se tocan ni invocan. A4: Scope Lock, V5, backend/security y pruebas requeridas completed/success exact-SHA antes de LISTO. A5: revisión independiente por otra Task Javier del código, requisitos y pruebas; FAIL histórico permanece. No desplegar QA/main/PROD. Revertir mediante git revert normal con readback de HEAD.
