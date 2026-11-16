# SOLQARYN V17 — C09-03.M02.05: eventos, idempotencia, correlación y límites del Social Hub

**Tipo:** R0 documental, sin runtime. **Autor:** PRIMARY DEV06 Javier, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Repo:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Autoridad:** STANDARD V5 FINAL, PLAN_MAESTRO fila 1380, COLA G/O=SI, DAG_TECNICO fila 1341 F=VALIDADO/G=SI en CLAIM. El documento NO crea APIs, jobs, colas, proveedores, mensajes, tablas, migraciones ni eventos ejecutables.

## A0 — Modelo de publicación futura sin autoridad ejecutiva

Contrato conceptual, derivado de E01–E16 de `C09-03.M02.01`, OP/DTO y errores de `M02.02`, persistencia `M02.03` y flags cerrados `M02.04`. Ningún evento sustituye autorización de backend. Toda emisión requerirá empresa actual, pertenencia efectiva, rol no revocado, consentimiento por propósito y canal, opt-out ausente, límites y proveedor autorizado **revalidados en el momento del efecto**. Un snapshot, token de frontend o ACK externo no son pruebas suficientes.

La arquitectura futura mantendrá Domain (invariantes), Application (casos y CAS), Infrastructure (outbox, persistencia y proveedor) y API (auth y composition). No se selecciona broker, scheduler, proveedor, coste, servicio externo o nuevo runtime. Mensajería queda desactivada por defecto.

## Catálogo conceptual de doce eventos EV01–EV12

| ID | Nombre futuro | Correlación/ownership obligatorio | Condición adversarial |
| --- | --- | --- | --- |
| EV01 | CampaignDraftCreated | empresa, CampaignId, Revision | no enviar sin aprobación |
| EV02 | ContentRevisionSaved | empresa, ContentId, Revision | sanear propiedad/media |
| EV03 | CampaignApproved | empresa, CampaignId, ApprovalVersion | revisor distinto de editor |
| EV04 | ConsentRecorded | empresa, SubjectRefHash, Channel, Purpose | prueba válida por ámbito |
| EV05 | ConsentRevoked | empresa, SubjectRefHash, Channel, Purpose | revocación antes de efecto |
| EV06 | OptOutApplied | empresa, SubjectRefHash, Channel, Purpose | supresión prevalece sobre caché |
| EV07 | SegmentSnapshotCaptured | empresa, SegmentId, SnapshotRevision | no autoriza envíos |
| EV08 | PublicationIntentPrepared | empresa, IntentId, IdempotencyKey | solo intención, no entrega |
| EV09 | PublicationAttemptStarted | empresa, IntentId, AttemptId | CAS contra doble escritor |
| EV10 | ProviderReceiptObserved | empresa, ReceiptId, AttemptId | recibo no implica autorización |
| EV11 | CampaignScheduleCancelled | empresa, ScheduleId, Revision | cancelación domina temporizador |
| EV12 | AggregatedMetricRecorded | empresa, CampaignId, TimeWindow | sin PII/umbral de cohorte |

Cada evento futuro debería tener `EventId` único, `TraceId` opaco, `EmpresaId` desde backend, `CausationId`, `CorrelationId`, `SchemaVersion`, `CreatedUtc`, `ActorRef` no reversible, `Revision`, `ResultCode` allowlist y `PayloadDigest` no sensible. Nunca enviar JWT, tokens, contactos, URLs internas, texto de mensajes, SQL, claves ni PII en la bitácora. Eventos de error usan códigos estables y no revelan existencia cross-tenant. **Estas propiedades son propuestas de contrato**, no serializadores ni payloads implementados.

## Doce garantías de idempotencia, autorización y reintento

| Regla | Precondición contractual | Rechazo esperado ante defecto |
| --- | --- | --- |
| ID01 | IdempotencyKey | per-tenant/campaign/channel/destination, same request digest -> same result |
| ID02 | ConflictingDigest | same key different digest -> conflict, never send |
| ID03 | AtomicClaim | attempt key lock/CAS, exactly one writer within tenant scope |
| ID04 | ConsentFreshness | re-evaluate consent just before external effect |
| ID05 | OptOutPrecedence | latest opt-out always denies even if snapshot stale |
| ID06 | RevokedRole | any lost role/membership invalidates queued intent |
| ID07 | TenantBinding | all events and outbox rows carry trusted EmpresaId |
| ID08 | ProviderReceipt | ACK must correlate attempt ID, do not infer delivery |
| ID09 | RetryBound | retry only on classified transient failures, bounded |
| ID10 | UnknownOutcome | unknown external outcome => reconcile, never blind resend |
| ID11 | CancellationRace | cancelled schedule prevents new dispatch after CAS |
| ID12 | AuditIntegrity | append-only privacy-minimal event with monotonic version |

