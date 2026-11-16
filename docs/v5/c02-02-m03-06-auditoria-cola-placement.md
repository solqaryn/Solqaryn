# SOLQARYN V17/V15 — C02-02.M03.06 auditoría segura de placement aislado

ID `C02-02.M03.06`, fila PLAN 343. Autor PRIMARY DEV06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`, riesgo R2 tenancy, concurrencia, permisos y datos de infraestructura.

## A0 — contrato ejecutable sin autoridad viva
Se incorpora `C0202PlacementStructuredAuditSink` Infrastructure .NET 10. Consume `C0202PlacementIsolatedDispatchQueue.Outcome` (M03.05) y contextos sintéticos acotados, redacta resultados para `ILogger` de evento fijo 202206 y nunca hace enrutamiento, migración, reserva de base, escritura MySQL, hosted worker, HTTP ni actualiza el plan. Valida tenant/actor sintéticos comparados ordinalmente, SHA canónico, membresía simulada vigente, revocación, correlation ID y consistencia del outcome de la cola. **El flag de contexto no certifica autenticación JWT real ni autoridad de almacenamiento**. No se inyecta en servicios productivos ni se añade endpoint, credencial o dependencia.

El registro sólo contiene código fijo, correlación no vacía si contexto válido y conteo sintético en rango 0–1. Nunca serializa tenants, actor, DSN, JWT, SHA, idempotency key, payload, razones externas, excepción, PII ni metadatos sensibles. Una respuesta nominal declara `SYNTHETIC_PREFLIGHT_ONLY_NO_AUTHORITY`; todos los recibos tienen grants de acceso, worker, DB, migración, auditoría durable, QA y deploy en **false**. Reversa: `git revert` ordinario sin sobrescribir commits ajenos ni borrar BITACORA.

## A1/A2 — pruebas dirigidas
13 métodos xUnit positivos y negativos capaces de fallar: nominal sólo preview, denegación conocida, código inyectado, tenant B/actor B, membresía revocada, SHA alterado o en mayúsculas, correlation vacía, intento de DSN/cutover, identidad mala o contexto nulo, conteo inconsistente, límite sintético, logger deshabilitado, excepción opaca, cancelación y dependencia nula. Dependencia real in-process de tipos Infrastructure de la cola; no falsificar JWT firmado.

## A3 — límites reales
No se modifican EF, MySQL, migraciones, frontend, HTTP, broker ni proveedores, por lo que MySQL 8.4 Category=Integration, browser y autenticación live son **NO_APLICA** al diff. Sólo se acredita compilación .NET 10, ejecución xUnit y composición in-process; no pruebas productivas.

## A4/A5 — verificación causal
El único commit de la microtarea incluye clase, 13 tests, este documento y sección de CHANGELOG con encabezado y viñetas contiguas; publicar por fast-forward/CAS en `dev` tras lock V17 `2000000343`. Transicionar a VALIDANDO con CI externa y readback institucional. LISTO_DEV sólo con Backend Release, build, seguridad multiempresa, Authorization, verify-solqaryn-project-scope y V5 completed/success SHA causal, P0/P1 del alcance cero. A5 pendiente de otra Task Javier distinta del autor. No QA/main/PROD.
