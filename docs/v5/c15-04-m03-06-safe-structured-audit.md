# C15-04.M03.06 — auditoría estructurada y errores opacos de UX (DEV)

**Propietario:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Scope:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`; PLAN fila 2287. **Riesgo:** R2, logging/tenancy y manejo de errores.

## A0: resultado técnico auténtico

Se añade `V5UxAuditStructuredEventSink` en Infrastructure, consumidor de `V5UxAuditBoundedReadOnlyWorker.BatchOutcome` creado en M03.05. No cambia la semántica de la evaluación preexistente ni introduce controladores, servicios hospedados, schedulers, migraciones, API, proveedores externos, cambios de autenticación, operaciones institucionales o acceso productivo. Usa `ILogger` existente y eventos estructurados con ID fijo 150406, sólo nivel y conteo acotado (0–32), sin guardar tenant, actor, SHA, tokens, claves, referencias o errores crudos.

Una respuesta `Recorded=true` significa únicamente que el proveedor `ILogger` habilitado retornó sin excepción. **NO** acredita entrega/durabilidad externa, auditoría institucional, métricas WCAG, Playwright, JWT/RBAC real, lectura MySQL, autorización de tenant ni A5. Para niveles deshabilitados o proveedor nulo, `Recorded=false`; excepciones quedan opacas. El evento de staging siempre reconoce `EXTERNAL_EVIDENCE_AND_A5_REQUIRED`; ninguna bandera concede acceso. El contrato rechaza outcomes falsificados, índices o conteos inconsistentes, lotes de más de 32 y declaraciones de certificado externo.

## A1–A3: validación causal proporcional

xUnit Release dirigido comprueba staging, rechazo, lote con denegación, outcomes sintéticos falsificados, límites de capacidad, logging deshabilitado, `NullLogger`, excepciones en `IsEnabled` y `Log`, cancelación y ausencia de datos sensibles. A2: separación tenant/autoridad permanece en Application/worker preexistente; el logger no consume contexto tenant ni concede accesos. A3: integración real sólo contra `ILogger` in-process mediante el runner .NET 10. Base de datos MySQL 8.4, JWT HTTP, navegador Chromium y proveedores de observabilidad persistente son **NO_APLICA** al diff; jamás afirmar certificación de esos sistemas.

## A4, revisión A5 y reversión

Requiere CI GitHub-hosted del SHA exacto: `verify-solqaryn-project-scope`, V5 provenance, backend Release, seguridad/authorization pertinentes y workflow dirigido `C15-04.M03.06 - Real .NET 10 xUnit structured logging`, todos `completed/success`, readback PLAN/COLA/DAG/BITACORA y ausencia de P0/P1 nuevos. `SKIPPED`, `CANCELLED`, `QUEUED`, `IN_PROGRESS` no son PASS. Un descendiente requiere equivalencia de blobs y dependencias demostrada.

A5 sólo por otra Task JAVIER con TASK_ID diferente y revisión propia del diff, asserts y CI. Reversión por `git revert` normal, sin forzar rama, borrar BITACORA, tocar QA/main/PROD ni secretos.
