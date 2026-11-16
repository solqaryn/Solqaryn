# SOLQARYN V17 — C13-01.M02.02: contratos conceptuales de operaciones, DTO y errores de privacidad

**Unidad:** C13-01.M02.02 · PLAN_MAESTRO fila 1881 · DAG_TECNICO fila 1842. **Autor:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Riesgo:** R0 documental. **Repositorio/ramal:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. Fuente: STANDARD V5 FINAL, PLAN/COLA/DAG/BITACORA, calidad V15 y exclusión V17.

## A0. Contrato documental y límites de confianza

La unidad **define operaciones conceptuales, futuros DTO, códigos de rechazo y oráculos**, pero **no crea endpoints HTTP, clases C#, servicios Angular, EF/MySQL, cookies, políticas jurídicas, proveedores, workers o permisos efectivos**. Un campo descrito aquí no prueba su implementación ni otorga capacidad de procesar PII. No se asume jurisdicción, base legal o plazo de retención; las decisiones D01–D10 y gates G01–G16 de M01 permanecen abiertos para ejecución real.

Antecedentes en HEAD revisado: C13-01.M01.01–06, incluidos M01.03 (brechas B01–B12), M01.04 (riesgos R01–R14, supuestos S01–S10 y decisiones D01–D10 abiertas), M01.05 (criterios AC01–16 futuros) y M01.06 (dieciséis gates G01–16 y rollback R01–08). C13-01.M02.01 propuso E01–E16, I01–I20 y T01–T12; su CI se trata separadamente como VALIDANDO y no se convierte en LISTO/A5 por cita. El DAG autoriza esta M02.02 por antecedente M01 independiente, no por cerrar M02.01.

## OP01–OP08: operaciones conceptuales FUTURAS

| Operación | Nombre conceptual | Contexto futuro | Entrada futura | Frontera obligatoria |
| --- | --- | --- | --- | --- |
| OP01 | ReadPublicNoticeVersion | anon/public | policyRef+locale+scope | public version only; no tenant or decision disclosed |
| OP02 | GetOwnPreferenceSnapshot | authenticated subject | tenantRef+subjectRef+policyRevision | revalidated membership/RBAC, own rows only |
| OP03 | ProposePreferenceDecision | authenticated subject | purposeKey+revision+decision+idempotencyKey | accept only permitted purpose, no inferred affirmative |
| OP04 | RevokeOwnDecision | authenticated subject | decisionRef+expectedRevision+reasonCode | revoke own, preserve history, deny on stale revision |
| OP05 | GetOwnDecisionEvidence | authenticated subject | evidenceRef+ownership proof | minimal digest/correlation; never raw token or PII |
| OP06 | SubmitRightsCaseDraft | verified subject | caseType+subjectRef+opaqueCorrelation | no promise of legal deadline or actual export |
| OP07 | PreviewRetentionRuleProposal | authorized custodian future | categoryRef+legalBasisReview | no DELETE/TTL; read-only proposal |
| OP08 | ReviewProcessorDestinationDraft | authorized reviewer future | purposeRef+processorRef+reviewVersion | no external call, secret or automatic approval |

**Semántica y compatibilidad:** las operaciones privadas exigirían autenticación efectiva, membresía activa, RBAC, tenant A/B y propiedad del sujeto/recurso desde backend .NET 10; no bastan Angular guard, selector de empresa o claim no revalidado. El diseño evita 200/201 implícitos, respuestas positivas a timeout y enumeración de recursos ajenos. OP07/OP08 son **propuestas de revisión**: no modifican retención ni efectúan llamadas externas. Este contrato no define URI, verbo HTTP ni política legal aplicable.

## DTO01–DTO12: datos tipados conceptuales FUTUROS

