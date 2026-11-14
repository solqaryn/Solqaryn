# SOLQARYN V5 — C11-03.M01.05: criterios de aceptación y evidencia futura de Outbox/eventos

**ID:** C11-03.M01.05, PLAN_MAESTRO fila 1662. **Autor:** PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Fecha 2026-11-14.

## Resultado y autoridad del documento

Definir criterios de aceptación **funcionales, técnicos, negativos y no funcionales observables** para el futuro lote C11-03 Outbox y eventos, cuya evidencia mínima de fase exige pruebas reales de repetición, orden y fallo parcial. Esta M01.05 es **preparación documental**: ningún criterio definido es una prueba ejecutada, un cambio de dominio/API/SQL ni permiso de activar un dispatcher. Al claim: PLAN H1662=PENDIENTE/I vacío, COLA G/O=SI y DAG_TECNICO!A1623=C11-03.M01.05, F=VALIDADO/G=SI. Se conserva la distinción entre gate de preparación y gate de M01.06/M06.06, A5 independiente y autorizaciones QA/main/PROD.

## Base de evidencia inspeccionada, no certificación

Antecedentes documentales versionados: `docs/v5/c11-03-m01-01-alcance-no-regresion-outbox-eventos.md` (blob `a058779633c30a155ac8df5dacb8159e979876df`), `docs/v5/c11-03-m01-02-actores-interfaces-fronteras-outbox.md` (blob `f66bb8296257ed72bb7a69b67704f99b9f0360b1`), `docs/v5/c11-03-m01-03-inventario-fuentes-evidencia-outbox.md` (blob `efdd66fcfcbd0cc93950dcee832ee7f9beb26117`), `docs/v5/c11-03-m01-04-riesgos-supuestos-decisiones-outbox.md` (blob `df2d177dc5cbe9f5f20902219e4424c2faf340eb`).

Fuentes de código reales: `MensajeOutbox.cs` (`891572626c19cd6cf8f522fca5ec876333286ffb`), `MensajeOutboxService.cs` (`a00d353c474d7d7f785a58f443c66c5f12a542f7`), `IMensajeOutboxRepository.cs` (`a7dc258df9b1123dfe54ea85ba6d4dcc2a1eb478`), `MensajeOutboxRepository.cs` (`056d5aa380b53ed697ddcb6852df55394e6544a7`), `OutboxRetryProcessor.cs` (`542c0a655a3ee0698e3d80f1854d43903b6470fa`), xUnit N72D/N74D/P2012D y `CorrelationIdMiddleware.cs` (`3b76dca41bb4bc0b44f89c2a4664c79907f9f242`). Los blobs indican código y pruebas existentes, **no** que el batch, SQL, workers o red se hayan ejecutado aquí.

## AC01–AC18: criterios de aceptación FUTUROS_NO_EJECUTADOS

| ID | Oráculo positivo y evidencia requerida en fase de código/integración |
| --- | --- |
| AC01 | Tenant A crea intención con EmpresaId resuelto en servidor y resultado correcto |
| AC02 | Tenant B mantiene intentos independientes aun con claves de negocio coincidentes |
| AC03 | Intención y operación de inventario/venta comparten commit transaccional |
| AC04 | Rollback de negocio retira también la intención de outbox no confirmada |
| AC05 | El mismo EventoId/idempotency key repetido produce un único efecto observable |
| AC06 | Payload distinto con misma clave retorna conflicto seguro, sin nueva mutación |
| AC07 | Claim concurrente de dos workers entrega a un solo ganador por intento |
| AC08 | Recuperación stale reprograma tras umbral UTC y conserva ownership correcto |
| AC09 | Primer fallo de proveedor reprograma con backoff y jitter acotados |
| AC10 | Máximo de intentos lleva a dead-letter sin loop infinito |
| AC11 | Fallo parcial de un lote no pierde eventos vecinos y permite replay seguro |
| AC12 | Lote con un evento de tenant B falla antes de enviar el primero de tenant A |
| AC13 | Confirmaciones/errores condicionan empresa, ID y intento/version esperado |
| AC14 | CorrelationId sanitizado permite trazabilidad sin tomarlo como autenticación |
| AC15 | Auditoría sólo registra campos permitidos, nunca tokens, PII ni payload |
| AC16 | Soporte read-only no puede publicar, reintentar ni modificar mensajes |
| AC17 | Orden por agregado se preserva si lo exige contrato de caso de uso |
| AC18 | Ningún evento de ChatGPT Task se ejecuta por una invocación documentada |

La demostración exige futuros tests reales contra implementaciones concretas, no el texto de este contrato. La presencia de xUnit existente sin run exact-SHA no acredita estos AC.

## N01–N18: escenarios negativos FUTUROS_NO_EJECUTADOS

