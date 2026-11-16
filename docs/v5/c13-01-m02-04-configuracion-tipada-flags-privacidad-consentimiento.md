# SOLQARYN V17 — C13-01.M02.04: configuración tipada y flags de privacidad futuros

**ID:** C13-01.M02.04, PLAN_MAESTRO fila 1883, DAG_TECNICO fila 1844. **Autor:** PRIMARY DEV06 JAVIER, TASK_ID 6af16bbad26881918c283abe06e1f32a. **Riesgo:** R0 documental. **Repositorio:** solqaryn/Solqaryn, rama dev. PROJECT_SCOPE_LOCK=STRICT. Fuente: STANDARD DE IMPLEMENTACION DE AUTOMATIZACIONES V5 FINAL; PLAN/COLA/DAG/BITACORA; calidad V15 y candado V17.

## A0. Alcance y no-implementación

El resultado es un modelo de configuración conceptualmente tipada para futuras políticas de privacidad, avisos, finalidades, consentimientos, revocaciones, derechos, retención y terceros. Ninguna de estas opciones o banderas existe por esta entrega en el código: no se añaden endpoints, DTO compilados, Angular, EF/MySQL, cookies, SDK, credenciales, worker ni textos legales publicados. No se afirma jurisdicción, base jurídica, plazo legal o cumplimiento real. El cierre documental de M01.06 con A5 independiente permite este diseño M02, no habilita runtime, QA/main/PROD ni decisiones jurídicas pendientes.

Los contratos M01.01–06 conservaron NR01–16, brechas B01–12, riesgos R01–14, decisiones D01–10 abiertas, aceptación AC01–16 futura y gates G01–16 de M01.06. Los modelos paralelos M02.01 y M02.02 son documentales y no se tratan como autorización. La elegibilidad de esta unidad se obtuvo de PLAN H=PENDIENTE/I vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI con candado V17 exclusivo; no se editan esas fórmulas.

## CFG01–CFG16: campos y ámbitos propuestos (sin código)

| Clave | Campo tipado conceptual | Tipo/ámbito futuro | Regla negativa |
| --- | --- | --- | --- |
| CFG01 | PolicyScope | tenant/public/platform | Reject cross-tenant fallback |
| CFG02 | PolicyVersion | immutable digest+revision | Reject stale/unknown |
| CFG03 | NoticeLocale | approved locale | No legal text guessed |
| CFG04 | PurposeKey | reviewed enumerated key | Unknown denied |
| CFG05 | PurposeCategory | technical/optional pending review | Unknown denied |
| CFG06 | LegalReviewReference | opaque approval reference | No implicit legal approval |
| CFG07 | SubjectScope | verified subject reference | Cannot trust browser |
| CFG08 | TenantScope | server resolved membership | Cannot trust header |
| CFG09 | RoleGrantRevision | effective role revision | Revoked role denied |
| CFG10 | DecisionRevision | monotonic expected revision | No default consent |
| CFG11 | RevocationRevision | current revocation version | Stale cache denied |
| CFG12 | EvidenceRedaction | minimal digest profile | No JWT/PII |
| CFG13 | RetentionRuleReference | category+legal hold review | No automatic DELETE |
| CFG14 | ProcessorReference | verified destination reference | No live call |
| CFG15 | ReleaseScope | environment and tenant | No PROD by DEV flag |
| CFG16 | AuditCorrelation | opaque tenant-scoped trace | No unredacted payload |

El contexto de seguridad proviene de servidor y membresía activa: el navegador y un flag no son fuente de privilegios. Cualquier revisión o digest debe ser opaco y referenciado por tenant/sujeto/versión cuando aplique. Nunca almacenar JWT, cookies privadas, claves, contenido PII libre o recursos de otro tenant en este esquema. Los cambios de configuración no reinterpretan consentimientos históricos.

## F01–F12: flags hipotéticos, todos FALSE por defecto

| Flag | Nombre futuro | Default | Barrera para activar en otro scope |
| --- | --- | --- | --- |
| F01 | noticePreviewEnabled | false | Revisión legal y técnica pertinente; RBAC, tenant A/B, revocación, A0–A5 y autorización de entorno |
| F02 | approvedPolicyReadEnabled | false | Revisión legal y técnica pertinente; RBAC, tenant A/B, revocación, A0–A5 y autorización de entorno |
| F03 | preferenceUiEnabled | false | Revisión legal y técnica pertinente; RBAC, tenant A/B, revocación, A0–A5 y autorización de entorno |
| F04 | optionalTrackingEnabled | false | Revisión legal y técnica pertinente; RBAC, tenant A/B, revocación, A0–A5 y autorización de entorno |
| F05 | decisionWriteEnabled | false | Revisión legal y técnica pertinente; RBAC, tenant A/B, revocación, A0–A5 y autorización de entorno |
| F06 | revocationWriteEnabled | false | Revisión legal y técnica pertinente; RBAC, tenant A/B, revocación, A0–A5 y autorización de entorno |
| F07 | rightsDraftEnabled | false | Revisión legal y técnica pertinente; RBAC, tenant A/B, revocación, A0–A5 y autorización de entorno |
| F08 | rightsExportEnabled | false | Revisión legal y técnica pertinente; RBAC, tenant A/B, revocación, A0–A5 y autorización de entorno |
| F09 | retentionAutomationEnabled | false | Revisión legal y técnica pertinente; RBAC, tenant A/B, revocación, A0–A5 y autorización de entorno |
| F10 | processorSyncEnabled | false | Revisión legal y técnica pertinente; RBAC, tenant A/B, revocación, A0–A5 y autorización de entorno |
| F11 | privateEvidenceReadEnabled | false | Revisión legal y técnica pertinente; RBAC, tenant A/B, revocación, A0–A5 y autorización de entorno |
| F12 | policyPublishEnabled | false | Revisión legal y técnica pertinente; RBAC, tenant A/B, revocación, A0–A5 y autorización de entorno |

