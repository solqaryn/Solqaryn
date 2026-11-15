# SOLQARYN V17 — C12-04.M03.02: servicio Application de revisión privada de cutover MySQL

**Riesgo V15:** R2 (tenancy, JWT/RBAC/MFA, seguridad, concurrencia, migraciones). **Repositorio:** solqaryn/Solqaryn, **rama:** dev, **PROJECT_SCOPE_LOCK:** STRICT. **Autor:** PRIMARY DEV 05 JAVIER, TASK_ID 6adc2e55f47c81919f6374e561687076. Fuente: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y PLAN/COLA/DAG C12-04.M03.02.

## A0 — Alcance y cambios reales

Servicio .NET 10 en Application \`C1204PrivateMySqlReadinessApplicationService\`, interfaz separada para futura lectura de evidencias \`IC1204CutoverReadinessEvidenceReader\`, ocho tests xUnit con falsos lectores aislados (no proveedor), este documento y CHANGELOG autocontenido. Reutiliza \`V5PrivateMySqlCutoverDomainRules\` de C12-04.M03.01, cuya revisión A5 independiente antecede la habilitación técnica de M03.02.

**No** registra API/DI de producción, DbContext, servicio MySQL, parser de certificados, migraciones, claves, cambios de GRANT, backup, restore, DNS, SQL, doble escritura ni credenciales. La interfaz de evidence reader carece de implementación de infraestructura; un mock o booleano de snapshot NO acredita grants, CI, MySQL 8.4 ni autoridad JWT. La futura capa API deberá autenticar al actor, establecer el tenant desde servidor, construir \`C1204VerifiedActor\` únicamente desde un contexto confiable y conectar adaptador auditado a fuentes reales.

## A1 — Aceptación ejecutable prevista

Pruebas xUnit con positivo restringido únicamente a \`TRUSTED_DEV_SANDBOX_REVIEW_REQUIRED\`, nunca aprobación SQL; actor anónimo, membresía/rol/MFA revocados, tenant B, QA/PROD, SHA discordante, solicitud mutante (cutover/GRANT/DDL/secreto), P0/P1, ausencia de evidencias, identidad de proveedor cruzada, grants excesivos, falta de prueba MySQL 8.4 efímera, fencing y cancelación/error del lector. El gateway falso prueba orquestación lógica y **no** integración MySQL/HTTP real. Contar PASS sólo tras CI pertinente del SHA exacto.

## A2 — Invariantes de aislamiento y seguridad

La validación previa al lector evita consultar evidencias para tenants ajenos o callers sin autenticación, membresía, MFA y permisos declarados; el resultado del lector exige correspondencia ordinal de actor, tenant y SHA con la petición. El validador Domain existente conserva la exigencia de pruebas reales y deniega claims sin privilegios mínimos, lease, restauración o CI. Las evidencias del lector NO deben provenir de DTO de cliente. El servicio sólo puede solicitar revisión independiente y **siempre retorna false** para permisos de SQL, migración, cutover o despliegue. Prohibido reutilizar evidencia de otro SHA, tenant o usuario.

## A3 — Integración y límites

El changeset sólo agrega Application aislado + tests de lógica de orquestación: sin acceso EF/MySQL, proveedor externo, HTTP ni UI; por eso pruebas de integración MySQL/HTTP/Playwright de **este diff** = NO_APLICA fundamentado. Si se crea después un lector Infrastructure o se toca EF/SQL, será obligatoria integración real **Category=Integration** sobre MySQL 8.4 efímero, restauración/rollback verificados, scopes A/B y evidencia explícita en CI. No llamar a los tests de gateway falso una prueba MySQL.

## A4 — Publicación y verificaciones

Un único changeset en dev bajo candado V17 por fila 1851, commit fast-forward con CAS de HEAD. CI requerida: .NET 10 build/xUnit Release, backend, seguridad y autorización, \`verify-solqaryn-project-scope\` y control V5 de encabezados/procedencia en SOURCE_SHA o descendiente probado. \`QUEUED/IN_PROGRESS/CANCELLED/SKIPPED/FAIL\` nunca PASS requerido. Registrar PLAN, COLA/DAG, BITACORA A:Z y telemetría con readbacks por transición. Esta documentación describe pruebas **preparadas**, no afirma que hayan pasado antes de sus resultados.

## A5 y reversión

A5 requiere Task de Javier distinta de DEV05 con lectura independiente de tests y límites. El autor jamás se autoaprueba. Reversión: git revert normal de los cuatro archivos de este ID, sin force-push ni reescritura histórica; nunca promover QA/main/PROD ni usar datos/secretos de Aiven o producción.