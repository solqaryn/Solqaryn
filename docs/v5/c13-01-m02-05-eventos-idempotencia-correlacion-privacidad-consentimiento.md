# SOLQARYN V17 — C13-01.M02.05: eventos conceptuales, idempotencia, correlación y límites de privacidad

**Unidad:** C13-01.M02.05 · PLAN_MAESTRO fila 1884 · DAG_TECNICO fila 1845. **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Riesgo:** R0 documental. **Fuente:** STANDARD V5 FINAL, PLAN/COLA/DAG, calidad V15, `PROJECT_SCOPE_LOCK=STRICT` y lock V17 sobre `solqaryn/Solqaryn@dev`.

## A0. Contrato documental y frontera

Esta unidad describe **eventos futuros**, envelope mínimo, idempotencia, correlación, límites y errores del lote de privacidad, preferencias, consentimiento, revocación, retención y derechos. **No introduce bus, outbox, mensajes reales, cookies, EF/MySQL, HTTP, Angular, tracking, clientes externos, secretos, policy legal ni datos reales.** La ejecución de estos eventos exige una implementación R2 posterior y revisión competente de finalidades, bases legales, plazos y proveedores. No se fabrican aprobaciones jurídicas ni tiempos de retención.

**Antecedentes verificados:** M01.01–06 (gates G01–G16 y A5 de cierre); M02.01, entidades E01–E16 e invariantes I01–I20; M02.02, operaciones OP01–08 y DTO01–12 con validaciones V01–16; M02.03 propone claves K01–16, MX01–08 y nonce+digest canónico, pero permanece en VALIDANDO hasta éxito causal de CI; M02.04 posee su owner independiente. Según DAG, esta M02.05 sólo requiere M01 completado/A5, nunca interpreta M02.03 VALIDANDO como autorización para liberar M02.06.

## EV01–EV12: catálogo de eventos FUTUROS_NO_EJECUTADOS

| ID | Evento conceptual | Semántica futura | Condición restrictiva |
| --- | --- | --- | --- |
| EV01 | PolicyVersionProposed | draft policy revision, no public policy effect | Review pending |
| EV02 | PurposeReviewRequested | purpose classification and approval request | No basis assumed |
| EV03 | PreferenceDecisionRequested | typed explicit choice with nonce and policy digest | No grant before auth+commit |
| EV04 | PreferenceDecisionCommitted | single committed decision with CAS revision | Never silent consent |
| EV05 | ConsentEvidenceReferenced | opaque evidence digest/reference after same transaction | No PII or JWT |
| EV06 | PreferenceRevocationRequested | own decision and expectedRevision | Reject stale/foreign |
| EV07 | PreferenceRevocationCommitted | revoked scope, audit and invalidation revision | No re-accept by old event |
| EV08 | PreferenceSnapshotInvalidated | post-commit revocation/version update | Cache not source of truth |
| EV09 | PrivacyRightsDraftOpened | verified subject case draft only | No automatic export/erase |
| EV10 | RetentionRuleReviewRequested | review request for category and hold | Never trigger DELETE |
| EV11 | ProcessorDestinationReviewed | allowlisted destination review outcome | No third-party transfer |
| EV12 | PrivacyAuditReadbackRecorded | minimal correlation and digest evidence | No raw secrets/PII |

**Secuencia propuesta:** autenticación/tenant/ownership efectivo -> validar propósito y versión -> reservar nonce+digest completo -> CAS transaccional de decisión/revocación -> registrar auditoría mínima -> proyectar snapshot invalidable. No publicar EV04/EV07 sobre transacción fallida ni confirmar efectos por mera recepción de un evento en memoria. Todo evento lleva tenant/sujeto verificados desde backend y nunca un tenant autoritativo desde cliente.

## ENV01–ENV12: envoltura mínima y campos seguros

| Campo | Significado futuro | Regla obligatoria |
| --- | --- | --- |
| ENV01 | eventId | Unique opaque reference, audit only |
| ENV02 | schemaRevision | Explicit version; unknown -> reject |
| ENV03 | eventType | EV01–EV12 strict allowlist |
| ENV04 | tenantRef | Backend-resolved effective tenant, not trusted client header |
| ENV05 | subjectRef | Backend ownership, anonymized opaque reference |
| ENV06 | actorScopeRef | Current membership/role revision, must revalidate |
| ENV07 | purposeAndPolicyRevision | Compound purpose+immutable version+digest |
| ENV08 | operationRef | Known OP01–08 and stage |
| ENV09 | requestNonce | Stable within one logical request, new for another |
| ENV10 | canonicalPayloadDigest | Full canonical security-relevant data, not redacted projection |
| ENV11 | expectedRevision | CAS for decisions/revocations; stale -> conflict |
| ENV12 | occurredAtUtcAndOpaqueTrace | UTC monotonic causal order with private correlation, no token |

