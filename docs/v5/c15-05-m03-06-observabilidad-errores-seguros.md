# SOLQARYN V5 FINAL — C15-05.M03.06 observabilidad estructurada y errores seguros

**Autor:** PRIMARY DEV 04 JAVIER, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. **Rama:** solqaryn/Solqaryn@dev. **Gobierno:** Scope Lock STRICT, STANDARD V5 FINAL, PLAN_MAESTRO/COLA/DAG/BITACORA. **Prerrequisito:** C15-05.M03.01–05 LISTO, A5 independiente para M03.05, DAG VALIDADO/SI y COLA G/O=SI. Alcance de desarrollo autenticado por lectura al reclamar.

## Implementacion .NET 10 y decisiones de seguridad

Se crea una envoltura Infrastructure de observabilidad llamada V5OperationalAuditObservedReadOnlyPreflight para el gateway read-only existente de C15-05.M03.05. Recibe ILogger y proveedor tipado; produce mensajes estructurados con EventId estables 150506–150509, siempre con **clases fijas de resultado** y no con texto libre del proveedor. No registra subject, tenant, SHA, payload, URL, token, excepcion, request o PII. Un snapshot aparentemente valido sigue devolviendo AUTHENTICATED_EVIDENCE_REQUIRED y el log clasificado NEEDS_EXTERNAL_VERIFICATION; una denegacion se clasifica DENIED, proveedor no disponible PROVIDER_UNAVAILABLE. Cancelacion se propaga sin filtracion. Error no esperado conserva denegacion segura AUDIT_OBSERVATION_UNAVAILABLE, sin revelar excepcion.

La envoltura no concede permisos. En el gateway original AllowsInstitutionalWrite, CertifiesA5, AllowsEventDispatch y AllowsTenantFilterBypass permanecen siempre falsos. No agrega endpoints, DI de produccion, proveedor live, consultas SQL, migraciones, secretos, tareas programadas, cola, webhook, dispatcher ni ejecucion de eventos entre Tasks. La validacion de origen, identidad real, GitHub CI, firma JWT, tenant y revision independiente exige un adaptador autenticado futuro, fuera de este alcance.

## Pruebas xUnit y limitaciones

Cinco casos xUnit cubren snapshot valido sin certificacion, rechazo de tenant B antes de leer proveedor, error del proveedor redactado, cancelacion y constructor con dependencias nulas. Se verifican EventId, nivel de log, codigos positivos y negativos, y ausencia de identificadores y secretos. Ejecutar tests reales GitHub-hosted net10.0, backend/seguridad/authorization pertinentes. No declarar A1–A4 PASS mientras estos checks permanezcan queued/in_progress/cancelled/failed. Para cierre, acreditar diff y blobs, Scope Lock y procedencia V5 del SHA exacto y readback PLAN/COLA/BITACORA. A5 independiente por otra Task Javier, nunca autor.

**Impacto:** un archivo C# nuevo Infrastructure, uno de tests xUnit, este documento y seccion autocontenida CHANGELOG_AI.md. Domain, Application, API y persistencia no cambian. **Rollback:** git revert normal + CI; sin alterar BITACORA historica. No se autoriza QA, main, PROD, DNS, secretos, proveedores externos ni datos productivos.