| ID | Rechazo, aislamiento o condición de fallo esperada |
| --- | --- |
| N01 | EmpresaId ajeno en request rechaza autoridad de B |
| N02 | Un único mensaje de B dentro de batch A impide todo dispatch |
| N03 | EventoId vacío o duplicado produce rechazo antes de efecto externo |
| N04 | ClaveIdempotencia en blanco no admite nueva intención |
| N05 | Misma key, payload distinto no causa doble operación |
| N06 | Dos workers no ganan el mismo claim a igual tiempo |
| N07 | Caída después de efecto y antes de ack no duplica cobro |
| N08 | Transacción de negocio fallida no deja outbox huérfano confirmado |
| N09 | Recuperación stale prematura no roba claim vigente |
| N10 | Reintentos no sobrepasan máximo configurado |
| N11 | Fallo de proveedor no se registra como entrega SUCCESS |
| N12 | Error de proveedor con JWT no filtra token en auditoría |
| N13 | CorrelationId malformado no modifica identidad ni permiso |
| N14 | Usuario soporte read-only no publica ni corrige un mensaje |
| N15 | Rol revocado o tenant inactivo no obtiene acceso a datos B |
| N16 | Repetición fuera de orden no corrompe stock ni contabilidad |
| N17 | `SKIPPED`, `CANCELLED`, `IN_PROGRESS` de test necesario no es PASS |
| N18 | Prompts/Tasks no activan dispatcher, webhook, cron o servicio de pago |

## RNF01–RNF12: condiciones no funcionales futuras

| ID | Criterio verificable pendiente |
| --- | --- |
| RNF01 | Tiempos de claim/retry medidos con volumen y concurrencia representativos |
| RNF02 | Capacidad por tenant con límites de batch y cuota explícitos |
| RNF03 | Carga de memoria bajo backpressure y fallos repetidos medida |
| RNF04 | Retención de mensajes y payload conforme a política aprobada |
| RNF05 | Observabilidad correlacionada y logs con redacción auditada |
| RNF06 | Migraciones EF/MySQL reversibles y sin pérdida de historia |
| RNF07 | Reducción de retries ante indisponibilidad de proveedor |
| RNF08 | Rate limit y permisos del eventual adaptador explícitamente autorizados |
| RNF09 | Cancelación y shutdown limpio sin efectos fantasma |
| RNF10 | Pruebas automatizadas positivas/negativas exact SHA con MySQL efímero |
| RNF11 | Regresión tenant A/B, JWT/RBAC, inventario y finanzas sin fallas |
| RNF12 | Rollback mediante despliegue/revert controlado, sin tocar producción no autorizada |

No inventar métricas numéricas, RPO/RTO, garantías exactly-once ni resultados. El SUT y sus límites deben fijarse al implementar M03/M05.

## Matriz de pruebas y límites de aplicación

**Nivel documental actual:** verificar trazabilidad AC/N/RNF, incompatibilidades con fuentes existentes, seguridad por revisión estática, Scope Lock y CHANGELOG. **Nivel futuro Domain/Application:** xUnit ejecutados para identidades, reintentos, auditoría y rollback. **Nivel futuro Infrastructure:** EF/MySQL efímero real con dos tenants y concurrencia, diferenciar SQLite/fakes. **Nivel futuro API/consumidor:** HTTP/JWT/RBAC, fallos controlados, payload mínimo y métricas; Playwright sólo si se altera UI. **Nivel futuro proveedor:** adaptador autorizado con recibos/idempotencia, sin credenciales productivas. El orden debe incluir resultado del SHA exacto, evidencia de jobs y logs, no promesas.

## Impacto y puertas A0–A5

**Impacto inmediato:** únicamente `docs/v5/` y nueva sección completa de `CHANGELOG_AI.md`, sin eventos, workers, SQL, webhooks, frontend, credenciales, cuentas o producción. **A0:** fuente V5, Skill canónica, HEAD, PLAN/COLA/DAG. **A1:** AC01–18, N01–18, RNF01–12 como oráculos futuros no ejecutados. **A2:** JWT/RBAC, empresa A/B, PII y actor soporte; no cambios de autorización real. **A3:** trazabilidad Domain/Application/Infrastructure/tests; runtime NO_APLICA al diff Markdown. **A4:** publicación CAS fast-forward, diff de dos Markdown, verificación exact SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` y readback PLAN/COLA/BITACORA. **A5:** revisión posterior de Task Javier distinta, pendiente para este scope.

**Rollback:** `git revert` normal tras revalidar HEAD actual, sin force-push, reescritura de BITACORA o tocar main/QA/PROD. `M01.06`, implementación real, migraciones y despliegue siguen condicionados por sus gates dinámicos, A5 y permisos específicos.
