# SOLQARYN V5 — C04-01.M02.01: contrato de entidades y ownership del catálogo de planes

**Autor:** PRIMARY DEV 02 JAVIER (TASK_ID `6af16bb9492c8191b8e8c7d606f11507`). **Fecha:** 2026-11-15. **Ámbito:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fuentes:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO, AGENTS.md, skill canónica y código actual.

**Alcance EXCLUSIVAMENTE DOCUMENTAL:** modelo de entidades, relaciones e invariantes de catálogo SaaS; no se implementan ni modifican clases, API, migraciones, planes comerciales, impuestos, precios, cobros, permisos, flags, datos reales, eventos, webhooks ni workers. **FUTUROS_NO_EJECUTADOS** identifica los oráculos que no son tests de runtime.

## 1. Entrada, dependencias y fuentes reales

- Preflight de `C04-01.M02.01`: PLAN H584=PENDIENTE, I584 vacío; COLA G584/O584=SI; DAG F=VALIDADO/G=SI. Tras el claim, PLAN H584=EN_PROGRESO e I584=TASK_ID propio con readback; no se presupone CAS atómico sobre H/I si hay carreras.
- Dependencia DAG: `C04-01.M01.01–06+A5`, seis entregas LISTO con A5 independiente. Terminal M01.06 en SHA `960443f9eabe54645059597c34e3c6ea4eeb0064`, documento blob `54ac2a2a5f3855837039d1e5e80f0ce15faf1379`.
- Código Domain: `backend/src/Domain/Entities/SuscripcionesSaaS.cs`, blob `d9b5dbe19fb728293b00b748f10ac10336debd2d`, contiene `Plan`, `PlanLimite`, `Suscripcion` y `EstadoSuscripcion`. Su presencia NO demuestra precios aprobados, revisión comercial inmutable, cobro o prorrateo implementado.
- Stack: ASP.NET Core net10.0, Domain → Application → Infrastructure, MySQL 8.4 y EF Core 10.0.12 con proveedor Oracle MySql.EntityFrameworkCore 10.0.9; Angular 22.2.1 solo experiencia. Los cambios de datos requieren una microtarea futura autorizada y pruebas efímeras.
- `C04-01.M02.02–05` son diseños independientes y `M02.06` es un terminal dependiente: ninguno recibe autorización o A5 de este documento.

## 2. Doce objetos lógicos — no son tablas existentes

| ID | Concepto y fuente de autoridad futura | Restricción principal |
| --- | --- | --- |
| E01 | PlanCatalogEntry: código, nombre, estado y versión | Catálogo global no crea grant RBAC |
| E02 | PlanRevision: plan, revisión, vigencia UTC | Impedir cambio retroactivo de tarifa |
| E03 | CapabilityDefinition: módulo y capacidad tipada | Capacidad restringe; nunca concede rol |
| E04 | PlanLimitDefinition: clave, unidad y máximo | Mayor que cero o ilimitado explícito |
| E05 | BillingPeriodPolicy: período y anclaje UTC | Periodicidad requiere aprobación |
| E06 | PriceProposal: moneda, importe decimal, decisión | No hay precio autorizado por diseño |
| E07 | TaxPolicyProposal: vigencia y criterio de jurisdicción | No calcular impuesto sin autoridad |
| E08 | TenantPlanAssignment: tenant verificado + revisión | Empresa proviene del backend |
| E09 | SubscriptionSnapshot: plan, estado, fechas y versión | Mantener historia de Suscripcion |
| E10 | PlanChangeRequest: plan anterior/nuevo y expectedVersion | Exigir grant, confirmación e idempotencia |
| E11 | CapabilityUsageSnapshot: tenant, capacidad, valor y origen | Consumo no sustituye permiso |
| E12 | PlanDecisionAudit: actor opaco, acción, timestamp y decisión | Append-only; sin JWT/PII/pago |

**Lo realmente existente:** `Plan.Codigo`, `Plan.Nombre`, `Plan.Activo`, `Plan.DefinirLimite` normalizan y validan catálogo; `PlanLimite.ValorMaximo` admite `null` como ilimitado y rechaza cero/negativos; `Suscripcion.EmpresaId`, `PlanId`, estado y período UTC expresan asignación empresarial. E02 y E05–E12 son conceptos FUTUROS, no clases/DB declaradas.

## 3. Relaciones y doce invariantes

