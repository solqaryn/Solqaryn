# SOLQARYN V5 FINAL — C04-01.M02.02: contratos API/DTO, errores y compatibilidad del catálogo de planes SaaS

**Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Repositorio:** `solqaryn/Solqaryn@dev`. **Scope Lock:** STRICT. **Fecha:** 2026-11-15. **Naturaleza:** contrato documental; rutas y DTO propuestos, NO implementados.

## Autoridad, dependencia y fronteras

Fuente operativa: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` (PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA), `AGENTS.md`, Scope Lock, Skill canónica, `docs/v5/c04-01-m01-06-bloqueo-rollback-liberacion-catalogo-planes.md`.

Preflight: `C04-01.M01.01–06` LISTO con A5 independientes, PLAN H585=PENDIENTE/I585 vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI. La unidad M02.01 de otro owner es independiente según el DAG; no reescribir sus archivos ni convertir su VALIDANDO en PASS. No se modifican controles, precios, planes reales, suscripciones productivas, impuestos, proveedores ni fondos. El diseño requiere autorización del propietario antes de decisiones comerciales.

El código actual referenciado por M01 incluye `SuscripcionesSaaS.cs` (Plan, PlanLimite, Suscripcion), `SuscripcionesSaaSService.cs`, `SuscripcionesSaaSController.cs`, `SuscripcionesSaaSRepository.cs` y `suscripcion-saas.service.ts`. **Que existan estas clases no demuestra** nuevos endpoints, tarifas oficiales, cobro, fiscalidad, catálogo versionado, prorrateo o migración. La semántica existente debe comprobarse en el HEAD de una implementación M03 autorizada.

## Doce contratos API futuros API01–API12, NO IMPLEMENTADOS

Son nombres lógicos de operaciones para comparar con el código vivo, **no URLs ni métodos HTTP desplegados**. Ninguno se activa por documentación.

| ID | Operación conceptual y entrada mínima | Autoridad/resultado esperado |
| --- | --- | --- |
| API01 | ListAvailablePlanVersions(visibility, cursor) | Sólo versiones publicables aprobadas, sin tarifa inventada; catálogo público saneado |
| API02 | GetPlanVersion(planRef, revision) | Identidad y versión inmutables, no revelar plan privado |
| API03 | GetPlanLimitDefinitions(planVersionRef) | Límites tipados con unidad, escala, vigencia y semántica NULL explícita |
| API04 | GetCurrentTenantSubscription() | JWT+membresía vigentes; tenant resuelto por backend |
| API05 | GetEffectiveTenantEntitlements(module) | Capacidad de plan es tope, nunca permiso RBAC |
| API06 | QuotePlanChange(targetVersion, effectiveAt) | **Simulación**: no cobro, cálculo final sujeto a decisión comercial |
| API07 | RequestPlanChange(targetVersion, expectedVersion, dedupeKey) | Sólo futuro con autorización comercial/tenant; no efecto ahora |
| API08 | PreviewDowngradeImpact(targetVersion) | Mostrar límites y recursos afectados sin eliminar datos |
| API09 | GetSubscriptionHistory(cursor) | Auditoría filtrada por tenant/sucursal, sin PII ajena |
| API10 | GetOwnUsageAgainstLimits(window) | Medición desde backend, denegar contador ajeno |
| API11 | ValidateCommercialPolicy(planVersionRef, policyVersion) | Política fiscal, moneda y consentimiento **pendientes**, sin autoaprobar |
| API12 | GetSubscriptionSafeStatus(requestRef) | Resultado backend observado, nunca `paid` de texto UI |

Una eventual versión HTTP debe preservar contratos existentes de suscripción, onboarding, límites y clientes Angular mediante migración aditiva y pruebas de regresión. La UI no define precio, moneda, rol, tenant, condiciones de pago ni estado fiscal como fuente de verdad.

## Doce DTO tipados propuestos D01–D12 — NO CREADOS

| ID | Contrato conceptual | Regla fail-closed |
| --- | --- | --- |
| D01 | PlanVersionRef {opaqueId, revision, validFromUtc} | No reutilizar versión para tarifa diferente |
| D02 | PlanLimitDefinition {module, metric, unit, max, nullPolicy} | Valor 0/negativo o null ambiguo: rechazo |
| D03 | TenantSubscriptionView {planRef, status, sourceVersion} | Tenant sale de JWT+membresía, no del request |
| D04 | EffectiveCapabilityView {metric, allowance, remaining} | Capacidad no concede RolPermiso |
| D05 | PlanChangePreview {targetRef, effectiveAtUtc, warnings} | No compromete un nuevo precio |
| D06 | PlanChangeRequest {targetRef, expectedVersion, dedupeKey} | Requiere permiso servidor, política comercial y CAS |
| D07 | DowngradeImpact {limitedResources, actionRequired} | No ejecuta borrados ni mutaciones |
| D08 | SubscriptionAuditEntry {opaqueEvent, status, utc} | Sin datos de pago ni PII |
| D09 | UsageWindow {metric, fromUtc, toUtc, count} | No unir contadores de empresas |
| D10 | CommercialPolicyRef {policyVersion, currency, taxRule} | NULL/ausencia de aprobación = NO_GO monetario |
| D11 | SafeSubscriptionError {safeCode, correlationId} | Nunca tokens, stack, detalles de tenant B |
| D12 | CompatibilityEnvelope {apiVersion, etag, deprecatedAfter} | Rechazo de versión incompatible antes de efecto |

El esquema deberá tener enums y rangos allowlisted, errores saneados, versiones explícitas, dinero decimal de escala controlada y referencias opacas; rechazar `empresaId`, `esAdministrador`, `precioFinal`, `moneda`, `allowPayment` o `grant` enviados por navegador como autoridad.

## Catorce validaciones V01–V14 y doce errores ERR01–ERR12 (propuestos)

| Validación | Condición futura exigida |
| --- | --- |
| V01 | JWT válido y membresía servidor vigente |
| V02 | RBAC real por recurso, acción y empresa |
| V03 | Tenant de suscripción coincide con tenant autenticado |
| V04 | Sucursal pertenece al tenant y grant |
| V05 | Plan versionado e inmutable, vigencia UTC comprobada |
| V06 | Límites válidos por unidad, escala y nullPolicy |
| V07 | Entitlement sólo restringe, no otorga permiso |
| V08 | Versión/If-Match para cambios concurrentes |
| V09 | Replay key aislada tenant+operación+payload |
| V10 | Importe/moneda/impuesto calculados sólo con política aprobada |
| V11 | PSP/cobro no invocable sin autorización distinta |
| V12 | Borrado/corte por downgrade prohibido sin autorización |
| V13 | Evidencia de auditoría saneada, sin JWT/PII |
| V14 | CI causal exact-SHA, A5 externa y readback antes de liberar |

| Error | Código futuro | Significado y efecto |
| --- | --- | --- |
| ERR01 | AUTH_REQUIRED | JWT inválido o ausente |
| ERR02 | MEMBERSHIP_REVOKED | Membresía denegada |
| ERR03 | PERMISSION_DENIED | RBAC o scope denegado |
| ERR04 | SUBSCRIPTION_NOT_FOUND | 404 sin revelar otro tenant |
| ERR05 | PLAN_VERSION_UNKNOWN | Identidad/versión no aprobada |
| ERR06 | INVALID_LIMIT_CONTRACT | Unidad/valor/null inválido |
| ERR07 | STALE_SUBSCRIPTION | Conflicto de versión |
| ERR08 | IDEMPOTENCY_CONFLICT | Mismo replay key, payload distinto |
| ERR09 | COMMERCIAL_POLICY_UNAPPROVED | Precio/impuesto/moneda no autorizados |
| ERR10 | PAYMENT_NOT_AUTHORIZED | Sin PSP o permiso comercial |
| ERR11 | DOWNGRADE_IMPACT_BLOCKED | Conservación de datos/límites no resuelta |
| ERR12 | PROVIDER_UNAVAILABLE | Fallo no implica cobro ni alta |

Los códigos son hipótesis contractuales. La implementación escogerá HTTP y mapping de consumidores tras revisar rutas reales, sin asumir que existen ahora.

## Doce pares positivos/negativos — FUTUROS_NO_EJECUTADOS

| ID | Positivo futuro esperado | Negativo que debe bloquear |
| --- | --- | --- |
| PN01 | Tenant A consulta su suscripción y límites A | Tenant B consulta ID A por header |
| PN02 | Catálogo público muestra sólo versión autorizada | Plan privado o tarifa no aprobada visible |
| PN03 | JWT y permiso permiten consulta de capacidades A | Plan premium crea grant RBAC inexistente |
| PN04 | Versión vigente permite cotización sintética | ETag obsoleto solicita cambio real |
| PN05 | Dos reintentos idénticos resultan una intención | Dedupe cross-tenant mezcla solicitudes |
| PN06 | Moneda/impuesto aprobado produce precio reproducible | Precio no decidido inventa monto o cobra |
| PN07 | Límite tipado produce cálculo correcto | Cero/negativo/null ambiguo se interpreta ilimitado |
| PN08 | Downgrade muestra impacto antes de optar | Downgrade elimina recursos sin autorización |
| PN09 | Auditoría A devuelve registros saneados A | Token, correo o registros B se filtran |
| PN10 | Suscripción revocada no permite nuevos efectos | JWT antiguo mantiene permiso tras revocación |
| PN11 | UI consume contrato existente compatible | Nuevo DTO rompe onboard o cliente Angular |
| PN12 | Check completed/success exact-SHA avala test real | Cancelled/skipped/queued se atribuye PASS |

**NINGUNO de estos casos se ejecutó.** Su futura validación requiere xUnit/HTTP, datos sintéticos A/B, controles de concurrencia, integración MySQL 8.4 efímero y pruebas frontend proporcionales, todo bajo microtarea técnica elegible.

## Seguridad, evolución, A0–A5 y rollback

**A0:** diff exclusivo de este Markdown y una sección contigua `CHANGELOG_AI.md`, sin SQL/DTO compilados/secrets/infra. **A1:** revisión de 12 API, 12 DTO, 14 validaciones, 12 errores y 12 pares P/N propuestos, sin tests runtime. **A2:** JWT/RBAC/tenant A/B, sucursal, propiedad, versionado, idempotencia, privacidad, PSP y dinero fail-closed sólo como contrato. **A3:** M01.01–06 LISTO con A5 independiente, coherencia con código existente y diseño M02.01 sin asignar una dependencia no presente en DAG; cambios .NET/Angular/MySQL = NO_APLICA en diff documental. **A4:** commit fast-forward CAS en dev, blobs/diff/HEAD exactos, Scope Lock y check V5 `completed/success` sobre SHA causal, PLAN/COLA/DAG/BITACORA readback. **A5:** otra Task Javier distinta del autor; no autoaprobación ni autorización QA/main/PROD.

**Política de reversión:** `git revert` normal de la documentación y CHANGELOG, sin modificar BITACORA histórica. No hay cobro, suscripción, base de datos o contrato de usuario que revertir por esta entrega. Quedan pendientes decisiones comerciales formales para cualquier implementación monetaria posterior.
