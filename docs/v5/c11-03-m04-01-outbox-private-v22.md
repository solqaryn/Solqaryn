# C11-03.M04.01 — ruta de observabilidad privada del outbox (V22)

SOLQARYN `dev`, PROJECT_SCOPE_LOCK STRICT. Acción atómica PLAN D1676: implementar una ruta y una superficie de lectura de las etapas de outbox sin habilitar operaciones sobre eventos. El módulo Angular standalone `C1103OutboxReadonlyComponent` se carga lazy bajo `/plataforma/eventos-outbox-v22`, protegido por el `plataformaGuard` existente y por un filtro local de sesión con ámbito Plataforma y administrador verificado. La autorización definitiva de cualquier futuro dato seguirá en el backend.

A0: app.routes, componente, Vitest, Playwright Chromium, workflow, documento y changelog; Angular 22.2.1, sin cambios en API, DTO, DB, DI, JWT, roles, proveedor o workers. R1 UI local; rollback normal con git revert fast-forward y readback.
A1: ruta única, lazy import, pantalla real con tres etapas, ausencia de acciones mutantes, denegación de sesión nula/empresa/sin administrador y revocación dinámica; Chromium sobre Angular efímero con anónimo, respuesta 403, responsive 320/375/768/1280, foco teclado y ninguna mutación HTTP.
A2: el guard frontend no otorga autorización del backend. Datos de eventos y PII no se leen ni se imprimen; el modo autorizado de navegador es una sesión **sintética de prueba**, NO una prueba de JWT real ni RBAC backend.
A3: Vitest Angular con TestBed y Playwright Chromium en GitHub-hosted. No hay nueva API/HTTP para eventos ni MySQL/PSP y por ello no se certifica integración de esos sistemas.
A4: exigir exact-SHA Scope Lock, V5 Changelog, Angular lint/build y workflow dirigido SUCCESS. No equiparar queued/in_progress/cancelled/skipped con PASS.
A5: revisión independiente de Task Javier distinta de DEV04 tras A0–A4. Sin datos productivos, QA/main/PROD, despliegue, pagos o secretos.

El estado visible `SIN_FUENTE_CONECTADA` es explícitamente una ausencia de evidencia, **no** representa eventos, entregas ni integraciones de MySQL verificadas. Las microtareas siguientes M04.02–M04.06 podrán incorporar estados u otras experiencias solo bajo sus propias dependencias.

## Reparación causal V22 DEV04 de CI en el SHA anterior

El job de GitHub-hosted 44774075889 falló exclusivamente en el paso de Scope Lock: el workflow tenía `defaults.run.working-directory: frontend` y ejecutaba `node ../scripts/verify-project-scope.mjs` desde ese cwd. El validador resolvía el árbol relativo a `frontend`, reportando falsamente ausencia de AGENTS.md, PROJECT_CONTEXT.md y los documentos de gobernanza. El check canónico `verify-solqaryn-project-scope` del SHA anterior había pasado, de modo que no se modifica ningún candado ni se relaja seguridad. La corrección entra en raíz con `cd ..` antes de invocar el verificador. Se exige nueva CI exact-SHA, Vitest, lint/build y Chromium; ningún FAIL/SKIPPED previo cuenta como PASS.
