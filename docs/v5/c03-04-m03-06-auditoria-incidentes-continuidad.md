# SOLQARYN V17/V15 — C03-04.M03.06 auditoría y errores seguros de continuidad

**ID:** C03-04.M03.06, fila PLAN 559. Autor PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. Rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`, clasificación R2 (incidentes, autenticación, tenancy y datos potencialmente sensibles).

## A0 — impacto y contrato
Implementación real `C0304IncidentContinuityStructuredAuditSink` en Infrastructure .NET 10, que consume los recibos tipados de `C0304IsolatedIncidentReadOnlyDispatchWorker` de Application (M03.05). Emite `ILogger` de evento fijo 30406 para una previsualización aislada de incidentes, sin registrar DI/hosted service, endpoints, bases, workers live, storage, backups, cola externa ni secretos. Comprueba lote finito de 1–24, códigos tipados conocidos, tenant/actor positivos, tenant A/B congruente, SHA hexadecimal minúscula de 40 caracteres, revocación, prohibición de autoridad/restauración cliente y correlación no vacía. La evidencia recibida es contrato para un FUTURO backend confiable; el flag de prueba NO autentica JWT ni verifica base de datos.

Un resultado nominal es exclusivamente `INCIDENT_PREFLIGHT_ONLY_NO_PERMISSION`. Todos los recibos declaran sin acceso a incidentes, mutación, descargas, workers, restauración, persistencia durable o despliegue. No es bitácora institucional ni auditoría de cumplimiento. Rollback: `git revert` normal del SHA publicado, preservando BITACORA y sin force-push.

## A1/A2 — pruebas que pueden fallar
13 métodos xUnit: nominal sin permisos, denegación de evento conocido, código arbitrario sin filtración, tenant A/B y revocación, SHA stale/caso, correlation, override de restore/autoridad, batch vacío/nulo/>24, contexto nulo, composición real in-memory con worker Application, logger deshabilitado, logger con excepción, cancelación y constructor inválido. Los campos sensibles (actor, tenant, idempotencia, SHA, razón, excepciones, JWT, PII) jamás se escriben; salida fija de tres parámetros (outcome, correlación sólo verificada, conteo limitado). Pruebas y build .NET Release, seguridad multiempresa y autorización se acreditan únicamente con logs GitHub Actions para SHA causal.

## A3 — integración y límites
Prueba de integración **in-process** Infrastructure→Application usando el worker real con un lote denegado; no se invoca ni certifica MySQL 8.4, EF/migraciones, API HTTP, tokens JWT firmados, Chromium, proveedores, backups/restores, Durable Audit ni entorno productivo. Estas superficies son **NO_APLICA** porque el diff no las modifica ni invoca; al conectarse una fuente real exigir nuevo scope R2 con integración pertinente.

## A4/A5 — cierre
Publicar un único changeset fast-forward/CAS en `dev` con clase, pruebas, este documento y sección autocontenida de CHANGELOG. Pasar a `VALIDANDO`, registrar EVENT_ID único de 26 columnas y readback institucional. Para `LISTO_DEV` exigir Backend Release/xUnit, build, seguridad multiempresa, Authorization, Scope Lock y procedencia V5 `completed/success` en SHA exacto o equivalencia probada, con P0/P1 del scope sin fallos. `A5_PENDIENTE` hasta revisión independiente de otra Task Javier; sin QA/main/PROD.
