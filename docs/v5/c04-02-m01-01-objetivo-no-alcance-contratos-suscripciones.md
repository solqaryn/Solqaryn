# SOLQARYN V5 — C04-02.M01.01: objetivo, no-alcance y no-regresión de contratos y suscripciones

**Autor:** SOLQARYN PRIMARY DEV 04 — JAVIER; TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Fecha:** 2026-11-14. **Rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Clase:** M01 preparación documental; no cambia el estado ejecutable de contratos ni las capacidades de cobro.

## 1. Autoridad y objetivo verificable

Fuente única: `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` V5 FINAL, documento Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. Entrada del plan `C04-02.M01.01`, fila 614, bloque `C04-02 — Contratos y suscripciones`: «Fijar objetivo, no-alcance y criterio de no-regresión». Resultado final del bloque: alta, renovación, pausa, cancelación, morosidad y cierre contractual con estados y transiciones consistentes y auditables.

**Resultado específico de esta microtarea:** definir las fronteras del futuro ciclo de vida de suscripción SaaS, las invariantes que no pueden relajarse y oráculos documentales para posteriores etapas M02–M06. El documento no constituye un motor contractual ni una certificación de una máquina de estados económica. `COLA!G614/O614=SI` y `DAG_TECNICO!F575=VALIDADO/G575=SI` fueron comprobados para preparación M01, no para autorización de pagos, migraciones o liberación de fases.

## 2. Línea de base de código realmente inspeccionada

| Fuente en dev | Blob observado | Hecho acreditado por lectura | No acredita |
| --- | --- | --- | --- |
| `backend/src/Domain/Entities/SuscripcionesSaaS.cs` | `d9b5dbe19fb728293b00b748f10ac10336debd2d` | `Suscripcion` tiene `Activa`, `Suspendida`, `Cancelada`, `Expirada`; `Suspender`, `Reactivar`, `Cancelar` y `EsVigenteEn`; relación `EmpresaId/PlanId` | Morosidad, cobro, renovación contractual automática o persistencia de historial de cambios de precio |
| `backend/src/Application/Services/SuscripcionesSaaSService.cs` | `bb501285f11cd4b32709a58a2c51efa19c3664ea` | `ResolverTenantAsync` valida el contexto antes de repositorio; onboarding exige `Idempotency-Key`; replay idéntico y conflicto distinto están previstos | Renovación económica, reintentos financieros o prueba de aislamiento MySQL real en esta tarea |
| `backend/src/API/Controllers/SuscripcionesSaaSController.cs` | `cab95db5a33c6a15448e2437481eaaac25ffc4fe` | Endpoints SaaS con `[Authorize]` y `[RequierePermiso]` para lectura/onboarding/entitlements; la ruta `empresaId` selecciona, no concede autoridad | Endpoint aprobado para cancelar, renovar, pausar por mora o emitir cargos |
| `backend/src/Application/DTOs/SuscripcionesSaaSApiContracts.cs` | `6045a167fb90fe3dea2fa87280d0a29cec57c6be` | DTO actual de onboarding `PlanCodigo/InicioUtc`, estados y errores `SAAS_*` | DTO de facturación, precio, impuesto, prorrateo, ciclo de pagos o pasarela |

Esta M01.01 no altera Domain, Application, Infrastructure, API ni Angular. La lectura de un método existente no equivale a evidencia A1–A3 de ejecución o autorización productiva.

## 3. Dentro y fuera del alcance futuro

**Diseño futuro sujeto a revisión:** estados legales y técnicos inequívocos para alta, renovación, suspensión/pausa, cancelación, morosidad y cierre; autoridad del actor que solicita cada transición; fechas UTC, vigencia y motivo; versionado de contrato/plan y registro de decisión; trazabilidad idempotente, auditoría segura y restauración no destructiva; separación entre elegibilidad comercial y RBAC de recurso. Toda tasa, periodo, moneda, vencimiento y tratamiento de mora deberá ser aprobado expresamente en su microtarea comercial.

**Fuera del alcance de esta entrega:** altas o modificaciones de suscripción en MySQL; cargos, impuestos, reembolsos, prorrateo, pasarela de pagos, webhooks o workers; migraciones EF, endpoints y controladores nuevos; nuevas roles, JWT o permisos; alteración de `EmpresaId` o membresías; reactivación de servicios externos; automatizaciones, cambios de QA/main/PROD, DNS, secrets o datos productivos. No se crea ningún evento ejecutable ni dispatcher.

