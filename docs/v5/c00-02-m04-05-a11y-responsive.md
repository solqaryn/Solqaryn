# C00-02.M04.05 — verificaciones reales de accesibilidad y responsive V5

**Artefacto ejecutable:** tres pruebas Playwright adicionales en `frontend/e2e/v5-plataforma-browser-guards.spec.ts`, sobre las rutas protegidas existentes `/plataforma/flujo-v5` y `/plataforma/cobertura-v5`.

**Criterios verificables:** (1) a 390 y 1280 píxeles los cuatro pasos mantienen lectura sin overflow horizontal y la navegación permite Tab; (2) tabla de cobertura conserva cinco encabezados y una región accesible navegable horizontalmente con teclado; (3) contraste computado del encabezado de contenido alcanza 4.5:1 con el tema efectivo.

**Seguridad y evidencia negativa:** se mantienen los tests de bloqueo sin token y respuesta 403; el token y la respuesta 200 son **sintéticos** exclusivamente para probar UI, y no acreditan JWT/MySQL real ni otorgan permisos. La pantalla no consulta ni escribe PLAN_MAESTRO, COLA o BITACORA. Se exige resultado Playwright/Chromium, lint/build Angular y Scope Lock del SHA publicado; fallos o jobs pendientes no son PASS.

**Clasificación:** requiere_node_angular=SI; requiere_e2e=SI; requiere_security=SI (control negativo de guard de UI, no autenticación live); requiere_scope_lock=SI; requiere_dotnet=NO_APLICA; requiere_mysql=NO_APLICA; requiere_docker=NO_APLICA: no hay cambios API, persistencia, infraestructura ni integraciones. A5_PENDIENTE requiere otra Task Javier. Rollback mediante git revert no destructivo, sin QA/main/PROD.
