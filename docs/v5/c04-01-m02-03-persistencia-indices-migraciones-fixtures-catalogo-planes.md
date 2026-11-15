# SOLQARYN V5 — C04-01.M02.03: persistencia, índices y migración futura del catálogo SaaS

**Autor:** PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-15. **Repositorio:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO, skill canónica, AGENTS.md.

**Tipo de entrega: CONTRATO DOCUMENTAL.** No se ejecutan migraciones, sentencias DDL, consultas a Aiven, cambios de EF, claves, tarifas, cobros, API, webhooks ni workers. Todas las pruebas/fixtures son `FUTUROS_NO_EJECUTADOS`.

## 1. Gate causal y baseline implementado

- Antes de reclamar `C04-01.M02.03`, PLAN H586=PENDIENTE/I586 vacío, COLA G586/O586=SI y DAG F=VALIDADO/G=SI; predecesores explícitos `C04-01.M01.01–06+A5` estaban LISTO con revisión independiente, incluyendo M01.06 documentada en SHA `960443f9eabe54645059597c34e3c6ea4eeb0064`.
- `backend/src/Domain/Entities/SuscripcionesSaaS.cs`, blob `d9b5dbe19fb728293b00b748f10ac10336debd2d`: ya define `Plan`, `PlanLimite`, `Suscripcion` y `EstadoSuscripcion`; `Plan.DefinirLimite` valida positivo o `null`; `Suscripcion.EmpresaId` relaciona tenant y `PlanId`; no presupone `PlanRevision`, tarifas o ledger nuevos.
- Se debe conservar compatibilidad con el servicio `SuscripcionesSaaSService`, repositorio `SuscripcionesSaaSRepository`, API/DTO y cliente Angular existentes, evaluados en M01.03. `C04-01.M02.01` es diseño relacionado y no es una autorización para crear SQL; `M02.06` y `M03` dependen de más gates.
- Stack de referencia: Domain → Application → Infrastructure, ASP.NET Core 10, EF Core 10.0.12, Oracle MySql.EntityFrameworkCore 10.0.9 y MySQL 8.4; ninguna migración sobre bases reales ni datos financieros.

## 2. Diez estructuras relacionales hipotéticas, SIN DDL

| ID | Estructura futura propuesta | Claves/relaciones y garantía |
| --- | --- | --- |
| DB01 | PlanRevision | planId, revisionId, effectiveFromUtc/ToUtc, status; historia inmutable |
| DB02 | PlanCapabilityBinding | revisionId, moduleCode, capabilityCode; no convierte capacidad en grant |
| DB03 | PlanLimitRevision | revisionId, limitKey, maxValue, unit; valor positivo o `null` aprobado |
| DB04 | SubscriptionRevisionBinding | empresaId validado, subscriptionId, planRevisionId, UTC; tenant aislado |
| DB05 | BillingPeriodContract | revisionId, periodCode, anchorUtc; sin cargos por sí mismo |
| DB06 | ProposedRateDecision | planRevisionId, currency, decimal amount, approvalRef; precio hipotético |
| DB07 | TaxPolicyDecision | proposedRateRef, jurisdiction, version, approvalRef; no cálculo actual |
| DB08 | PlanTransitionRequest | tenant, requestId, oldPlan/newPlan, expectedVersion; idempotente |
| DB09 | CapabilityConsumptionPoint | tenant, capability, count, measurementUtc; solo lectura autoritativa futura |
| DB10 | CatalogAuditRecord | actorHash, tenantHash, eventId, policyVersion, utc; sin JWT/PII |

**Las diez DBxx son contratos conceptuales**, no tablas creadas ni mapeos EF existentes. Las entidades actuales Plan, PlanLimite y Suscripcion continúan siendo la fuente implementada; no crear una segunda autoridad paralela de suscripciones.

## 3. Ocho índices propuestos, requieren EXPLAIN antes de implementarse

| Índice futuro | Claves y propósito |
| --- | --- |
| IX01 | PlanRevision(planId, revisionId) UNIQUE; revisión inequívoca |
| IX02 | PlanRevision(planId, effectiveFromUtc, effectiveToUtc); seleccionar vigencia |
| IX03 | PlanCapabilityBinding(revisionId, moduleCode, capabilityCode) UNIQUE; deduplicación |
| IX04 | PlanLimitRevision(revisionId, limitKey) UNIQUE; un límite por clave |
| IX05 | SubscriptionRevisionBinding(empresaId, subscriptionId, effectiveFromUtc); aislamiento |
| IX06 | PlanTransitionRequest(empresaId, requestId) UNIQUE; idempotencia tenant |
| IX07 | CapabilityConsumptionPoint(empresaId, capabilityCode, measurementUtc); lectura acotada |
| IX08 | CatalogAuditRecord(tenantHash, utc, eventId); auditoría y correlación saneada |