## 4. Invariantes de no-regresión para etapas posteriores

| ID | Invariante exigible | Fallo que obliga NO_GO |
| --- | --- | --- |
| NR01 | El contrato pertenece a un tenant verificado server-side, no al `empresaId` recibido sin validar | Usuario A administra o lee contrato B |
| NR02 | Entitlement comercial y autorización RBAC siguen siendo barreras separadas | Un plan habilita por sí solo permisos privilegiados |
| NR03 | Toda transición válida conserva estado anterior, nuevo, actor, motivo y instante UTC | Pérdida de historia o transición sin auditoría |
| NR04 | Cancelada/Expirada no se reactivan por un replay de alta o cambio de plan | Resurrección silenciosa de contrato cerrado |
| NR05 | Pausa y reactivación requieren preestado permitido y permiso explícito | Cambios desde estados incompatibles o usuario revocado |
| NR06 | Una renovación futura no altera vigencia o precio de periodos cerrados | Reprecio retroactivo o inconsistencia contable |
| NR07 | La mora no se infiere de un timeout sin ledger o fuente de cobro aprobada | Suspensión arbitraria por error transitorio de red |
| NR08 | Una misma clave idempotente y payload equivale a un único efecto | Doble suscripción, doble cargo o replay con payload distinto |
| NR09 | Ante servicio externo caído la decisión falla cerrada sin conceder accesos ni cargos | Continuación privilegiada por callback tardío o duplicado |
| NR10 | La reversión futura conserva relaciones y datos legítimos, sin `DROP`/borrado irreversible | Destrucción de historial de suscripciones o pagos |

## 5. Oráculos de aceptación FUTUROS, no ejecutados

**P01–P05 (positivos):** alta autorizada de A con plan vigente; consulta de su estado y vigencia UTC; pausa de Activa a Suspendida con motivo; reactivación de Suspendida a Activa con control backend; cancelación idempotente que preserva fechas e historia.

**N01–N10 (negativos):** ausencia de JWT; RBAC insuficiente; tenant B en ruta con usuario A; membresía revocada; cancelar con fecha anterior al inicio; reactivar una Cancelada o Expirada; doble alta concurrente o replay con payload distinto; renovar con versión/precio no aprobado; marcar mora por timeout/duplicado del proveedor; perder historia al revertir la transición. Cada negativo debe fallar de forma verificable cuando la etapa de código real implemente el contrato.

Los escenarios son **expectativas especificadas**, nunca pruebas .NET, HTTP, SQL o Playwright declaradas como ejecutadas por este Markdown. La A1–A3 de esta microtarea corresponde a revisión documental de cobertura, coherencia y límites de seguridad, no a runtime.

## 6. Puertas de salida, impactos y rollback

La presente entrega impacta solo un nuevo `docs/v5/*.md` y un bloque autocontenido de `CHANGELOG_AI.md`. Sucesoras/consumidores: futuras M01.02–.06, M02 del mismo bloque y módulos de contratos, billing, tenant y seguridad; ningún dato o API se modifica ahora. Riesgo residual: transiciones financieras no modeladas ni autorizadas; no cerrar M01.06/M02 por esta preparación.

**A0:** alcance y código de referencia leídos. **A1:** objetivos y cinco positivos especificados. **A2:** diez negativos, fronteras tenant/JWT/RBAC y no permisividad definidos. **A3:** consistencia documental con código existente y Plan V5. **A4:** comprobar commit/diff/Scope Lock/CHANGELOG en el SHA exacto publicado y readback PLAN/COLA/BITACORA; si el check requerido está queued, failed, cancelled o skipped, no marcar PASS. El check general backend no sustituye ni bloquea un diff Markdown irrelevante y se etiqueta `NO_APLICA` con motivo.

Tras cierre legítimo de A0–A4, queda `A5_PENDIENTE` para otra Task Javier, nunca autorrevisión. **Rollback documental:** `git revert` normal del commit sin force-push, preservando BITACORA histórica. No se concede acceso a producción, QA, main, migraciones ni facturación.
