# SOLQARYN V5 — C15-03.M02.03: persistencia, índices, migraciones y fixtures de seguridad/tenancy

**Autor:** PRIMARY DEV 05 JAVIER; TASK_ID 6adc2e55f47c81919f6374e561687076. **Repositorio:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. **Naturaleza:** diseño documental; NO_IMPLANTADO, sin DDL, migraciones, API nueva, pentest, acceso MySQL ni despliegue.

## 1. Autoridad y predecesores

Fuente ejecutable: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive ID 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. PLAN_MAESTRO fila 2242, COLA G/O=SI, DAG_TECNICO F=VALIDADO/G=SI. La unidad depende de C15-03.M01.01–06+A5, ya LISTO y revisado independientemente. M02.01–02 no se atribuyen como dependencias completadas ni se alteran sus archivos.

Antecedentes: M01.03 identifica quince fuentes reales y doce brechas de evidencia NO_VERIFICADAS; M01.05 especifica AC01–AC16 y N01–N16 FUTUROS_NO_EJECUTADOS; M01.06 establece bloqueos; M02.01 propone SEC01–SEC16 e INV01–INV12. Son contratos, no certificaciones de seguridad.

Fuentes de código inspeccionadas: backend/src/Infrastructure/Persistence/AppDbContext.cs (blob c4b32f6637f1b4426655bb0cd12af8dd9d229866), CommercialTenantPhysicalConstraints.cs (d021861d357de201049fece1b6c63b2c9c218250), TenantQueryBypassGuard.cs (2ba91bab0420f7e0fc840cf9c96e2b974838694d), CommercialCrossTenantWriteGuard.cs (1989e290a855a991782c59b4ee4f147be817f347) y backend/src/API/Filters/TenantPermissionContext.cs (df7d49f3e185c538f3b3dafabb36c3a971a7257d). Backend net10.0, EF Core 10.0.12, MySql.EntityFrameworkCore 10.0.9; esquema real no cambiado.

## 2. Modelo lógico PROPUESTO: sin DbSet ni tabla nueva

La opción preferida en M02 es no crear persistencia adicional para auditoría: usar evidencias CI sanitizadas, hashes verificables y BITACORA append-only; acceso protegido, política de retención y ninguna PII/JWT/secretos en logs. Si una futura M03 requiere almacenamiento propio, necesitará habilitación DAG+A5, revisión de EF, permisos, retención, MySQL efímero y rollback demostrado.

| ID | Concepto FUTURO | Clave/garantía |
| --- | --- | --- |
| PD01 | AuditCut | sourceSha + evidenceRevision; no mezclar ejecuciones |
| PD02 | TenantBoundaryObservation | cutId + verifiedTenantId; solo backend autenticado |
| PD03 | PrincipalPermissionSnapshot | cutId + roleRef + actionRef; sin token ni PII |
| PD04 | SecurityControlResult | cutId + controlKey + caseKey; inicial NOT_EXECUTED |
| PD05 | NegativeScenario | scenarioKey + cutId; rechazo esperado separado de ejecución |
| PD06 | Finding | findingId + cutId; P0/P1 no se declara cero sin test |
| PD07 | RemediationEvidence | findingId + fixSha + retestSha; sin cierre automático |
| PD08 | ArtifactReference | hash + retentionClass; ACL server-side |
| PD09 | PermissionBoundary | verifiedTenantId + resourceOwner; JWT/RBAC y grant vigente |
| PD10 | TransactionProbe | cutId + fixtureId + beforeHash + afterHash |
| PD11 | IndependentReview | microtaskId + authorTaskId + reviewerTaskId distintos |
| PD12 | AuditLedgerEvent | eventId + idempotencyKey; no reescritura de BITACORA |

PD01–PD12 no son entidades EF creadas, ni afirman que sus claves existen en MySQL. El selector X-Empresa-Id es una preferencia de contexto, nunca prueba de autorización. La marca de tenant autorizado proviene del backend y las comprobaciones relacionales de membresía/rol/sucursal vigente. Cliente A no puede consultar objeto B aun conociendo el ID.

## 3. Índices y restricciones HIPOTÉTICOS: no SQL

| ID | Acceso/constraint a estudiar en MySQL efímero | Negativo imprescindible |
| --- | --- | --- |
| IX01 | UNIQUE(cutId, controlKey, caseKey) | replay no duplica resultado |
| IX02 | (verifiedTenantId, cutId, ownerKey) | sin tenant válido, denegar consulta |
| IX03 | (findingId, cutId) | tenant A no enumera finding B |
| IX04 | UNIQUE(idempotencyKey) | mismo ID con otro payload no sobrescribe |
| IX05 | (cutId, resultStatus) | NOT_EXECUTED no se agrega como PASS |
| IX06 | (fixSha, retestSha, findingId) | SHA viejo no valida HEAD nuevo |
| IX07 | (artifactHash, retentionClass) | hash no sustituye ACL/retención |
| IX08 | FK lógica (tenantId, parentId) donde exista recurso privado | referencia cross-tenant A/B aborta transacción |

