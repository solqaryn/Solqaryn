# C06-01.M04.04 — navegación, mensajes y prevención de doble acción

2026-11-21. SOLQARYN V22, repo único solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK STRICT. PLAN D887 manda verificación concreta; PLAN E887 es meta acumulativa M04, NO cierre de fase. R1: pequeño cambio de UI público sin datos, API, JWT, tenant ni MySQL.

## Defecto causal y reparación
La landing publica `/conoce-solqaryn` contenía tres anchors internos `href="#beneficios"` y `href="#alcance"` que el HTML `base href="/"` de Angular resuelve a `/#beneficios` o `/#alcance` fuera de la ruta. El flujo de skip-link ya exigió reparación equivalente. Se añaden handlers limitados al id allowlisted `beneficios|alcance`, `preventDefault` y focus de secciones con `tabindex=-1`, conservando anchors semánticos, navegación pública y CTA a login. Las rutas privadas permanecen protegidas.

## Evidencia y limitaciones
A0: diff de componente existente, 3 pruebas de navegación real Chromium y workflow exact-SHA + changelog aditivo íntegro. A1: click header Qué resuelve/Alcance y CTA héroe mantienen ruta, foco y mensajes; doble evento click hacia login sin POST/PUT/PATCH/DELETE; ausencia de formularios mutables. A2: no permisos/jwt/tenant alterados; seguridad HTTP/MySQL NO_APLICA motivado. A3: Angular Router real, Playwright/Chromium (no mocks). A4: Require CI exact-SHA complete SUCCESS, Skill/ScopeLock/V5, no SKIPPED/CANCELLED/IN_PROGRESS como PASS. A5 independiente por Task Javier distinta de DEV05 tras LISTO.

LIMITACION: no se certifican formularios comerciales aún inexistentes, backend, pagos, precios, la fase M04 entera ni producción. Rollback normal revert FF en dev, conservar historia BITACORA.
