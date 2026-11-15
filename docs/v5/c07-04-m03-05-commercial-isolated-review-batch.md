# SOLQARYN V17/V15 — C07-04.M03.05 bounded commercial review batch

**ID:** C07-04.M03.05; PLAN_MAESTRO fila 1134; autor Task Javier DEV05 6adc2e55f47c81919f6374e561687076. Rama dev, PROJECT_SCOPE_LOCK=STRICT, V17 named-range lock 2000001134. **Riesgo R2:** facturas, pagos, estado documental, multi-tenancy, RBAC, idempotencia, no producción.

## A0 — Implementación y dependencias

Se incorpora a Infrastructure la clase real V5CommercialDocumentIsolatedReviewBatch, un procesador de revisión **sin hosted worker** y limitado a 16 tickets por llamada. Reutiliza el adaptador de lectura M03.04, el guard Application M03.03 y las reglas Domain M03.01; no cambia contratos de terceros. Cada ticket pasa validación de recurso/SHAs/idempotencia y luego una lectura actual de identidad, permiso y documento filtrado por tenant. Detecta duplicados idénticos y conflictos por igual clave con distinto contenido dentro del lote, sin conceder autorización por reenviar un comando. Resultados son recibos no privilegiados, no payload de cobro, datos privados ni órdenes persistidas.

**Límite fundamental:** no crea cola durable, no realiza procesamiento en segundo plano, no ejecuta worker hosted, proveedor PSP/fiscal, SQL, EF, MySQL, migraciones, HTTP, firma fiscal, exportación, pagos, anulaciones ni operaciones contables. Una respuesta favorable demanda un **adaptador autenticado y transaccional independiente** que revalide autorización vigente, identidad, tenant, replay/idempotencia durable y CAS al momento del efecto. No se infiere certificación de pago o factura por preflight.

## A1/A2 — prueba .NET 10 y seguridad

Diez xUnit de casos nominales sin grant, lote nulo/vacío/excesivo, ticket/sha/digest malformados, replay duplicado, conflicto de hash/documento, tenant B persistido, membresía/roles revocados, versión/monto/divisa/operación fiscal rechazados, máximo 16 y cancelación. Exigir GitHub Actions Release con resultado exact-SHA, build, seguridad multiempresa y Authorization files/secrets; no declarar pruebas no ejecutadas como PASS. Los ensayos de PSP/fiscal en proveedor real y de MySQL 8.4 deben hacerse al implementar sus adaptadores.

## A3/A4/A5

Al no tocar MySQL/EF, HTTP, UI, DI ni desplegar worker, integración live de DB, Playwright y PSP es NO_APLICA motivado a este changeset, no un PASS de ellos. Requiere Scope Lock STRICT, V5 procedencia, CI pertinente, CHANGELOG autocontenido, diff y readback PLAN/COLA/DAG/BITACORA. Si CI sigue queued/in_progress, conservar VALIDANDO owner DEV05 y event idempotente. A5 revisión independiente por otra Task Javier. Rollback git revert normal preservando HEAD concurrente, no force-push, sin QA/main/PROD o secretos.