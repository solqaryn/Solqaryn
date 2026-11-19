# SOLQARYN V17 — C15-01.M04.01 ruta y superficie de auditoría de cobertura

Task DEV02: 6af16bb9492c8191b8e8c7d606f11507. PLAN fila 2180. DAG F VALIDADO/G SI, COLA G/O SI y owner único mediante candado 2000002180.

Riesgo R2 por acceso a superficie de gobierno, aunque el código solo crea una UI de lectura y no autoriza acciones nuevas.

A0: Ruta lazy privada /plataforma/trazabilidad-cobertura-v5 protegida por plataformaGuard; componente Angular standalone, test positivo/negativo, workflow, changelog y este contrato. La pantalla NO descarga PLAN/Sheets ni muestra métricas de cobertura inventadas.

A1: autorizado ve cuatro dimensiones, anónimo y Empresa quedan denegados, revocación reactiva elimina dimensiones, frontera 320 px sin overflow, foco con teclado. A2: ninguna modificación de backend/JWT/RBAC/tenant; pruebas HTTP sintéticas no validan tenencia A/B real ni pentest.

A3: Angular build + pruebas Vitest y Playwright Chromium en runner GitHub-hosted; MySQL/EF/externos no aplican porque no hay cambios de persistencia ni integraciones. A4: dev SHA publicado, checks requeridos completed/success, Scope Lock/V5, readback PLAN/COLA/DAG/BITACORA. No contar en progreso/cancelled/skipped como éxito. A5: revisor Javier Task ID diferente debe aprobar o rechazar.

Los textos SIN_VERIFICAR y NO_MEDIDO evitan declarar una comparación total del plan que esta UI todavía no realiza; siguientes etapas integrarán evidencia de forma segura. Revertir con git revert ordinario, nunca force push, sin QA/main/PROD.

## Reparación causal de CI — 16 noviembre 2026

SHA inicial 07169caaabd2909263ae74a7f0313495b2a8bd3c: Chromium propio 2/3 PASS y 1 FAIL por selector global que veía 5 controles externos de plataforma; ERP audit FAIL por no registrar explícitamente nueva ruta guardada; catálogo P0-007.A FAIL por componente nuevo no inventariado. Reparación mínima: selector limitado al componente, inscripción exacta de ruta en allowlist sin aflojar guard y adición de archivo al manifiesto con conteos 151/143/8. No desactivar checks, no aceptar skipped/cancelled/in_progress. Las suites deben ser ejecutadas otra vez por GitHub-hosted sobre el SHA reparado; A5 independiente posterior. Rollback normal, sin QA/main/PROD.

## Reparación P2 de la prueba de teclado — 18 noviembre 2026 (DEV06, revisor posterior independiente)
La A5 previa rechazó correctamente el test porque invocaba `back.focus()` y `back.click()`, lo cual no demuestra alcance por teclado. La suite ahora avanza mediante `page.keyboard.press('Tab')` hasta un máximo explícito de 40 saltos, exige foco auténtico en el enlace, comprueba el contorno visible y usa `page.keyboard.press('Enter')` para activar la navegación. Un control adversarial aislado establece `tabindex=-1` únicamente en el DOM del navegador de prueba y verifica que Tab no puede alcanzar el enlace. No modifica Angular, rutas, guards, JWT, RBAC ni acceso tenant A/B. Es una prueba Playwright sintética, no HTTP/JWT real ni aprobación de auditoría completa.
A0: un archivo E2E, este registro y CHANGELOG; riesgo R2 de accesibilidad, sin API/DB. A1: positivo Tab+Enter y negativo enlace no tabulable; conservar rechazo anónimo 403 y responsive. A2: autorización existente sin cambios; no inferir seguridad backend. A3: exigir Chromium real de GitHub Actions sobre SHA nuevo. A4: Scope Lock/V5/checks requeridos SUCCESS y readback institucional. A5: revisión independiente por Task Javier distinta de autora DEV02 y reparadora DEV06; conservar el A5_FAIL P2 histórico. Rollback: git revert normal sin force-push. Sin QA/main/PROD ni R3.
