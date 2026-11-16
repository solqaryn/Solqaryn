# SOLQARYN V17 — C04-03.M02.03: modelo de persistencia, índices, migración y datos sintéticos

**Autor:** PRIMARY DEV 04, JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Riesgo:** V15 R0 documental (un futuro cambio de EF/MySQL será R2). **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5, PLAN_MAESTRO fila 658, COLA G/O fila 658, DAG_TECNICO F/G fila 619. **Lock:** `SQV17_C04_03_M02_03` / `2000000658`. Exclusivamente `solqaryn/Solqaryn@dev` y `PROJECT_SCOPE_LOCK=STRICT`.

## A0 — alcance y precondiciones

M02.03 es un contrato de **persistencia futura**, no cambia tablas, DbContext, migrations, SQL, índices, configuraciones, proveedores, datos ni conexiones a Aiven. Deriva de M01 (LISTO y A5 correspondiente) y de `C04-03.M02.01` (entidades/ownership) y `M02.02` (operaciones/DTO/errores). Los A5 propios de M02.01 y M02.02 continúan pendientes y por ello este documento no libera sucesoras que los exijan. Las decisiones comerciales D01–D08 (precios, moneda, cuotas, retención, conciliación y reglas afines) continúan **PENDIENTE**, sin autorización de facturación ni pagos.

Referencias técnicas futuras son MySQL 8.4 efímero para pruebas, EF Core 10 y proveedor Oracle MySql.EntityFrameworkCore 10.0.9 de la arquitectura actual; el proveedor efectivamente ejecutado deberá acreditarse en CI cuando exista migración. No introducir Pomelo o una DB nueva por inferencia.

## Tablas lógicas propuestas, nunca creadas

| Concepto | Clave compuesta propuesta | Invariante principal |
| --- | --- | --- |
| `MetricCatalogVersions` | `(MetricCode, Version)` | versión de unidad inmutable una vez usada |
| `TenantMeteringPolicies` | `(EmpresaId, PolicyRevision)` | pertenencia por tenant y aprobación comercial explícita |
| `TrustedOperationEvidence` | `(EmpresaId, SourceSystem, OperationId)` | prueba de procedencia backend no falsificable por cliente |
| `UsageEvents` | `(EmpresaId, EventId)` | evento append-only, UTC y cantidad/unidad verificadas |
| `UsageDedupKeys` | `(EmpresaId, SourceSystem, IdempotencyKey)` | un único efecto efectivo por origen y tenant |
| `UsageLedger` | `(EmpresaId, LedgerEntryId)` | registro inmutable de efecto de consumo |
| `UsageWindows` | `(EmpresaId, MetricCode, WindowKey, PolicyRevision)` | ventana UTC explícita con reglas de frontera |
| `UsageWindowProjections` | `(EmpresaId, WindowKey, MetricCode, ProjectionRevision)` | snapshot derivado recalculable del ledger |
| `QuotaEntitlementReferences` | `(EmpresaId, EntitlementRevision)` | referencia a autoridad comercial real, no fuente de cobro |
| `UsageAdjustments` | `(EmpresaId, AdjustmentId)` | compensación aditiva con enlace a ledger original |
| `MeteringReconciliationCases` | `(EmpresaId, CaseId)` | divergencia registrada y revisión auditada |
| `MeteringAuditEvidenceLinks` | `(EmpresaId, EvidenceId)` | propósito, autor/revisión/hash sin secretos |

Toda FK de entidad tenant-owned debe incluir `EmpresaId` en la relación y en la restricción de referencia; ninguna tabla global se usa para inferir ownership. El catálogo de métricas es global y **no** contiene consumo ni ACL empresarial. La propuesta no concede permisos a un usuario que tenga `EventId` conocido.

## Índices lógicos previstos IDX01–IDX12 (NO DDL)

| Índice candidato | Columnas y orden razonado | Riesgo a comprobar en CI futura |
| --- | --- | --- |
| IDX01 | UNIQUE `UsageEvents(EmpresaId,EventId)` | idempotencia frente a colisión entre empresas |
| IDX02 | UNIQUE `UsageDedupKeys(EmpresaId,SourceSystem,IdempotencyKey)` | carreras de dos workers con misma clave |
| IDX03 | `UsageEvents(EmpresaId,OccurredUtc,MetricCode)` | rangos UTC y paginación restringida por tenant |
| IDX04 | `UsageEvents(EmpresaId,SourceSystem,OperationId)` | trazabilidad del origen backend |
| IDX05 | UNIQUE `UsageLedger(EmpresaId,LedgerEntryId)` | doble commit y compensación |
| IDX06 | `UsageLedger(EmpresaId,EventId,EntryKind)` | lectura de evento+ajustes sin cruzar empresa |
| IDX07 | UNIQUE `UsageWindows(EmpresaId,MetricCode,WindowKey,PolicyRevision)` | diferencias de versión/periodo |
| IDX08 | `UsageWindowProjections(EmpresaId,WindowKey,MetricCode,ProjectionRevision)` | lectura exacta de proyecciones |
| IDX09 | `TenantMeteringPolicies(EmpresaId,PolicyRevision)` | revocación/consistencia de política |
| IDX10 | `UsageAdjustments(EmpresaId,OriginalLedgerEntryId)` | histórico aditivo verificable |
| IDX11 | `MeteringReconciliationCases(EmpresaId,WindowKey,Status)` | reproceso sin filtración multiempresa |
| IDX12 | `MeteringAuditEvidenceLinks(EmpresaId,SourceEventId,CreatedUtc)` | consulta segura y retención |

