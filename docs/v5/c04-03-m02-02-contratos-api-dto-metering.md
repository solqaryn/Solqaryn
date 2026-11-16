# SOLQARYN V17 — C04-03.M02.02: contratos de operaciones, DTO, errores y compatibilidad de metering

**Autor:** PRIMARY DEV 04 JAVIER (`6aeacb7820ac8191b7d768a6f33dfa09`). **Clasificación V15:** R0, documento conceptual sin API ni runtime. **Identidad:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fuentes:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES; PLAN_MAESTRO fila 657; COLA fila 657 G/O=SI; DAG_TECNICO fila 618 F=VALIDADO/G=SI. **Candado:** `SQV17_C04_03_M02_02`, ID `2000000657`.

## A0 — objetivo, dependencias y límites

Se especifican operaciones, solicitudes, respuestas, códigos de error, validaciones y compatibilidad para el futuro metering por tenant (usuarios, almacenamiento, operaciones, archivos y uso). Se apoyan en el modelo conceptual `C04-03.M02.01` ya LISTO, en M01.01–06 LISTO con A5 para abrir M02, y en el contrato terminal M01.06. **M02.01 aún requiere su propio A5 independiente** para sucesoras que expresamente lo exijan; este documento no lo concede. **D01–D08** de política/precios/retención siguen pendientes. Aquí NO se implementan controllers, endpoints, DTO de código, permisos, persistencia, workers, migraciones, cuotas, facturas, cobros ni APIs de proveedor; las rutas siguientes son **nombres de operaciones conceptuales**, no URLs disponibles.

## Contratos conceptuales C01–C08

| Contrato futuro (NO endpoint) | Input mínimo esperado | Output lógico esperado | Precondición de confianza |
| --- | --- | --- | --- |
| C01 `ProposeUsageEvent` | `TrustedOperationRef, MetricCode, MetricVersion, SourceEvidenceRef, IdempotencyKey` | `EventRef, Decision, EvidenceRef` | Actor+EmpresaId derivados del backend; cantidad de fuente verificable |
| C02 `ReadUsageWindow` | `WindowKey, MetricCode, Cursor?` | `WindowRevision, Entries, NextCursor?` | RBAC lectura y filtros tenant server-side |
| C03 `ReadQuotaPreview` | `MetricCode, WindowKey, PolicyRevision` | `Measured, LimitState, PolicySource, IsAuthoritative=false` | Snapshot comercial vigente; preview no habilita uso |
| C04 `ReconcileUsageWindow` | `WindowKey, ExpectedLedgerRevision, CorrelationId` | `CaseRef, DiscrepancyState, AuditRef` | Servicio autorizado e idempotencia; nunca factura automáticamente |
| C05 `ProposeMeteringAdjustment` | `OriginalEntryRef, ReasonCode, EvidenceRef, ExpectedRevision` | `AdjustmentRef, ProposedState` | Permiso de aprobación distinto; ajuste aditivo reversible |
| C06 `ReadUsageAuditEvidence` | `EventRef, PurposeCode, Cursor?` | `MinimalEvidence, RedactionPolicy, NextCursor?` | Propiedad tenant, finalidad y privacidad comprobadas |
| C07 `ValidateMeteringPolicy` | `PolicyRevision, MetricVersions, WindowRules` | `Violations[], SafeToApply=false` | Decisiones D01–D08 aprobadas externamente antes de aplicación |
| C08 `SimulateUsageRollback` | `LedgerRevision, WindowKey, FailureScenario` | `DryRunOnly, Compensations[], UnresolvedRisks[]` | Sólo simulación aislada; no DB/PROD |

Los identificadores enviados por el cliente son selectores no autoritativos. Cada operación futura debe recibir `EmpresaId`, actor, roles, fuente de cantidad y revisión comercial desde un contexto confiable backend, no desde campos admitidos libremente. Las respuestas nunca contienen secretos, DSN, JWT ni PII completa. Cualquier `Cursor` deberá firmarse o vincularse a tenant, ventana y versión con expiración y rechazo en cross-tenant.

## DTO conceptuales y propiedades de validación

- `UsageSubmissionDraft`: referencias fuente, idempotencia y métrica/versionado; no acepta `EmpresaId` elegible desde browser, cantidad arbitraria ni importe.
- `UsageEventDecision`: `Accepted|Duplicate|Denied|PendingVerification`, ID de evidencia y estado explícito; `PendingVerification` no se factura.
- `UsageWindowQuery`: `MetricCode`, ventana UTC, cursor validado y límite de página acotado.
- `UsageWindowSnapshot`: versión fuente, entradas mínimas, totales tipados por unidad y política aplicada.
- `QuotaPreviewEnvelope`: límite opcional tipado, `IsUnlimited` explícito autorizado, razones de denegación; no confundir NULL/0.
- `ReconciliationCaseDraft`: revisiones esperadas, discrepancia y `CorrelationId` para dedupe.
- `AdjustmentProposalDraft`: referencia a entrada original, justificación, sujeto revisor y autorización separada.
- `MeteringAuditView`: actor/fecha/revisión/hash de evidencia mínimos, con redacción y propósito de lectura.

