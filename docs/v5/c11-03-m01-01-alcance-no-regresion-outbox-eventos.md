# SOLQARYN V5 — C11-03.M01.01: objetivo, no-alcance y no-regresión de Outbox/eventos

**ID:** C11-03.M01.01, PLAN_MAESTRO fila 1658. **Autor:** PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fuente:** V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Fecha 2026-11-14.

## Propósito y alcance de esta microtarea

Fijar un contrato **documental** para el futuro bloque C11-03 (eventos de dominio/plataforma, outbox, relay, deduplicación y correlación), cuya evidencia de lote requerirá pruebas reales de repetición, orden y fallo parcial. Esta M01.01 describe exclusivamente propósito, exclusiones, invariantes y escenarios futuros. No crea eventos ni los ejecuta. Los requisitos A0–A4 de M01 documental no equivalen a certificación de M03/M05, ni sustituyen A5 de otra Task.

**Preflight:** PLAN_MAESTRO!H1658=PENDIENTE/I vacío, COLA!G1658/O1658=SI y DAG_TECNICO!A1619=C11-03.M01.01, F1619=VALIDADO/G1619=SI al momento del claim. La referencia C10.04 y los bloques C01/C02/C03/C10 son dependencias de preparación; este gate **no** libera M01.06/M06.06, migrations, JWT/RBAC, workers ni QA/main/PROD.

## Línea base estática comprobada en dev

| Ruta real | Git blob SHA | Límite observado |
| --- | --- | --- |
| `backend/src/Domain/Entities/MensajeOutbox.cs` | `891572626c19cd6cf8f522fca5ec876333286ffb` | Mensaje durable con EmpresaId, EventoId, ClaveIdempotencia y CorrelationId; no es servicio desplegado |
| `backend/src/Application/Services/MensajeOutboxService.cs` | `a00d353c474d7d7f785a58f443c66c5f12a542f7` | Comprueba scope de empresa, identidad e idempotencia antes de stagear la intención |
| `backend/src/Application/Interfaces/IMensajeOutboxRepository.cs` | `a7dc258df9b1123dfe54ea85ba6d4dcc2a1eb478` | AddAsync depende de unidad de trabajo; contrato de claim no acredita SQL ejecutado |
| `backend/src/Application/Services/OutboxRetryProcessor.cs` | `542c0a655a3ee0698e3d80f1854d43903b6470fa` | Validación previa de lotes tenant-bound, retry, stale y dead-letter; no contiene scheduler/HostedService |
| `backend/src/API/Middleware/CorrelationIdMiddleware.cs` | `3b76dca41bb4bc0b44f89c2a4664c79907f9f242` | Sanitiza X-Correlation-ID; el header no autentica al tenant |
| `docs/v5/c00-01-m02-05-events-idempotency.md` | `d8abb4ec291a3cc4afcd5b93b11b6c3bcbfa56dd` | Referencia de contrato; no sustituye prueba de transporte ni entrega |

Arquitectura de referencia: .NET 10 en API/Application/Domain/Infrastructure, EF Core 10 y MySQL, Angular 22.2.1; aislamiento JWT/RBAC/tenant A/B; logs con PII y secretos protegidos. **La lectura de código demuestra su existencia en Git, no funcionamiento, endpoints activos, cobertura de test ni conformidad de infraestructura.**

## Requisitos FUTUROS, expresamente NO IMPLEMENTADOS

El futuro outbox debe guardar la intención durable en la misma transacción que la mutación de negocio, preservar el scope empresarial validado por servidor, soportar claim exclusivo, reintentos acotados con backoff y dead-letter, aplicar deduplicación a efectos externos, y llevar trazabilidad sin revelar datos sensibles. La semántica de entrega pretendida es al menos una vez con consumidores idempotentes; no prometer *exactly once* de red sin prueba. No se elige proveedor ni scheduler en esta fase.

