# SOLQARYN V5 — C04-01.M02.06: contrato terminal de dependencias y salida del catálogo de planes

**Autor:** PRIMARY DEV 02 JAVIER `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-15. **Ámbito:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. Fuentes exclusivas: STANDARD V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA, skill canónica, AGENTS.md.

**M02.06 es cierre documental de diseño, NO implementación M03:** no hay SQL, cambios de suscripciones, migración, cálculo monetario, tarifa aprobada, endpoint nuevo, webhooks, workers, nuevas Tasks, QA, main ni PROD. Todos los escenarios P/N están `FUTUROS_NO_EJECUTADOS`.

## 1. Puerta de entrada y cadena verificable

Se verificó `C04-01.M02.06` en PLAN H589=PENDIENTE, I589 sin owner, COLA G/O=SI, DAG F=VALIDADO/G=SI **después** de confirmar LISTO y A5 independiente de las cinco unidades M02.01–05, además de M01.01–06+A5. Tras la reclamación, H589=EN_PROGRESO e I589=TASK_ID propio con readback. Una lectura aislada de H/I no es transacción CAS multi-celda si existe otra Task escritora.

| Microtarea | Evidencia documental que se integra | SHA fuente |
| --- | --- | --- |
| `C04-01.M02.01` | doce objetos conceptuales, ownership, límite SaaS no igual a permiso | `3797f99e564f8f7fd52b5a3284744be5f8e75705` |
| `C04-01.M02.02` | operaciones API y DTO hipotéticos, compatibilidad, validaciones y errores | `bb223b465c35ae0c92ab39c646042d66e0c4abfa` |
| `C04-01.M02.03` | diez estructuras de persistencia, índices, migración reversible futura y fixtures | `f219bbf90561db00a67620c2a0e886d218acb24a` |
| `C04-01.M02.04` | configuración tipada y flags OFF, sin afectar entornos | `e555b42df5d0becf8277cae747d2fe26ded455cc` |
| `C04-01.M02.05` | ocho eventos hipotéticos, idempotencia y correlación, no emitidos | `ea9f99c02d7909936d14cac78d5d86328156d140` |

Se comprobó en PLAN_MAESTRO cada autor original, estado LISTO y A5 de TASK_ID independiente. El gate de este lote es dinámico, no una lista manual de autorizaciones. La M01.06 terminal original permanece en `960443f9eabe54645059597c34e3c6ea4eeb0064` con su auditoría independiente.

**Baseline de código real:** `backend/src/Domain/Entities/SuscripcionesSaaS.cs` blob `d9b5dbe19fb728293b00b748f10ac10336debd2d` contiene `Plan`, `PlanLimite`, `Suscripcion`, `EstadoSuscripcion`. Los documentos no crean `PlanRevision` física, tarifas, factura, ledger, endpoint ni algoritmo de prorrateo.

## 2. Dieciséis gates de salida y NO_GO explícitos

| Gate | Condición previa a implementación M03 | Bloquear si falta |
| --- | --- | --- |
| G01 | M01.01–06 y M02.01–05 LISTO con A5 independiente real | Antecedente en VALIDANDO, A5 propia o FAIL |
| G02 | Cada futura micro M03 con PLAN PENDIENTE libre y DAG F=VALIDADO/G=SI | No forzar COLA o fórmula |
| G03 | Identidad JWT firmada y membresía empresarial verificada por servidor | Selector de empresa/cliente crea autoridad |
| G04 | RolPermiso y ABAC por recurso/tenant, no por plan premium | Entitlement concede permisos administrativos |
| G05 | Revisiones de catálogo inmutables con períodos UTC | Reprecio o modificación retrospectiva de suscripciones |
| G06 | Límite normalizado positivo o `null` ilimitado bajo contrato aprobado | Cero, negativo o NULL ambiguo |
| G07 | Una empresa solo consulta su suscripción y uso | Cruce A/B, BOLA, IDOR y suscripción de tercero |
| G08 | Alta/cambio con versión esperada y bloqueo transaccional | Doble alta/cambio por carrera |
| G09 | Decisiones comerciales verificadas de precios, monedas e impuestos | Precio o tasa inventados por IA/cliente |
| G10 | Consentimiento, prorrateo, periodicidad y downgrade aprobados | Cargo, corte o eliminación sin autorización |
| G11 | Idempotencia por tenant/actor/acción/payload y replay comprobado | Key común A/B, payload conflictivo duplica efecto |
| G12 | EF y MySQL 8.4 efímero con FK, tests negativos, EXPLAIN y restore | SQL/ALTER en Aiven o índice sin rollback |
| G13 | API/DTO y cliente Angular con compatibilidad y migración aditiva | Cambios rompen SaaS actual o filtran datos de B |
| G14 | Eventos futuros sólo observan transacción backend confirmada | Webhook concede plan/grant, cobra o despierta Tasks |
| G15 | Git SHA exacto, Scope Lock y CI causal completed/success | SKIPPED/CANCELLED/IN_PROGRESS/FAIL atribuido PASS |
| G16 | Revisión A5 distinta del autor y autorización separada de ambiente | Autoaprobación o deploy QA/main/PROD por documento |

**Precio, moneda, tasas, plan comercial, pasarela y contratos monetarios aún requieren decisión del propietario.** Este lote solo delimita diseño. No crear billing ni conectarse a proveedor por deducir que existe un catálogo de planes.

## 3. Encadenamiento hacia M03/M04 sin liberación artificial

- **M03 Domain:** identificar invariantes existentes `Plan.DefinirLimite` y `Suscripcion`, incorporar solo funcionalidades contempladas por microtarea futura técnicamente habilitada y tests reales positivos/negativos.
- **M03 Application/API:** JWT/RBAC, owner y tenant A/B por solicitud, versiones, idempotencia y errores saneados en código autenticado; comparar DTO/endpoints actuales antes de agregar rutas.
- **M03 Infrastructure:** si un scope autorizado introduce EF/MySQL, ensayar integración con MySQL 8.4 efímero, constraints, reversibilidad y consumo de índices; nunca datos de Aiven o migración en producción.
- **UI Angular 22.2.1:** flags tipados futuros OFF hasta autorización, feedback accesible, datos derivados de backend. Un guard no reemplaza RBAC del servidor.
- **M04/M05/M06:** necesitan DAG propio, CI pertinente por SHA, readback, A5 externo y autorización de entorno diferenciada. Documentar M02 no promete ni activa QA/main/PROD.

## 4. Doce pares de aceptación FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro | Negativo que debe denegarse |
| --- | --- | --- |
| P01/N01 | Membresía/grant A permite ver propia suscripción A | JWT A accede ID de suscripción B |
| P02/N02 | Plan vigente y revisión correcta respeta historia A | Versión antigua repricia retrospectivamente |
| P03/N03 | Un límite positivo o ilimitado explícito se preserva | Cero/negativo/nulo ambiguo concede privilegio |
| P04/N04 | Entitlement limita acción previamente autorizada | Plan premium otorga RolPermiso ausente |
| P05/N05 | Cambio A con CAS correcto y grants se confirma una vez | Replay B/A o versión obsoleta duplica transición |
| P06/N06 | Suspensión/cancelación A respeta estado final | Evento retrasado reactiva cancelada |
| P07/N07 | Export propio filtra fila/campo por tenant | CSV/medio con datos B sale desde A |
| P08/N08 | Configuración desconocida aplica DENY y flags OFF | Flag cliente activa PSP, grant o escritura |
| P09/N09 | Error DB/EF revierte con auditoría consistente | Migración parcial o FK cross-tenant pasa |
| P10/N10 | Precio aprobado y política fiscal verificada anteceden cobro futuro | Cliente aporta importe/moneda no aprobados |
| P11/N11 | EventId reintentado con digest igual no genera doble cargo | Mensaje idéntico cobra o habilita doble |
| P12/N12 | CI causal finalizada con success y A5 independiente avalan cierre | Check cancelled o revisión de autor se cuenta PASS |

**No se ejecutaron** xUnit, SQL, HTTP, Playwright, cobros, prueba de aislamiento A/B ni migraciones por este documento. El code-first de M03 exige evidencia genuina, no nombres hipotéticos de clases ni casillas de PLAN. La revisión de M02.06 debe ser de otra Task Javier.

## 5. Auditoría A0–A5 y rollback

- **A0:** comprobar solo nuevo Markdown de contrato y sección CHANGELOG_AI.md autocontenida; comparar fuentes y no secretos.
- **A1:** verificar los cinco documentos M02.01–05, dieciséis gates y doce pares positivos/negativos escritos como FUTUROS_NO_EJECUTADOS.
- **A2:** revisar tenant A/B, JWT, RBAC, suscripciones, tarifas, seguridad monetaria, no migraciones/PII y DENY por defecto.
- **A3:** coherencia con plan y servicio existente, M01+A5 y M02+A5; .NET/Node/MySQL/HTTP/E2E = NO_APLICA al diff documental y no se declaran PASS.
- **A4:** publicación solo dev por fast-forward CAS contra HEAD exacto, diff y SHA/blob, Scope Lock y V5 `completed/success`, eventos idempotentes y readback institucional.
- **A5:** pendiente de una de las otras cinco Tasks Javier, distinta de autor, antes de sucesoras que requieran revisión de M02.06.
- **Rollback:** `git revert` normal, evento compensatorio aditivo, preservar historia BITACORA y autores. Sin datos, dinero, permisos, QA, main, PROD o infraestructura externa afectados por esta microtarea.
