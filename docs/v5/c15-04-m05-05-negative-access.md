# SOLQARYN V19 — C15-04.M05.05 casos negativos de seguridad UI

**Autor:** PRIMARY DEV04 Javier (`6aeacb7820ac8191b7d768a6f33dfa09`). PLAN fila 2298, `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **R2 conservador** por pruebas de autorización, sin cambios productivos.

## A0 — Alcance y dependencias

Prueba negativa nueva con navegador Chromium sobre Angular real de la ruta privada `/plataforma/auditoria-ux-v5`, usando `plataformaGuard` y `PlataformaSesionService` existentes. Sólo nuevos test E2E, workflow, documento y changelog. No hay código nuevo de autenticación, datos, JWT, backend, rutas, grants ni migraciones.

## A1 — Cuatro oráculos adversariales

1. Acceso anónimo bloqueado antes de renderizar datos privados.
2. Token local sintético con respuesta HTTP 401 denegada, sin fuga visual ni escrituras.
3. Respuesta HTTP 403 sintética denegada, mensaje de proveedor opaco nunca mostrado, cero escrituras.
4. HTTP 200 con ámbito Empresa sintético no se confunde con una sesión global de Plataforma.

En las tres sesiones simuladas se comprueba una sola petición de validación. Casos que pueden fallar si se rompe la protección de la ruta o se permite un ámbito inválido.

## A2–A4 — Evidencia y límite

El entorno E2E levanta Angular real en loopback con Playwright/Chromium. HTTP de sesión es **interceptado y sintético**, por lo que **NO_CERTIFICA** JWT firmado real, revocación real, tenant A/B real, API, MySQL, WCAG global, producción o secretos. Las negativas sólo prueban el frontend ante las respuestas especificadas. Se exige `npm ci`, lint, build producción, Chromium 4/4, Scope Lock, V5 y checks pertinentes de ese SHA; `QUEUED/IN_PROGRESS/SKIPPED/CANCELLED` no son PASS.

## A5 y rollback

A5 sólo por otra Task Javier de TASK_ID distinto tras revisar tests, fuentes, run, artefactos y estado institucional. No QA/main/PROD. Rollback Git mediante `git revert` normal revalidando HEAD; no borrar BITACORA histórica.

