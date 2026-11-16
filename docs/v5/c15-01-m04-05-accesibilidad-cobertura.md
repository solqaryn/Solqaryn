# SOLQARYN V17 — C15-01.M04.05 accesibilidad de cobertura y trazabilidad

**Autor:** PRIMARY DEV 03 Javier; TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Rama:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`; fila PLAN_MAESTRO 2184, candado V17 2000002184.

## A0 — alcance, impacto y reversión
Microtarea habilitada por COLA!G/O=SI y DAG_TECNICO F=VALIDADO/G=SI; puerta dependiente de M03/A5 y C00-03.M06.06/A5 ya calculada. Se amplían **únicamente** las pruebas Chromium de `/plataforma/trazabilidad-cobertura-v5` y su workflow SHA-aislado. No se cambian componentes runtime, rutas, guards, DTO, backend, JWT/RBAC, tenancy, datos, MySQL, configuraciones productivas ni permisos de negocio. Riesgo R2 conservador por superficie interna y denegación sintética. Reversión mediante `git revert` del changeset sobre dev, sin force-push.

## A1–A3 — oráculos negativos y positivos
Cinco escenarios Playwright adicionales: rechazo anónimo/403/ámbito Empresa sin exponer dimensiones; responsive 320/375/768/1280 sin overflow; labels y estados semánticos sin acciones de escritura ni resultado falsificado; navegación Tab real con foco visible >=3px y blanco táctil >=44px; contraste computado del texto de estado >=4.5:1. Se conservan tres E2E previos y cuatro Vitest, que se ejecutan en GitHub-hosted con Angular lint/build. Las pruebas pueden fallar ante defectos reales de accesibilidad, responsive, permisos simulados o contraste.

**Límite de seguridad:** sólo sesiones HTTP simuladas en navegador. No acreditan JWT firmado, MFA, revocación backend, tenant A/B reales, Oracle EF/MySQL, APIs de producción, auditoría institucional ni métricas globales. Integración pertinente: Playwright/Chromium real + lint/build Angular; MySQL/EF/proveedores = NO_APLICA porque no se modifica ni invoca persistencia o proveedor.

## A4–A5 y continuidad
El éxito requiere GitHub Actions `completed/success` de E2E dirigidos + Scope Lock + V5 y checks pertinentes del SHA publicado o equivalencia documentada de blobs. `SKIPPED`, `CANCELLED` y `IN_PROGRESS` no son PASS. PLAN/COLA/DAG/BITACORA requieren readback tras transición. A5 por **otra** Task Javier, nunca por autor. Ninguna promoción QA/main/PROD; `NO_MEDIDO` permanece veraz hasta comparación real institucional.
