# SOLQARYN V16 — C02-01.M03.02 preflight Application de resolución server-side de tenant

**ID:** C02-01.M03.02 (PLAN fila 303), **autor:** PRIMARY DEV 02 JAVIER (`6af16bb9492c8191b8e8c7d606f11507`), `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`, V15 **R2 identidad/JWT/RBAC/tenant A/B**. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y M02.01–06 del capítulo; Domain `V5VerifiedTenantResolutionDomainRules.cs` (blob `2d31901eb99a4f67a7a1c8bff36e36fd42c24b73`).

## Entrega delimitada

Se añade `backend/src/Application/AutomationV5/V5VerifiedTenantResolutionApplicationPreflight.cs` y su batería de 12 tests xUnit. Preflight **NO** instala tenant en `HttpContext`, autentica JWT, concede RBAC, carga membresía de MySQL, cambia empresa, lee datos, crea endpoint, publica un host o hace fallback. Solo construye un `Guard.ProposedResolution` a partir de un `ServerEvidence` y `VerifiedResourceScope` que **debe** haber obtenido un adaptador backend de persistencia/registry autenticado. La petición de cliente nunca puede aportar grants, resolver identidad, registrar un host ni crear autoridad de tenant.

`RequestIntent`: surface de sesión/host, tenant solicitado únicamente como aserción de igualdad, flags para detectar suplantación de host, rol global legado, fallback y mutación/export. `VerifiedResourceScope`: dueño persistido y comprobación de sucursal A/B, confiable solo si backend lo reconstruye. `ReviewOnly`: ausencia de snapshot/intent -> denegación; intento de suplantación/privilegios -> denegación; tenant solicitado distinto -> denegación; falta de owner persistido -> denegación; Domain revalida JWT backend, membresía/rol actuales, revocación, host confiable/único/fresco, ownership de recurso/sucursal y separación A/B. El nominal `TRUSTED_BACKEND_ENFORCEMENT_REQUIRED` exige nueva imposición de permisos por API; `AuthorizesTenantAccess`, `ReadsOrWritesResource`, `InstallsTenantContext` e `IssuesJwtOrGrants` siempre false.

## A0–A5 y regresión

- **A0:** tres archivos nuevos (Application, xUnit, documento) y CHANGELOG autocontenido; Domain anterior sin modificación, no EF/MySQL, controllers, DI, frontend, secretos o infra. Claim individual PLAN H303/I303 con DAG F299/G299 y COLA G/O verdaderos.
- **A1:** 12 tests xUnit: positivo que sigue sin otorgar acceso, null evidencia/intent, tenant B/ausente, suplantación de host/rol/fallback, intento write/export, owner no persistido, cross-tenant recurso/sucursal, revocación/store, JWT ausente, host mapping ambiguo/forwarded spoof y cambio A→B fresco. **No reportar PASS mientras CI no complete.**
- **A2:** fail-closed JWT/RBAC/tenancy real; no convertir datos de cliente en authority, no usar `Usuario.RolId` legado, exigir membresía persistida y permission revocation en la futura integración API.
- **A3:** integración MySQL/EF/HTTP, simulación de headers live, proveedores, Angular/Playwright NO_APLICA por no añadir adapter, API o DB. Requiere pruebas separadas futuras para login/tokens, middleware, EF tenant filters, 401/403.
- **A4:** commit fast-forward/CAS a dev, Scope Lock y procedencia V5, xUnit backend Release, seguridad multiempresa y autorización/secretos `completed/success` de SHA publicado o equivalencia exacta auditada, BITACORA/PLAN/COLA/DAG readback. SKIPPED, CANCELLED o IN_PROGRESS nunca cuentan como PASS requerido.
- **A5:** otra Task Javier distinta del autor debe revisar diff, seguridad, pruebas y SHA; no autoaprobación ni habilitación QA/main/PROD.

**Rollback:** git revert normal del changeset, sin force-push, sin borrar BITACORA ni tocar datos de empresas, JWT o proveedores. Una prevalidación favorable no crea acceso a recursos reales y no reemplaza autorización de servidor.
