# SOLQARYN V5 FINAL — C04-01.M02.04: configuración tipada y feature flags del catálogo SaaS

**Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Rama:** `solqaryn/Solqaryn@dev`. **Scope Lock:** STRICT. **Fecha:** 2026-11-15. **Alcance:** sólo documento de diseño, no configuración runtime.

## Fuentes y límites de autorización

El STANDARD V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO, `AGENTS.md`, Skill canónica y `docs/PROJECT_SCOPE_LOCK.md` gobiernan este contrato. El gate de esta microtarea exige `C04-01.M01.01–06+A5` LISTO con revisión independiente. Al claim se verificaron PLAN PENDIENTE sin owner, COLA G/O=SI y DAG F=VALIDADO/G=SI. Los contratos `C04-01.M02.01–03` son relacionados pero no dependencia técnica de este ID por conveniencia.

**Ninguna clave ni flag aquí escrita se instala, habilita o configura.** No editar `appsettings`, Angular environment, GitHub secrets, SQL/EF, suscripciones reales, planes, precios, impuestos, PSP, webhooks, trabajadores, CI, dominio, QA, main ni PROD. Los nombres son hipótesis tipadas para M03 elegible, pendientes de aprobación comercial y pruebas reales.

Fuentes del código existente referenciadas en M01: `SuscripcionesSaaS.cs`, `SuscripcionesSaaSService.cs`, `SuscripcionesSaaSRepository.cs`, `SuscripcionesSaaSController.cs` y `suscripcion-saas.service.ts`. Su presencia no acredita nuevos flags ni catálogo comercial aprobado. La capacidad de un plan **sólo restringe** una operación: no genera permiso de RBAC, membresía, identidad ni autorización de cobro.

## Precedencia tipada y fail-closed

Orden conceptual de resolución: gobierno/Scope Lock → plan comercial **aprobado/versionado** → configuración backend de ambiente → tenant autenticado/membresía vigente → versión de suscripción persistida → límites y unidades tipadas → permiso RBAC real → UI. El resultado DENY tiene precedencia absoluta; `tenantId`, `esAdministrador`, `planId`, precio, moneda o bandera del cliente nunca crean permiso. Un valor desconocido, ausente, obsoleto, corrupto o conflictivo produce denegación o `NO_CONFIGURED`, sin default ilimitado ni cobro.

## Dieciséis campos de diseño CFG01–CFG16 — NO IMPLEMENTADOS

| ID | Campo futuro tipado | Valor por defecto y regla estricta |
| --- | --- | --- |
| CFG01 | `catalog.schemaVersion` | version requerido; ausente bloquea |
| CFG02 | `catalog.planVersionRef` | referencia opaca inmutable, sin `latest` |
| CFG03 | `catalog.approvalRef` | ninguna aprobación por defecto |
| CFG04 | `catalog.visibility` | enum PRIVATE salvo autorización pública |
| CFG05 | `catalog.effectiveAtUtc` | fecha opcional, nunca retroactiva implícita |
| CFG06 | `catalog.expireAtUtc` | vigencia coherente o bloqueo |
| CFG07 | `catalog.limitUnit` | enum allowlist por métrica |
| CFG08 | `catalog.nullLimitPolicy` | DENY/EXPLICIT, nunca ilimitado implícito |
| CFG09 | `catalog.limitScale` | decimal/rango validado |
| CFG10 | `catalog.tenantScopeSource` | BACKEND_VERIFIED_ONLY |
| CFG11 | `catalog.entitlementMode` | RESTRICT_ONLY, no RBAC |
| CFG12 | `catalog.planChangeVersionRequired` | true, control ETag |
| CFG13 | `catalog.idempotencyScope` | tenant + operación + fingerprint |
| CFG14 | `catalog.moneyPolicyRef` | UNAPPROVED; no monetización sin decisión |
| CFG15 | `catalog.paymentExecutionAllowed` | false en este alcance |
| CFG16 | `catalog.environmentPromotionAllowed` | false, sin QA/PROD |

Estos campos requieren parser y validación backend de claves/tipos, resolución de origen autorizado, incompatibilidad explícita y decisión comercial versionada. Ninguno demuestra que hoy exista un servicio configurador.

## Doce feature flags hipotéticas F01–F12 (todas OFF)

