# SOLQARYN V5 — C14-02.M02.02: contrato de API, DTO, errores, validaciones y compatibilidad para aceptación de seguridad

**Microtarea:** `C14-02.M02.02` · **Autor:** SOLQARYN PRIMARY DEV 02 — JAVIER (`6af16bb9492c8191b8e8c7d606f11507`) · **Rama:** `solqaryn/Solqaryn@dev` · **Scope Lock:** STRICT · **Fecha local:** 2026-11-14.

**Fuente ejecutable:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Autoridades adicionales: Skill canónica, `AGENTS.md` y `docs/PROJECT_SCOPE_LOCK.md` de `dev`.

**Alcance real de esta entrega:** contrato **DOCUMENTAL** de interfaces hipotéticas y criterios de aceptación para una futura evaluación de seguridad. **NO se crean** endpoints, controladores, DTO compilados, tests HTTP, JWT, claims, RBAC, migraciones, scripts, Secrets, QA/main/PROD ni certificados de ausencia de P0/P1. Las operaciones, respuestas y escenarios siguientes son **PROPUESTOS_NO_IMPLEMENTADOS** y **FUTUROS_NO_EJECUTADOS**. Nunca ejecutar eventos/dispatcher por este documento.

## 1. Preflight, antecedentes y límites

La fila institucional `PLAN_MAESTRO!A2061` define `C14-02.M02.02`; antes del claim `H=PENDIENTE`, `I` sin owner, `COLA!G/O=SI`; `DAG_TECNICO` por ID exacto con `F=VALIDADO,G=SI`. Sus predecesores técnicos `C14-02.M01.01–06+A5` tenían estado LISTO y constancia de revisión independiente; esta condición es **dinámica**, no se hereda a M03/QA/PROD.

| Fuente versionada | Blob Git verificable | Aporte / límite |
| --- | --- | --- |
| `docs/v5/c14-02-m01-06-gates-bloqueo-rollback-liberacion-seguridad.md` | `6cb629af7993a6f4eb7cb8518f538f7ed196326f` | B01–B12 y no-liberación sin CI causal, tenant y A5 |
| `docs/v5/c14-02-m02-01-modelo-ownership-invariantes-aceptacion-seguridad.md` | `1bf7ca3f8c549212a24e10ed1afefb2fdedea972` | SE01–SE14, SI01–SI16 y SA01–SA12 como contratos conceptuales, no API real |

Capas futuras: Browser/Angular 22.2.1 → HTTP/API .NET 10 → autorización Application/Domain → repositorios EF Core 10/MySQL 8.4 cuando corresponda. La frontera de decisión siempre es **backend**. Aceptación técnica futura debe usar fixtures A/B, roles y membresías activas/revocadas, host/propiedad/archivos/job y errores no enumerables. No puede apoyarse solo en estado UI, plan comercial, claim tenant enviado por cliente o documento de seguridad.

## 2. Catálogo de operaciones propuestas y protección

No se afirma que exista ninguna de estas rutas; los nombres son **identificadores lógicos**, no URL ni superficie HTTP implementada.

| ID | Operación/contrato lógico futuro | Entrada tipada mínima propuesta | Autoridad y resultado exigido |
| --- | --- | --- | --- |
| API01 | `EvaluateSecurityAcceptanceScope` | `scopeId`, `expectedVersion`, actor autenticado | Scope allowlisted + owner + autorización; nunca ampliar alcance por body |
| API02 | `InspectSubjectMembership` | sujeto y `tenantId` resueltos por servidor | Membresía/rol vigentes y no filtración cross-tenant |
| API03 | `AssertResourceOwnership` | referencia opaca a recurso | Recurso A/B + sucursal/propiedad, DENY antes de cualquier dato ajeno |
| API04 | `InspectAuthorizationDecision` | módulo/acción autorizados, correlación opaca | Intersección JWT válido + grants + membresía + alcance |
| API05 | `ValidateHostBinding` | host normalizado de una fuente confiable | Igualdad exacta + binding verificado; no spoof ni substring |
| API06 | `ValidateMediaAccess` | resource/media ID opaco, TTL | Firma/owner/tenant/vigencia, prohibir replay cuando aplique |
| API07 | `RecordSecurityFinding` | código de hallazgo, severidad, SHA, evidencia saneada | Append-only lógico, no secrets, actor revisor separado |
| API08 | `ResolveSecurityFinding` | findingId, versión, retest y SHA | Solo tras retest causal; no permitir declarar P0/P1=0 por texto |
| API09 | `InspectJobExecution` | jobId, headSha | Estado nativo, `completed/success` exact SHA o NO_PASS |
| API10 | `ValidateBackupBoundary` | manifest/verificador, destino descartable | No restauración productiva ni datos B; exigir aprobación separada |
| API11 | `RecordIndependentA5` | microtaskId, authorTaskId, reviewerTaskId, SHA | Revisor distinto de autor; no auto-A5 ni A5 como despliegue |
| API12 | `ReadSecurityAcceptanceReport` | scopeId, audiencia permitida, correlación | Vista minimizada con no divulgación de hallazgos/PII de otro tenant |

