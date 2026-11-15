# SOLQARYN V5 FINAL — C12-04.M02.02: API, DTO y errores del cutover MySQL privado

**Autor:** PRIMARY DEV 02 / JAVIER_CHATGPT / `6af16bb9492c8191b8e8c7d606f11507`. **Repositorio:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK STRICT. **Entrega:** contrato conceptual M02.02, sin agregar rutas, DTO compilados, migraciones, datos o proveedores.

## Alcance, predecesores y límites

El bloque `C12-04` trata sobre **MySQL privado y cutover**. La microtarea consiste en definir operaciones futuras, formas de datos, validación, errores y compatibilidad. `PLAN_MAESTRO` refleja `C12-04.M01.01–06` LISTO+A5 independiente, con `M01.06` revisada por Task distinta de la autora. La condición exacta `C12-04.M01.01-06+A5` fue comprobada en DAG F=VALIDADO/G=SI; COLA G/O=SI y PLAN H=PENDIENTE/I vacío habilitaron este contrato documental mediante CAS exclusivo.

**No se conectó Aiven, Render ni MySQL; no existe autorización de cortar tráfico, migrar datos, probar restauración en DB real, exportar credenciales, cambiar DNS o tocar QA/main/PROD.** Los pasos y DTO siguientes son propuestas con rutas simbólicas `/internal/v5/cutover/*`: **ningún endpoint de este nombre fue implementado ni se considera disponible**.

## Operaciones contractuales futuras OP01–OP09

| ID y operación propuesta | Entrada tipada prevista | Resultado y restricción |
| --- | --- | --- |
| OP01 `GetReadiness` | `CutoverScopeId`, `ExpectedSchemaVersion` | `ReadinessSnapshot`: fuentes, bloqueos y fecha; solo lectura autorizada |
| OP02 `ValidatePreflight` | `CutoverPreflightRequest` con SHA, permisos y entorno | `PreflightDecision` que niega por defecto, nunca muta |
| OP03 `DescribeManifest` | `ManifestId`, tenant/ambiente verificados | `ManifestSummary` sin DSN, contraseña, backup URL ni PII |
| OP04 `CompareSchema` | `SchemaReference`, `SnapshotReference` | `SchemaDiffSummary` para DB efímera; no aplica DDL |
| OP05 `CompareTenantCounts` | `TenantScope` backend, `ExpectedRevision` | `CountDiffSummary` redactado; prohíbe muestras cross-tenant |
| OP06 `RunRestoreRehearsal` | `EphemeralRestoreRequest`, grant exacto | Recibo `RehearsalJobRef`; únicamente en infraestructura efímera autorizada |
| OP07 `GetRehearsalOutcome` | `RehearsalJobRef` y owner | `IntegrityDecision`: hash, FK, conteos, latencia observada |
| OP08 `RequestCutoverApproval` | `CutoverApprovalRequest` SHA/versión/motivo | `ApprovalPending`; requiere auditor independiente, no hace cutover |
| OP09 `DescribeRollbackPlan` | `CutoverPlanId`, `ManifestId` | `RollbackPlanSummary` con requisitos, no ejecuta restauración |

Las operaciones mutantes futuras están **NO_HABILITADAS**; OP06 solo sería permitida si existe una concesión separada de servicio efímero, y OP08 no constituye autorización de ejecutar. No aceptar `Environment=PROD` ni selector de tenant desde navegador como autoridad. Un token válido no sustituye grant, membresía activa y ownership persistido.

## DTO conceptuales (NO COMPILADOS)

| DTO | Campos estrictamente propuestos | Regla de privacidad |
| --- | --- | --- |
| `CutoverScopeId` | `Environment`, `TenantScopeHash`, `Revision` | Scope derivado de backend, no de URL arbitraria |
| `CutoverPreflightRequest` | `ExpectedHeadSha`, `SchemaVersion`, `ReadOnly`, `CorrelationId` | Nunca contiene contraseña/DSN/backup key |
| `ReadinessSnapshot` | `Checks`, `BlockingReasons`, `SourceSha`, `TimestampUtc` | Checks etiquetados status y SHA, sin PASS inventado |
| `ManifestSummary` | `ManifestId`, `Checksum`, `Bytes`, `CreatedUtc`, `ExpiresUtc` | Identificador opaco; sin enlace bearer |
| `SchemaDiffSummary` | `ExpectedVersion`, `ActualVersion`, `MissingObjects`, `Warnings` | Sin datos privados de filas ni DDL de producción |
| `CountDiffSummary` | `TenantScopeHash`, `BeforeCount`, `AfterCount`, `HashMatch` | Agregados por tenant; redacción mínima |
| `RehearsalJobRef` | `JobId`, `LeaseVersion`, `DeadlineUtc` | No constituye scheduler; requiere CAS verdadero |
| `IntegrityDecision` | `SchemaMatch`, `FkMatch`, `HashMatch`, `SafeToProceed` | SafeToProceed false por defecto, no activa producto |
| `CutoverApprovalRequest` | `ActorId`, `ReviewerId`, `Reason`, `SourceSha`, `ExpiryUtc` | Autor != revisor, sin autoaprobación |
| `RollbackPlanSummary` | `AllowedEnvironment`, `ManifestId`, `CompensationSteps` | Solo evaluación, no aplica rollback |

