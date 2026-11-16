# SOLQARYN V18 — C08-04.M03.06 auditoría estructurada y errores seguros

**Scope:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Autor:** PRIMARY DEV 05, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Fila:** PLAN_MAESTRO 1279. **Riesgo R2** por auditoría, tenant, permisos y contrato de proveedor. CLAIM exclusivo V17 `SQV17_C08_04_M03_06` / `2000001279`.

## A0 — implementación real y límites

Se añade `C0804SectorModuleAuditBoundary` en Application como envoltura fail-closed del probe de solo lectura de M03.05, sin modificar autorización ni Domain. Exige un puerto `IC0804SectorModuleAuditWriter` antes de consultar la fuente, registra ATTEMPT y OUTCOME vinculados por un identificador aleatorio y nunca devuelve readiness positiva si no pudo registrar la salida. El registro es tipado y deliberadamente mínimo: correlationId, fase, operación clasificada, código de resultado acotado. **No se registra** identidad, tenant, clave de módulo, SHA, token, secreto, mensaje de excepción o contenido de proveedor. Se devuelve únicamente un código estático sin autorización. Cancelación se propaga, fallo de auditoría o proveedor no concede acceso ni certificación.

Esta es una frontera .NET real **en proceso** con puerto de auditoría inyectable. No existe implementación DI ni sink durable registrado, acceso a logs productivos, API, HTTP/JWT, DB, EF/MySQL, cola, worker alojado, proveedor externo o migración. No declarar auditoría persistente hasta un alcance independiente con implementación real, retención y control de acceso; tampoco usarla como autorización para activar módulos.

## A1–A3 — evidencia y pruebas

La suite `C0804SectorModuleAuditBoundaryTests` comprueba éxito de sondeo no autorizante con dos eventos seguros, rechazo pre-I/O de dispatch cliente, tenant B y rol revocado, rechazo cuando falla el evento ATTEMPT, denegación del resultado si falla OUTCOME, excepciones de proveedor sanitizadas, cancelación previa y dependencias nulas. Validar xUnit real en .NET 10 Release mediante GitHub Actions, controles de seguridad/tenancy, Scope Lock y procedencia V5 en SHA causal. Las pruebas usan fuentes sintéticas para demostrar límites in-process, **no** proveedor ni persistencia real. En este diff, MySQL Category=Integration, JWT HTTP y Playwright son NO_APLICA (no tocados), no se reportan como PASS.

## A4–A5, continuidad y rollback

A4 exige `verify-solqaryn-project-scope`, V5, backend Release, seguridad y autorización `completed/success` en SHA exacto (o descendiente equivalente demostrado), diff y blobs cotejados, readback PLAN/COLA/DAG/BITACORA. Mientras CI esté QUEUED/IN_PROGRESS: permanecer VALIDANDO con owner, RUN y SHA. A5 requiere otra Task Javier distinta del autor y revisión independiente de código y pruebas. Reversión por commit `git revert` ordinario en dev, conservando BITACORA. No QA, main, producción, secretos ni cambios a programaciones.