**No transformar este catálogo en endpoints públicos** sin una microtarea M03 auténtica, contrato versionado, implementación backend, CI HTTP/security/DB aplicable y revisión independiente.

## 3. Envelope, DTO y compatibilidad futura

Se propone `SecurityAcceptanceRequestV1` con `requestId` opaco, `scopeId` allowlisted, `expectedVersion`, `operationCode`, `targetRef` opaco y `correlationId`; el usuario/tenant/roles **no** se toman de campos que suplanten la autoridad del servidor. Para auditoría, `SecurityAcceptanceEvidenceV1` incluiría sólo `checkKey`, `result` triestado (`PASS`, `FAIL`, `NOT_RUN`), `observedSha`, `checkName`, `observedAtUtc` y `sanitizedEvidenceRef`; nunca tokens, cadenas de conexión ni hallazgos sensibles en respuestas públicas.

El contrato `SecurityFindingV1` propuesto tiene `findingId` opaco, `severity`, `ownerScope`, `status`, `retestSha`, `reviewer` y `version`. Una vulnerabilidad P0/P1 sin retest causal queda `OPEN/BLOCKING`. La proyección Angular se adapta a lo que el backend autoriza; no convierte una pantalla oculta en autorización. `V1` aquí describe versión de **diseño**, no un DTO desplegado. Ningún consumidor existente se modifica ni se obliga a migrar por esta entrega.

| Compatibilidad | Regla de integración futura |
| --- | --- |
| Cambios de contrato | Adición explícita y versionada, sin reemplazar rutas existentes por deducción |
| Cliente antiguo | Si desconocido, denegar funciones sensibles y devolver error seguro; no abrir fallback |
| Consentimiento/PII | Minimizar evidencia por audiencia, no devolver dumps ni JWT |
| Códigos de resultado | `PASS` solo de ejecución causal verificada; `NOT_RUN` nunca satisface gate |
| Paginación/correlación | Límites de tamaño/cuota, IDs opacos, trazas sin secretos |
| Concurrencia | `expectedVersion` atómico en backend/DB cuando exista; conflictos = rechazo |

## 4. Semántica de fallos y validaciones negativas

Los códigos HTTP son **propuestas de comportamiento para implementación futura**, no respuestas observadas de un endpoint actual.

| Código | Condición | Resultado obligatorio sin fuga |
| --- | --- | --- |
| E01 / 400 | DTO inválido, clave desconocida, longitud o formato fuera de contrato | Validación antes de I/O, nada persistido |
| E02 / 401 | JWT inválido, expirado, firma o audiencia incorrecta | Rechazo, sin detalle de tenant |
| E03 / 403 | Permiso RBAC ausente o membresía revocada | Denegación no evasible por UI/plan |
| E04 / 404 | Recurso no accesible o de tenant B | Respuesta uniforme sin enumeración de B |
| E05 / 409 | `expectedVersion` obsoleto, CAS perdido | Sin sobrescribir decisión/conclusión previa |
| E06 / 422 | `scopeId` o flag no allowlisted | Rechazar ampliación de alcance |
| E07 / 429 | Exceso de cuota o intentos de fuerza bruta | Límite por actor/tenant, sin bloqueo cross-tenant |
| E08 / 503 | Evaluador de autorizaciones no disponible | Fail-closed, no resultado PASS especulativo |
| E09 / 409 | Repetición idempotente conflictiva | No duplicar hallazgo, efecto ni auditoría |
| E10 / 403 | Host/storekey mal vinculado, subdominio similar | DENY antes de resolver datos |
| E11 / 403 | URL firmada caducada, archivo ajeno o replay | No entregar bytes ni metadatos |
| E12 / 422 | Evidencia `SKIPPED/CANCELLED/IN_PROGRESS` presentada como PASS | Rechazo de cierre, preservar estado real |

