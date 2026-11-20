# C15-02.M05.04 — aceptación positiva de vista privada de arquitectura

La acción atómica de PLAN_MAESTRO D2225 es ejecutar un caso positivo de aceptación; E2225 se interpreta exclusivamente como meta M05 acumulativa y no certifica proveedores ni una fase completa.

**R1 test-only, alcance dev.** Dos escenarios Playwright GitHub-hosted en Chromium real sirven el Angular 22.2.1 en loopback: recorrido de los cinco referentes locales NO_VERIFICADO, títulos y navegación válida sin mutaciones HTTP, viewport móvil sin overflow; recarga de la ruta autenticada sintéticamente y recorrido real Tab/Enter de regreso a plataforma. No se alteró ningún componente runtime, backend/API/DTO, base de datos, proveedor ni secreto.

A0: diff de E2E, workflow, doc y CHANGELOG, SHA CAS de dev. A1: asserts positivos verificables y negativos de no-muta/no-certificación; la suite falla si faltan referencias, enlaces, navegación o resguardo. A2: la sesión HTTP se simula en Playwright, no es prueba JWT real ni autorización de empresa/tenant y no puede certificar despliegue. A3: Chromium y servidor Angular en GitHub Actions sin acceso a proveedores; MySQL, R2, DNS y Cloudflare NO_APLICA porque el cambio es test-only y D2225 no exige su integración. A4: PASS del job propio, frontend, V5 y Scope Lock del SHA de código, no contar checks en progreso o SKIPPED. A5: revisión genuina independiente por Task Javier distinta de DEV03.

Limitaciones: la experiencia de plataforma frente a JWT firmado/tenant real, datos y producción siguen no certificados; NO QA/main/PROD. Rollback: git revert normal, sin force-push.

Referencia Angular origen angular/angular 6808966993bf2b2d455d9b6245e960a59a01b9a9, ruta skills/dev-skills/angular-developer/SKILL.md. No se copiaron scripts externos.
