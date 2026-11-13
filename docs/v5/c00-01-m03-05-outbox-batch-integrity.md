# C00-01.M03.05 — Integridad del lote de despacho outbox

**Autor:** PRIMARY DEV 06 — JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fuente:** V5 FINAL; repositorio `solqaryn/Solqaryn@dev`; seguridad Scope Lock STRICT.

## Riesgo y cambio

El procesador de reintentos `OutboxRetryProcessor.ProcesarLoteAsync` validaba el tenant de todos los claims, pero un adaptador de repositorio defectuoso podía devolver el mismo `EventoId` o `ClaveIdempotencia` repetido dentro del mismo lote. Eso podía disparar efectos externos más de una vez antes de la detección de confirmaciones supersedidas. El cambio valida **el lote completo antes del primer dispatch** y rechaza evento vacío, clave vacía, evento duplicado o clave duplicada con comparación ordinal. Mantiene las garantías existentes de aislamiento entre empresas A/B, auditoría allow-listed, backoff, dead letter y cancelación.

## Impacto

Capa Application: `backend/src/Application/Services/OutboxRetryProcessor.cs`, pruebas en `backend/tests/Solqaryn.Tests/N72DOutboxRetryProcessorTests.cs`. No se crea scheduler, nuevo proveedor, transacción ni migración MySQL; no se modifican DTO/API, JWT, RBAC, datos, Cloudinary, SMTP o Angular. La validación es O(n) y BatchSize <= 200.

## Evidencia exigida

- Positivo: lote de dos eventos distintos con claves diferentes entrega cada uno una sola vez.
- Negativos: mismo evento duplicado o clave reutilizada entre dos eventos no invoca dispatcher, no confirma ni registra un falso fallo.
- A0 diff, análisis de secretos, dependencias y código muerto dirigido; A1 xUnit positivo/negativo; A2 tenancy A/B y denegación por defecto; A3 .NET 10 y MySQL 8.4/Playwright como regresión relevante por GitHub Actions exact-SHA; A4 fast-forward, runs/jobs/logs, PLAN/COLA/BITACORA; A5 por Task distinta.
- Rollback: `git revert` normal, sin force-push ni cambios de datos. No se contactan proveedores productivos.

**Límite de pruebas:** los casos dirigidos usan FakeRepository/CountingDispatcher para observar el efecto y no son prueba de una entrega real por proveedor externo; CI MySQL/Playwright complementa con integración cuando corresponda.
