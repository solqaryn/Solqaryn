# P0-010.B — Certificación negativa: issue vacío no prueba ausencia de defectos

**Scope comercial:** P0-010.B, PLAN_MAESTRO/COLA fila 21. Dependencias P0-010.A y P0-012.B LISTO. DEV solo, PROJECT_SCOPE_LOCK=STRICT.

## Corte reproducible, no estado en tiempo real

A las 2026-10-30T21:22:36Z se leyó COLA y el buscador de Issues de GitHub mediante cuentas autorizadas. Los 24 P0 sumaban 22 LISTO, 1 VALIDANDO (P0-008.B), 1 EN_PROGRESO (esta microtarea) y 0 PENDIENTE; de 32 P1, 1 LISTO y 31 PENDIENTE. El buscador `repo:solqaryn/Solqaryn is:issue is:open` devolvió un Issue #3730 sobre coordinación histórica, no un defecto de aplicación demostrado. No se puede extrapolar que no existen incidencias de producto, ni que el backlog de microtareas es un catálogo de bugs.

Los datos se guardan como snapshot fechado `docs/baselines/P0-010-B-DEV-20261030.json`. El verificador y los valores son de **ese corte solamente**; se debe reconsultar Sheets y GitHub antes de cualquier nueva actuación y no afirmar paridad, despliegue o ausencia de errores. Incidencias fuera del tracker siguen `NOT_VERIFIED`.

## Test positivo/negativo

`node scripts/verify-p0-010-b-issue-inference.mjs` reutiliza el verificador histórico P0-010.A y verifica el snapshot fechado nuevo y su reconciliación P0/P1. Rechaza 12 alteraciones sintéticas: issue count a cero con declaración "sin defectos"; otras atribuciones falsas de ausencia de incidencias; cobertura exhaustiva del tracker; backlog supuestamente terminado; números inflados o inconsistentes; reclasificación injustificada del Issue de coordinación; convertir corte histórico a certificación LIVE; eliminar límites; conteo inválido.

**Resultado exigido:** caso positivo y 12/12 negativos PASS en CI, además Scope Lock, seguridad multiempresa y build backend 3/3 SUCCESS sobre HEAD exacto, aprobación independiente de alguien distinto de autor/editor antes del merge, merge protegido a dev y readback de PLAN_MAESTRO/COLA/BITACORA.

Esta prueba **no ejecuta tests ERP reales ni verifica todos los incidentes posibles**, sino que impide una conclusión documental indebida. No toca QA/main/PROD, API, RBAC, datos, usuarios, migraciones ni secretos. Rollback: revert protegido del JSON fechado, test, workflow, acta y changelog.
