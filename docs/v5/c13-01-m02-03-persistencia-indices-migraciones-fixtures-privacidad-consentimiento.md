# SOLQARYN V17 — C13-01.M02.03: persistencia conceptual, índices, migraciones y fixtures de privacidad

**Unidad:** C13-01.M02.03 · PLAN_MAESTRO fila 1882 · DAG_TECNICO fila 1843. **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Riesgo:** R0 documental. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`.

## A0. Alcance y autoridad

Definir una propuesta verificable de persistencia, índices, migración futura reversible y fixtures sintéticos para privacidad, avisos, preferencias, consentimientos, revocación, retención y derechos. Este documento **no crea tablas, clases EF, migraciones, SQL, datos, consentimientos, derechos, cookies, políticas publicadas, endpoints ni acceso a proveedores**. No determina jurisdicción, plazos, bases legales ni medidas de tratamiento finales. Es un contrato de diseño M02, no certificación de runtime ni habilitación M03, QA, main o PROD.

**Fuentes del mismo lote:** C13-01.M01.01–06 con gates G01–G16, y M02.01 (E01–E16, I01–I20, T01–T12) y M02.02 (OP01–OP08, DTO01–12, ER01–12, V01–16). La elegibilidad de M02.03 deriva de M01 completo + A5, según DAG vigente; no se inventa dependencia técnica de M02.04 ni se declara A5 de este documento. Arquitectura futura: ASP.NET Core 10 por Domain/Application/Infrastructure/API, Oracle MySql.EntityFrameworkCore 10.0.9 con EF Core 10.0.12 sobre MySQL 8.4, autenticación JWT y RBAC revalidados server-side. Ningún archivo ejecutable se modifica.

## K01–K16: claves, índices y reglas candidatas (NO CREADAS)

| Clave | Entidad conceptual | Columnas/ámbito futuros | Índice o integridad propuesta | Condición de seguridad |
| --- | --- | --- | --- | --- |
| K01 | PolicyVersionReference | scope, locale, policyRef, revision | UNIQUE(scope,locale,policyRef,revision), immutable digest | Only approved public version; no inferred publication |
| K02 | TenantSubjectReference | tenantRef, subjectRef | UNIQUE(tenantRef,subjectRef); server-resolved membership | Cross-tenant FK never allowed; no PII in identifiers |
| K03 | PreferenceDecisionDraft | tenantRef,subjectRef,purposeKey,policyRef,decisionRef,revision | UNIQUE(tenantRef,subjectRef,decisionRef); composite FK K02 | No consent from silence, timeout or another version |
| K04 | ConsentEvidenceEnvelope | tenantRef,subjectRef,decisionRef,evidenceRef | INDEX(tenantRef,subjectRef,decisionRef,occurredAtUtc) | Digest and opaque correlation only; no JWT/PII payload |
| K05 | RevocationIntent | tenantRef,subjectRef,decisionRef,revocationRef | UNIQUE(tenantRef,subjectRef,revocationRef), expectedRevision CAS | Prevent late events from re-enabling revoked purpose |
| K06 | PreferenceSnapshotReference | tenantRef,subjectRef,purposeKey,lastRevision | INDEX(tenantRef,subjectRef,purposeKey,lastRevision) | Derived, revocable cache; never authority |
| K07 | PrivacyRightsCaseDraft | tenantRef,subjectRef,caseRef,verificationRef | UNIQUE(tenantRef,subjectRef,caseRef) | No export/erasure without identity and legal review |
| K08 | RetentionRuleProposal | categoryRef,scope,ruleRef,reviewRevision | UNIQUE(scope,categoryRef,ruleRef,reviewRevision) | Draft only; no TTL/DELETE or invented period |
| K09 | HoldAndExceptionReference | tenantRef,ruleRef,holdRef,reviewState | INDEX(tenantRef,ruleRef,reviewState) | Hold wins over destructive retention proposal |
| K10 | ProcessorDestinationCatalog | purposeKey,processorRef,regionRef,reviewRevision | UNIQUE(purposeKey,processorRef,regionRef,reviewRevision) | No SDK call or credential implied |
| K11 | AuditReadbackReference | tenantRef,eventRef,eventDigest,occurredAtUtc | INDEX(tenantRef,occurredAtUtc,eventRef) | Append-only minimal evidence; no token or email in log |
| K12 | PermissionContextReference | tenantRef,actorRef,membershipRevision,roleRevision | INDEX(tenantRef,actorRef,membershipRevision) | Server-side JWT/RBAC and revocation must be revalidated |
| K13 | IdempotencyKeyReference | tenantRef,subjectRef,purposeKey,operation,policyRevision,requestNonce | UNIQUE(tenantRef,subjectRef,purposeKey,operation,policyRevision,requestNonce) | Request nonce stable for retries of ONE logical intent, distinct for new intent |
| K14 | RequestPayloadDigestReference | K13 composite scope,payloadCanonicalDigest | UNIQUE(K13, payloadCanonicalDigest), compare-and-reject conflicting retry | Digest of full canonical security-relevant payload, not redacted display |
| K15 | DataCategoryCatalog | scope,categoryRef,sensitivityRevision | UNIQUE(scope,categoryRef,sensitivityRevision) | PII classification not determined by prose alone |
| K16 | PolicyRevisionChain | scope,policyRef,revision,previousDigest | UNIQUE(scope,policyRef,revision), immutable predecessor digest | Never reinterpret old decisions as approval of new text |

**Límites de cardinalidad:** TenantSubjectReference E05/E06 delimita toda decisión E08; evidencia E09 y revocación E10 mantienen clave compuesta con tenant y sujeto y referencia de decisión. Un snapshot E11 sólo deriva de decisiones confirmadas, nunca concede acceso. Hold E14 prevalece sobre reglas de retención E13. E15 referencia proveedor sin almacenar credenciales; E16 es lectura mínima y no reemplaza la BITACORA institucional. Ningún identificador opaco global sin tenant+subject verificados por backend acredita ownership.

**Idempotencia sin colisiones semánticas:** K13 incluye **requestNonce estable por solicitud lógica** (reutilizado sólo en reintentos del mismo intento), además de tenant, sujeto, propósito, operación y versión. K14 compara el **digest seguro del payload canónico completo relevante para autorización**; un digest de representación redactada no sirve para decidir igualdad. Igual K13+igual K14 = recibo original sin efecto adicional; igual K13+distinto K14 = conflicto seguro; distinto nonce = nueva intención, no duplicado falso. La cadena se decide en transacción con CAS y auditoría; no existe procesamiento real en M02.03.

## MX01–MX08: etapas futuras de migración aditiva y reversión (NO EJECUTADAS)

| Fase | Nombre | Puerta futura | Rollback/NO_GO |
| --- | --- | --- | --- |
| MX01 | Inventory/freeze | Catalogue existing EF entities, migrations, ownership, policy versions and incident holds in DEV; design-only now | Abort if schema/tenant uncertainty; no writes |
| MX02 | Isolated harness | Provision an ephemeral MySQL 8.4 test service only when implementation work is separately authorized; Oracle EF 10 provider match | No Aiven/prod or customer data |
| MX03 | Additive shadow model | Propose additive nullable entities and composite tenant FKs behind disabled flags in a future R2 change | Backout by disabling read paths; never destructive DROP |
| MX04 | Synthetic seed and backfill | Use tenant A/B fabricated fixtures and deterministic versions; backfill only after approved data mapping | No historic consent inferred or invented |
| MX05 | Dual-write/idempotency verification | Exercise CAS, conflicting payload digest, nonce replay, revocation and out-of-order events in a transaction | Rollback transaction and keep refusal as default |
| MX06 | Read-path verification | Compare tenant-scoped reads, indexes, EXPLAIN and ownership, including revoked memberships and holds | Any cross-tenant result is NO_GO |
| MX07 | Controlled cutover gate | Require real A0-A5, integration checks, legal/retention review and separately authorized environment change | Default flag remains OFF; no cutover this task |
| MX08 | Reversal/recovery rehearsal | Test disabling gates, maintaining immutable evidence and restoring synthetic fixture state with audit trail | No irreversible deletion or manual prod mutation |

Para un futuro cambio R2, acreditar que `Category=Integration` realmente ejecutó sobre servicio **MySQL 8.4 efímero** con proveedor Oracle EF Core previsto, migraciones aplicadas, reversión y 0 fallos; un build verde o filtro `Category!=Integration` no bastan. Se deberán revisar query plans/EXPLAIN, aislamiento tenant A/B, índices compuestos y FK desde la aplicación, y preservar datos/hold sin pérdida. **No ejecutar ni preparar comandos destructivos contra Aiven, QA o producción** con este artefacto.

## F01–F12: fixtures sintéticos propuestos para futuros tests

| Fixture | Dato inventado de prueba | Oráculo esperado futuro |
| --- | --- | --- |
| F01 | Tenant A; verified subject, purpose P1/version v1, explicit decision | Accept same-tenant read only |
| F02 | Tenant B with same opaque subject suffix as A | Deny foreign decision or existence leak |
| F03 | Revoked membership with unexpired JWT | Deny read and mutation |
| F04 | Role removed while cached snapshot exists | Deny despite UI/cache state |
| F05 | Silent/timeout/malformed choice | No affirmative evidence created |
| F06 | Version v2 with prior version v1 accepted | Require explicit new evaluation |
| F07 | Revocation then out-of-order old accept event | Revocation remains effective |
| F08 | Same stable requestNonce, same canonical payload | Return same receipt without second effect |
| F09 | Same requestNonce, different canonical payload | Reject conflict; preserve original receipt |
| F10 | Different logical intent with distinct nonce, same purpose | Do not falsely deduplicate new decision |
| F11 | Rights request with unverified subject and cross-tenant ID | Deny without enumeration |
| F12 | Retention proposal with active legal hold/provider failure | No deletion, transmission or optimistic PASS |

Todos son datos hipotéticos; **0/12 fixtures creados o ejecutados**. Si un futuro test requiere JWT, usar emisor de prueba y roles efectivos, nunca secretos o tokens productivos. No copiar datos reales ni generar claims de consentimiento por ser fixtures.

## T01–T12: pares positivos/negativos adversariales (FUTUROS_NO_EJECUTADOS)

| Par | Aserción positiva futura | Negativa que debe detectar defecto |
| --- | --- | --- |
| T01 | Tenant A access by verified subject resolves only own decision | Tenant B using A decisionRef receives no evidence/existence leak |
| T02 | Authorized RBAC + live membership permits scoped read | Revoked role or forged tenant header is denied |
| T03 | Explicit affirmative under approved policy version binds correct digest | Silence, UI error or stale version creates no consent |
| T04 | Same nonce + same full canonical payload replays one receipt | Same nonce + different payload digest yields conflict not false replay |
| T05 | Distinct nonce represents a separate intent and receives separate handling | Missing nonce or reused nonce for new intent is rejected |
| T06 | Valid revocation CAS increments revision, preserves evidence | Stale update or delayed accept cannot reauthorize |
| T07 | Private evidence view emits opaque digest only | JWT, complete email, cookie or raw PII appear in audit |
| T08 | Versioned policy chain verifies prior digest and immutable revision | Tampered previousDigest or mutable policyRef accepted |
| T09 | Rights case is bound to verified own subject | IDOR/BOLA exposes or exports foreign subject |
| T10 | Retention review reads proposed rule and legal hold | Unapproved TTL/DELETE destroys invoice, evidence or backup |
| T11 | Ephemeral Oracle EF/MySQL 8.4 composite FK rejects foreign tenant | Global FK or missing tenant composite allows cross-tenant row |
| T12 | Failed provider/restore retains deny and auditable state | Network timeout or partial rollback yields successful tracking |

**Ejecución:** 0/12 pares. Ninguna prueba xUnit, MySQL, HTTP, Playwright, Cloudinary o proveedor fue ejecutada por esta entrega. Son especificaciones para una implementación R2 posterior y no resultados `PASS`.

## Dependencias, seguridad, observabilidad e impacto

- **Domain/Application:** requisitos de inmutabilidad, revocación, ownership, idempotencia y denegación; no se tocan modelos ni servicios.
- **Infrastructure/MySQL:** propuesta de FK/index compuesto, CAS transaccional, migración aditiva y rollback; no se crean migraciones ni datos.
- **API/frontend:** consultas por sujeto autenticado, seguridad RBAC/JWT y UX accesible futura; la UI nunca sustituye controles server-side.
- **PII y auditoría:** digests opacos, minimización, no serializar email/JWT/cookies/credenciales; eventos correlacionables sin datos privados. Retención, backup, legal hold y derechos requieren revisión competente distinta de este diseño.
- **Dependencias DAG:** M02.06 exige LISTO+A5 real de M02.01–05 y las demás puertas del DAG; M03 y entornos fuera de dev no son autorizados por aprobar este Markdown.
- **Rollback documental:** revertir por nuevo commit Git tras verificar HEAD y diferencias de otros autores; nunca force-push, borrar BITACORA o modificar datos reales.

## A0–A5 y entrega controlada

**A0:** requisitos exactos del PLAN/COLA/DAG y comparación con M01.01–06/M02.01–02; diff sólo este archivo Markdown más sección autocontenida de `CHANGELOG_AI.md`. **A1:** revisión estática de K01–K16, MX01–MX08, F01–F12 y T01–T12 con oráculos negativos, **0 tests runtime ejecutados**. **A2:** tenant A/B, sujeto, ownership, JWT/RBAC vigente, revocación, IDOR/BOLA, nonce, digest canónico, hold y PII examinados como diseño, no prueba de API. **A3:** MySQL/EF, HTTP, navegador, migraciones y proveedor **NO_APLICA al diff documental R0**; deberán ejecutarse realmente en tareas R2. **A4:** exigir SHA de publicación fast-forward en dev, Scope Lock y procedencia V5 `completed/success` exact-SHA (o equivalencia auditada), readback PLAN/COLA/BITACORA y lock V17; cualquier `FAILED`, `CANCELLED`, `SKIPPED` o `IN_PROGRESS` requerido no es PASS. **A5:** `PENDIENTE` para otro TASK_ID Javier, revisión independiente con posibilidad de rechazo.

No hay aprobación legal ni migración aplicable, ni despliegue, ni habilitación automática de cookies, permisos, productos o entidades por la sola publicación de este contrato.