| ID | Flag futura | Default, sin efecto de autorización |
| --- | --- | --- |
| F01 | `ShowPublicPlanComparison` | false, solo contenido aprobado |
| F02 | `ShowTenantPlanSummary` | false, requiere membresía |
| F03 | `ShowUsageAgainstLimits` | false, datos del propio tenant |
| F04 | `ShowPlanChangePreview` | false, ninguna mutación |
| F05 | `ShowDowngradeImpact` | false, sin borrar registros |
| F06 | `EnableSyntheticPricingDryRun` | false, sin precio real ni PSP |
| F07 | `ShowCommercialPolicyStatus` | false, no aprueba reglas |
| F08 | `ShowSubscriptionAuditReadOnly` | false, lectura redactada |
| F09 | `ShowLimitWarning` | false, no concede capacidad adicional |
| F10 | `EnableSyntheticTenantABPreview` | false, sin datos reales |
| F11 | `ShowIdempotencyDryRun` | false, sin transición |
| F12 | `ShowA5EvidenceSummary` | false, no revisa ni aprueba A5 |

**Defaults OFF son propuestas en este documento; ninguna variable/flag fue creada.** Aun en una futura UI, cada dato debe pasar primero por autorización backend; la vista no determina la identidad, el contrato comercial, el plan efectivo ni el permiso.

## Doce pares de prueba futura PN01–PN12, NO EJECUTADOS

| ID | Positivo futuro | Negativo que debe fallar |
| --- | --- | --- |
| PN01 | Versión aprobada presenta plan propio | Versión privada/no aprobada aparece públicamente |
| PN02 | Tenant A ve sus límites A | Tenant B recibe métricas de A por header |
| PN03 | Unidad y rango válidos calculan límite | Null/0/negativo se asume ilimitado |
| PN04 | Capacidad aprobada restringe una acción | Plan premium concede permiso RBAC ausente |
| PN05 | Preview de cambio no muta suscripción | UI con flag ON ejecuta cambio real |
| PN06 | Dedupe por tenant conserva una intención | Replay A reutilizado por tenant B |
| PN07 | Versión/ETag vigente permite decisión autorizada | Versión obsoleta altera el plan |
| PN08 | Downgrade informa recursos afectados | Downgrade borra recursos antes de autorización |
| PN09 | Política comercial aprobada permite cotización futura | Precio/moneda/impuesto sin aprobación se inventa |
| PN10 | Sin PSP, el modo sintético no genera cargo | Flag de simulación ejecuta pago real |
| PN11 | Auditoría propia entrega referencias saneadas | Token/PII/tenant B aparecen en logs |
| PN12 | CI completed/success exact-SHA acredita test real | Skipped/cancelled/in_progress se anuncia PASS |

**Estado de pruebas:** ninguna de las anteriores se ejecutó. Para implementar deberá existir microtarea M03 técnicamente autorizada, CI de .NET/Angular, fixtures A/B, pruebas negativas de RBAC/moneda, rollback y aprobación comercial independiente antes de cualquier dinero real.

## Regresión, recuperación y salida A0–A5

**R01** Configuración desconocida → denegar sin defaults abiertos. **R02** Tenant equivocado → aislar backend y rechazar. **R03** Entitlement sin grant → denegación. **R04** Versión obsoleta → conflicto sin mutación. **R05** Policy monetaria no aprobada → no cobrar. **R06** CI pendiente/cancelada → no PASS. **R07** Scope Lock FAILED → reparar causa, nunca desactivar verificador. **R08** Rollback de esta entrega → `git revert` normal del documento/CHANGELOG, sin tocar datos.

**A0:** dos archivos Markdown y encabezado del CHANGELOG contiguo, sin cambios a código, secrets ni entornos. **A1:** 16 CFG, 12 flags OFF, 12 pares positivos/negativos y ocho rutas de recuperación como contratos futuros, no tests runtime. **A2:** reglas tenant A/B, membresía/JWT, RBAC y monetización fail-closed en texto, sin afirmar certificación funcional. **A3:** M01.01–06 LISTO+A5, compatibilidad conceptual con API/suscripción actual y M02 pares independientes; MySQL/Angular/.NET runtime NO_APLICA al diff Markdown. **A4:** publicación fast-forward/CAS a dev, blob/diff/HEAD exacto, Scope Lock y check V5 completed/success, readbacks PLAN/COLA/DAG/BITACORA. **A5:** otra Task Javier distinta de la autora con evidencia de SHA y diff, sin autoaprobación ni autorización QA/main/PROD.

**Salida:** contrato documental M02 LISTO_DEV únicamente tras A0–A4; A5_PENDIENTE. Toda operación comercial, despliegue o migración sigue bloqueada hasta autorizaciones y pruebas técnicas reales.
