# C07-01.M04.03 — Marca verificada del catálogo universal V22

## A0: acción atómica (PLAN_MAESTRO!D1030)
Aplicar a la lista de Productos la marca dinámica **ya verificada** mediante `EmpresaIdentidadService.nombreSistema` y el diseño CSS existente de SOLQARYN. La marca es solo presentación; no habilita lectura de productos, asignación de empresa, JWT/RBAC ni altera estado de cuenta. No tocar QA, main, producción, pagos ni datos. Riesgo R2 por contexto empresarial. Autor: DEV06 Javier, Task ID `6af16bbad26881918c283abe06e1f32a`.

## Alcance de arquitectura y seguridad
- Frontend Angular 22.2.1: componente ProductosList, template y SCSS; señal reactiva conectada al servicio existente, sin nuevas API ni dependencias.
- La única autoridad tenant continúa en `TenantContextService` y backend; el shell llama `EmpresaIdentidadService.cargarTenant` solo después de la verificación real del contexto. Nombres hostiles o respuestas malformadas fallan cerrado a SOLQARYN. La UI nunca muestra nombre crudo mediante `innerHTML`.
- CSS respeta las variables actuales y 320/1280 px; no oculta controles existentes, no cambia permisos ni rutas.
- Rollback: revert normal y fast-forward del commit propio, conservando cambios concurrentes, sin reset ni force-push.

## A1/A2/A3: pruebas adversariales
Nueva suite GitHub-hosted Playwright/Chromium en Angular loopback: marca válida A, marca válida B sin herencia, contenido hostil con salida neutra y ausencia de elementos HTML inyectados, navegación anónima y tenant/rol denegados, pantalla móvil 320 y escritorio 1280; contador de cero operaciones mutantes. Los mocks son **sintéticos**; NO son evidencia de JWT firmado, HTTP real ni MySQL 8.4. La garantía runtime existente corresponde a los servicios de auth y tenancy no modificados.

## A4/A5 y limitaciones
Requeridos para cerrar únicamente esta microtarea: workflow dirigido con 4 casos Chromium ejecutados, `tsc`/Angular build, checks de Scope Lock y V5 pertinentes `completed/success` en SHA causal, readback Sheets PLAN/COLA/BITACORA y revisión A5 posterior por Task Javier distinta de DEV06. `queued`, `in_progress`, `skipped`, `cancelled`, `failed` no son PASS. No se certifica toda la fase M04, ni IAM/DB/HTTP fuera de diff. CHANGELOG_AI.md histórico intacto por tamaño y preservación de atribución, evidencia documentada aquí y en BITACORA.