| DTO | Propósito | Campos propuestos | Restricción |
| --- | --- | --- | --- |
| DTO01 | PublicNoticeQuery | scope,locale,version? | no PII input |
| DTO02 | PublicNoticeView | policyRef,locale,digest,effectiveState | only approved public draft in future |
| DTO03 | SubjectScopeContext | opaqueActorRef,tenantRef,membershipRevision | constructed backend, not client-claimed |
| DTO04 | PreferenceSnapshotView | purposeStates,policyRevision,evaluatedAtUtc | own subject only; stale denial |
| DTO05 | DecisionCommand | purposeKey,policyRef,decision,requestNonce | deny missing/unknown/duplicated purpose |
| DTO06 | DecisionReceipt | decisionRef,revision,digest,occurredAtUtc | minimal and auditable; no JWT |
| DTO07 | RevocationCommand | decisionRef,expectedRevision,reasonCode | optimistic CAS and ownership |
| DTO08 | RevocationReceipt | revocationRef,newRevision,occurredAtUtc | no reassignment across tenant |
| DTO09 | EvidenceLookup | evidenceRef,scopeProof | no enumeration |
| DTO10 | EvidenceSummary | opaqueCorrelation,digest,state | redacted and limited |
| DTO11 | RightsCaseDraftCommand | caseType,subjectRef,verificationRef | no new legal rights inferred |
| DTO12 | RightsCaseDraftReceipt | caseRef,reviewState,opaqueTrace | no promised SLA or data export |

Las referencias de versión, actor, tenant, clave y digest serían opacas y verificadas; el contexto confiable DTO03 **se construye en el servidor**, nunca debe aceptar un `tenantId` autoritativo del navegador. Los recibos son proyecciones mínimas; no contienen JWT, contraseña, cookie privada, nombres completos, correo, PII libre ni URI arbitraria de proveedor.

## ER01–ER12: matriz de denegación y error futuro

| Error | Código conceptual | Garantía/negación |
| --- | --- | --- |
| ER01 | UNAUTHENTICATED | 401-or-approved non-disclosing response; no subject data |
| ER02 | MEMBERSHIP_REVOKED | deny even if JWT not expired |
| ER03 | TENANT_MISMATCH | 403/404 per approved policy, no foreign existence leak |
| ER04 | ROLE_OR_OWNER_DENIED | deny if grant, tenant, subject or resource not authorized |
| ER05 | POLICY_VERSION_STALE | deny implicit migration; require explicit review |
| ER06 | PURPOSE_NOT_APPROVED | deny unknown or unapproved purpose/cookie category |
| ER07 | DUPLICATE_OR_REPLAY | idempotency scoped tenant+subject+purpose+revision |
| ER08 | REVOCATION_CONFLICT | stale expectedRevision; never revive previous permission |
| ER09 | EVIDENCE_NOT_FOUND | no enumeration, no sensitive metadata |
| ER10 | LEGAL_REVIEW_PENDING | no inferred timeframe or destructive retention |
| ER11 | PROCESSOR_NOT_VERIFIED | no third party execution or data movement |
| ER12 | INTEGRITY_OR_PROVIDER_FAILURE | no optimistic PASS, no positive decision |

Los posibles estados HTTP 401/403/404/409/422/503 deberán decidirse en un diseño API posterior y probarse con identidad real, evitando IDOR/BOLA y filtración por diferencia de mensajes. Ningún código anterior representa respuesta real de un servidor implementado.

## V01–V16: condiciones de aceptación del futuro contrato

| Validación | Aserción necesaria cuando exista runtime |
| --- | --- |
| V01 | actor authenticated when private request, anonymous only for approved public notice |
| V02 | backend resolves tenant from active membership, never URL/header/cookie |
| V03 | subjectRef belongs to effective tenant and authenticated identity |
| V04 | RBAC/ownership checked for each read and mutation, no UI guard as authority |
| V05 | policyRef and revision/digest match immutable approved version |
| V06 | purposeKey is recognized, applicable, reviewed and scoped; no global wildcard |
| V07 | decision explicit, typed and never inferred from silence/error/default |
| V08 | idempotency composite tenant+subject+purpose+revision+operation+nonce |
| V09 | revocation expectedRevision equals current server revision before CAS |
| V10 | revocation propagates to future use; out-of-order events cannot reopen |
| V11 | DTO responses minimize PII and never serialize JWT, cookies or credentials |
| V12 | evidence lookups require scopeProof and return no other tenant's existence |
| V13 | rights case verifies identity; reason and deadlines remain legally unapproved |
| V14 | retention rule requires category, legal review, holds and backups analysis |
| V15 | processor destination requires contract/region/allowlist verified before runtime |
| V16 | failure or missing context => DENY/NO_GO; no implicit grant or effects |