Antes de cualquier DDL futuro: calcular selectividad, verificar EXPLAIN, duplicados, locks, compatibilidad backward y cardinalidad con fixtures aislados. CommercialTenantPhysicalConstraints define doce FK compuestas empresa/referencia del dominio comercial y no es autorización para alterar esas tablas. Las filas legacy EmpresaId NULL siguen sin propietario atribuido; prohibido backfill automático.

TenantQueryBypassGuard devuelve query sin añadir filtro si AuthorizedReadEmpresaId falta: su seguridad requiere probar además cuarentena middleware, flujo de request e IgnoreQueryFilters; este documento no certifica su protección. CommercialCrossTenantWriteGuard no sustituye filtros de lectura ni RBAC. No aceptar datos tenant de URL/header como autoridad.

## 4. Migración, idempotencia y recuperación CONDICIONADAS

- MF01: identificar scope M03 expresamente habilitado y A5 propia independiente; inventariar el esquema real y sus consumidores.
- MF02: threat model de claves, PII, permisos y retención; impedir tokens, credenciales y IDs cruzados en evidencia.
- MF03: proponer solo expand-only, revisar EF snapshot, FK compuesta, claves únicas y compatibilidad sin romper registros legacy.
- MF04: crear servicio MySQL 8.4 efímero en GitHub Actions, fixtures sintéticos A/B, migración reversible, datos antes/después.
- MF05: ejecutar positivos y negativos, EXPLAIN, concurrencia/idempotencia, rollback transaccional y lectura cruzada denegada.
- MF06: comprobar rollback/restauración efectivamente ejecutados y SHA CI completed/success; QA/main/PROD/Aiven requieren nueva orden expresa.

**MF01–MF06 no fueron ejecutados.** El rollback del documento consiste en git revert normal del changeset en dev, readback y evento reconciliador aditivo, nunca force-push/reset.

## 5. Fixtures y oráculos POSITIVO/NEGATIVO, FUTUROS_NO_EJECUTADOS

| ID | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| FX01 | A consulta su hallazgo con permiso válido | JWT A sobre recurso B deniega sin revelar B |
| FX02 | Selector empresa único y autorizado | header duplicado, inválido o B no eleva permisos |
| FX03 | Sucursal A y membresía vigente autorizan A | sucursal B o miembro revocado es rechazado |
| FX04 | EF consulta sólo datos A con marca verificada | IgnoreQueryFilters sin marca no muestra A/B |
| FX05 | FK cliente-producto A conserva ownership | FK B o EmpresaId NULL no se reasigna |
| FX06 | usuario activo con RBAC correcto obtiene su dato | grant revocado bloquea aun con JWT válido |
| FX07 | evidencia privada usa ACL y hash | enlace caducado/cross-tenant no descarga |
| FX08 | mismo idempotencyKey conserva único resultado | replay no duplica fila ni modifica payload |
| FX09 | job del SHA correcto completed/success | cancelled/skipped/queued no es PASS |
| FX10 | rollback efímero conserva datos A/B íntegros | fallo entre escrituras no deja parcialidad |
| FX11 | sourceSha y blobHash corresponden al cambio | SHA antiguo no certifica HEAD nuevo |
| FX12 | A5 de otra de seis Tasks Javier con revisión causal | autovalidación o sin evidencia no libera DAG |

FX01–FX12 siguen FUTUROS_NO_EJECUTADOS: no se usó MySQL, API HTTP, navegador, worker, datos reales ni pentest. Oráculos futuros exigirán denegación y ausencia de filas ajenas, efectos laterales y secretos en logs.

## 6. Auditorías y alcance de publicación

A0: verificar PD01–PD12, IX01–IX08, MF01–MF06 y FX01–FX12, fuentes y sin encabezados vacíos. A1 documental: cada FX incluye oráculo positivo/negativo, sin afirmar tests de runtime. A2 documental: aislamiento A/B, JWT/RBAC, permisos y NULL legacy, sin alterar guardias ni PII. A3 documental: contraste M01.03/M01.05/M01.06/M02.01, PLAN/COLA/DAG y código EF; builds/.NET/MySQL/Playwright NO_APLICA para este diff solo Markdown, nunca PASS. A4: commit fast-forward con HEAD esperado, verificar diff de dos Markdown, Scope Lock y procedencia V5 completed/success del SHA exacto, estados y BITACORA readback.

Consumidores: M02.04 configuraciones, M02.05 eventos como diseño, M02.06 salida documental y M03 implementación real solo si DAG habilita. A5_PENDIENTE de otra Task Javier distinta del autor; no autoriza liberación técnica, QA/main/PROD, migraciones, webhooks, workers o nueva Task.