**Claves de deduplicación**: `tenantRef + subjectRef + purposeKey + policyRevision + operationRef + requestNonce`, con nonce estable en reintentos del **mismo intento lógico**. El digest deriva del **payload completo canónico relevante para seguridad** (tipos y normalización deterministas), no de una representación redactada que colapse cargas distintas. Igual ámbito+nonce+digest permite recuperar el mismo recibo; igual ámbito+nonce con digest distinto es conflicto obligatorio; nueva intención requiere nuevo nonce. Sin una transacción y bloqueo idempotente reales, el diseño no garantiza exactly-once.

## I01–I12: invariantes que la implementación debe demostrar

| ID | Invariante exigible en futura implementación |
| --- | --- |
| I01 | Never emit a private EV without verified tenant+subject+active role/permission context |
| I02 | Tenant A event cannot be read, correlated or replayed under tenant B |
| I03 | EV03 cannot create affirmative decision from silence, default, timeout or partial failure |
| I04 | A requestNonce is stable solely for retries of one logical intent, unique for different intents |
| I05 | Same tuple tenant+subject+purpose+policyRevision+operation+nonce and same full payload digest reuses one receipt |
| I06 | Same tuple with different canonical payload digest is conflict; a redacted display digest cannot decide equality |
| I07 | EV04 and EV07 require atomic CAS and exactly-once logical effect; no publish on rolled-back transaction |
| I08 | EV07 invalidates EV08 future snapshots and blocks old EV04 replay; retention of historical evidence distinct |
| I09 | Failed authorization/JWT/RBAC/revocation check returns deny without event containing PII |
| I10 | Old policy version does not imply valid decision for revised policy without separate approved rule |
| I11 | No event triggers provider SDK, data export, deletion or retentive timer absent separate gates |
| I12 | No logs, DLQ or audit references may include JWT, raw cookie, password, email or identity documents |

Para replay tardío, los cambios de revisión y revocación constituyen gates en el servidor, no señales confiables de hora del navegador ni flags visuales. EV08 es invalidación de proyección, no autoridad sobre consentimiento. Auditoría de soportes globales debe ser excepcional, limitada y autorizada, nunca acceso automático por una UI.

## L01–L08: límites y políticas pendientes

| ID | Área | Puerta/valor futuro no aprobado |
| --- | --- | --- |
| L01 | Per-subject event rate | Bounded according to future policy; no value invented |
| L02 | Retry budget | Finite and idempotent; no infinite retry loop |
| L03 | Retention limits | Not approved until legal/category/hold review |
| L04 | Payload size | Strict typed schema and opaque digest; reject oversize |
| L05 | Ordering | Revision/CAS + monotonic readback, not clock ordering alone |
| L06 | Dead-letter | Quarantine with no PII; not automatically replayed |
| L07 | Readback | Only authorized scoped actor can inspect redacted summary |
| L08 | Provider boundary | Disabled unless explicit destination approval and independent gate |

## ER01–ER08: códigos de error conceptuales y denegación segura

| ID | Error | Efecto futuro |
| --- | --- | --- |
| ER01 | UNAUTHENTICATED_OR_REVOKED | Deny no private event leakage |
| ER02 | TENANT_SUBJECT_MISMATCH | Deny BOLA/IDOR without existence leak |
| ER03 | ROLE_OR_PURPOSE_DENIED | Deny missing RBAC or unapproved purpose |
| ER04 | REQUEST_NONCE_CONFLICT | Reject same nonce/different payload |
| ER05 | POLICY_VERSION_STALE | Do not reinterpret earlier consent |
| ER06 | REVISION_CONFLICT | Reject stale expectedRevision or replay |
| ER07 | HOLD_OR_LEGAL_REVIEW_PENDING | No actual delete/transfer |
| ER08 | PROVIDER_OR_INTEGRITY_FAILURE | No optimistic PASS, safe audit correlation |