**Relaciones:** OP02/03/04/05 usan E05 tenant, E06 sujeto, E07 contexto y E08/E09/E10/E11; OP01 usa E01/E02; OP06 usa E12; OP07 E04/E13/E14; OP08 E03/E15. E16 registra únicamente referencia de auditoría mínima. La revocación no borra eventos que deban conservarse por fundamento aprobado; una decisión de otro propósito/versión no se hereda silenciosamente.

## T01–T12: pares positivos/negativos FUTUROS_NO_EJECUTADOS

| Par | Positivo esperado en futura prueba | Negativo que debe rechazar implementación defectuosa |
| --- | --- | --- |
| T01 | anon reads a published notice draft version | anonymous user accesses private evidence |
| T02 | tenant A reads its subject's snapshot | tenant B reads A's decision by ID |
| T03 | authorized subject chooses an approved purpose explicitly | silence or malformed selection becomes affirmative |
| T04 | duplicate request returns same scoped receipt | tenant B replays A's idempotency key |
| T05 | valid revocation advances revision and preserves history | stale event re-enables revoked decision |
| T06 | policy revision validated and version remains immutable | old acceptance silently covers new version |
| T07 | authorized auditor reads redacted evidence | support bypasses owner and extracts JWT/PII |
| T08 | rights draft records verified actor and scope | unverified caller retrieves private rights case |
| T09 | retention preview remains read-only until legal approval | draft TTL deletes invoices or audit evidence |
| T10 | approved processor reference may be reviewed only | unverified SDK calls outside destination allowlist |
| T11 | fail closed on provider/timeout and preserves pending status | network failure produces successful consent |
| T12 | future UI exposes equally accessible accept and reject | keyboard cannot reach rejection control |

**Resultados ejecutados en esta entrega:** 0/12. No se realizaron solicitudes HTTP, autenticación JWT de extremo a extremo, Playwright, MySQL efímero, tests xUnit, revisión jurídica ni llamadas de terceros. Estos son oráculos estáticos para etapas posteriores, **NO PASS de runtime**.

## Impacto, dependencias, rollback y A0–A5

- **Frontend Angular:** sólo contratos objetivo para futura UX accesible de avisos/preferencias; no se modifica componente/ruta/estilo.
- **Backend Domain/Application/Infrastructure/API:** únicamente delimitación conceptual. No se toca .NET 10, DTO compilado, EF Oracle/MySQL 8.4, transacciones, seguridad existente, migración ni observabilidad operativa.
- **Seguridad:** BOLA/IDOR, sujeto y tenant A/B, JWT/RBAC, revocación, idempotencia, contenido de auditoría, fallo de proveedor y privacidad por defecto quedan como invariantes exigibles. Las decisiones legales y bases de tratamiento siguen sin aprobarse.
- **Preflight:** PLAN H=PENDIENTE/I vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI; candado V17 `SQV17_C13_01_M02_02` y CAS puntual, un solo EN_PROGRESO propia. El cierre de M02.01 no es prerequisito de esta unidad según DAG; no forzar M02.06 ni M03.
- **A0:** revisar requisitos y artefactos limitados a este Markdown y sección íntegra de CHANGELOG. **A1:** coherencia estática de ocho operaciones, doce DTO, doce errores, dieciséis validaciones y doce casos negativos: no afirmar ejecución. **A2:** control documental tenant A/B/RBAC/revocación/PII. **A3:** MySQL/API/Angular/Playwright/proveedor **NO_APLICA al diff R0** por no introducir runtime. **A4:** publicar fast-forward/CAS a dev, Scope Lock y V5 `completed/success` en SHA exacto o equivalencia demostrada, readback PLAN/COLA/BITACORA y owner; failure/in_progress/skipped/cancelled no PASS. **A5:** revisión independiente de otra Task Javier; el autor nunca se aprueba.
- **Salida y reversión:** M02.02 puede quedar LISTO_DEV sólo con A0–A4; A5 pendiente. M02.06 requiere M02.01–05 LISTO+A5 cuando DAG lo determine y no habilita cookies reales/legales, QA/main/PROD. Rollback documental mediante nuevo `git revert` sin fuerza ni borrar BITACORA; ningún dato o entorno se alteró con esta unidad.
