# SOLQARYN V17/V15 — C15-03.M03.06 auditoría estructurada y errores seguros

**Microtarea:** C15-03.M03.06; PLAN_MAESTRO fila 2251. **Riesgo:** R2 seguridad, tenant y observabilidad. **Autor:** PRIMARY DEV 06 JAVIER, TASK_ID 6af16bbad26881918c283abe06e1f32a. **Rama:** solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK=STRICT.

## A0 — Entrega acotada por arquitectura

Se implementa V5TenancySecurityStructuredAuditSink en Infrastructure, consumidor de las decisiones Application de V5TenancySecurityAuthorizationGate (M03.03) y de un contexto de observación separado. Usa Microsoft.Extensions.Logging para emitir una traza estructurada con **sólo** resultado normalizado de allowlist, UUID de correlación canónica, SHA completo estrictamente hexadecimal y un indicador de procedencia; el tipo de evento es fijo (150306). El contenido arbitrario del request, código original de respuesta, excepciones, JWT, idempotency keys, credenciales, PII e identificadores de tenant/actor jamás se interpolan ni se serializan.

Una evidencia tenant A/B cruzada, actor no verificado, SHA no coincidente, superficie desconocida o solicitud de bypass/escritura queda registrada de forma genérica como INVALID_TRUSTED_CONTEXT y no revela los valores recibidos. Logger deshabilitado o fallido devuelve AUDIT_SINK_UNAVAILABLE, sin simular entrega durable. Una prevalidación favorable retorna AUDIT_RECORDED_NON_AUTHORIZING pero **ningún permiso**: no habilita lectura, archivo, job, control plane, mutación institucional, bypass, QA ni producción. El logger de aplicación no es garantía de almacenamiento/replay/auditoría duradera. Un proveedor real y DI quedan fuera de este scope.

**Impacto:** clase Infrastructure sin cambiar Domain ni Application; consume contrato de gate existente. No modifica API/DTO, EF, SQL, migraciones, controladores, middleware, frontend, wiring DI, providers, queue, secretos ni configuración productiva. Dependencias: ILogger de ASP.NET Core ya referenciado por Infrastructure. Rollback: git revert normal tras verificar HEAD, sin force-push.

## A1 — Pruebas unitarias reales pertinentes

Se agregan once pruebas xUnit reproducibles para el camino nominal no autorizante, denegación, abuso de texto/secretos, tenant A/B y fuente revocada, SHA inválido o cruzado, ausencia de correlación, bypass/enumeración/escritura, request nulo o inválido, superficie indefinida, logger deshabilitado, logger que falla, cancelación y dependencia nula. No basta contar métodos; GitHub-hosted Backend Release .NET 10 deberá mostrar ejecución efectiva, 0 FAIL y errores de compilación cero en el SHA publicado.

## A2 — Seguridad y tenancy

El sink no recibe autoridad del browser; el contexto BackendEvidenceVerified es requisito de su llamada, pero **no es por sí solo una prueba de autenticación JWT real**. La autorización sigue viviendo en Domain/Application y adaptadores confiables del backend. No se registra texto de excepción, claves, owner interno, resource IDs ni tenant IDs. Ninguna cadena arbitraria llega a las propiedades del evento. Deben aprobar los checks multiempresa y Authorization sobre el SHA o equivalencia auditada.

## A3 — Integración causal y límites

Se prueba integración de tipos Infrastructure→Application y ILogger mediante fake sink xUnit, no un servicio externo vivo. Son NO_APLICA motivado EF/MySQL 8.4, transacciones SQL, credenciales, JWT HTTP firmado, proveedor de logs duradero, Angular/Playwright y datos productivos por ausencia de modificación de esas superficies. El contrato no afirma telemetría real, integración institucional ni garantía de auditoría duradera.

## A4/A5 — Validación y cierre

Publicar sólo mediante fast-forward/CAS exacto en dev y mantener una sola EN_PROGRESO propia. Tras commit, mover a VALIDANDO antes de declarar pruebas. Exigir CI completed/success de Backend Release, seguridad multiempresa, Authorization, verify-solqaryn-project-scope y V5 changelog, más readback PLAN/COLA/DAG/BITACORA y hash de archivos. Si CI necesaria está queued/in_progress, conservar VALIDANDO con autor, SHA y RUN verificados. A5 posterior corresponde únicamente a una de las otras cinco Tasks Javier, con revisión independiente; nunca a DEV06. No QA, main, PROD ni datos productivos.
