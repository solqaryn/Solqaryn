# SOLQARYN V5 — C11-03.M01.04: riesgos, supuestos y decisiones pendientes para Outbox y eventos

**Responsable:** PRIMARY DEV 02 JAVIER, Task `6af16bb9492c8191b8e8c7d606f11507`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO fila 1661; `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`; 2026-11-14.

## Alcance y clasificación

C11-03.M01.04 registra riesgos **potenciales**, supuestos **no verificados** y decisiones **pendientes** para el futuro Outbox/relay de plataforma. Su salida es una matriz de análisis documental y no constituye detección de defectos, emisión de eventos, aprobación de arquitectura o ejecución de pruebas.

**Preflight institucional de claim:** PLAN_MAESTRO H1661=PENDIENTE, I1661 vacío; COLA G/O1661=SI, DAG_TECNICO A1622=C11-03.M01.04, F1622=VALIDADO y G1622=SI. La independencia de M01 preliminar no libera M01.06/M06.06, ni IAM/JWT/RBAC/tenant, esquema, workers, API, Cloudflare/Hostinger, QA/main/PROD o revisión A5 propia.

## Evidencia delimitada a fuentes versionadas

Los antecedentes del mismo bloque son `docs/v5/c11-03-m01-01-alcance-no-regresion-outbox-eventos.md` (blob `a058779633c30a155ac8df5dacb8159e979876df`), `docs/v5/c11-03-m01-02-actores-interfaces-fronteras-outbox.md` (blob `f66bb8296257ed72bb7a69b67704f99b9f0360b1`) y `docs/v5/c11-03-m01-03-inventario-fuentes-evidencia-outbox.md` (blob `efdd66fcfcbd0cc93950dcee832ee7f9beb26117`). Se inspeccionaron las fuentes reales `MensajeOutbox.cs` (`891572626c19cd6cf8f522fca5ec876333286ffb`), `MensajeOutboxService.cs` (`a00d353c474d7d7f785a58f443c66c5f12a542f7`), `IMensajeOutboxRepository.cs` (`a7dc258df9b1123dfe54ea85ba6d4dcc2a1eb478`), `MensajeOutboxRepository.cs` (`056d5aa380b53ed697ddcb6852df55394e6544a7`), `OutboxRetryProcessor.cs` (`542c0a655a3ee0698e3d80f1854d43903b6470fa`) y fuentes xUnit de N72D/N74D/P2012D, todas por blobs Git. Esta lectura **no** implica que un test se haya ejecutado ni que un relay exista en producción.

## R01–R16: matriz de riesgos potenciales, NO FALLOS CONFIRMADOS

| Riesgo | Escenario a evitar | Mitigación futura y evidencia exigible |
| --- | --- | --- |
| R01 | Mensaje B incluido en claim de A | Verificar lote completo antes del dispatch y test A/B |
| R02 | EmpresaId impuesto por cliente | Scope server-side y autorización de objeto real |
| R03 | Doble claim entre workers | CAS por tenant, mensaje e intento; test concurrencia MySQL |
| R04 | Crash después del efecto y antes del ack | Consumidor idempotente y replay controlado |
| R05 | Misma clave para payload distinto | Hash/fingerprint y conflicto sin efecto lateral |
| R06 | Outbox confirmado sin mutación de negocio | Unidad de trabajo atómica y test rollback |
| R07 | Mutación confirmada sin intención durable | Persistencia del mensaje en la misma transacción |
| R08 | Reintentos ilimitados congestionan proveedor | MaxAttempts, backoff, jitter y dead-letter |
| R09 | Stale claim retomado antes de vencimiento | Lease/TTL con clocks UTC y filtro de versión |
| R10 | Eventos fuera de orden rompen saldo/inventario | Contrato de orden por agregado y pruebas dirigidas |
| R11 | Payload con PII o JWT en auditoría | Allowlist y redacción segura de logs |
| R12 | CorrelationId malicioso usado como identidad | Sanitización y rechazo como mecanismo de auth |
| R13 | Reintento con soporte read-only o rol revocado | Capacidad vigente verificada server-side |
| R14 | Contrato de proveedor falla y se declara PASS | CI/proveedor real causal, cero success ficticio |
| R15 | Nuevos jobs arrancan por prompt de ChatGPT | Prohibir dispatcher entre Tasks y activación implícita |
| R16 | Cambios a SQL/cola afectan PROD por accidente | Gate explícito de permisos, ambiente efímero y rollback |

