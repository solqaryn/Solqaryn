# SOLQARYN V5 — C03-02.M02.03: persistencia, índices, migraciones y fixtures RBAC/ABAC

**Owner:** PRIMARY DEV 06 JAVIER (`6af16bbad26881918c283abe06e1f32a`), `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fecha:** 2026-11-15. **Autoridad:** STANDARD V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO y Skill canónica. **Tipo de entrega:** contrato exclusivamente documental M02.03; no se ejecuta SQL ni se cambia el modelo de datos actual.

## 1. Gate de entrada y fuentes

Preflight por ID `C03-02.M02.03`: PLAN H478=PENDIENTE/I478 vacío, COLA G478=SI/O478=SI, DAG F449=VALIDADO/G449=SI; predecesor exigido `C03-02.M01.01–06+A5` en LISTO con revisiones independientes. Se reclamó owner único mediante sustitución condicional y readback. Los documentos M02.01 y M02.02, elaborados en paralelo según DAG independiente, son referencias conceptuales; su A5 futura no se considera PASS por esta relación.

Fuentes: `docs/v5/c03-02-m01-06-bloqueo-rollback-liberacion-rbac-abac.md`, el modelo conceptual de `C03-02.M02.01`, contrato de interfaces de `C03-02.M02.02`, AGENTS.md, Scope Lock y arquitectura. Las clases actuales `TenantPermissionContext`, `RequierePermisoAttribute`, `VerifiedCommercialTenant` y `PlatformSupportPolicy` son antecedentes de código, **no prueba de que las tablas, índices o reglas de este documento existan**.

**Baseline técnico:** .NET 10, EF Core 10.0.12, Oracle MySql.EntityFrameworkCore 10.0.9, MySQL 8.4, Angular 22.2.1. Cualquier cambio real futuro a MySQL necesita un servicio efímero de CI, tests positivos y negativos A/B, comprobación de transacciones, revert y Scope Lock. Aiven/producción permanecen fuera del alcance.

## 2. Diez entidades relacionales hipotéticas — SIN DDL

| Objeto futuro | Identificador, vínculos y restricciones | Invariante de autorización |
| --- | --- | --- |
| DB01 TenantMembershipVersion | tenant, user, version, activeFrom/To, revokeUtc | Membresía verificada de servidor; nunca claim cliente |
| DB02 RoleBindingVersion | tenant, role, member, version | Rol vinculado a tenant, sin fallback global |
| DB03 ModuleActionGrant | tenant, role, module, action, effectiveUtc | RolPermiso explícito; no grant por plan SaaS |
| DB04 BranchMembership | tenant, branch, user, version | Sucursal pertenece a tenant y usuario autorizado |
| DB05 ResourceOwnerBinding | tenant, resourceRef, branchRef, ownerRef, version | BOLA/IDOR denegado antes de proyectar |
| DB06 AccessRevocation | tenant, user, grantVersion, reasonCode, utc | Revocación prevalece sobre JWT/caché obsoletos |
| DB07 ExportAuthorization | tenant, actor, datasetRef, purpose, expiry | Export por fila/campo con grant propio |
| DB08 MediaAuthorization | tenant, actor, objectRef, purpose, expiry | URLs firmadas temporales sólo con recurso A |
| DB09 SupportReadSession | platformActor, exactTenant, allowlistedModule, expiry | GET permitido; no escritura ni tenant arbitrario |
| DB10 AuthorizationAuditEnvelope | eventId, actorHash, scopeHash, decision, utc, policyVersion | Append-only, sin JWT/PII/secretos |

Los identificadores `tenant` deben ser resueltos por servicios backend autenticados; los posibles campos almacenados preservan integridad referencial, pero **no son prueba suficiente de permiso**. Restricciones por recurso/filas, grants y contexto por solicitud deben comprobarse en Application/API, no solo en FK. Ninguna estructura anterior sustituye la base existente ni declara DDL autorizado.

## 3. Ocho índices propuestos, sujetos a EXPLAIN y cardinalidad real efímera

| Índice | Columnas conceptuales | Propósito y negativo |
| --- | --- | --- |
| IX01 | TenantMembershipVersion(tenant,user,version) UNIQUE | Evitar miembro ambiguo/cross-tenant |
| IX02 | RoleBindingVersion(tenant,member,role,version) | Filtrar vínculos de rol efectivos por empresa |
| IX03 | ModuleActionGrant(tenant,role,module,action) UNIQUE | Un único grant efectivo por acción y ámbito |
| IX04 | BranchMembership(tenant,branch,user) | Verificar sucursal A sin escanear B |
| IX05 | ResourceOwnerBinding(tenant,resourceRef,branchRef) | Evitar BOLA/IDOR por lookup sin tenant |
| IX06 | AccessRevocation(tenant,user,revokeUtc) | Obtener revocación vigente sin dependencia de caché |
| IX07 | ExportAuthorization(tenant,actor,expiry) | Filtrar permisos de exportación caducados |
| IX08 | AuthorizationAuditEnvelope(scopeHash,utc,eventId) | Auditar decisiones sin filtrar PII ni datos B |

Los índices se diseñarán con FKs, unique constraints, nulabilidad estricta, compatibilidad de consumidores y test de `EXPLAIN` en MySQL 8.4 efímero. No fijar una migración productiva ni invocar `ALTER`, `DROP`, locks globales o privilegios en Aiven.

## 4. Plan de migración FUTURA_NO_EJECUTADA

1. **MG01 Baseline:** inspeccionar entidades EF/tabla de permisos existentes, provider/versión, consumidores y SHA; declarar mapping de continuidad y no-regresión.
2. **MG02 Preflight:** generar dataset sintético tenants A/B, branches A1/A2, miembros activos/revocados y restore snapshot efímero con hashes.
3. **MG03 Expand:** agregar tablas/columnas/índices aditivos en MySQL 8.4 **efímero en CI**; registrar SQL generado, locks, duración y planes.
4. **MG04 Backfill:** migrar fixtures con batches pequeños e idempotencia; preservar asociaciones previas y verificar FKs por empresa.
5. **MG05 Verify/contract:** ejecutar consultas positivas/negativas de tenant A/B, concurrencia, invalidación y EXPLAIN; no quitar columnas antiguas sin prueba causal de consumidores.
6. **MG06 Restore:** revert/restore efímero a snapshot anterior y comprobar conteos, versiones, integridad, digest y logs redactados.

La etapa MG03 necesita una microtarea de **código M03 elegible** y permiso efectivo; esta documentación no proporciona dicha autorización. La reversión de un texto Git nunca revierte datos. Todo fallo de migración o validación posterior mantiene NO_GO hasta evidencia causal.

## 5. Doce fixtures y oráculos positivos/negativos FUTUROS_NO_EJECUTADOS

| Fixture | Resultado positivo futuro | Negativo que debe fallar cerrado |
| --- | --- | --- |
| FX01 | Usuario A, membresía A activa, permiso A, recurso A → lectura permitida | ID de recurso B accedido con JWT A |
| FX02 | Sucursal A1 y vínculo A1 → acción limitada | Recurso de A2 accedido desde A1 |
| FX03 | Grant A en módulo X permite acción X | Plan premium de A intenta acción sin grant |
| FX04 | Revocación A bloquea la siguiente request | JWT viejo mantiene permiso tras revocación |
| FX05 | Export A devuelve solo filas/columnas A | Consulta de B se mezcla en CSV/PDF de A |
| FX06 | Media A con grant vigente produce referencia efímera | URL firmada de B generada desde A |
| FX07 | Soporte GET allowlisted consulta scope exacto | Soporte realiza PUT/DELETE o GET módulo ajeno |
| FX08 | Alta de policy version válida preserva tenant y rol | Policy con tenant NULL se interpreta global |
| FX09 | Dos peticiones A/B paralelas usan contextos aislados | Caché estática intercambia usuarios o grants |
| FX10 | Falla transaccional revierte escritura protegida | Timeout deja grant parcial o auditoría falsa |
| FX11 | Índice/FK de A impide reference ID de B | FK huérfana o colisión cross-tenant se acepta |
| FX12 | Restauración efímera conserva conteos y hashes | Restauración parcial se declara PASS sin comparar |

Estos oráculos no fueron ejecutados. Para acreditarlos en M03–M05 se requieren suites reales con runner, SQL/EF generado, proveedor y logs de GitHub Actions por SHA; las suites canceladas, omitidas o en curso no son PASS obligatorio.

## 6. Seguridad, integración, A0–A5 y reversión

La autoridad efectiva es el conjunto JWT firmado y vigente, membresía activa, rol empresarial y `RolPermiso`/action grant, propietario real de recurso, sucursal, contexto de petición y política ABAC de servidor. La compañía seleccionada por Angular, `X-Empresa-Id`, resourceRef, URL o documento no crea permiso; un NULL de tenant no equivale a empresa global. Registrar correlation IDs opacos sin JWT, secretos, tokens firmados ni PII, y denegar exportación/medios/soporte cross-tenant incluso ante errores de infraestructura.

**A0:** diff exclusivo de este documento Markdown y encabezado/viñetas contiguos de `CHANGELOG_AI.md`; sin EF, SQL, archivos binarios, secretos ni cambios de auth. **A1:** revisar diez entidades lógicas, ocho índices, seis pasos futuros de migración y doce fixtures positivos/negativos etiquetados FUTUROS_NO_EJECUTADOS. **A2:** verificar diseño de denegación por tenant/branch/recurso, soporte GET, privacidad, revocación y control de concurrencia. **A3:** confirmar PLAN M01.01–06 LISTO+A5 y coherencia de contratos M02 sin suponer A5 futura; .NET/Angular/MySQL/E2E = NO_APLICA a este diff exclusivamente documental. **A4:** publicar solo a dev por fast-forward/CAS exact HEAD, comprobar blob/diff y checks Scope Lock/V5 `completed/success` sobre SHA publicado, actualizar BITACORA y releer PLAN/COLA/DAG. **A5:** requiere Task Javier distinta de DEV06, con revisión técnica original exact-SHA; no autoaprobación.

**Rollback:** `git revert` normal del commit de documentos tras reconciliar HEAD nuevo, sin editar BITACORA histórica; cualquier estado institucional se compensa con evento nuevo. No hay migración, REST endpoint, evento, worker, export, archivo, dato real, QA/main/PROD ni servicio externo afectado por esta entrega.
