# C04-01.M04.05 — Angular Chromium responsive, teclado y contraste (V22)

PLAN D600: verificar responsive, teclado, foco, labels y contraste. Riesgo R2 conservador por contexto de SaaS/billing. Entrega test-only de la pantalla de suscripción ya existente en /configuracion, con Angular 22.2.1. No se cambia ruta, API, JWT/RBAC, tenancy, ni MySQL; no se interfiere con M04.01 desarrollada por DEV03. Solo dev y PROJECT_SCOPE_LOCK STRICT.

A0/A1: pruebas Playwright en Chromium de Angular loopback para 320/375/768/1280 px y tamaño de texto 100/200 %, Tab real sin enfocar artificialmente, headings/labels y contraste computado >= 4.5, simulaciones negativas tenant/permisos y filtro GET sin writes.
A2: los fixtures HTTP son sintéticos y el token NO es JWT firmado, no prueban autorizacion real ni aislamiento MySQL. Integracion HTTP/JWT/EF/MySQL de producto no cambia, NO_APLICA causal a este test-only; queda limitacion de fase. A3: browser real GitHub-hosted. A4: exigir check Chromium + build Angular, V5, ScopeLock y CI relevante completados SUCCESS exact-SHA; FAIL/SKIPPED/CANCELLED/IN_PROGRESS requeridos NO son PASS. A5 independiente de DEV04. Rollback git revert normal; no R3, PSP, QA/main/PROD ni secretos.

Referencias: governance canónica exact HEAD; Angular Developer de angular/angular pin 6808966993bf2b2d455d9b6245e960a59a01b9a9 (define-routes, route-guards, e2e-testing) y Impeccable pin 2149fcce39a90bb409df5f16515f316a76dc6199 solo metodología (no ejecutar launcher).
