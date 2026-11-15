# SOLQARYN V16/V15 — C12-04.M03.01: guard Domain fail-closed de cutover MySQL privado

**Autor:** PRIMARY DEV 06 JAVIER, `6af16bbad26881918c283abe06e1f32a`. **Riesgo:** R2 — identidad, tenancy, privilegios y cutover de MySQL. **Rama:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN/COLA/DAG y los seis diseños M02 C12-04, revisados independientemente.

## Alcance implementado
- Clase ejecutable pura `V5PrivateMySqlCutoverDomainRules` en Domain/AutomationV5. Reglas de prevalidación de snapshots *propuestos* sin conectar a EF, MySQL, Aiven, Hostinger, DNS, API, secretos ni producción.
- `TrustedSnapshot` requiere pruebas de autenticación servidor, RBAC, MFA, membresía vigente, tenant persistido, least privilege sin WITH GRANT OPTION, CI exact-SHA, prueba de integración MySQL 8.4 efímero, backup y restore, rollback, revisión independiente y fencing/lease. **Estos booleanos NO constituyen prueba auténtica**: el servicio backend futuro debe verificar su procedencia desde sistemas autorizados; un cliente no puede decidirlos.
- `Candidate` limita entorno exacto `dev`, tenant A/B propietario, manifiesto igual al SHA esperado de 40 hex y prohibición categórica de conexión cliente, cutover/dual-write real, DDL destructivo, elevación de grants, secretos, fallback multiempresa o P0/P1 sin resolver.
- Veredicto positivo `TRUSTED_DEV_SANDBOX_REVIEW_REQUIRED` **sólo pide verificación adicional servidor**, `AuthorizesSqlOrMigration`, `AuthorizesCutover`, `AuthorizesDeployment` siempre `false`. No autoriza activación ni cambio de runtime.

## Matriz de calidad y seguridad
**A0:** cuatro archivos nuevos/modificados: Domain puro, pruebas xUnit con factores positivos/negativos, este documento y sección autónoma de CHANGELOG. Ninguna escritura a DB/migraciones/proveedores.

**A1:** pruebas xUnit reales preparadas para que fallen ante errores: aceptación restringida a revisión, entradas nulas, roles/MFA/revocación, ambientes QA/main/PROD, tenant B, SHA inconsistente, DDL y dual-write, grants, falta CI/aprobación, backups/restore y exclusividad de writer. Los resultados sólo se cuentan al verificar logs GitHub Actions de SHA aplicable.

**A2:** JWT/RBAC/tenant A/B sólo se inspeccionan como evidencia confiable a revalidar por backend; no introducir un bypass de autorización. Ninguna decisión del navegador se considera verificada. Rechazos fail-closed ante secretos y PII.

**A3:** esta clase Domain no cambia EF, SQL, provider, DbContext, migration ni esquema, así que pruebas MySQL de integración de *este diff* = `NO_APLICA`. No obstante, **una migración/cutover futuro exige evidencia real de Category=Integration en MySQL 8.4 efímero** más restore/rollback auditados: ningún booleano de prueba es certificación. No se ha ejecutado restore ni tocado datos.

**A4:** commit individual fast-forward/CAS a dev, build Release/xUnit y checks backend/autorización/seguridad pertinentes, Scope Lock y procedencia V5 `completed/success` en SHA o equivalencia documentada; registrar plan y BITACORA con readback. `QUEUED/IN_PROGRESS/CANCELLED/SKIPPED/FAILED` no son PASS.

**A5:** revisión independiente de otra Task JAVIER distinta de autor, capaz de rechazar una implementación insuficiente antes de liberar DAG técnico. No promueve QA/main/PROD.

## Reversión
`git revert` normal con HEAD revalidado, recompilación/test y trazabilidad BITACORA aditiva. No cambiar permisos, infra, secretos, proveedores, MySQL real ni scheduling. Fuera de alcance: seleccionar servidor, adquirir lease real, restaurar DB, aplicar migración, desplegar o accionar cutover.
