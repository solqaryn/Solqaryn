# SOLQARYN V17 — C07-04.M04.05: accesibilidad pedidos y documentos

Riesgo **R2**: contexto financiero/fiscal, pero alcance del diff limitado a pruebas frontend y workflow CI aislado. Autor de la implementación de pruebas: DEV05 (TASK_ID `6adc2e55f47c81919f6374e561687076`). Rama `dev`, PROJECT_SCOPE_LOCK=STRICT. A5 por otra Task Javier.

## A0

La unidad verifica la ruta ERP existente `/pedidos-venta` sin alterar servicio/API/DTO, persistencia, JWT, permisos, auditoría, caja, pagos, facturas ni comportamiento fiscal. El panel conserva `authGuard`, `permisoGuard`, `Ventas:Ver`, tenant de servidor verificado y `NO_MEDIDO` para integración financiera. La entrega agrega cuatro pruebas Playwright sobre una superficie existente, con un workflow SHA-scoped no cancelable por otro SHA. Las pruebas no implican cambios de permisos ni nuevos endpoints.

## A1–A3

- Prueba positiva: panel read-only con cuatro criterios y viewport de 320/768/1280 sin desplazamiento global horizontal.
- Teclado real: Tab desde documento hasta filtro; comprobar foco visible del control sin simular `.focus()`.
- Contraste elevado: comprobar que el panel mantiene bordes sólidos y texto computado no transparente. Esto **no** equivale por sí solo a una auditoría completa de ratio WCAG.
- Negativos: acceso anónimo y permiso de ventas denegado, sin exponer tarjetas ni controles transaccionales.
- Reutiliza Vitest de permisos, tenant, revocación y ruta de M04.01; los interceptores E2E son sintéticos, no demuestran JWT real, aislamiento A/B del backend, PSP ni MySQL.
- No se modifican EF, MySQL ni contratos de persistencia: integración MySQL 8.4 NO_APLICA a este diff. La ejecución real de Chromium en GitHub-hosted es obligatoria.

## A4–A5 y continuidad

Exigir GitHub Actions del SHA publicado: Vitest, build/lint Angular, Playwright/Chromium 4/4, Scope Lock, V5 y controles pertinentes terminados SUCCESS. Cualquier fallo obliga reparación y nueva CI, jamás conversión de SKIPPED o IN_PROGRESS en PASS. Registrar BITACORA y readback dirigido de PLAN/COLA/DAG; solo otra Task Javier distinta podrá registrar A5_PASS. Rollback mediante git revert normal después de revalidar HEAD, sin force-push ni afectar QA/main/PROD.

Estado de publicacion: test Playwright SHA `b9af7f8a6ddf9a093018e9b3be82397495a69249` y workflow SHA `61b7871e451cd52bf0f923ea30c902caf9f10841` publicados en dev. **CI requerida pendiente de evidencia A0–A4, NO LISTO y A5 pendiente** hasta leer checks exact-SHA y readback institucional; no confundir entrega con aprobacion.