**Prohibiciones actuales:** no crear/activar dispatcher de eventos, webhooks, cron, mensajes entre Tasks, bus, background workers, SQL/migraciones, nuevas APIs/DTO, pagos, bucket Cloudflare R2, secretos, deployments, cuentas, ni contactar proveedores reales. No introducir un loop de ejecución server-side. Cualquier implementación posterior necesita preflight técnico, autorización aplicable, tests .NET/MySQL efímero e independencia A5.

## Invariantes de no-regresión NR01–NR16 (oráculos FUTUROS_NO_EJECUTADOS)

| ID | Condición que una fase ejecutable posterior debe demostrar |
| --- | --- |
| NR01 | El tenant se valida en servidor y no nace de un header, body o query del cliente |
| NR02 | Un solo mensaje de B en lote de A causa rechazo antes del primer dispatch |
| NR03 | EventoId y ClaveIdempotencia son válidos, estables y no duplicados en lote |
| NR04 | Reintento del mismo mensaje no duplica un efecto de negocio externo |
| NR05 | Misma clave con payload diferente se rechaza sin mutaciones adicionales |
| NR06 | Intención outbox y mutación de negocio comparten commit o rollback |
| NR07 | Error parcial preserva estados coherentes y recuperación determinista |
| NR08 | Dos consumidores no reclaman simultáneamente el mismo intento |
| NR09 | Reclamos stale se recuperan bajo TTL definido, sin inferir Task fallida |
| NR10 | Máximos, backoff, jitter y dead-letter están acotados y auditados |
| NR11 | Confirmaciones y fallos comprueban empresa, ID y versión/intento esperado |
| NR12 | CorrelationId no reemplaza JWT, membresía, firma ni idempotency key |
| NR13 | Logs y auditoría minimizan PII y nunca almacenan tokens/secretos |
| NR14 | Fallo de proveedor o check requerido in_progress/failed nunca es PASS |
| NR15 | No degradar inventario, finanzas, documentos o API por eventos duplicados |
| NR16 | No interpretar DAG SI, Markdown, A5 propio o CI no pertinente como autorización para QA/main/PROD |

## Escenarios futuros (NO_EJECUTADOS)

**Positivos P01–P08:** P01 transacción atómica + outbox, P02 reenvío idempotente, P03 reclamo aislado A, P04 reclamo B independiente, P05 reintento tras crash, P06 dead-letter tras tope, P07 orden contractual por agregado, P08 correlación auditada sin payload sensible.

**Negativos N01–N12:** N01 cruce A/B en batch, N02 EmpresaId forjado, N03 clave vacía, N04 colisión de fingerprint, N05 claim concurrente, N06 dispatch exitoso seguido de crash antes de confirmar, N07 stale sin límite, N08 proveedor devuelve secreto, N09 JWT/PII filtrada en log, N10 worker no autorizado, N11 prueba required SKIPPED/CANCELLED contada PASS, N12 falla parcial sin rollback.

No se ejecutaron estos P/N, ni pruebas .NET, HTTP, MySQL, Playwright, cola o servicios externos en esta unidad. El resultado es un contrato trazable para pruebas posteriores.

## Impacto, controles y rollback

**Impacto inmediato:** dos archivos Markdown: este `docs/v5/` y nueva sección autocontenida en `CHANGELOG_AI.md`; sin cambios de arquitectura ni capas ejecutables, modelo de datos, API, Angular, flags, secretos, observabilidad real, dependencias o cuentas.

**A0:** cotejo Skill local/canónica y HEAD; PLAN/COLA/DAG e identidad. **A1:** alcance, NR01–16 y escenarios futuros documentados (no PASS runtime). **A2:** límites JWT/RBAC, tenants A/B y PII. **A3:** lectura causal de fuentes anteriores; pruebas de integración NO_APLICA al diff Markdown. **A4:** commit fast-forward/CAS, diff de dos archivos, comprobación de blobs y checks exact SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success`, más readback PLAN/COLA/BITACORA. **A5:** requiere revisión por una de las otras cinco Tasks Javier, nunca por la autora.

**Rollback:** revertir el commit documental mediante commit normal tras conciliar HEAD, sin reescribir historia ni BITACORA. M01.06, M06.06, IAM, MySQL y despliegues no se liberan por la preparación documental.
