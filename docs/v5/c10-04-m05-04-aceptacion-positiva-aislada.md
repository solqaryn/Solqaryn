# C10-04.M05.04 — aceptación positiva aislada, DEV

**Gobierno:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT; 6 Tasks Javier.
**Riesgo:** R2 (cache, jobs, backup, export, multiempresa). **Entrega:** pruebas reales .NET 10, sin activar runtime, proveedores ni datos externos.

## A0: alcance y dependencias

PLAN_MAESTRO fila 1577, H=PENDIENTE/I vacío en el CLAIM; COLA G/O=SI, DAG F=VALIDADO/G=SI. La fase M04.01–06 y sus A5 conforman la precondición calculada. Se prueba exclusivamente el pipeline ya existente Domain -> Application -> Infrastructure de **revisión aislada en memoria**; no se crean endpoints, mutaciones, permisos, migraciones o cambios de arquitectura. El candado institucional V17 es solo exclusión del trabajo de desarrollo, nunca prueba de CAS de negocios.

## A1–A3: ejecución y delimitaciones

GitHub-hosted .NET 10 ejecuta `C1004CacheJobsM0504PositiveAcceptanceTests` junto con las regresiones de worker y reglas ya versionadas. Casos positivos: export.request y cache.invalidate allowlisted; dos operaciones diferentes y ordenadas; último reintento permitido con service grant *sintético*; replay idéntico no ejecutable. Casos adversariales: digest cambiado, tenant B rechazado sin envenenar tenant A, modificación posterior del allowlist, cancelación previa. Todos los veredictos deben certificar **ausencia** de enqueue externo, lease, escritura, lectura privada, restore o acceso cruzado.

A3 pertinente: integración de clases reales .NET (Domain/Application/Infrastructure) en test host GitHub, NO integración de proveedor externo. **NO_APLICA por ausencia de cambios de EF/DB/UI/HTTP**: MySQL efímero, API con JWT firmado, Playwright, Cloudflare R2 vivo, restauración duradera y worker productivo. Ningún PASS de xUnit acredita esas capacidades. Una restauración o acceso R3 real exigiría orden y gate independientes. No se presenta esta prueba como aceptación global de la funcionalidad de backups.

## A4, A5 y rollback

Antes de LISTO_DEV: verificar commit fast-forward en dev, run y job exact-SHA de este workflow con `completed/success`, tests realmente ejecutados, Scope Lock, V5/procedencia, backend Release y seguridad pertinentes; leer PLAN/BITACORA y conservar owner. Nunca aceptar `queued/in_progress/skipped/cancelled/failure` como PASS. Revisión A5 independiente por otra Task Javier, sin autoaprobar. Rollback: `git revert` normal del commit publicado, tras verificar HEAD y exclusión. No modificar QA/main/PROD, secretos, R2 vivo, restauraciones o datos productivos.