Un ID/manifest/URL o estado `approved` de cliente nunca concede permisos; todos los DTO deben pasar validación del servidor y un adaptador de persistencia de propiedad verificada.

## Rechazos y compatibilidad E01–E14

| Código propuesto | Motivo y tratamiento futuro |
| --- | --- |
| E01 `AUTHENTICATION_REQUIRED` | Actor no autenticado: 401 saneado |
| E02 `TENANT_SCOPE_DENIED` | Tenant A intenta B: 403 o 404 sin enumeración |
| E03 `APPROVAL_SEPARATION_REQUIRED` | Actor intenta aprobar su propio cutover |
| E04 `SOURCE_SHA_MISMATCH` | Commit o revisión de esquema ajena: 409 |
| E05 `SCOPELOCK_NOT_SUCCESS` | Check requerido no es SUCCESS: NO_GO |
| E06 `MANIFEST_NOT_VERIFIED` | Hash inexistente o corrupto: 422 |
| E07 `RESTORE_NOT_TESTED` | Sin restore efímero verificable: NO_GO |
| E08 `EPHEMERAL_ENV_ONLY` | Request en producción/QA no autorizado: 403 |
| E09 `LEASE_CAS_CONFLICT` | Reclamación simultánea: 409, sin segundo escritor |
| E10 `REVISION_EXPIRED` | Permiso/manifest/cutover window expirado: 409 |
| E11 `DEPENDENCY_NOT_READY` | FK, predecesor o A5 faltante: 409 |
| E12 `QUOTA_EXCEEDED` | Cuota finita agotada: 429, no retry sin límite |
| E13 `SENSITIVE_OUTPUT_BLOCKED` | DTO contiene secreto, DSN, JWT o PII: rechazar y registrar redactado |
| E14 `PROVIDER_UNAVAILABLE` | Aiven/servicio caído: NO_GO, jamás éxito ficticio |

Los números HTTP son política de diseño futura, **no comportamiento ya observado**. Mantener contratos API existentes y versionar rutas nuevas sin cambio rompedor; toda operación futura debe definir compatibilidad, rollback y estrategia idempotente bajo revisión de seguridad.

## Matriz futura P/N (no ejecutada)

| Par | Positivo para fases futuras | Negativo obligatorio |
| --- | --- | --- |
| P01/N01 | Preflight validado con SHA exacto | CI cancelled/skipped se cuenta PASS |
| P02/N02 | Actor A consulta readiness A | Actor B enumera manifests de A |
| P03/N03 | Schema diff de fixture efímero correcto | DDL toca base real |
| P04/N04 | Manifiesto cifrado e íntegro validado | Hash corrupto se considera respaldo |
| P05/N05 | Restore EF efímero prueba FK, counts y hashes | Copia sin restore declarado recuperable |
| P06/N06 | Mismo lease CAS tiene un único ganador | Dos operadores ejecutan cutover |
| P07/N07 | Aprobador independiente documenta evidencia | Autor se autoaprueba |
| P08/N08 | Cancelación preserva datos y evita promoción | Fallo parcial se registra como LISTO sin prueba |

**Estos casos no se ejecutaron en M02.02.** Futuras etapas con .NET 10, EF Core, MySQL 8.4 efímero, JWT/RBAC y tenant A/B requieren GitHub Actions causal por SHA, auditoría independiente y aprobaciones de entorno separadas.

## A0–A5 e impacto

**A0:** fuente V5, identidad, Scope Lock, seis antecedentes M01 y A5, DAG/PLAN/COLA y exclusividad. **A1:** integridad del contrato OP01–09, diez DTO, E01–14 y ocho pares P/N redactados; no atribuir pruebas reales. **A2:** aislamiento A/B, permisos, secreto/PII, aprobación separada, lease CAS, revocación y fallo seguro. **A3:** revisar compatibilidad conceptual con MySQL/EF/infra actuales; compilación, migración y restauración NO_APLICA al diff de solo Markdown y nunca PASS funcional. **A4:** fast-forward Git CAS, Scope Lock/V5 exact-SHA completados success, diff/blobs/readback PLAN/COLA/DAG/BITACORA. **A5:** otra Task Javier distinta del autor. Rollback del documento con `git revert` normal; sin mutaciones DB, restauración ni cambios comerciales.