**Validaciones V01–V16:** V01 actor autenticado; V02 `EmpresaId` backend; V03 membresía actual; V04 RBAC específico; V05 `MetricCode` permitido; V06 `MetricVersion` inmutable; V07 `IdempotencyKey` no vacío y con alcance tenant+origen; V08 fuente backend verificable; V09 misma empresa en referencias; V10 fecha UTC y ventana sin ambigüedad; V11 cursor firmado/ligado a tenant; V12 límite de página acotado; V13 `ExpectedRevision` CAS sin pérdida; V14 entitlement vigente y semántica NULL explícita; V15 entradas inmutables/ajustes aditivos; V16 fallo parcial, cancelación o timeout sin doble efecto. Regla fail-closed ante cualquiera de V01–V16 fallida; sin fallback cliente ni acceso global accidental.

## Taxonomía de errores conceptuales E01–E14

| Código futuro | Significado y respuesta segura |
| --- | --- |
| E01 `ACTOR_REQUIRED` | Falta identidad autenticada; denegar sin inferir tenant |
| E02 `TENANT_SCOPE_DENIED` | Tenant o recurso ajeno; respuesta genérica sin revelar existencia |
| E03 `MEMBERSHIP_REVOKED` | Membresía/rol inválido; ninguna acción persistente |
| E04 `METRIC_UNKNOWN` | Métrica/versionado no aprobados; no utilizar fallback unitario |
| E05 `SOURCE_UNVERIFIED` | Cantidad o procedencia no acreditadas; rechazar evento |
| E06 `IDEMPOTENCY_CONFLICT` | Clave repetida con payload distinto; no duplicar consumo |
| E07 `WINDOW_INVALID` | UTC, límites o versión inconsistente; sin evento |
| E08 `POLICY_UNAPPROVED` | D01–D08 pendientes o política revocada; no habilitar cobros |
| E09 `ENTITLEMENT_DENIED` | Cuota/estado no autorizado; negar, sin interpretar NULL como ilimitado |
| E10 `REVISION_CONFLICT` | CAS/ETag no coincide; reconsultar autoridad antes de reintentar |
| E11 `CURSOR_INVALID` | Cursor caducado/ajeno/manipulado; sin filtración |
| E12 `STORAGE_UNAVAILABLE` | Transacción no confirmada; no informar éxito |
| E13 `RECONCILIATION_REQUIRED` | Ledger/proyección divergen; caso auditado |
| E14 `CANCELLED_OR_TIMEOUT` | Cancelación con resultado indeterminado; consultar idempotencia antes de retry |

No se definen aquí códigos HTTP reales, porque no hay controller ni endpoint en el diff. Una futura capa API mapeará la taxonomía a HTTP con documentación OpenAPI versionada sin revelar información multiempresa.

## Compatibilidad y versionado

- **Compatibilidad C01:** agregar campos opcionales sólo con semántica explícita y validaciones de ausencia; nunca convertir ausente en autorización.
- **C02:** los nombres y unidades de métricas llevan versión; cambios de unidad/magnitud exigen nueva versión, no edición in-place.
- **C03:** errores existentes no se transforman en éxito silencioso; clientes antiguos deben recibir denegación segura.
- **C04:** tokens de paginación o idempotencia no se reutilizan entre empresas/ventanas/versiones.
- **C05:** revisar consumidores Angular/HTTP/SDK antes de publicar contratos reales, sin introducirlos en esta microtarea.
- **C06:** rollback futuro es despliegue reversible y compensación, nunca eliminación de ledger/bitácora.
- **C07:** modelo documental no autoriza decisiones de pricing, facturación, retención ni pago.
- **C08:** ningún cambio real de API/DB llega a QA/main/PROD por estar M02 LISTO.

## A1/A2 — escenarios positivos/negativos FUTUROS, NO EJECUTADOS

| Positivo futuro | Negativo obligatorio |
| --- | --- |
| P01 actor de A consulta ventana A | N01 actor de A enumera ventana B |
| P02 retry idéntico retorna mismo evento | N02 clave repetida con payload distinto suma dos |
| P03 métrica versionada conserva unidad | N03 cliente cambia bytes/unidades para inflar cargo |
| P04 revocación impide operaciones nuevas | N04 JWT/rol expirado permite lectura de consumo |
| P05 ventana UTC conserva corte determinado | N05 reloj cliente altera periodo |
| P06 CAS detecta versión obsoleta | N06 overwrite de reconciliación concurrente |
| P07 falla de DB devuelve resultado no confirmado | N07 éxito aparente sin COMMIT |
| P08 cursor pertenece a tenant/ventana | N08 cursor de B filtra filas en A |
| P09 ajuste conserva original e historia | N09 ajuste negativo destruye evidencia |
| P10 política no aprobada produce NO_GO | N10 D01–D08 pendientes se consideran aprobadas |

`P01–P10` y `N01–N10` son contratos de prueba **pendiente** para implementación futura R2. Ninguna llamada HTTP, xUnit, Playwright ni MySQL fue realizada por esta tarea documental.

## A3/A4/A5 — alcance de pruebas y cierre

A3: `NO_APLICA` a compilaciones .NET, providers, MySQL 8.4, migraciones, Angular/browser, servicios reales o pruebas transaccionales: sólo cambia Markdown y CHANGELOG. A4: exigir commit de dos archivos, sección CHANGELOG íntegra, Scope Lock y comprobación V5 de procedencia `completed/success` del SHA exacto; PLAN/COLA/DAG, BITACORA y lock V17 deben tener readback. A5: revisión independiente por Task Javier diferente de este autor, capaz de rechazar; `LISTO_DEV` por sí solo no acredita A5 ni habilita M03/PROD. Reversión por `git revert` normal, sin reescribir historia compartida.
