# C13-04.M04.05 — coherencia visual, teclado, responsive y contraste

**Autora técnica:** SOLQARYN PRIMARY DEV 01 — Task 6adbb9fff1548191a1bd5b30d9e9984a. **Scope:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT; PLAN fila 2004. **Riesgo:** R2 por revisión de superficies privadas, solo archivos test/CI/doc.

## A0 — Alcance concreto
Nuevas pruebas transversales sobre DOS vistas YA EXISTENTES: consola global /plataforma/consola-global-v5 y cobertura /plataforma/trazabilidad-cobertura-v5. No implementa catálogo integral de módulos M04.01 ni estados M04.02 ni modifica runtime, Angular, API, JWT, RBAC, tenant, MySQL, migraciones o datos. Consumidor: Playwright Chromium con Angular build real GitHub-hosted. 4 archivos cambiados: spec, workflow, documento y CHANGELOG. Sin cuentas de pago ni proveedores live.

## A1/A2/A3 — oráculos
Cuatro regresiones capaces de fallar: (1) anónimo y 403 sintético no muestran tarjetas ni mensaje privado; (2) dos títulos/nombres accesibles y cuatro elementos renderizados, reflow en 320/375/768/1280 sin overflow; (3) Tab desde foco inicial hasta enlace accesible en ambas pantallas, outline visible y diana >=44px SIN .focus(); (4) contraste CSS texto/fondo >=4.5:1 en tarjeta y estado. En el navegador se usa token local SINTÉTICO y page.route de sesión: estas pruebas NO son JWT HTTP, RBAC real, aislación de tenant A/B en backend ni auditoría durable. A3: Chromium real y frontend compilado, sin MySQL/EF; integración DB NO_APLICA al diff. No inventar WCAG manual ni métricas de producción.

## A4/A5 y reversión
Workflow GitHub-hosted aislado por SHA, cancel-in-progress=false, Node 24, Angular 22.2.1, npm 11.19 y Playwright 1.63.0. Exigir scope lock, procedencia V5, frontend, seguridad y Chromium dirigido SUCCESS exact-SHA, más PLAN/COLA/DAG/BITACORA readback. Si CI QUEUED/IN_PROGRESS, conservar VALIDANDO con SHA y run; FAIL/CANCELLED/SKIPPED no son PASS. A5 solo por Task Javier diferente de la autora, validando código, negativos, flujos y exact-SHA; jamás autoprobación o promoción QA/main/PROD. Reversión Git revert normal aditivo sin reescritura ni borrar BITACORA.
