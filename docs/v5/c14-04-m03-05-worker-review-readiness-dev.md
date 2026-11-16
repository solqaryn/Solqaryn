# SOLQARYN V17 — C14-04.M03.05 worker acotado de revisión de readiness DEV

**Autor:** PRIMARY DEV 01 JAVIER, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a. Rama `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Requisitos: PLAN_MAESTRO C14-04.M03.05, COLA G/O=SI, DAG F=VALIDADO/G=SI, predecesoras M02 y M03.01–04/A5 según DAG real. Riesgo **R2**, no R3. CLAIM V17 exclusivo `SQV17_C14_04_M03_05`, ID `2000002142`.

## A0 — Entrega concreta y límites
- Implementación .NET 10 de `V5ReleaseReadinessBoundedReviewWorker`, uno por invocación manual, con `IV5ReleaseReadinessReviewInbox` para obtener **hasta 16** candidatos de fuente backend autenticada. No hay reader/provider/scheduler real registrado ni tareas nuevas. No crea colas, timers, webhook, event dispatcher, redes, MySQL, migraciones, backups ni acceso a R2/producción.
- Prevalida **todos** los identificadores, claves idempotentes, releases y SHA de 40 hex minúsculos antes de cualquier lectura; rechaza duplicados en el mismo lote, rutas inseguras, fuentes no autenticadas y entornos distintos de `dev`. La clave de deduplicación es **solo intra-lote**, sin persistencia ni garantía cross-run/exactly-once.
- Integra en forma secuencial el existente `V5ReleaseReadinessIsolatedSnapshotAdapter.InspectAsync`, que revalida fuente autenticada, membresía/RBAC, tenant backend, autor/revisor distintos, Scope Lock/CI, snapshot inmutable/auditable y plan de rollback. Un resultado apto sigue requiriendo revisión externa y A5; jamás autorización de despliegue.
- Domain y otros servicios no se modifican. No se expone payload ni PII. Errores en el reader se devuelven como códigos opacos; cancelación se propaga. Rollback Git normal `git revert` en dev sin borrar eventos institucionales.

## A1–A3 — Riesgo y pruebas reales
- `14` pruebas xUnit `[Fact]` positivas/negativas con integración **real en proceso** de Inbox sintético → Worker → Adapter → Application/Domain: caso nominal sin efecto, dos unidades ordenadas, ausencia de fuente, dev-only, lote vacío/17, colisión de ID/clave, SHA/path inválido, tenant B/membresía revocada, CI CANCELLED/SKIPPED/FAIL, self-review, fallo opaco de inbox/adaptador, cancelación y constructor fail-closed.
- A2: barreras JWT/RBAC/tenant reales de un backend futuro **NO se certifican** mediante estos mocks. El port requiere un proveedor confiable todavía ausente; el adapter existente valida autoridad y evidencia sintéticas conforme a su contrato.
- A3: integración in-process .NET 10 xUnit; **NO_APLICA** a diff para ejecución MySQL 8.4 real, EF/migraciones, navegador Playwright, OAuth/HTTP, almacenamiento durable, watchdog, API de proveedores, SLO, DNS/Cloudflare, producción o restauración real, porque no se modifican ni conectan.
- A1/A2/A4 solo se acreditan tras checks requeridos del SHA exacto: Backend Release .NET xUnit, Build backend, Seguridad multiempresa, Authorization, Scope Lock y procedencia V5 en `completed/success`. Nada `queued`, `skipped` ni `cancelled` es PASS.

## A4–A5 — Estados y separación
- Publicar una sola entrega con fuente, pruebas, este documento y sección CHANGELOG autocontenida mediante fast-forward/CAS a dev; transicionar a VALIDANDO antes de acreditar A1–A3, registrar RUN/check/SHA y readback institucional; conservar owner mientras GitHub Actions ejecute CI.
- A5 independiente **otra Task Javier** (distinto TASK_ID del autor) revisará código, ausencia de efectos, autenticación/tenancy, pruebas adversariales y SHA. No autoaprobar, no elevar permisos, no escribir QA/main/PROD. El processor es un handler manual de consulta, no un scheduler o mecanismo de mantenimiento de producción.