No se decide tipo físico de columna, FK, lock o estrategia de reconstrucción sin inspeccionar `AppDbContext`, migraciones y planes reales. `EXPLAIN`, integridad de FK, cardinalidad y huella de índices serán pruebas de integración en MySQL 8.4 efímero, no supuestos.

## 4. Seis pasos para migración futura, NO EJECUTADOS

1. **MG01 Baseline:** mapear tablas existentes, provider EF, relaciones Plan/PlanLimite/Suscripcion, consumidores, nulabilidad, datos históricos y versión Git exacta.
2. **MG02 Snapshot:** crear respaldo **efímero** restaurable, fixtures tenants A/B y hashes de suscripciones/planes; no utilizar PII ni Aiven real.
3. **MG03 Expand:** definir migración aditiva versionada de nuevas revisiones, FKs e índices con límites de locks y rollback; ejecutarla solo en runner MySQL aislado.
4. **MG04 Backfill:** cargar fixtures sintéticos en batches idempotentes, preservar empresa, versiones, vigencia UTC y asociaciones antiguas; ningún precio real.
5. **MG05 Verify:** afirmar con resultados CI causales consistencia y transacciones, isolation A/B, replays, rollback, EXPLAIN, latencia y consultas legacy.
6. **MG06 Restore:** revertir migración y restaurar snapshot efímero, comparar conteos, claves, digests, entidades y eventos auditables sin fugas.

Esta documentación no representa autorización para ejecutar MG03. Se necesita microtarea M03 elegible, CI concreta, revisión de seguridad y permiso de entorno; no usar ALTER/DROP o escrituras en la base productiva.

## 5. Doce fixtures futuros, positivos/negativos

| Caso | Oráculo futuro positivo | Oráculo futuro negativo |
| --- | --- | --- |
| FX01 | Usuario A consulta suscripción A con membresía y grant | JWT A consulta suscripción B (BOLA/IDOR) |
| FX02 | Revisión vigente de plan conserva contrato A | Revisión B o expirada se asigna a A |
| FX03 | Un límite positivo se asocia a clave normalizada | Límite cero/negativo o duplicado se acepta |
| FX04 | `null` ilimitado explícito se evalúa restrictivamente | `null` ambiguo eleva cuota o permiso |
| FX05 | Capacidad A limita acción concedida por RBAC | Plan premium concede acción sin RolPermiso |
| FX06 | Cambio autorizado A conserva versión e historia | Upgrade altera plan de B o historial antiguo |
| FX07 | Replay idempotente crea una transición lógica | Misma key, diferente payload crea doble cargo |
| FX08 | Fallo DB revierte cambio con audit limpio | Timeout deja suscripción parcial o grant |
| FX09 | Catálogo aprobado expone moneda/tarifa verificada | Precio inventado por cliente se acepta |
| FX10 | Dos tenants A/B simultáneos nunca comparten estado | Cache/FK sin tenant filtra una suscripción B |
| FX11 | Auditoría almacena campos mínimos y hash opaco | JWT, PII, dato bancario o URL sensible queda en log |
| FX12 | Restore efímero preserva conteos, FK, digest y vigencia | Restore parcial declarado PASS sin prueba |

Los doce casos NO fueron ejecutados en xUnit, SQL, EF, HTTP ni E2E. La ejecución futura debe tener transacciones, aislamiento de tenant, negativos y pruebas reales MySQL 8.4 efímeras. Pagar, cobrar o asignar tarifas no está autorizado.

## 6. Seguridad, A0–A5, evolución y reversión

- **A0:** comparar diff de exactamente dos Markdown (este contrato y un bloque autocontenido de `CHANGELOG_AI.md`), comprobar procedencia, no secretos, y base `dev` viva.
- **A1:** revisar diez estructuras DB, ocho índices, seis pasos MG y doce fixtures FUTUROS_NO_EJECUTADOS; no contar casos diseñados como tests reales.
- **A2:** verificar contratos fail-closed JWT/RBAC/tenancy A/B, revocación, integridad por empresa, historial, idempotencia, auditoría sin PII y ausencia de efectos monetarios.
- **A3:** coherencia con M01.01–06 LISTO+A5, Plan/PlanLimite/Suscripcion Domain y arquitectura; MySQL/Docker/EF/Node/builds = NO_APLICA para documento sin cambio runtime.
- **A4:** publish fast-forward CAS a `dev`, SHA/blob/diff exactos, `verify-solqaryn-project-scope` y comprobación V5 de CHANGELOG `completed/success` en ese SHA; BITACORA y PLAN/COLA/DAG readback real.
- **A5:** pendiente de otra de las cinco Tasks Javier distintas del TASK_ID autor, con original SHA y evidencia, antes de sucesoras que lo exijan.
- **Rollback:** `git revert` normal sobre HEAD actualizado y evento compensatorio aditivo; no hay datos/SQL/planes que revertir en esta entrega. No QA/main/PROD, pasarela, worker, webhook, secreto, DNS ni servicio externo.
