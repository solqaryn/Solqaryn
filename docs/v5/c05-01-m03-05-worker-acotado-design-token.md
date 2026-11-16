# SOLQARYN V17 — C05-01.M03.05: worker acotado de previsualización de design tokens

Autor: PRIMARY DEV 06, TASK_ID=6af16bbad26881918c283abe06e1f32a. Repositorio solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK=STRICT; R2 por pertenencia tenant, permisos, revocación y concurrencia.

## Entrega

C0501DesignTokenBoundedReviewWorker es una fachada Infrastructure .NET 10 real, secuencial y en memoria para inspeccionar hasta 12 intenciones sintéticas de design tokens por petición. Usa el adaptador aislado C05-01.M03.04, el servicio Application C05-01.M03.02 y reglas Domain C05-01.M03.01. Antes de consultar el adaptador valida todas las entradas, actor autenticado observado, tenant exacto, epoch de sesión, revisión, SHA origen, claves ordinales, duplicados y prohibición de escrituras. Si una entrada no es válida o está revocada, el lote completo se deniega sin devolver resultados parciales. Las respuestas sólo contienen posición y código de requerimiento de nueva revisión autenticada; no revelan claves, valores CSS, identificadores tenant, JWT ni PII.

No instala hosted worker, no aplica CSS/temas, no autentica JWT, no concede RBAC, no toca MySQL, no autoriza operaciones de frontend, no acredita contraste/accesibilidad ni despliega. La decisión efectiva de aplicación de tema requerirá un adaptador backend autenticado separado. No se fabrican resultados del navegador.

## R2 — Evidencia A0–A5

A0: código Infrastructure + xUnit + documentación y CHANGELOG, sin interfaces API ni migraciones. A1: asserts positivos, negativos, errores, fronteras y cancelación con rechazo por lote atómico. A2: empresa A/B, actor ajeno, revocación, rol, epoch, revisión y SHA fallan cerrado; sin fuga de datos. A3: CI .NET 10 Release/xUnit real en GitHub-hosted; MySQL/Playwright/HTTP NO_APLICA para este diff aislado. A4: Scope Lock, procedencia V5, build, backend/security/auth pertinentes exact SHA y PLAN/COLA/BITACORA con readback. A5: revisor Javier con TASK_ID diferente, sin autoaprobación. Rollback git revert normal; QA/main/PROD fuera de alcance.
