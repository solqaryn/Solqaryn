# SOLQARYN V17/V15 — C15-03.M03.04: adaptador aislado de evidencia de seguridad y tenancy

**Alcance:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`, microtarea C15-03.M03.04, PLAN_MAESTRO fila 2249, autor PRIMARY DEV 06 JAVIER (`6af16bbad26881918c283abe06e1f32a`). **Riesgo R2** por seguridad, RBAC y aislamiento. Solo una unidad EN_PROGRESO con candado V17 `SQV17_C15_03_M03_04` / `2000002249`. A5 será de otra Task Javier.

## Entrega REAL .NET 10 y delimitación

- `backend/src/Infrastructure/AutomationV5/V5TenancySecurityIsolatedSnapshotAdapter.cs`: adaptador de snapshot **aislado, read-only y no autorizante**, que captura evidencia inmutable por recurso, actor, tenant y SHA, comprueba que cada observación coincida con la captura, y reutiliza el `V5TenancySecurityAuthorizationGate` de Application y el guard de Domain de M03.01.
- Rechaza inputs nulos, IDs inválidos, SHA obsoleto, actor/tenant/recurso cruzados o evidencia de captura alterada. No se puede blanquear evidencia de B mediante argumentos de A.
- Delega al gate real las comprobaciones de membresía, permiso, revocación, propiedad persistida declarada, sucursal, enlace vencido, grant de job, idempotencia y ámbito de control plane. Incluso un snapshot nominal retorna solamente `AUTHENTICATED_EXECUTION_ADAPTER_REQUIRED`. **Nunca concede lectura, descarga, job, administración, escritura, acceso a EF ni bypass del filtro de tenant**.
- Este adaptador recibe una copia de una evidencia confiable **supuesta para pruebas**; no demuestra que una implementación real de `IV5TenancySecurityVerifiedAuthorizationEvidenceReader` autentique JWT, consulte roles o pertenencia en MySQL. Ningún servicio se registra en DI de API ni se ejecuta proveedor externo.

## A0–A4 con pruebas que deben ejecutarse

**A0**: archivar diff del único adaptador Infrastructure, la suite xUnit, este documento y sección autocontenida en `CHANGELOG_AI.md`; sin modificación de EF, migraciones, HTTP, entidad, Role/JWT, frontend ni secretos. Predecesoras C15-03.M02.01–06+A5 y M03.01–03+A5 requieren gates reales y se consultaron COLA/DAG vigentes. La implantación aislada es alternativa de M03.04, NO reemplaza una integración persistente pendiente.

**A1**: `backend/tests/Solqaryn.Tests/Infrastructure/V5TenancySecurityIsolatedSnapshotAdapterTests.cs` incluye 12 pruebas xUnit; positivo sólo de preflight sin autorización, negativos para recurso/tenant/actor, SHA, evidencia manipulada, membresía y permiso revocados, rol, tenant padre B, branch, link expirado, bypass, enumeración, escritura institucional, jobs/control plane, constructor y cancelación. Exigir ejecución real en GitHub Actions .NET 10 Release, no considerar la presencia del archivo como PASS.

**A2**: el gate no puede producir capabilities; los negativos A/B, revocación y control plane deben resultar verdes en backend y security/authorization CI pertinente por SHA. Aprobación nominal sólo habilita otro adaptador autenticado, no concede acceso. No declarar control de JWT/tenant en HTTP real.

**A3**: build de Infrastructure + suite xUnit que usa el Gate Application y reglas Domain existentes; **NO_APLICA MySQL 8.4 `Category=Integration`, Playwright y proveedores** a este diff aislado que no consulta EF/DB, no agrega endpoints ni modifica UI. No usar este NO_APLICA para certificar el producto global o liberar M03 posteriores que exijan integración efectiva.

**A4**: confirmar GitHub `dev` fast-forward/CAS, el SHA causal, Scope Lock exact-SHA SUCCESS, procedencia V5 SUCCESS, checks backend/security/authorization pertinentes SUCCESS en el SHA o descendiente con blobs iguales y dependencias equivalentes; PLAN H/I, COLA/DAG y eventos BITACORA A:Z con readback. Si CI sigue QUEUED/IN_PROGRESS, conservar VALIDANDO y owner, A4 PENDIENTE, sin LISTO ficticio. A5 requiere revisión independiente de otra de las cinco Tasks Javier.

## Limitaciones conocidas, riesgo residual y rollback

El `TenantQueryBypassGuard.RespectCurrentTenant` de producción todavía retorna el IQueryable recibido cuando `AuthorizedReadEmpresaId` no está fijado; este adaptador **NO lo modifica ni lo certifica seguro**. El confinamiento HTTP de cuarentena y los permisos efectivos deben verificarse en tareas de integración con MySQL 8.4 efímero y pruebas de solicitudes reales, incluyendo `IgnoreQueryFilters`, tenant A/B y recursos parent/hijos. El adaptador no arregla ni agrava por sí mismo dicho riesgo abierto. Se prohíben despliegues, migraciones y permisos QA/main/PROD inferidos de un PASS del mock.

Rollback: `git revert` ordinario del commit particular tras revalidar HEAD, sin force-push, conservando BITACORA histórica y, cuando corresponda, evento compensatorio idempotente. Cualquier corrección posterior debe respetar escritor/lock V17 y reejecutar controles R2.
