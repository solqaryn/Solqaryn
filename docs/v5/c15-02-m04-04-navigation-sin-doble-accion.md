# C15-02.M04.04 — navegación y rechazo sin doble acción
SOLQARYN/dev; PROJECT_SCOPE_LOCK=STRICT; autor DEV03 ID 6adc2e4bd8a48191ac7d8641ed95a1a3. Riesgo R2 por UI protegida.

A0: pruebas E2E de una ruta Angular existente, sin modificar componente, guard, backend, API, DTO, EF, MySQL, DNS ni secretos. Scope dependiente de M04.01/M04.02; ausencia de nuevos permisos y de proveedores reales. Rollback normal git revert.

A1: cuatro pruebas Playwright Chromium que exigen anónimo rechazado, 403/Empresa sin mensaje secreto, foco Tab y Enter reales sin foco programático, navegación legítima, responsive 320/375/768/1280, destino 44px, ausencia de formularios, mutaciones y doble acción aun tras clic doble en etiqueta informativa. Aserciones positivas/negativas pueden fallar ante defectos.

A2: servidor conserva autoridad JWT/RBAC/tenant. Los mocks de sesión NO acreditan JWT firmado, dos empresas reales, HTTP backend auténtico ni certificaciones de proveedores.

A3: GitHub-hosted Node24/npm11, Angular 22, build y Playwright Chromium en loopback. MySQL/EF no aplica al diff de solo pruebas/documentación. A4: exigir exact SHA Scope Lock/V5, check Chromium y controles relevantes SUCCESS, nunca QUEUED/IN_PROGRESS/SKIPPED como PASS, readback del plan y BITACORA.

A5 por otra Task Javier distinta de DEV03 tras revisión real; no QA/main/PROD ni R3.