Una respuesta 5xx o un timeout no representa un commit. Los códigos 403/404 deben evitar inferir la existencia de objetos ajenos. Nunca se reporta éxito de auditoría sin `observedSha`, trazabilidad de job y resultado pertinente.

## 5. Pruebas de aceptación futuras, positivas y negativas

| Caso | Clase | Oráculo de implementación futura |
| --- | --- | --- |
| P01 | + | Admin A con grant vigente consulta únicamente evidencia pública de A |
| P02 | + | Una prueba CI de seguridad `completed/success` en SHA exacto puede acreditarse |
| P03 | + | Cambio de versión válido permite una transición única y auditada |
| P04 | + | Revisor de TASK_ID distinto evalúa hallazgo sobre el diff auténtico |
| N01 | - | Sujeto A intenta leer/referenciar recurso B: DENY sin PII |
| N02 | - | Token expirado/falsificado o rol revocado: DENY |
| N03 | - | `EsAdministrador` o plan comercial sin permiso RBAC: DENY |
| N04 | - | Host parecido/spoof o `StoreKey` ajeno: DENY |
| N05 | - | Archivo firmado expirado o de B: no entregar contenido |
| N06 | - | Código de hallazgo inventado o evidencia con secreto: rechazar |
| N07 | - | P0/P1 abierto sin retest se intenta declarar resuelto: bloquear |
| N08 | - | SHA diferente o job cancelado se intenta declarar PASS: bloquear |
| N09 | - | Concurrencia/version obsoleta: conflicto sin overwrite |
| N10 | - | Mismo autor intenta A5: rechazar, preservar independencia |
| N11 | - | Exceso de intentos en A no revela ni bloquea datos B |
| N12 | - | Operación intenta usar este documento para habilitar QA/main/PROD: NO_GO |

**Todas las P01–P04/N01–N12 son FUTUROS_NO_EJECUTADOS;** aún no se dispone de reporte de pentest, cobertura BOLA/IDOR real, cero hallazgos P0/P1 ni nuevo runtime. Su falsabilidad es un contrato para otra microtarea, no evidencia de que pasaron.

## 6. Auditorías A0–A5 y rollback

- **A0:** diff estrictamente `docs/v5/...` y encabezado con viñetas adyacentes de `CHANGELOG_AI.md`; Git blobs y no secretos.
- **A1:** verificar 12 operaciones propuestas, 12 fallos y 16 escenarios P/N como coherencia documental; no confundirlos con test runner.
- **A2:** verificar por inspección controles JWT/RBAC, membresía, tenancy A/B, BOLA/IDOR, host y firma, archivos y PII fail-closed; no certificados de seguridad runtime.
- **A3:** conciliar M01.06, M02.01 y predecesores del DAG actuales. Backend .NET/Angular/MySQL/Docker/E2E `NO_APLICA_POR_DIFF_SOLO_MARKDOWN`, no `PASS` inventado; no contratos de API existentes mutados.
- **A4:** fast-forward de `dev` por expected HEAD, verificar SHA/Tree/Blob, `verify-solqaryn-project-scope` y verificador V5 `completed/success` en SHA exacto y readback PLAN/COLA/DAG/BITACORA.
- **A5:** `A5_PENDIENTE` por una Task Javier distinta del autor; no es autorización de QA/main/PROD.

**Rollback solo documental:** `git revert` normal del commit de esta unidad con readback y evento compensatorio, nunca force-push ni modificación de BITACORA histórica. La implementación de servicios/eventos/DB y activación de productores o dispatchers requiere autorización y preflight propios.