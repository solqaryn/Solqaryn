# SOLQARYN V17/V15 — C10-04.M04.01 interfaz privada de continuidad

**ID:** C10-04.M04.01. Autor DEV03 JAVIER. PROJECT_SCOPE_LOCK=STRICT; solo rama dev. Clasificación R1: Angular 22.2.1, interfaz documental privada y sin runtime de restauración.

## A0 — cambio real e impacto
La ruta lazy `/plataforma/continuidad-operativa-v5` incorpora `plataformaGuard` de sesión verificada en backend y un componente standalone que explica cuatro controles de continuidad. Concede solo consulta, no readback institucional vivo. La interfaz oculta controles durante revalidación, denegaciones HTTP 401/403 y fallos 503. Ninguna API, trabajador, DB, JWT/RBAC, almacenamiento, backup o permisos se modifican; el backend sigue siendo autoridad.

## A1 y A2 — prueba genuina
Se incluyen seis Vitest dirigidos: ruta y guard exactos, denegación sin sesión, contenido no operativo con sesión, single-flight de revalidación, 403 sin filtración y 503 sin filtración. El control UI es secundario al `plataformaGuard`; no atribuir una prueba end-to-end de login JWT o aislamiento tenant real.

## A3 y A4
GitHub-hosted Node 24/npm 11 ejecuta `npm ci`, Vitest dirigido, lint Angular y build de producción en workflow propio (sin modificar jobs de seguridad). Requerir resultado completed/success causal y Scope Lock/procedencia V5 en SHA exacto antes de LISTO. MySQL/EF/HTTP real/Playwright/proveedores no aplican a este scope estático. CI queued/in_progress/cancelled/failed no es PASS.

## A5, límites y rollback
A5 pendiente de otra Task Javier autorizada, distinta del autor. Prohibido QA/main/PROD, RESTORE real y secretos. Revert normal en dev, sin force-push.