No se escoge HTTP status sin contrato API y tests pertinentes. Las negativas deben minimizar diferencias observables que revelen recursos de otro tenant; el funcionamiento ante errores de proveedor jamás implica efecto autorizado.

## T01–T12: pares positivos/negativos FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro | Negativo adversarial que debe fallar |
| --- | --- | --- |
| T01 | Authorized A event with owned subject accepted in future | B tries A eventRef and is denied without existence disclosure |
| T02 | Live actor membership and role permit bounded event | Revoked membership with unexpired JWT cannot emit event |
| T03 | Explicit approved-purpose decision yields EV04 after commit | Silence or timeout cannot emit EV04 |
| T04 | Same stable nonce + same canonical digest returns original receipt | Retry with same nonce + changed hidden payload must conflict |
| T05 | New intent with new nonce is separately classified | Different request has old nonce and is wrongly deduplicated |
| T06 | Valid revocation CAS emits EV07 and invalidates snapshot | Old EV04 delivery after EV07 re-enables revoked decision |
| T07 | Minimal EV05 readback has opaque digest | JWT, raw cookie or email included in log or DLQ |
| T08 | Policy revision v2 prompts new decision | Version v1 evidence assumed valid for v2 without rule |
| T09 | Rights case stays draft for verified owner | Foreign subject exports or erases data by forged ID |
| T10 | Retention review with legal hold remains pending | Unapproved TTL deletes evidence or backup |
| T11 | Integrity/provider failure yields deny and idempotent retry | Timeout creates affirmative consent or third-party transfer |
| T12 | Atomic transaction records one effect and one audit readback | Concurrent requests create duplicates, partial commit or cross-tenant correlation |

**Estado real:** 0/12 pares ejecutados, 0 mensajes publicados, 0 pruebas MySQL/HTTP/browser/jobs, 0 revisiones legales. Son oráculos para tests posteriores, no resultados de certificación.

## Arquitectura afectada, dependencia y rollback

- **Domain/Application:** potenciales contratos de decisiones, revisión de miembro, estados de revocación e idempotencia; sólo propuesta de diseño.
- **Infrastructure (Oracle EF Core/MySQL 8.4):** una futura transacción atómica con clave compuesta, CAS de revisión, idempotency inbox/outbox y replay acotado necesita pruebas de integración reales en MySQL 8.4 efímero antes de implementarse. Ninguna migration ni tabla se crea aquí.
- **API/Angular:** verificaciones server-side de JWT/RBAC/tenant A/B y acceso por sujeto; la UI no puede conceder permiso al enviar un evento.
- **Proveedor/PII:** no activar SDK, broker, correo, tracking ni exportación. Logs, retries y DLQ sin JWT, cookies, credenciales o datos personales libres.
- **Puertas DAG:** M02.06 sólo después de M02.01–05 LISTO + A5 independientes y restantes gates reales; M03/QA/main/PROD no se desbloquean por conceptos.
- **Rollback:** para este Markdown, nuevo `git revert` tras revisar HEAD; nunca force-push ni borrar BITACORA. Para futuros servicios, reversión y compensación en una microtarea R2 independiente.

## A0–A5 de esta entrega R0

**A0:** alcance PLAN exacto, DAG validado, diff limitado al presente Markdown y sección autocontenida de `CHANGELOG_AI.md`. **A1:** 12 EV, 12 ENV, 12 invariantes, 8 límites, 8 errores y 12 pares positivos/negativos inspeccionados estáticamente; todos **FUTUROS_NO_EJECUTADOS**. **A2:** tenant A/B, ownership, JWT/RBAC, revocación, IDOR/BOLA, nonce y digest canónico revisados a nivel de contrato; no asegura HTTP real. **A3:** MySQL/EF, browser, APIs, proveedor, broker y CI de integración **NO_APLICA al diff exclusivamente R0**; deben probarse realmente al implementarse. **A4:** commit fast-forward CAS dev, Scope Lock y procedencia V5 `completed/success` en SHA causal o descendiente con blobs equivalentes, PLAN/COLA/BITACORA readback y exclusividad V17. **A5:** revisión posterior por otra Task Javier distinta, con potestad A5_FAIL.

Ninguna aceptación de esta unidad constituye aprobación para política legal pública, acceso a información privada, paso a QA/main/PROD o alteración de datos y servicios.