**Planificación:** revisar longitudes reales de claves/índices bajo MySQL 8.4, collation ordinal y precisión temporal, selectividad, cardinalidad y `EXPLAIN` en datos sintéticos. Índices no son autorización: toda consulta y vínculo incluye filtro `EmpresaId`. Un texto de SKU, hash, cursor o evento externo no puede servir de `EmpresaId`. Evitar índices innecesarios si los planes reales de consulta muestran costes indebidos.

## Secuencia reversible propuesta M01–M08, NO EJECUTADA

- **M01 Preflight:** snapshot de esquema vigente y backup restaurable verificado en MySQL 8.4 efímero; identificar conflictos y ownership actual.
- **M02 Expand:** crear tablas aditivas vacías y claves de tenant; ninguna eliminación de columna ni cambio de datos productivos.
- **M03 Backfill seguro:** únicamente información histórica legitimada y con autorización explícita, cursor idempotente y delimitación de tenant; sin inferir cantidades de logs incompletos.
- **M04 Índices y FK:** instalar índices compuestos y referencias con prueba de cardinalidad/colisión inter-tenant; rollback antes de uso vivo.
- **M05 Dual-read diagnóstico:** comparar ledger/proyecciones sintéticas, sin escritura financiera duplicada ni habilitar pagos.
- **M06 Cutover controlado:** requiere decisión comercial, permisos, CI categoría Integration real, semáforo, copia/restore y autorización de entorno separada; NO permitido por M02.03.
- **M07 Rollback:** volver a lectura anterior bajo gate autorizado, conservar ledger y eventos aditivos, registrar compensaciones sin `DROP` destructivo de historia.
- **M08 Contract diferido:** retirar compatibilidad antigua sólo después de medición, auditoría y nuevo permiso; ningún paso irreversible en esta microtarea.

Si falla cualquier etapa, detener escritura nueva, registrar razón, preservar evidencias y ejecutar compensación no destructiva. No reinterpretar un FAIL/CANCELLED/SKIPPED como PASS; no editar PLAN histórico ni BITACORA previa.

## Datos sintéticos propuestos (no existentes ni insertados)

| Escenario | Empresa A | Empresa B | Resultado esperado FUTURO |
| --- | --- | --- | --- |
| S01 | EventId compartido 7 | EventId compartido 7 | ambas filas válidas separadas; ninguna lectura cruzada |
| S02 | misma IdempotencyKey dos veces | aislada | un único efecto de A |
| S03 | cantidad bytes 4096 | operaciones 3 | magnitudes no mezcladas |
| S04 | periodo UTC antes de corte | después del corte | ventanas diferentes con frontera estable |
| S05 | actor revocado | actor vigente | A rechaza nueva acción; B normal |
| S06 | timeout antes del COMMIT | sin fallo | ningún éxito falso ni doble ledger |
| S07 | dos jobs simultáneos same key | ninguno | sólo un ganador y evidencia consistente |
| S08 | ajuste negativo de ledger | otro ledger | original inmutable; corrección propia sin B |
| S09 | política no aprobada | política aprobada | A NO_GO, B no recibe permisos de A |
| S10 | cursor firmado de B | contexto A | rechazo sin revelar contenido de B |
| S11 | dedupe clash con payload distinto | libre | conflicto y cero nuevos efectos |
| S12 | restore de snapshot sintético | aislada | mismo conteo sin reconstrucción destructiva |

El estado de **todas las pruebas S01–S12 es NO_EJECUTADO / NO_MEDIDO**. No se aporta cifra falsa de duración, throughput, registros migrados o cobertura de MySQL.

## A1/A2/A3 por riesgo

**A1 R0:** inspeccionar que las 12 entidades, 12 candidatos de índices, ocho etapas de migración y doce pares de pruebas futuras están definidos y son internamente coherentes con los DTO M02.02 y ownership M02.01. **A2:** el contrato prescribe tenant A/B, RBAC, revocación, PII minimizada, no facturación sin D01–D08, no cross-tenant JOIN, idempotencia, rollback. No es una prueba de producción. **A3:** MySQL 8.4, EF, integración, HTTP, xUnit, Angular, Playwright y proveedor externo son `NO_APLICA` en este cambio de Markdown, no PASS. Cuando se implementen tablas o migraciones en otra microtarea R2, exigir job que ejecute efectivamente `Category=Integration` en MySQL 8.4 aislado, logs con 0 FAIL, migración más reversión y pruebas de concurrencia y tenant A/B.

## A4, A5 y rollback

Un único commit fast-forward/CAS debe contener este Markdown y una sección de `CHANGELOG_AI.md` con encabezado y viñetas juntas; se requiere `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en `completed/success` del SHA fuente. Readback de PLAN/COLA/DAG/BITACORA y candado V17; si CI requerida está en progreso, estado VALIDANDO sin afirmar PASS. A5 requiere Task Javier distinta de DEV04 y revisión propia de artefactos; no habilita M03 ni compras, secretos, QA/main/PROD. Revertir por `git revert` normal sin force-push, conservando evidencias.
