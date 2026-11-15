# SOLQARYN V17 — C08-03.M02.03 — Persistencia conceptual, índices, migraciones y datos de prueba de membresías de gimnasio

**Autor:** PRIMARY DEV 01 — JAVIER; TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 2026-11-15. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, `C08-03.M02.03`, PLAN_MAESTRO fila 1234. **Riesgo R0:** documento sin migración, tablas ni runtime. **Entorno:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`.

## 1. Objetivo y no-alcance

Diseñar proyecciones de persistencia, índices candidatos, plan de migración y rollback futuros y fixtures de prueba para membresías comerciales y accesos de gimnasio. **Nada de esta propuesta es esquema existente confirmado ni migración ejecutada.** No escribir SQL, generar migraciones EF, modificar `DbContext`, crear entidades .NET, abrir API, cobrar, emitir QR, conectar lectores, modificar usuarios/roles JWT/RBAC, activar trabajadores o desplegar.

Las predecesoras M01.01–06 están LISTO+A5 independiente. M02.01 documentó veinte entidades conceptuales E01–E20 y M02.02 definió ocho operaciones, ocho DTO, 12 errores y 16 validaciones, ambas sin runtime. Su A5 es trabajo separado; esta M02.03 fue habilitada individualmente por DAG/PLAN/COLA y no implica desbloqueo de código M03 ni terminal M02.06. D01–D08 de M01.04 siguen como decisiones de negocio PENDIENTES. La membresía comercial del gimnasio no reemplaza `UsuarioEmpresa` ni `SuscripcionesSaaS`, y `Cliente.EmpresaId` opcional jamás identifica automáticamente propietario.

## 2. Proyecciones conceptuales de persistencia P01–P16 — NO CREADAS

Cada propuesta usa `TenantId` validado en servidor, `SucursalId` bajo el mismo tenant donde corresponda, claves opacas, versión para concurrencia, `CreatedAtUtc` y auditoría proporcional. No se definen nombres reales de tablas, migrations o convenciones EF definitivas.

| ID | Proyección futura | Referencias seguras y restricción principal |
| --- | --- | --- |
| P01 | `GymBranch` | FK tenant+sede; sede B no visible para A |
| P02 | `GymArea` | tenant+sede+área; check-in sólo recurso permitido |
| P03 | `GymMemberProfile` | titular opaco, referencia verificada a persona, PII minimizada |
| P04 | `GymCommercialPlan` | plan comercial versionado por tenant; no SaaS subscription |
| P05 | `GymPlanRevision` | revisión inmutable de términos, sin overwrite de historia |
| P06 | `GymMembership` | FK compuesta tenant+miembro+plan; estado, versión y período |
| P07 | `GymMembershipValidity` | intervalos UTC [inicio,fin), IANA, validación de DST |
| P08 | `GymSuspension` | razón y fecha de efecto, sin borrar historia de revocación |
| P09 | `GymRenewalRequest` | PENDIENTE hasta prueba comercial/ERP auténtica |
| P10 | `GymBranchEntitlement` | intersección plan+sede+área+vigencia, fail-closed |
| P11 | `GymAccessCredential` | alias hash/versión/TTL, jamás QR secreto plano |
| P12 | `GymAccessEvaluation` | decisión DENY por defecto, motivo saneado, UTC |
| P13 | `GymAccessAudit` | actor seudónimo, tenant, sede y correlación; PII no |
| P14 | `GymAccessPolicy` | configuración versionada, zona/horario y estado |
| P15 | `GymIdempotencyLedger` | key hash+payload fingerprint+tenant+operación, TTL, único efecto |
| P16 | `GymCommercialReconciliation` | referencia al ERP sin duplicar factura ni pago |

La intersección `TenantId/BranchId/MemberId/PlanRevisionId` se valida en servidor y debe resistir referencias manipuladas. La reconciliación P16 referencia el ERP como fuente económica autoritativa sin mutarlo desde un documento. Un `QR` es referencia efímera sin privilegios del SaaS; acceso físico sólo tras autorización y diseño de hardware separados.

## 3. Restricciones e índices candidatos I01–I12 — SIN DDL

| ID | Índice / constraint candidato | Invariante futura y negativa imprescindible |
| --- | --- | --- |
| I01 | `(TenantId, BranchId)` por entidad de sede | FK compuesta evita sede B bajo A |
| I02 | `(TenantId, MemberRef)` lookup acotado | referencia B nunca visible desde A |
| I03 | `(TenantId, PlanId, Revision)` único | planes SaaS no se reutilizan |
| I04 | `(TenantId, MembershipId, RowVersion)` CAS | versión obsoleta no pisa suspensión |
| I05 | `(TenantId, MembershipId, StartUtc, EndUtc)` | vigencia con inicio menor que fin; fin exclusivo |
| I06 | `(TenantId, MemberRef, MembershipStatus)` filtrado | consulta paginada sólo autorizada |
| I07 | `(TenantId, MembershipId, SuspensionStartUtc)` | suspensión efectiva no se elude con caché |
| I08 | `(TenantId, MembershipId, BranchId, AreaId)` FK | área A no se acepta desde B |
| I09 | `(TenantId, CredentialHash, CredentialVersion)` único | token revocado/reutilizado no revive |
| I10 | `(TenantId, Operation, IdempotencyKeyHash)` único | retry idéntico converge; fingerprint distinto 409 |
| I11 | `(TenantId, EvaluationId, OccurredAtUtc)` | auditoría mínima, sin secretos/PII |
| I12 | `(TenantId, ERPReference, OperationType)` único | factura/renovación no se duplica |

No asumir que un índice temporal por sí solo impide superposición de rangos: requieren reglas transaccionales y tests de carrera reales. No imponer cascadas destructivas en membresías, facturas ni eventos de acceso; preferir soft-state, versionado, retención autorizada y reconciliación. Los índices solo se materializan después de revisión de dominio, cardinalidad, provider Oracle, plan de consultas y CI MySQL 8.4.

## 4. Plan FUTURO de migración reversible MG01–MG08 — NO EJECUTADO

| Etapa | Gate y entrega futura | Rollback o denegación segura |
| --- | --- | --- |
| MG01 | Resolver D01–D08, alcance comercial, retención y tenant autoritativo | No crear esquema sin decisiones |
| MG02 | Diseño EF Core 10/Oracle MySQL 8.4 revisado en rama dev y backup de prueba | Validar migración reversible en DB efímera |
| MG03 | Crear objetos aditivos, FKs compuestas y check constraints soportados | Retirar tablas nuevas no usadas sin pérdida |
| MG04 | Ensayar backfill desde registros verificados, no inferir ownership histórico nullable | Cuarentena a huérfanos, nunca asignar tenant arbitrario |
| MG05 | Ensayar UTC/IANA/DST, suspensión y no solapamiento | Error => negar acceso, restaurar snapshot efímero |
| MG06 | Ensayar idempotencia y operación concurrente transaccional | Dos escrituras => exactamente un efecto; compensar |
| MG07 | Separar estado de pagos ERP de derecho de acceso; reconcilio firmado futuro | Pago no acreditado => renovación PENDIENTE |
| MG08 | Test de rollback, integridad, auditoría y regresión antes de habilitar flag | Rollback probado, sin cambiar QA/main/PROD automáticamente |

La futura integración deberá ejecutar realmente `Category=Integration` con MySQL 8.4 **efímero** en GitHub Actions y guardar filtros/logs; un build verde o mocks no prueban MySQL ni transacciones. No usar Aiven real, datos productivos, dispositivos, PSP ni secretos reales en los ensayos de diseño.

## 5. Fixtures futuros positivos/negativos F01–F12 — NO EJECUTADOS

| ID | Fixture positivo futuro | Fixture negativo / frontera futura |
| --- | --- | --- |
| F01 | Empresa A + sucursal A con FK coherentes | sede B no enlazable por A |
| F02 | Miembro A con plan comercial de A | `SuscripcionesSaaS` no concede derecho de gimnasio |
| F03 | Inicio UTC válido antes de fin | fin igual a inicio o DST ambiguo: rechazo |
| F04 | Instante antes de expiración permitido | instante igual a `EndUtc`: acceso denegado |
| F05 | Suspensión confirmada invalida credential | caché/QR anterior no reautoriza |
| F06 | Rol activo consulta registro propio | rol revocado/tenant B: denegación sin PII |
| F07 | Reintento idéntico de renovación converge | misma key con payload modificado: conflicto |
| F08 | Dos check-in paralelos idénticos: único efecto | concurrentes distintos no superan cupo |
| F09 | ERP verifica operación comercial única | callback falso o pago PENDIENTE no activa |
| F10 | Credencial válida de corto TTL autoriza evaluación futura | QR copiado, expirado o sin firma: DENY |
| F11 | Evento auditable preserva correlación mínima | JWT, QR durable, PAN, biometría, PII: no log |
| F12 | Rollback de esquema de prueba restaura conteos | fallo parcial no deja FK huérfana ni saldo falso |

Cada fixture requerirá Arrange/Act/Assert e instrumentación causal en su fase implementable. **Resultado aquí: 0 tests DB, 0 migraciones, 0 llamadas HTTP, 0 hardware, 0 pagos.** Son datos y oráculos planeados.

## 6. A0–A5, impacto, seguridad y rollback

**A0:** contraste con plan, DAG/PLAN/COLA, modelos M01/M02, diff de este Markdown y `CHANGELOG_AI.md`, sin cambios a Domain/Application/Infrastructure/API/UI ni DB. **A1:** verificación de consistencia estructural de P01–P16, I01–I12, MG01–MG08 y F01–F12; se diseñan resultados negativos, no se ejecutan tests funcionales. **A2:** diseño tenant A/B, JWT/RBAC no alterado, suspensión, QR, idempotencia, privacidad/PII y UTC/DST. **A3:** MySQL 8.4/EF/HTTP/Angular/Playwright/proveedores = `NO_APLICA_R0` porque no hay código runtime. **A4:** commit único fast-forward/CAS dev, verificaciones Scope Lock/V5 exact SHA `completed/success`, PLAN/BITACORA readback y `CHANGELOG` autocontenido; los checks requeridos queued/failed/cancelled nunca son PASS. **A5:** revisión por una Task Javier distinta del autor; hasta entonces A5_PENDIENTE y ninguna sucesora técnica desbloqueada por inferencia.

**Reversión:** `git revert` normal del commit documentado, conservar BITACORA y recalcular DAG. No forzar CI, modificar CONFIG/Tasks, liberar QA/main/PROD ni inferir una migración de un documento. El contrato se revisará antes de cualquier implementación EF, dato personal, pago o control de acceso.
