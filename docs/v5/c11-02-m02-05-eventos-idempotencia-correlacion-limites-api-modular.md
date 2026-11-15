# SOLQARYN V5 — C11-02.M02.05: eventos, idempotencia, correlación y límites contractuales de API modular

**Microtarea:** C11-02.M02.05, etapa M02 documental, C11-02 API modular. **Autor:** PRIMARY DEV 01 JAVIER_CHATGPT, TASK_ID=6adbb9fff1548191a1bd5b30d9e9984a. **Repositorio/branch:** solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK=STRICT. **Fuentes:** STANDARD V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA; Skill y arquitectura canónicas.

## 1. Alcance autorizado y no-alcance

Este archivo diseña **contratos futuros** de eventos e idempotencia entre el borde API, Application y una posible infraestructura de outbox. **NO IMPLEMENTAR EVENTOS TODAVÍA:** no conectar ni ejecutar dispatcher, relay, event bus, webhook, HostedService, cron, worker, llamadas entre Tasks, API nueva, migraciones SQL, EF, proveedor, secreto o proceso permanente. Tampoco alterar roles, JWT, tenancy, QA/main/PROD ni datos reales. No se declara entrega efectiva ni conformidad runtime: el resultado es revisión documental para posteriores fases técnicas explícitamente autorizadas.

Antes del CLAIM: C11-02.M01.01–06 estaban LISTO con A5 independiente, DAG del ID F=VALIDADO/G=SI, PLAN H=PENDIENTE/I vacío y COLA G/O=SI; preflight no concede permisos de migraciones, código de seguridad o servicios. La ejecución actual mantiene owner exclusivo en PLAN; pasar esta unidad a LISTO A0–A4 no equivale a una aprobación A5.

## 2. Fuentes reales inspeccionadas (no modificadas)

| Fuente en dev | Blob Git | Evidencia concreta |
| --- | --- | --- |
| backend/src/API/Middleware/CorrelationIdMiddleware.cs | 3b76dca41bb4bc0b44f89c2a4664c79907f9f242 | Cabecera X-Correlation-ID, longitud máxima 64 y trazabilidad; no autoridad de tenant |
| backend/src/Application/Common/ApiResponse.cs | aba995eae50ad756712961c9c394f5038a31a142 | Envoltorio HTTP Success, Data, Message, Errors; no cambiar sin compatibilidad |
| backend/src/API/Middleware/ExceptionHandlingMiddleware.cs | baee40143e4e48b8df200e77892606af96d60b7f | Manejo de errores y saneamiento; requiere negativos ejecutados para certificar |
| backend/src/Application/Services/MensajeOutboxService.cs | a00d353c474d7d7f785a58f443c66c5f12a542f7 | Validación tenant-bound y conflicto de clave idempotente antes de staging |
| backend/src/Application/Services/OutboxRetryProcessor.cs | 542c0a655a3ee0698e3d80f1854d43903b6470fa | Retry finito y validación de lotes; no prueba activación de scheduler |
| backend/src/Domain/Entities/MensajeOutbox.cs | 891572626c19cd6cf8f522fca5ec876333286ffb | EmpresaId, EventoId, ClaveIdempotencia, intentos y estados existentes |
| docs/v5/c11-02-m01-06-gates-rollback-salida-api-modular.md | a096c5b0cdc4a520664ca41d2dfc5c53d48a4cb9 | Gates G01–G16, rollback y pruebas futuras de API modular |
| docs/v5/c11-03-m02-05-eventos-idempotencia-correlacion-limites-outbox.md | 62d4f13b10a3a1276898a26ad4414ffd749653aa | Contrato conceptual vecino del outbox, no permiso para implementar |

La existencia de interfaces/código de outbox no significa que se haya activado mensajería ni que un HTTP request tenga permiso implícito de crear efectos. No usar CorrelationId como clave de idempotencia o autenticación.

## 3. Diez límites de frontera API/Application EV01–EV10

| ID | Contrato conceptual futuro |
| --- | --- |
| EV01 | Un comando autenticado vincula usuario, EmpresaId y rol al ámbito validado por backend |
| EV02 | IdempotencyKey identifica la intención por tenant+operación, nunca global entre A/B |
| EV03 | La repetición de misma operación/key/payload preserva único efecto lógico |
| EV04 | Misma key y payload o autorización diferentes produce conflicto seguro |
| EV05 | CorrelationId transporta trazabilidad acotada, nunca grants o identidad tenant |
| EV06 | Eventos permitidos se registran en catálogo tipado/allowlist, no nombres arbitrarios de cliente |
| EV07 | Payload se valida, limita y redacta antes de persistir; no PII/tokens en telemetría |
| EV08 | API no afirma éxito de proveedor externo sin ACK durable confirmado |
| EV09 | Un futuro relay requiere CAS de claim y reintentos finitos; DeadLetter no se auto-reactiva |
| EV10 | Ninguna acción de API puede despertar directamente Tasks ChatGPT o crear ejecución perpetua |

## 4. Dieciséis invariantes de seguridad e idempotencia ID01–ID16

