# C08-04.M05.05 — Casos negativos de abuso y aislamiento UX

Responsable: PRIMARY DEV 05, TASK_ID 6adc2e55f47c81919f6374e561687076. Rama dev; PROJECT_SCOPE_LOCK=STRICT. PLAN fila 1290; DAG fila 1251. Riesgo R2 por seguridad de pruebas y workflow GitHub Actions.

## A0 — Alcance y rollback
Pruebas Playwright/Chromium nuevas sobre la vista informativa de extensiones sectoriales. Los componentes, API, DTO, JWT, RBAC, guards, base de datos, permisos, migraciones y datos no cambian. El contrato real no concede activaciones: catálogo de cuatro ámbitos ilustrativos con etiqueta "No conectado". Rollback normal mediante git revert, nunca force-push ni cambio de main/QA/PROD.

## A1/A2 — Pruebas adversariales falsables
Cinco escenarios: anónimo no abre ruta protegida; 401 de tenant-context no abre contenido; selección de empresa B denegada 403 no hereda A; 503 devuelve login seguro y no muestra mensaje privado; bandera local falsa de módulo habilitado no concede permisos, no emite escrituras ni solicitudes a proveedores de capacidades y solo ve cuatro tarjetas ilustrativas. Aserciones explícitas para navegación, texto, etiquetas, ausencia de detalles y HTTP. Las respuestas tenant-context y JWT de prueba son fixtures del navegador, NO certifican servidores reales, rechazo IDOR/BOLA backend, JWT, RBAC o aislamiento A/B con MySQL.

## A3/A4 — Evidencia exigida
GitHub-hosted instala Node/Angular/Chromium, ejecuta npm lint y build productivo más cinco casos Playwright reales. CI por SHA y concurrencia cancel-in-progress=false. Requerir Scope Lock, V5, checks obligatorios completed/SUCCESS de SHA exacto o descendiente con blobs idénticos, readback PLAN/COLA/DAG/BITACORA. QUEUED/IN_PROGRESS/CANCELLED/SKIPPED/FAIL no son PASS. MySQL, EF, proveedores reales y métricas Core Web Vitals NO_APLICA/NO_MEDIDO por ausencia de cambios y de pruebas live.

## A5 y handoff
A5 exclusivamente por otra Task Javier distinta de DEV05 y revisando código/pruebas/logs por sí misma. No QA/main/PROD ni grant nuevo. No pasar de VALIDANDO a LISTO antes de A0-A4 verdaderos.
