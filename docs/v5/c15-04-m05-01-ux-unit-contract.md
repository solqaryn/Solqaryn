# SOLQARYN V19 — C15-04.M05.01: contrato unitario reproducible de auditoría UX

**Autor:** PRIMARY DEV04 Javier, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. Único ámbito: `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. PLAN_MAESTRO fila 2294. Riesgo **R1**: sólo nuevos Vitest, workflow GitHub-hosted, documento y changelog; el componente, sus rutas y el backend permanecen intactos.

## A0 — Criterio, dependencias y superficie

La microtarea M05.01 exige prueba unitaria o regla ejecutable con resultado reproducible. Se comprueba el contrato real de la vista Angular privada **C15-04.M04.01**, después de M04.06/A5 independiente: ruta lazy con `plataformaGuard`, cinco superficies visibles con identificadores estables, datos de cobertura honestamente `NO_MEDIDO`, navegación semántica y ausencia de operaciones. Los cuatro anchos se prueban aquí como **configuración declarativa**, no como medición de render; Playwright de M04.06 acredita sólo la vista de referencia en 320/375/768/1280. No se interpreta M05.01 como auditoría de todos los módulos, landing, ERP, builder o portal.

## A1 — Cinco casos que pueden fallar

1. Ruta única privada y lazy; rechazo de omitir `plataformaGuard`.
2. Cinco tarjetas e IDs únicos (landing/portal/ERP/builder/global), encabezados y objetivos alineados.
3. Cinco estados `NO_MEDIDO` y textos explícitos `SIN_VERIFICAR` para WCAG; impide certificar métricas inexistentes.
4. Semántica main/título/enlace de retorno; rechazo si aparecen controles de mutación, formularios o botones no autorizados.
5. Límites declarativos exactos 320/375/768/1280 y texto «Objetivos, no resultados».

## A2–A4 — Alcances y evidencias

Riesgos de la pantalla privada se mitigan en la ruta existente; su guard se revisa como contrato de configuración, **no** se ejecutan JWT firmados ni RBAC/tenant server-side. Integración pertinente es **Angular TestBed real / Vitest** en GitHub-hosted Node24/npm11 con `npm ci`, tipado/lint y build de producción. MySQL, EF, API HTTP, Web Vitals, contraste WCAG global, integración con proveedores y producción **NO_APLICA** a este diff test-only; no deben acreditarse ni inferirse. Para A4 exigir prueba dirigida, Scope Lock, V5, checks aplicables completed/success sobre SHA exacto, readback PLAN/COLA/DAG/BITACORA. `SKIPPED`, `CANCELLED` y `IN_PROGRESS` no son PASS.

## A5 y reversión

A5 requerirá inspección independiente de otra de las cinco Tasks Javier, distinta de DEV04, del contrato, fuente, Vitest, logs y SHA. No se crean permisos, rutas, endpoints, migraciones, ramas, Tasks, secretos ni servicios. Rollback: `git revert` normal sobre `dev` después de revalidar HEAD; preservar BITACORA histórica.