- `PlanCatalogEntry → PlanRevision → CapabilityDefinition/PlanLimitDefinition` es contrato futuro; ningún nuevo FK ni DDL.
- `TenantPlanAssignment → SubscriptionSnapshot → PlanRevision` se resuelve únicamente tras JWT firmado, membresía, permiso y empresa servidor, sin confiar en `EmpresaId` del cliente.
- `PlanChangeRequest → PlanDecisionAudit` requiere transición transaccional, versión esperada y rechazo de replays; no hay cobro automático.

1. El plan y entitlement SaaS limitan capacidades, **nunca elevan** RBAC o autorización ABAC.
2. Token, query, ruta, `X-Empresa-Id` y Angular no crean empresa autorizada.
3. Revisión/versionado temporal no repricia una suscripción histórica sin aprobación.
4. Código y clave de límite siguen normalización de Domain, sin colisiones silenciosas.
5. Valores de límite cero/negativos son inválidos; `null` requiere semántica explícita.
6. La empresa de una suscripción pertenece al tenant autorizado de la operación.
7. Un cambio de plan verifica estado vigente, revisión, grants, consentimiento y versión.
8. Precios, moneda, impuestos, prorrateo y periodicidad quedan sin decisión comercial: NO_GO económico.
9. Idempotencia futura por tenant/actor/acción/versión/payload; llave igual, payload diferente = conflicto.
10. Cancelación/revocación no se revive por cache, evento ni petición duplicada.
11. DB, auditoría o servicio caído produce denegación y cero estado parcial.
12. Soporte, catálogo público, export y medios no exponen datos de otro tenant, JWT, PII, secretos o pagos.

## 4. Doce pares de aceptación FUTUROS_NO_EJECUTADOS

| Par | Futuro positivo esperado | Negativo obligatorio |
| --- | --- | --- |
| P01/N01 | A con membresía y grant ve solo plan/suscripción A | JWT A consulta suscripción B por ID (BOLA) |
| P02/N02 | Código de plan válido se normaliza | Código vacío/colisión se acepta |
| P03/N03 | Límite válido mayor que cero o ilimitado aprobado | Cero/negativo concede capacidad |
| P04/N04 | Revisión vigente conserva el estado histórico | Revisión antigua repricia datos pasados |
| P05/N05 | Grant y capacidad A permiten acción específica | Plan premium sin RolPermiso concede escritura |
| P06/N06 | Cancelar preserva historia y auditoría | Replay reactiva cancelada |
| P07/N07 | Upgrade con permiso A y versión vigente es consistente | A modifica suscripción B |
| P08/N08 | Mismo key/digest produce un resultado lógico | Key duplicada/payload distinto duplica alta |
| P09/N09 | Transacción fallida revierte mutación | Timeout deja asignación parcial |
| P10/N10 | Importe futuro solo usa política comercial aprobada | Cliente selecciona precio/tributo arbitrario |
| P11/N11 | Soporte GET allowlisted se audita y caduca | Soporte realiza PUT o lectura de B |
| P12/N12 | CI requerida completada success para SHA exacto es acreditable | cancelled/skipped/in_progress se trata como PASS |

Estos escenarios **no se ejecutaron** contra HTTP, JWT real, MySQL, Playwright ni servicios de cobro. M03 debe introducir código auténtico y pruebas xUnit, HTTP de tenant A/B, control de versiones, concurrencia, integración MySQL efímera y rollback según su alcance; no dar por probados permisos por el diseño.

## 5. A0–A5, exclusiones y rollback

- **A0:** diff restringido a este Markdown y bloque autocontenido de CHANGELOG_AI.md; sin claves ni cambios ejecutables.
- **A1:** verificar doce objetos, doce invariantes y doce pares P/N documentados, siempre FUTUROS_NO_EJECUTADOS.
- **A2:** revisar JWT/RBAC/tenant A/B, propiedad de suscripción, control de límites, economía no autorizada y datos sensibles.
- **A3:** contrastar M01.01–06 LISTO+A5, `SuscripcionesSaaS.cs` con blob comprobado y fronteras Domain/Application/API; .NET/Angular/MySQL/E2E = NO_APLICA al diff exclusivamente documental.
- **A4:** fast-forward serializado con CAS sobre HEAD de dev, comprobar archivo y CHANGELOG, Scope Lock y V5 `completed/success` exact SHA; readback PLAN/COLA/DAG/BITACORA.
- **A5:** pendiente de revisión independiente por una de las otras cinco Tasks Javier, distinta de autor.
- **Rollback:** `git revert` normal, sin force-push ni borrar BITACORA histórica; no hay datos/planes/cobros que restaurar por esta entrega.
- **NO GO permanente por este documento:** QA, main, PROD, DNS, Cloudflare, Hostinger, migraciones reales, pasarela, tarifas inventadas, compras y secretos.
