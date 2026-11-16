# SOLQARYN V17 — C11-01.M02.02: contratos conceptuales API, DTO, errores, validación y compatibilidad

**ID:** C11-01.M02.02 · **PLAN:** fila 1593 · **Autor:** PRIMARY DEV 04, Task 6aeacb7820ac8191b7d768a6f33dfa09 · **Ámbito:** solqaryn/Solqaryn@dev · **PROJECT_SCOPE_LOCK=STRICT** · **Riesgo:** R0, sólo documentación sin ejecución.

**Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), Skill canónica, V15–V17 y PLAN/COLA/DAG. La microtarea pide definir API, DTO, errores, validaciones y compatibilidad del futuro modelo relacional normalizado. Ningún nombre de operación, DTO o código de respuesta de este documento demuestra un endpoint ya implementado.

## 1. Preflight, fuentes y límites

Antes del CLAIM: PLAN H1593=PENDIENTE e I1593 vacío; COLA G/O=SI; DAG_TECNICO fila 1554 F=VALIDADO G=SI. Se adquirió candado SQV17_C11_01_M02_02 (ID 2000001593), H fue cambiado con CAS occurrencesChanged=1, owner DEV04 y evento BITACORA verificados. Una EN_PROGRESO propia.

- C11-01.M01.01, `docs/v5/c11-01-m01-01-objetivo-no-alcance-modelo-relacional.md`, blob `530958065528c4d2f8b0d9044b2e55776b0697e6`: 14 invariantes y exclusiones.
- C11-01.M01.03, `docs/v5/c11-01-m01-03-inventario-codigo-evidencia-modelo-relacional.md`, blob `4f45d732115187fcc9cc6b3a0e85e4c2896c96b8`: 15 fuentes, 12 brechas de evidencia, no vulnerabilidades probadas.
- C11-01.M01.05, `docs/v5/c11-01-m01-05-criterios-aceptacion-modelo-relacional.md`, blob `5873d61759e973762a780125408f92f9c179991f`: 16 positivos y 16 negativos futuros.
- C11-01.M01.06, `docs/v5/c11-01-m01-06-gates-rollback-liberacion-modelo-relacional.md`, blob `f6908d23b07a1e0646e245287add9be8c18f7913`: 16 gates y rollback; A5 de otro autor registrada.
- C11-01.M02.01, `docs/v5/c11-01-m02-01-entidades-ownership-relaciones-invariantes.md`, blob `d1b1a09a096fdfbaa2f2acacb33e53f05bff39fc`: 16 entidades/invariantes, 12 pares futuros; LISTO_DEV y A5 de otra Task aún pendiente.

**Límites reales:** Domain <- Application <- Infrastructure, API como composition root; ASP.NET Core 10, EF Core 10.0.12 y MySql.EntityFrameworkCore Oracle 10.0.9/MySQL 8.4 objetivo. Este cambio sólo define contratos: sin DbContext, DDL, repositorios, migraciones, endpoints, JWT, Angular, datos o transacciones. EmpresaId desde cliente nunca es autoridad. Las relaciones FK, índices o filtros presentes en fuente no equivalen a certificación de esquema Aiven.

## 2. Ocho operaciones conceptuales, NO IMPLEMENTADAS

| Código | Intención futura | Entrada, autorización y condición de seguridad |
| --- | --- | --- |
| OP01 | ReadScopedEntity | Tipo/id en allowlist; actor/tenant verificados por backend; deny cross-tenant sin enumeración |
| OP02 | ListScopedEntities | Paginación acotada y filtros parametrizados por empresa; sin filas B |
| OP03 | ResolveEffectiveMembership | UsuarioEmpresa vigente, rol scoped y revocación; RolId legacy no eleva permisos |
| OP04 | ReconcileLegacyOwnership | Quarantine para Cliente/Sucursal con EmpresaId NULL hasta decisión y evidencia |
| OP05 | ValidateParentChildTenant | Padre, hijo, empresa y sucursal coherentes; FK compuesta sólo propuesta |
| OP06 | UpdateWithVersion | ExpectedVersion/idempotencyKey, rechazo a replay y segundo escritor |
| OP07 | CommitCommercialUnitOfWork | Venta, stock, factura y pago todo-o-nada, con rollback real futuro |
| OP08 | ReadSchemaCompatibility | Mappings, migration history, índices y provider verificables sin ejecutar DDL |

## 3. Doce DTO contractuales propuestos, NO clases C# nuevas