| ID | Regla de aceptación obligatoria para implementación futura |
| --- | --- |
| ID01 | Actor A autenticado y membresía vigente jamás lee/emite tenant B |
| ID02 | EmpresaId de body/header/query es selector, no autoridad |
| ID03 | Plataforma global y soporte read-only no heredan permiso mutable de empresa |
| ID04 | Repetición idempotente conserva OperationId y scope de tenant |
| ID05 | Conflicto key/payload no produce segundo efecto |
| ID06 | Doble claim del mismo mensaje exige un solo ganador con CAS |
| ID07 | ACK obsoleto no confirma intento nuevo |
| ID08 | Excepción/timeout no se convierte en éxito presunto |
| ID09 | Batch mezclado A/B se deniega antes de cualquier efecto |
| ID10 | EventoId/CorrelationId no permiten localizar datos ajenos |
| ID11 | Se rechazan eventos no allowlisted y schema version desconocida |
| ID12 | Reintentos/backoff/cuota/tamaño son finitos y configurados por servidor |
| ID13 | Logs excluyen SQL crudo, JWT, Password, OTP, PII y payload privado |
| ID14 | Errores HTTP mantienen shape compatible o versión publicada con consumidores |
| ID15 | Desactivación del futuro relay bloquea todos los efectos aunque existan mensajes |
| ID16 | Required CI = completed/success del SHA causal; A5 por Task Javier distinta del autor |

## 5. Diez casos positivos y negativos P01/N01–P10/N10 FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro | Negativo que debe denegarse |
| --- | --- | --- |
| P01/N01 | Actor A emite intención con permiso válido | Actor B intenta operar A |
| P02/N02 | Replay idéntico produce un solo efecto | Mismo tenant/key con payload diferente |
| P03/N03 | CorrelationId acotado preserva traza | CorrelationId abre acceso al tenant B |
| P04/N04 | Evento conocido/versionado es procesable | EventType arbitrario invoca código |
| P05/N05 | Claim único gana bajo carrera | Dos procesos se adjudican el mismo intento |
| P06/N06 | ACK durable confirma operación vigente | Timeout/ACK stale se marca Entregado |
| P07/N07 | Retry finito alcanza DeadLetter | DeadLetter se activa en bucle |
| P08/N08 | Log saneado preserva referencia técnica | Log incluye JWT/Password/PII |
| P09/N09 | Cuotas tenant A/B aisladas | Carga A consume presupuesto B |
| P10/N10 | SHA exacto, CI y A5 ajena acreditan avance | CHECK cancelled/skipped/queued o A5 propia pasa |

**NO SE EJECUTARON** estos P/N en .NET, HTTP, MySQL, Docker, Playwright o entorno real. Son oráculos para las fases M03/M05 posteriores cuando estén técnicamente habilitadas. Tampoco se emiten eventos, métricas ni solicitudes externas por este documento.

## 6. Ocho barreras de recuperación RB01–RB08

| ID | Política |
| --- | --- |
| RB01 | Rollback de esta documentación por git revert normal, conservando HEAD ajenos |
| RB02 | BITACORA histórica es append-only, nunca borrar FAIL antiguo |
| RB03 | Futuro dispatcher desactivado ante error tenant, idempotencia o contrato |
| RB04 | Conservar llave idempotente para reconciliación de timeout y fallo parcial |
| RB05 | No reactivar DeadLetter sin permiso individual y evento auditado |
| RB06 | Cambios MySQL futuros requieren migración y rollback probados en CI efímera |
| RB07 | Checks required pendientes/fallidos dejan scope en VALIDANDO, no PASS |
| RB08 | Ningún revert o pase dev autoriza QA/main/PROD, proveedores, cuentas o secretos |

## 7. A0–A5 y criterio de salida

**A0**: identidad, Scope Lock, HEAD, owner único PLAN H/I, DAG F/G y COLA G/O; diff limitado a este archivo y CHANGELOG_AI.md. **A1**: cotejar diez límites EV, dieciséis invariantes ID, diez pares P/N futuros y ocho rollback RB, sin atribuir tests inexistentes. **A2**: tenant A/B, RBAC/JWT, autorización backend, secretos/PII, idempotencia y claim CAS fail-closed documentales. **A3**: leer y contrastar fuentes API/Application/Domain y contrato vecino Outbox, sin tocar código: builds, tests .NET y MySQL/Playwright no son causales al diff Markdown, se consignan NO_APLICA, jamás PASS. **A4**: publicar fast-forward/CAS en dev, comprobar SHA/blob/diff y checks Scope Lock/V5 completed/success exact SHA, eventos BITACORA, readback PLAN/COLA/DAG. **A5**: Task Javier distinta de DEV01 con revisión real de SHA/diff/CI; queda A5_PENDIENTE tras LISTO_DEV.

La salida de M02.05 **no** desbloquea M02.06 o M03 por sí sola: esas microtareas requieren sus predecesores LISTO + A5 y gate calculado. Ningún proceso de eventos ha sido instalado.
