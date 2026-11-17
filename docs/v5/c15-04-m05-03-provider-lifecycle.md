# SOLQARYN V19 — C15-04.M05.03 integración de sesión en UI observacional

**Autor:** DEV04 Javier `6aeacb7820ac8191b7d768a6f33dfa09`. Rama única `dev`, Scope Lock STRICT, PLAN 2296. Riesgo **R2 conservador** por contrato de proveedor de sesión, sin modificaciones productivas.

## A0 — Integración pertinente y exclusiones

La pantalla C15-04 de auditoría UX es **informativa**: no guarda observaciones, no tiene persistencia ni proveedor de auditoría propio. Por tanto, integración MySQL, almacenamiento de métricas, EF y proveedores de telemetría **NO_APLICA** a este alcance. Se prueba en cambio la integración real entre **Angular Router + plataformaGuard + PlataformaSesionService + navegador Chromium**, con transporte de la sesión deliberadamente simulado (`page.route`), preservando el requisito de revalidación de autorización al navegar.

## A1 — Regresiones adversariales

1. Primera navegación 200 autorizada muestra cinco tarjetas; el siguiente GET 403 debe ocultarlas, redirigir a login y no conservar el identificador sensible de sesión en storage.
2. Después del primer 200, la siguiente navegación 503 es fail-closed, no filtra el mensaje interno y no efectúa escritura HTTP.
3. HTTP 200 que devuelve `success=false` con un DTO aparentemente autorizado no crea acceso privado.

Estas pruebas pueden fallar ante caché de autorización, conservación indebida de datos sensibles o manejo incorrecto de respuestas. La suite no afirma integración live de MySQL o proveedor externo.

## A2–A4 — Tecnología y evidencia

GitHub-hosted Node 24/npm 11 ejecuta `npm ci`, lint, Angular build y Playwright/Chromium con aplicación local real. El servicio HTTP es **sintético**, así que JWT firmado, RBAC backend, tenant A/B live, restauración MySQL, WCAG global y Web Vitals siguen **NO_CERTIFICADOS**. Exigir Scope Lock, V5, CI dirigida, frontend/seguridad pertinentes completed SUCCESS del SHA exacto, readback institucional. Cualquier SKIPPED/QUEUED/IN_PROGRESS/FAIL de pruebas requeridas es NO_PASS.

## A5 y rollback

A5 exige Task Javier distinta de DEV04 y revisión autónoma de los tres escenarios y sus límites. Rollback sólo con `git revert` normal tras releer HEAD, sin force-push, QA/main/PROD, secretos ni operaciones de datos. Mantener BITACORA histórica.