| Código | Campos futuros | Invariante de validación |
| --- | --- | --- |
| D01 ScopedReadRequest | type, id, fieldMask | Tipo y campos allowlist |
| D02 ScopedListQuery | cursor, size, sort, filters | Máximo acotado, orden estable y SQL parametrizado |
| D03 SafeEntityResponse | publicId, safeAttributes, version | Sin secretos, PII excesiva ni B |
| D04 MembershipSnapshot | userId, empresaId, roles, revokedAt | Backend revalida permisos; no confiar cliente |
| D05 LegacyOwnershipDecision | entityRef, evidenceRef, state | NULL nunca concede acceso |
| D06 ParentChildCandidate | parent, child, tenant, sucursal | FK tenant-aware comprobable luego |
| D07 VersionedCommand | expectedVersion, idempotencyKey, allowedPayload | Una mutación lógica |
| D08 UnitOfWorkResponse | state, operationId, versions | Éxito sólo tras commit atómico |
| D09 SchemaCompatibilityReport | expected, observed, mismatches | Informe sólo lectura |
| D10 ControlledProblem | code, safeMessage, correlationId | No SQL, token, PII ni cadena |
| D11 TestEvidenceLocator | sourceSha, testedSha, runId, jobName | SHA y pruebas causales completed/success |
| D12 MigrationReadinessGate | decision, backup, rollback, reviewer | NO_GO sin autorización y MySQL efímero |

## 4. Dieciséis errores contractuales futuros

| Código | Negativo a reconocer | Respuesta futura segura (no certificada) |
| --- | --- | --- |
| ER01 | Token inválido/ausente | 401 controlado |
| ER02 | Rol revocado | 403 sin elevación |
| ER03 | Tenant A consulta B | 403/404 sin filtrar existencia |
| ER04 | Padre de B e hijo de A | 409/422, ninguna escritura |
| ER05 | Legacy EmpresaId=NULL | Quarantine, nunca asignación silenciosa |
| ER06 | Filtro/tipo arbitrario | 400, no interpolación SQL |
| ER07 | ID no existente | 404 genérico |
| ER08 | IdempotencyKey conflictiva | 409, no segundo efecto |
| ER09 | Versión stale | 409, no lost update |
| ER10 | Venta/stock/factura a medio camino | Rollback atómico y error seguro |
| ER11 | FK/índice único no válido | Error acotado, sin SQL interno |
| ER12 | Migración no reversible | NO_GO, preservar snapshot |
| ER13 | Soporte read-only intenta mutar | Deny, cero cambios |
| ER14 | CI skipped/cancelled/failed/pending | No A4 PASS ni LISTO técnico |
| ER15 | Payload/paginación fuera de límites | 400/413 según contrato futuro |
| ER16 | Timeout/rate limit/cancelación | Sin retry duplicador de efecto |

## 5. Doce pares de oráculos FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro | Negativo que debe fallar |
| --- | --- | --- |
| P01/N01 | A lee su entidad A | A pide ID B sin disclosure |
| P02/N02 | Lista A paginada | Filtro SQL arbitrario/pageSize gigante |
| P03/N03 | UsuarioEmpresa vigente autoriza | Rol revocado y RolId legacy denegados |
| P04/N04 | Cliente NULL preservado | NULL tratado como permiso |
| P05/N05 | Padre/hijo A válido | Padre B mezclado en A |
| P06/N06 | Clave natural scoped duplicable en A/B | Índice global bloquea empresa B |
| P07/N07 | Version vigente permite update | Stale overwrite rechazado |
| P08/N08 | Retry idempotente no duplica | Dos reintentos paralelos duplican venta |
| P09/N09 | Fallo parcial hace rollback | Stock/factura divergentes |
| P10/N10 | Soporte puede leer | Soporte ejecuta SaveChanges |
| P11/N11 | MySQL efímero Up/Down con checksum | Down borra registros/rompe FKs |
| P12/N12 | Error controlado con correlación | Log expone PII/token/SQL |

**Ejecución de pruebas runtime en M02.02: 0/12.** Los oráculos futuros deberán convertirse en tests xUnit, HTTP autenticado/JWT, tenancy A/B, roles/revocación y MySQL 8.4 efímero con proveedor Oracle EF real cuando un cambio ejecutable sea autorizado; ningún mock sustituye integración.

## 6. Compatibilidad, A0–A5 y rollback

**Compatibilidad futura:** consumidores legacy con EmpresaId nullable y versiones de API/DTO deben coexistir durante expand/contract; no endurecer NOT NULL sin censo/backfill autorizado; índices se justifican con EXPLAIN; API/Angular jamás sustituyen controles backend; esquemas reales se revisan sin tocar Aiven productivo.

**A0:** diff sólo este Markdown y sección de CHANGELOG autocontenida, scope/plan/dag y cinco antecedentes. **A1:** revisión estática de 8 operaciones, 12 DTO, 16 errores y 12 pares positivos/negativos no ejecutados. **A2:** requerimientos conceptuales tenant A/B, revocación RBAC, IDOR, nullable legacy, soporte read-only, PII/secretos. **A3:** pruebas HTTP, EF, MySQL, .NET, Angular, Playwright NO_APLICA a R0 sin runtime; no declarar PASS. **A4:** source SHA, Scope Lock y V5 CHANGELOG completed/success exactos, dos blobs, PLAN/COLA/DAG/BITACORA readback. **A5:** pendiente de Task Javier distinta de la autora.

**Rollback:** corrección aditiva o git revert normal con HEAD/CAS y readback; conservar BITACORA, owner y fórmulas. Operaciones reales futuras exigen R2, permisos, prueba en MySQL 8.4 efímero, respaldo y restauración verificable. No QA/main/PROD, datos reales, cuentas externas ni pagos.
