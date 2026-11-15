# SOLQARYN V5 — C14-02.M02.03: persistencia, índices, migraciones y datos de prueba de aceptación de seguridad

**Microtarea:** `C14-02.M02.03` · **Autor:** PRIMARY DEV 02 — JAVIER (`6af16bb9492c8191b8e8c7d606f11507`) · **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT` · **Fecha:** 2026-11-14.

**Autoridad única:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica, `AGENTS.md`, Scope Lock estricto.

**Entrega:** contrato exclusivamente **DOCUMENTAL** sobre datos potenciales para aceptación de seguridad: propiedad, índices, migraciones reversibles hipotéticas y fixtures sintéticos positivos/negativos. **No se agrega** entidad, tabla, columna, índice físico, migración, seeder, permiso, usuario, clave, endpoint, evento, CI nueva ni operación en Aiven/MySQL. Los tests descritos son **FUTUROS_NO_EJECUTADOS**; no existe certificación de cero P0/P1, BOLA/IDOR o recuperación productiva por esta entrega.

## 1. Dependencias, capas y baseline verificado

Al tomar `C14-02.M02.03`, PLAN H2062=PENDIENTE, I2062 sin owner, COLA G/O=SI y `DAG_TECNICO` F=VALIDADO/G=SI para el ID exacto; precedentes M01.01–06 están LISTO con A5 independiente. Cada sucesora se recalcula por fórmula después de una transición y requiere nueva prueba de identidad/estado/DAG.

| Fuente en HEAD de dev | Blob Git | Hecho verificable y límite |
| --- | --- | --- |
| `docs/v5/c14-02-m01-06-gates-bloqueo-rollback-liberacion-seguridad.md` | `6cb629af7993a6f4eb7cb8518f538f7ed196326f` | Puertas B01–B12 contractuales, sin pentest ejecutado |
| `docs/v5/c14-02-m02-01-modelo-ownership-invariantes-aceptacion-seguridad.md` | `1bf7ca3f8c549212a24e10ed1afefb2fdedea972` | Modelo conceptual SE01–14, SI01–16, oráculos SA01–12 |
| `docs/v5/c14-02-m02-02-api-dto-errores-validaciones-compatibilidad-seguridad.md` | `9de9d398cc2d238ce2ba66f27890c54b3317d782` | Doce contratos API propuestos y errores, no código compilado |
| `backend/src/Infrastructure/Persistence/CanonicalMySqlPhysicalContract.cs` | `f590f85fa5e372051ef3d2a3fab9552f6e7c8cbb` | Declara verificaciones y suplementos físicos MySQL canónicos; existencia en código NO acredita su aplicación en Aiven |

Arquitectura futura: ASP.NET Core 10 Domain/Application/Infrastructure/API; EF Core 10.0.12 + proveedor Oracle `MySql.EntityFrameworkCore` 10.0.9; MySQL 8.4; Angular 22.2.1 consumidor visual. El backend verifica autenticación JWT, RBAC, membresía y tenancy; la UI no otorga grants. Ningún modelo descrito aquí sustituye tablas actuales ni introduce claves foráneas en vivo.

## 2. Modelo de persistencia PROUESTO_NO_IMPLEMENTADO

Todas las estructuras son **candidatas lógicas**, sin nombres SQL, DDL ni esquema autorizado.

| ID | Agregado lógico candidato | Clave y propietario obligatorios | Regla de retención/aislamiento |
| --- | --- | --- | --- |
| PD01 | `SecurityAcceptanceRun` | `runId`, scope autorizado, SHA y versión | Snapshot de ámbito explícito, nunca alcance global implícito |
| PD02 | `SecurityAcceptanceSubject` | ref opaca de actor y audiencia | Nunca token JWT, contraseña ni email crudo |
| PD03 | `SecurityAcceptanceTenant` | `tenantId` server-side y owner | Partición lógica A/B, sin unir evidencia de B en A |
| PD04 | `SecurityAcceptanceCase` | `caseId`, versión y `runId` | Caso positivo/negativo con resultado no inventado |
| PD05 | `SecurityAcceptanceResult` | `caseId`, estado `PASS/FAIL/NOT_RUN` | `NOT_RUN` no satisface ningún gate |
| PD06 | `SecurityEvidenceRef` | URI opaco y checksum autorizados | No archivos confidenciales, firma temporal ni secretos en fila pública |
| PD07 | `SecurityFinding` | `findingId`, severidad, tenant/scope | Hallazgo P0/P1 permanece abierto sin retest genuino |
| PD08 | `SecurityFindingRetest` | `findingId`, SHA causal, revisor | Aprobación independiente, sin autocertificación |
| PD09 | `SecurityBoundaryProof` | `caseId`, tenant A/B, permiso probado | Deny explícito, sin almacenar contenido cruzado |
| PD10 | `SecurityAuditChange` | `runId`, actor, correlación opaca | Evidencia append-only lógica, retención autorizada |
| PD11 | `SecurityAcceptanceLease` | `scopeId`, owner y versión | Evitar dos escritores, CAS atómico requerido en persistencia real |
| PD12 | `SecurityAcceptanceSummary` | `runId`, conteos y SHA comprobado | No declarar P0/P1=0 por ausencia de resultados |

Los campos exactos, políticas de retención, cifrado, claves y ubicación requieren aprobación técnica/privacidad antes de crear objetos físicos. El nombre de un DTO no constituye autorización para agregar una tabla.

## 3. Índices y restricciones candidatas

| ID | Restricción futura | Test negativo imprescindible |
| --- | --- | --- |
| IX01 | Unicidad `(scopeId,runId)` | Repetición no crea segunda corrida |
| IX02 | Unicidad `(runId,caseId,version)` | Reintento no duplica resultado |
| IX03 | Consulta de casos siempre acotada a scope y tenant | A nunca pagina datos de B |
| IX04 | Relación de `Finding` con `Run` y `Tenant` del mismo dueño | FK cruzada A→B rechazada |
| IX05 | Unicidad de retest por `findingId,sourceSha,reviewerId` | No repetir A5 ni simular SHA |
| IX06 | Índice de retención/acceso por ámbito y expiración | Evidencia vencida no autorizada |
| IX07 | Versionado/compare-and-swap en transición de hallazgo | Escritor viejo no pisa revisión reciente |
| IX08 | Referencia de artifact restringida y minimizada | Ruta externa u objeto ajeno rechazado |

Ningún índice se declara creado. Para MySQL real, diseñar con política de `NULL`, collation, longitud, transacciones y FKs del proveedor efectivo, y comprobar con MySQL 8.4 efímero antes de cualquier migración. La integridad física no se deduce de un documento.

## 4. Plan de migración y reversión **futuro**

1. **Preflight:** determinar si el modelo ya existe, compatibilidad exacta con EF/AppDbContext, tenants, autorizaciones, volumen, costes, plan versionado y ninguna escritura simultánea.
2. **Diseño aditivo:** nueva entidad/índice sólo bajo microtarea M03 con permiso y semántica aprobados; no renombrar datos históricos ni modificar filas vivas por inferencia.
3. **Validación CI:** migrar a MySQL 8.4 efímero con datos sintéticos A/B, verificar constraints y negativas; comparar schema y registros con contratos Domain/Application.
4. **Compatibilidad:** conservar lectura de consumidores antiguos si se aprobó explícitamente; de otro modo feature queda DENY y no usa fallback global.
5. **Rollback:** ensayo reversible sobre base descartable, sin pérdida de registros legítimos ni apertura cross-tenant; documentar decisiones no reversibles y autorización humana concreta.
6. **Promoción:** no está autorizada en este documento. A5 independiente, Scope Lock exact-SHA y orden separada para QA/main/PROD son necesarias cuando apliquen.

## 5. Fixtures sintéticos de aceptación futura

| Caso | Clase | Entradas hipotéticas | Resultado exigible |
| --- | --- | --- | --- |
| P01 | + | Tenant A, actor con grant y scope A | Solo resultados de A, auditados y versionados |
| P02 | + | Retest genuino en SHA válido por otro revisor | Transición única después de comprobar CI y diff |
| P03 | + | Insert de hallazgo con relación interna correcta | FK, ownership y checksum consistentes |
| P04 | + | Rollback de esquema en MySQL descartable | Igualdad datos A/B antes/después |
| N01 | - | ID de B en request de A | DENY sin lectura ni escritura B |
| N02 | - | Token/membresía revocada | DENY independientemente de plan o flag |
| N03 | - | FK de hallazgo A hacia corrida B | Rechazo físico, sin registro huérfano |
| N04 | - | Misma clave idempotente en otra empresa | Sin compartir efecto/resultado |
| N05 | - | `NOT_RUN`/CI cancelada declarada PASS | Rechazar cierre |
| N06 | - | Dos escritores con misma versión | Un solo ganador CAS, otro conflicto |
| N07 | - | Evidencia con secreto/JWT o PII B | Rechazo/redacción y mínima retención |
| N08 | - | Retest sin SHA o revisor igual al autor | A5 rechazado |
| N09 | - | Restaurar dump A en DB B | Bloquear, no conectar destino |
| N10 | - | Cambiar índices/DDL en Aiven por este documento | NO_GO por falta de autorización |
| N11 | - | Supuesto cero P0/P1 sin pruebas | Bloquear conclusión y promoción |
| N12 | - | Migración no reversible o datos ajenos afectados | Fallar seguro y requerir plan/autoridad |

Ninguno de estos 16 casos fue ejecutado. Las P/N definen oráculos medibles para una microtarea de código posterior. No hay migración nueva, cobertura certificada ni runtime modificado.

## 6. Cierre A0–A5 proporcional y rollback documental

**A0:** verificar dos archivos Markdown, blobs reales y sección CHANGELOG autocontenida sin encabezados vacíos; revisar que no se introduzcan secretos ni identidad retirada. **A1:** revisar 12 agregados, 8 índices, 6 etapas y 4 positivos/12 negativos como diseño, nunca afirmar resultado de `dotnet test`. **A2:** comprobar DENY para tenant A/B, JWT, grants, BOLA/IDOR, PII, artifact replay, revisión independiente. **A3:** conciliar tres contratos existentes y el `CanonicalMySqlPhysicalContract` sin alterar código; backend/Angular/Docker/MySQL tests = `NO_APLICA_POR_DIFF_SOLO_MARKDOWN`. **A4:** publicación `dev` fast-forward con expected HEAD; checks `verify-solqaryn-project-scope` y V5 completos y exitosos sobre SHA publicado, readback PLAN/COLA/DAG/BITACORA. **A5:** `A5_PENDIENTE` por otra Task Javier distinta del autor; no QA/main/PROD.

**Rollback de esta entrega:** `git revert` normal del commit documental y evento BITACORA aditivo; no fuerza ramas, no restaura bases ni modifica histórico.
