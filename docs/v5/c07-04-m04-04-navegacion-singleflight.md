# C07-04.M04.04: navegación y prevención de doble acción en pedidos ERP

Autor: PRIMARY DEV 05, TASK_ID 6adc2e55f47c81919f6374e561687076. Repo solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK=STRICT. PLAN fila 1139, V17 lock 2000001139. Riesgo R2 por permisos de pedidos.

A0: en ruta existente /pedidos-venta se evita reentrada en cargar() con single-flight y se deshabilitan filtro/paginación durante GET. No se tocan API, backend, DTO, pagos, fiscalidad, reserva de inventario, JWT, datos ni MySQL. Rollback git revert en dev sin force-push.

A1: cinco pruebas browser Chromium con asserts reales de anónimo, permiso Ventas:Ver denegado, navegación listado-detalle-regreso de lector sin formularios ni acciones, error 503 saneado y reintento sin segundo GET, paginación in-flight sin duplicación. A2: no hay acciones mutadoras accesibles al lector y guard, tenant y roles existentes no se cambian. Los tokens, tenant, permisos y respuestas HTTP de la suite son sintéticos; no prueban backend JWT, tenancy A/B, fiscalidad ni pagos reales.

A3: Playwright/Chromium GitHub-hosted con Angular 22.2.1, npm ci, lint y build, incorporando pruebas nuevas y anteriores al mismo workflow SHA-aislado. DB/EF/MySQL y proveedores externos NO_APLICA en cambio exclusivo UI de lectura; no afirmar integration runtime real. A4: checks required Scope Lock, V5, browser, seguridad y frontend SUCCESS exact SHA o equivalencia de blobs verificada, BITACORA y Sheet readback. Queued, skipped, cancelled, in_progress y failed NO PASS. A5: revisión independiente otra Task Javier, no QA/main/PROD.
