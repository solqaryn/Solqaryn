# SOLQARYN V16 - C02-02.M03.01: reglas Domain de placement shared/dedicated

**Riesgo V15:** R2, tenancy, almacenamiento, versionamiento, rollback. **Fuente:** plan V5 FINAL autorizado, contratos C02-02.M02.01-06, PLAN/COLA/DAG de fila 338; repositorio solqaryn/Solqaryn en dev con PROJECT_SCOPE_LOCK=STRICT. **Autor:** PRIMARY DEV 02 Javier, TASK_ID 6af16bb9492c8191b8e8c7d606f11507.

## Implementacion real y limite

Implementada clase pura V5SharedDedicatedPlacementDomainRules en backend/src/Domain/AutomationV5 con dos operaciones de preflight (consultar asignacion y proponer intencion de migracion), dos modos shared/dedicated, evidencia trusted de JWT y membresia actual, permisos de revision/propuesta, propietario tenant, binding opaco de catalogo backend, revision CAS esperada y rechazo de estado suspendido.

Deniega tenant B para actor A, DSN/servidor aportado por cliente, fallback cruzado, live cutover, dual-write, DDL destructivo, revision obsoleta o nueva, binding objetivo malicioso, ausencia de idempotency key, ausencia de recuperacion/restore sandbox, checksum o reconciliacion de datos. Un resultado positivo solicita enforcement en servidor; siempre AuthorizesConnection=false y AuthorizesMigration=false.

No agrega tablas, DbContext, EF migraciones, conexiones, configuration secrets, API, workers, router runtime, DNS, proveedores o infraestructura real. M03.02 y siguientes deben ejecutar las decisiones reales mediante servicios autorizados y probar transacciones, consistencia, fence/lease y almacenamiento con MySQL 8.4 efimero cuando corresponda.

## Pruebas y auditorias

- A0: cuatro archivos (una clase Domain, una suite xUnit, este documento, CHANGELOG autocontenido), diff y scope sin secretos.
- A1: once pruebas xUnit ejecutables con ramas positivas, negativas, valores limite y error: assignment, propuesta dedicated, JWT/membresia revocada, tenant A/B, DSN, fallback, cutover, revision 0/stale/futura, rol, idempotencia, restore y modo desconocido. No declarar PASS antes de la CI del SHA.
- A2: tenancy A/B, RBAC, conexion server-only, revocacion, idempotencia, sin privilegios nuevos ni bypass a JWT. Los veredictos no autorizan consultas ni migraciones.
- A3: codigo puro Domain sin EF/SQL, MySQL/Oracle/Docker/Playwright NO_APLICA motivado para el diff; evidencia restauracion es campo de precondicion del preflight, NO prueba de restore ejecutada.
- A4: Git fast-forward/CAS dev, Scope Lock, V5 changelog y backend/security/auth pertinentes completed/success en SHA causal o equivalencia de blobs auditada, readback PLAN/COLA/DAG/BITACORA; CANCELLED/SKIPPED/IN_PROGRESS no son PASS.
- A5: revision independiente obligatoria por otra de las cinco Tasks Javier despues de LISTO; no liberacion QA/main/PROD.

Rollback del codigo por git revert normal, sin revertir datos porque esta unidad no realiza mutaciones.
