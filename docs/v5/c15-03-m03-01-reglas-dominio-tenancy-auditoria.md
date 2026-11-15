# SOLQARYN V5 FINAL — C15-03.M03.01: guard de dominio de seguridad y tenancy

**Autor:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Rama:** `solqaryn/Solqaryn@dev`. **Scope Lock:** STRICT.

## Entrega .NET 10 real y alcance

Se implementó una regla pura de dominio `backend/src/Domain/AutomationV5/V5TenancySecurityDomainRules.cs` con pruebas xUnit `backend/tests/Solqaryn.Tests/Domain/V5TenancySecurityDomainRulesTests.cs`. La unidad es M03.01, no una auditoría transversal terminada. PLAN fila 2246 PENDIENTE/sin owner y COLA G/O=SI/DAG F=VALIDADO/G=SI antes de claim; M02.06 LISTO+A5 independiente revalidada sobre SHA `0a69700f4ab8d2a1454b9b1303cdf8cb77de7aaf`.

## Controles verificables

La autoridad `TrustedAuthority` sólo puede construirse por backend autenticado, con tenant y actor positivos, membresía y permiso vigente. `PersistedResource` sólo puede construirse desde pertenencia persistida verificada; nunca de host/header/body/slug. Todas las superficies BusinessRead, BranchRead, FileDownload, BackgroundJob y ControlPlane niegan consultas con bypass de filtros tenant o enumeración cruzada; empresa/parent NULL o de B se rechaza. Sucursales exigen owner de branch igual al contexto autenticado; archivos caducados/revocados no pasan; jobs exigen grant de servicio, auditoría y key idempotente válida; control plane exige grant y auditoría.

Los resultados fallidos son sólo códigos sin IDs ni metadatos de tenant ajeno: VERIFIED_SERVER_AUTHORITY_REQUIRED, UNKNOWN_SECURITY_SURFACE, TENANT_QUERY_BYPASS_DENIED, RESOURCE_OWNER_MISMATCH, BRANCH_SCOPE_MISMATCH, FILE_ACCESS_REVOKED_OR_EXPIRED, SERVICE_JOB_AUTHORITY_REQUIRED, CONTROL_PLANE_AUTHORITY_REQUIRED. `READY_FOR_SERVER_SECURITY_ENFORCEMENT` **no es autorización de consultas ni prueba HTTP**, sólo un resultado de preflight de Domain.

## Riesgo pendiente, pruebas, A0–A5

El riesgo específico documentado en M02.06 sobre `TenantQueryBypassGuard` y ausencia de `AuthorizedReadEmpresaId` **NO se declara reparado por esta regla pura**. Faltará conectarla a Application/Infrastructure y ejecutar pruebas negativas HTTP/EF/MySQL 8.4 para demostrar que no existe bypass real. No se altera EF, API, IAM/JWT/RBAC, DB, claves, archivos, servicios live, workers, seguridad de archivos ni control plane. La regla no realiza consultas, downloads, jobs, migraciones ni despliegues.

**A0:** código Domain sin dependencia Infrastructure; **A1:** xUnit positiva/negativa de contexto, permiso y validación; **A2:** negativos tenant A/B, NULL, sucursal, archivo, jobs, control plane y query bypass; **A3:** pruebas .NET/build en GitHub Actions sobre SHA exacto, sin atribuir MySQL/HTTP; **A4:** fast-forward CAS y checks Scope Lock/V5/backend/seguridad pertinentes completados SUCCESS exact-SHA con PLAN/COLA/DAG/BITACORA readback antes de LISTO_DEV; **A5:** otra Task Javier, no la autora. CI queued/in_progress o CANCELLED no equivalen a PASS. Rollback mediante git revert normal, BITACORA append-only, sin force-push ni operaciones QA/main/PROD.
