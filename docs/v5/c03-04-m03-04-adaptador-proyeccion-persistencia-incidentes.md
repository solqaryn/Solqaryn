# SOLQARYN V17 - C03-04.M03.04: adaptador aislado de proyeccion de incidentes

Riesgo V15 R2: tenancy, JWT/RBAC, integridad de evidencias, sucursal y continuidad. Autor PRIMARY DEV 05 JAVIER (6adc2e55f47c81919f6374e561687076). Alcance: solqaryn/Solqaryn rama dev, PROJECT_SCOPE_LOCK STRICT. Fuente: STANDARD V5 y PLAN/COLA/DAG.

## A0: entrega funcional

Adaptador real .NET 10 en Application y ocho metodos xUnit. Proyecta metadatos de un registro de persistencia sintetico al contrato Domain PersistedIncident (C03-04.M03.01), tras comprobacion de tenant A/B, sucursal, SHA, version, revocacion, sanitizacion y estado. El resultado no es un grant de lectura, escritura ni restauracion; requiere el gate Application M03.03 con JWT/RBAC reales. Dos archivos de codigo/test, este documento y CHANGELOG.

## A1: pruebas

Casos ejecutables para proyeccion positiva acotada, origen nulo, source no verificado, empresa B, sucursal distinta, SHA invalido, version negativa, revocacion, evidencia insegura, estados desconocidos y Contained/Resolved sin permisos de mutacion. Tests xUnit preparados; no atribuir PASS hasta CI.

## A2: seguridad

El adaptador nunca acepta ownership de empresa ajena, branch diferente, evidencia insegura ni estado desconocido. OwnershipReadFromPersistence es un dato de entrada y NO certifica una lectura real, identidad JWT ni privilegios. No expone payload, PII, secretos o evidencia; ningun resultado concede permiso SQL/backup/restore.

## A3: integracion

Se elige expresamente la alternativa de ADAPTADOR AISLADO, no una migracion ni repositorio concreto. No se tocan EF, DbContext, SQL ni infraestructura, por lo que MySQL 8.4 Category=Integration, HTTP y Playwright no aplican al diff; un adaptador persistente posterior si exigira pruebas reales de DB efimera, restore, rollback y aislamiento A/B. Ninguna prueba sintetica equivale a certificacion de MySQL real.

## A4: publicacion

Una entrega por commit fast-forward/CAS sobre dev y candado V17 en PLAN fila 557. Requiere backend Release y xUnit reales, seguridad/authorization, Scope Lock exact-SHA, procedencia V5 y readbacks institucionales. QUEUED/IN_PROGRESS/CANCELLED/SKIPPED/FAILED no son PASS.

## A5 y rollback

Revision independiente por otra Task Javier distinta del autor, capaz de rechazar. Reversion git revert normal; no QA/main/PROD, secretos, proveedores, MySQL real ni migraciones.