Son riesgos a modelar para futuras fases, no hallazgos de seguridad confirmados contra el entorno vivo.

## S01–S10: supuestos NO_VERIFICADOS

| Supuesto | Comprobación pendiente |
| --- | --- |
| S01 | Existencia de UoW que cubra mutación y outbox en cada consumidor |
| S02 | Compatibilidad real de transacciones EF/MySQL con el proveedor actual |
| S03 | Garantía de índice único tenant + clave idempotente en esquema desplegado |
| S04 | Estrategia de orden causal por agregado y competencia entre eventos |
| S05 | Capacidad de recuperación de reclamos stale sin doble efecto |
| S06 | Cuotas/timeout y contrato de autenticación del eventual proveedor externo |
| S07 | Política de retención y eliminación segura de payload/outbox |
| S08 | Métricas, tracing y auditoría sin exposición de PII |
| S09 | Compatibilidad de consumers Angular/API presentes y futuros |
| S10 | Existencia de pruebas de integración MySQL efímero exact-SHA realmente aprobadas |

No tratar un supuesto como hecho sin evidencia autenticada posterior.

## D01–D10: decisiones PENDIENTES, no autorizaciones

| Decisión | Opciones y gate objetivo |
| --- | --- |
| D01 | Seleccionar adaptador de dispatch sólo tras permiso y amenaza/ROI |
| D02 | Política de mensajes: versión, retención y payload mínimo |
| D03 | Semántica entrega at-least-once + idempotencia por consumidor |
| D04 | Política de orden por agregado frente a throughput |
| D05 | Contrato de retries/stale claim/dead-letter por categoría |
| D06 | Propiedad transaccional y persistencia durable en EF/MySQL |
| D07 | Auditoría mínima, redacción y métricas observables |
| D08 | Migración reversible, esquema y compatibilidad sin pérdida |
| D09 | Separar bus de negocio de scheduler de Tasks ChatGPT |
| D10 | Gate para deploy futuro, controles CI y A5 independiente |

Toda decisión requiere microtarea posterior explícitamente autorizada. Esta fase no contrata ni configura servicios de pago o redes.

## Criterios futuros: P/N NO_EJECUTADOS

**P01–P08:** P01 outbox en transacción; P02 idempotencia repetida; P03 claim exclusivo A; P04 claim separado B; P05 retry acotado; P06 dead-letter; P07 log seguro; P08 rollback atómico.

**N01–N12:** N01 lote A/B contaminado; N02 EmpresaId forjado; N03 doble cobro tras crash; N04 misma key payload diferente; N05 claim duplicado; N06 stale sin plazo; N07 JWT en log; N08 soporte read-only escribe; N09 rol revocado autoriza; N10 proveedor caído pero PASS ficticio; N11 CI skipped/in_progress contada success; N12 Task ajena ejecuta un dispatcher. Ninguno se probó en este análisis.

## Impacto y cierre A0–A5

**Impacto inmediato:** sólo `docs/v5/` y `CHANGELOG_AI.md`; no código .NET 10/Angular 22.2.1, SQL/MySQL, JWT/RBAC, infraestructura, proveedor, cuenta ni scheduled Task modificada.

**A0:** Skill/canónica/HEAD/PLAN-COLA-DAG y originalidad de autor. **A1:** matriz R01–R16/S01–S10/D01–D10, riesgos no confirmados. **A2:** JWT/RBAC, tenant A/B, soporte read-only, secretos y PII. **A3:** cotejo documental de Domain/Application/Infrastructure/API/test; runtime NO_APLICA por Markdown. **A4:** publicar por fast-forward CAS exact HEAD, diff sólo documentos y verificaciones GitHub `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` del SHA exacto, readback institucional. **A5:** independiente de la autora y posterior a LISTO DEV.

**Rollback:** `git revert` ordinario después de releer HEAD y evitar sobrescrituras, sin modificar BITACORA histórica, force-push, QA/main/PROD ni activar eventos.
