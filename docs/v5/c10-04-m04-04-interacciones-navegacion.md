# SOLQARYN V17 — C10-04.M04.04: navegación y prevención de doble acción

**Autor:** PRIMARY DEV 02 JAVIER, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. **Riesgo:** R2 por UI de plataforma privada; `PROJECT_SCOPE_LOCK=STRICT`. PLAN fila 1571; V17 lock 2000001571. Rama exclusivamente dev.

## A0 — alcance, contratos y reversión

Extiende únicamente `ContinuidadOperativaV5Component`: su revalidación conserva una sola petición pendiente, oculta controles durante la espera, enlaza internamente a /plataforma, y no muestra formularios, restauración, rutas externas ni permisos nuevos. Corrige una falla de proveedor síncrono: `defer` convierte un error al invocar `PlataformaSesionService.obtener()` en el handler existente de error seguro, impidiendo un estado `cargando` permanente que bloqueaba el botón. No cambia JWT, autorización backend, routing, tenant, DB, APIs, servicios ni dependencias de terceros.

## A1 / A2 — afirmaciones adversariales

La suite `continuidad-interacciones-v5.spec.ts` exige: guía privada y enlace interno sin formulario; botón single-flight y `aria-busy`; respuesta nominal; 403 y 503 sin revelar payload; sesión revocada durante respuesta positiva; proveedor que falla de forma síncrona sin dejar botón bloqueado; anónimo sin controles. Conserva y ejecuta las pruebas previas M04.01/.02/.03. Ningún mock concede autoridad backend; los resultados de prueba no se atribuyen PASS antes de CI.

## A3 — integración pertinente

Workflow GitHub Actions de un solo scope: npm ci, Vitest real, lint + build Angular 22.2.1 y Chromium Playwright localhost con API sintética de /plataforma/sesion y la suite existente `c10-04-m04-02-continuidad-estados.spec.ts`. Es prueba real del navegador con sesión sintética; JWT HTTP real, MySQL/EF 8.4, proveedores, Cloudflare R2, restauración productiva y transacciones no están conectados y son NO_APLICA al diff.

## A4 / A5 — criterio de cierre

Publicación en una sola revisión fast-forward CAS a dev con componente, spec, workflow, este documento y sección CHANGELOG autocontenida. Requiere el check `C10-04.M04.04 - Vitest, Angular y Chromium` y los controles pertinentes Angular, seguridad, Scope Lock exact-SHA y procedencia V5 en SUCCESS (o descendiente con blobs idénticos demostrados). Mientras CI siga queued/in_progress, conservar VALIDANDO y owner, no marcar LISTO. Revisión A5 únicamente por otra Task Javier de autoría distinta y readback PLAN/COLA/DAG/BITACORA. Rollback mediante git revert normal; nunca QA, main ni producción.
