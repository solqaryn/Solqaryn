# C11-04.M04.03 — Identidad visual tipada de observabilidad privada

Repositorio: solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK=STRICT. Autor: DEV03 Javier, 6adc2e4bd8a48191ac7d8641ed95a1a3. Riesgo R1: interfaz Angular local sin integración ni cambio de autorizaciones.

## Alcance y A0
La ruta privada /plataforma/observabilidad-v5 conserva plataformaGuard y la verificación de sesión de backend introducidos por M04.01/M04.02. Se agrega identidad visual local con dos variantes tipadas: institucional y neutra. Las cadenas y estilos se fijan en código, nunca se interpolan datos de tenant, configuraciones de servidor, HTML arbitrario o tokens. Un valor de entrada no reconocido cae a variante neutra; la selección visual NO modifica permisos.

Solo se tocan componente Angular, tests Vitest y Playwright, este contrato y CHANGELOG. No hay cambios de API, DTO, Domain/Application/Infrastructure, EF, migraciones, JWT, RBAC, CORS, rutas, proveedores, secrets ni datos reales.

## A1 y A2
Vitest dirigido añade cinco aserciones adversariales: identidad institucional autorizada; variante neutra sin solicitud HTTP; entrada inválida con fallback; anónimo/Empresa sin región; revocación inmediata sin identidad. Los diez tests previos de estados y guard se conservan. Chromium prueba variante institucional en 375px sin overflow y 403 sintético sin branding ni divulgación de mensaje. El backend sigue siendo la única fuente de autorización; no se concede identidad de empresa por configuración visual.

## A3 y A4
La CI dirigida existente v17-c11-04-m04-01-observabilidad.yml ejecuta Vitest ampliado, Angular lint/build y Playwright/Chromium de la misma ruta y sus negativos. Exigir resultado completed/success en SHA exacto, además de Scope Lock y procedencia V5; si está queued/in_progress, mantener VALIDANDO. MySQL 8.4, API HTTP real, proveedor de métricas y auditoría productiva: NO_APLICA porque el cambio es solo UI. Estas pruebas de navegador usan sesión sintética, NO certifican tenant A/B backend ni JWT HTTP.

## A5 y rollback
Revisión A5 independiente de una Task Javier con TASK_ID distinto. Mantener A5_PENDIENTE hasta revisión real de diff/tests/checks. Rollback por commit git revert normal sin force-push; no QA/main/PROD ni datos.