**Límite clave:** idempotencia de creación de intención y recepción de acuse NO demuestra entrega externa exactamente una vez. Un timeout tras envío puede tener efecto remoto desconocido: retener `UNKNOWN_OUTCOME`, reconciliar con referencia de proveedor y nunca reemitir automáticamente sin contrato idempotente verificable. El resultado del proveedor nunca modifica permisos internos. A5 futuro debe examinar carreras, replay y recuperación sin borrar consentimiento ni auditoría.

## Correlación y flujo de estados propuestos

`DRAFT -> APPROVED -> INTENT_PREPARED -> ATTEMPT_CLAIMED -> PROVIDER_RESULT_PENDING -> ACKED_OR_RECONCILE`; `CANCELLED`, `OPTOUT_BLOCKED`, `REVOKED` y `DENIED` son terminales para ese intento; cambio de permisos exige nueva evaluación. Ningún salto de estado debe eludir aprobador distinto, versión de contenido o consentimientos. La clave de idempotencia conceptual usa una tupla tenant/campaña/canal/destino, con digest de solicitud, límite UTC, CAS de revisión y outbox futuro transaccional con auditoría; la futura transacción será probada en MySQL aislado, no en este diff.

**Límites:** plazos UTC, máximo reintentos finito, cuotas por empresa y proveedor, supresión opt-out inmediata, retención legal revisada por propietario, tamaños de payload acotados y cohortes métricas con umbral mínimo. No prometer entrega, atribución ni cumplimiento legal sin infraestructura efectiva. Ante autorización incompleta, fallo de proveedor, tenant ambiguo, configuración desconocida o cancelación, fallar cerrado.

## Doce escenarios positivos/negativos futuros: 0/12 ejecutados

| Caso | Positivo diseñado | Negativo/frontera diseñado |
| --- | --- | --- |
| T01 | Draft event A with own campaign | campaignId B or tenant null |
| T02 | Valid explicit approval by other reviewer | self-approval or revoked RBAC |
| T03 | Consent proof matches channel/purpose | consent different channel |
| T04 | Opt-out applied suppresses send | stale cache bypasses opt-out |
| T05 | New segment snapshot A | member B or stale revision |
| T06 | Idempotent same digest replay | same key new digest |
| T07 | Single writer claims attempt | two concurrent writers |
| T08 | Provider ACK correlates attempt | forged foreign receipt |
| T09 | Queue retry on transient 429 | unknown response causes blind resend |
| T10 | Calendar UTC valid | DST ambiguous local or expired schedule |
| T11 | Aggregated metric above threshold | PII or cohort below threshold |
| T12 | Audit events survive rollback | rollback deletes opt-out/audit |

Las pruebas futuras deberán cubrir tenant A/B, JWT revocado y RBAC, consentimiento efectivo, opt-out posterior, CAS/replay, proveedor deshabilitado, cancelación, idempotencia y confidencialidad de logs. Los escenarios enumerados no son tests xUnit, HTTP, MySQL, Vitest ni Playwright ejecutados. Una migración real posterior exigirá `Category=Integration` ejecutado sobre MySQL 8.4 EF Core 10 Oracle aislado y logs comprobables; usar credenciales reales queda fuera de esta microtarea.

## A0–A5 y reversión

- **A0:** entrega autónoma de este Markdown más encabezado y viñetas contiguas en CHANGELOG_AI.md, sin código ejecutable. Su alcance es eventos/idempotencia/correlación/límites.
- **A1:** revisión estructural documental de doce eventos, doce garantías y doce pares P/N; `0/12` runtime ejecutados.
- **A2:** diseño fail-closed para identidad, tenant A/B, permisos, revocación, consentimiento, opt-out y secretos; no atribuir seguridad runtime nueva.
- **A3:** .NET, JWT/HTTP, MySQL/EF, browser y proveedores reales son `NO_APLICA_R0` al diff Markdown; deben probarse en sus microtareas de código.
- **A4:** publicar fast-forward en `dev`, verificar blobs y Scope Lock + V5 en SHA exacto completed/success, readback PLAN/COLA/DAG/BITACORA; mantener VALIDANDO mientras checks necesarios pendientes.
- **A5:** revisión técnica independiente por otra Task Javier de diferente TASK_ID; puede rechazar incluso contrato documental.

**Rollback:** revertir el commit con nuevo fast-forward sin borrar BITACORA ni editar historia. No QA, main, PROD, datos reales o cambios de cuenta.