Si falta el campo, la versión, la identidad, el permiso, la revisión jurídica o el proveedor, se mantiene false y DENY. En particular, optionalTrackingEnabled, rightsExportEnabled, retentionAutomationEnabled, processorSyncEnabled y policyPublishEnabled no pueden activarse con instrucciones documentales ni con un override administrativo. No existe rollout porcentual, fallback de consentimiento positivo ni permiso implícito ante timeout.

## EC01–EC08: errores cerrados de futura configuración

| Código | Causa futura | Resultado exigido |
| --- | --- | --- |
| EC01 | TENANT_UNKNOWN | DENY if tenant invalid |
| EC02 | PURPOSE_UNKNOWN | DENY unreviewed purpose |
| EC03 | POLICY_STALE | DENY version mismatch |
| EC04 | LEGAL_REVIEW_PENDING | No timeframe presumed |
| EC05 | MEMBERSHIP_REVOKED | DENY even valid JWT |
| EC06 | REPLAY_OR_CONFLICT | Never restore revoked acceptance |
| EC07 | ENV_OR_DESTINATION_DENIED | No QA/PROD or provider action |
| EC08 | CONFIG_MISSING | Fail closed, never auto-enable |

Una configuración errónea no ejecuta DELETE de facturas, borra una revocación o reabre tracking. Revertir la interfaz debe mantener evidencias de decisiones válidas y preservar excepciones de conservación legal comprobadas. El runbook de rollback M01.06 permanece una obligación futura, no una operación ejecutada aquí.

## T01–T12: pares positivo/negativo FUTUROS_NO_EJECUTADOS

| Caso | Comportamiento positivo futuro | Regresión negativa que debe fallar |
| --- | --- | --- |
| T01 | approved policy visible only in correct scope | unapproved version becomes effective |
| T02 | tenant A sees only own version/preferences | B sees A by changed selector |
| T03 | explicit valid purpose decision records version | silence becomes affirmative |
| T04 | revocation invalidates future optional use | stale snapshot reauthorizes |
| T05 | config revision invalidates stale decision | old acceptance covers new version |
| T06 | missing flag remains false | missing config enables a tracker |
| T07 | authorized actor reads redacted evidence | support extracts JWT or PII |
| T08 | rights draft verifies requesting subject | foreign subject exported |
| T09 | retention hold prevents destructive action | unapproved TTL deletes invoice |
| T10 | provider reference validated before use | arbitrary URL receives data |
| T11 | DEV scope cannot enable PROD | DEV flag triggers production |
| T12 | future UI permits accessible reject | keyboard cannot reach refusal |

**Ejecución real:** 0/12 pares. Son oráculos estáticos que pueden detectar fallos en una implementación futura. No se ha probado HTTP/JWT, Playwright, MySQL 8.4, scripts de cookies, terceros, xUnit, ley aplicable ni entornos reales en este cambio. NO_MEDIDO para latencia, cobertura runtime y tasa de fallo.

## Arquitectura y A0–A5

Angular 22.2.1 sólo mostraría opciones accesibles, jamás resolvería el tenant de confianza. ASP.NET Core 10 resolvería membresía, RBAC, dueño y revocación del lado servidor; Domain/Application/Infrastructure no reciben cambios. Cualquier persistencia real exigiría índices por tenant, constraints, migración reversible e integración en MySQL 8.4 aislado en otra microtarea. Los proveedores y la finalidad de tratamiento deben verificarse antes de invocar cualquier SDK; ningún valor sensible se guarda en Git.

**A0:** revisar el contrato y un diff restringido a este Markdown y la sección completa del CHANGELOG. **A1:** coherencia estática de 16 campos, 12 flags, 8 errores y 12 pares adversariales; 0 tests runtime. **A2:** revisión documental tenant A/B, JWT/RBAC, revocación, mínimo privilegio, PII y denegación por defecto. **A3:** MySQL, HTTP, Playwright, API y terceros NO_APLICA al diff R0 porque no se modifica runtime; esos gates permanecen pendientes para el código futuro. **A4:** fast-forward/CAS en dev y checks Scope Lock/V5 completed/success en SHA exacto o equivalencia demostrada, con readback PLAN/COLA/BITACORA. **A5:** sólo otra Task Javier distinta del autor puede aceptar o rechazar este cambio.

**Dependencias y salida:** M02.06 exige sus predecesoras LISTO+A5; M03/QA/main/PROD siguen inhabilitadas según DAG y autorizaciones reales. El rollback documental es un nuevo commit git revert después de verificar HEAD, sin force-push ni modificación de BITACORA histórica. No se han modificado leyes, cookies, datos, proyectos o proveedores.