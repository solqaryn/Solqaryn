# SOLQARYN V18 — C08-04.M04.01: superficie privada de extensiones sectoriales

**Autor:** PRIMARY DEV 03 JAVIER, TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3. **Scope:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **Candado V17:** SQV17_C08_04_M04_01, id 2000001280. **Riesgo R1.** PLAN_MAESTRO fila 1280 y COLA G/O=SI, DAG F=VALIDADO/G=SI comprobados antes de CLAIM.

## A0 — Implementación y límites

Se crea ruta privada lazy /extensiones-sectoriales bajo authGuard existente. Este guard revalida sesión + empresa verificada mediante backend antes de autorizar una navegación (ningún token o host del navegador es autoridad por sí mismo). Componente standalone Angular 22.2.1 OnPush con Signals para cuatro ámbitos estáticos: reservas, membresías, catálogo y sitios por rubro. Estado explícito «Sin módulos conectados». No hay formularios, cambios de estado, activación de módulos, API nueva, permisos RBAC declarados ni consultas a capabilities. No se fabrica estado del tenant ni permiso desde el frontend. Se reutiliza la navegación a dashboard existente. Los contratos M03.01-.06 preceden a M04; M03.06 está LISTO con A5 independiente. No modifica Domain, Application, Infrastructure, MySQL/EF, JWT, permisos backend, entidades, secretos, integración, dato ni deployment. Contratos M01/M02 no sustituyen pruebas de operación real. Reversión: git revert normal del commit y evidencia intacta.

## A1–A3 — Pruebas causales

Vitest dirigido verifica ruta única, guard, lazy-loader, cuatro ámbitos y ausencia de controles de activación. Playwright/Chromium GitHub-hosted verifica redirección anónima, denegación sintética 403 de tenant, navegación con sesión sintética permitida, ausencia de requests de módulos y ancho a 320/1280 px. El entorno sintético no acredita JWT HTTP real, RBAC backend de módulos ni separación A/B de producción. La seguridad aplicable usa authGuard + tenant verificado por su servicio existente. Integración MySQL, API nuevas, proveedores y migraciones NO_APLICA: este diff es solo UI estática con route guard.

## A4/A5 y handoff

Exigir SHA publicado en dev, verificación de Scope Lock/V5, lint, build, Vitest y Chromium CI con completed/success; las pruebas QUEUED/IN_PROGRESS/CANCELLED/SKIPPED son PENDIENTE, no PASS. Mantener estado VALIDANDO y owner si CI externa corre, registrar RUN/SHA. LISTO_DEV solo con A0–A4 y readback PLAN/COLA/DAG/BITACORA. A5 siempre independiente de otra Task Javier; métricas de uso NO_MEDIDO. No QA/main/PROD.
