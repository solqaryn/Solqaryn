# SOLQARYN V5 — C11-03.M02.05: contrato de eventos, idempotencia, correlación y límites

**Microtarea:** `C11-03.M02.05`, C11-03 Outbox y eventos, etapa documental M02. **Responsable:** PRIMARY DEV 05 — JAVIER_CHATGPT, `TASK_ID=6adc2e55f47c81919f6374e561687076`. **Rama:** `solqaryn/Solqaryn@dev`. **Gobierno:** PROJECT_SCOPE_LOCK STRICT, STANDARD V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO y BITACORA institucional.

## 1. Contrato de preparación — NO IMPLEMENTAR EVENTOS TODAVÍA

Este documento describe un posible futuro contrato de productor/consumidor con restricciones de seguridad, idempotencia, orden y cuotas. **No crea, ejecuta ni conecta eventos nuevos**, bus, outbox genérico nuevo, relay, worker, webhook, llamadas entre Tasks, cron, endpoint HTTP, proveedor, migración EF, SQL, cambios en API ni red/infraestructura. No hay scheduler server-side perpetuo por escribir estas reglas.

Fuentes reales Git:
- `backend/src/Domain/Entities/MensajeOutbox.cs`, blob `891572626c19cd6cf8f522fca5ec876333286ffb`, ya tiene `EmpresaId`, `EventoId`, `ClaveIdempotencia`, `TipoEvento`, `PayloadJson`, `CorrelationId`, estado e intentos.
- `backend/src/Application/Services/MensajeOutboxService.cs`, blob `a00d353c474d7d7f785a58f443c66c5f12a542f7`, valida scope vigente y compara la intención ante una clave idempotente repetida.
- `backend/src/Infrastructure/Repositories/MensajeOutboxRepository.cs`, blob `056d5aa380b53ed697ddcb6852df55394e6544a7`, usa filtros empresariales y actualización condicional `ExecuteUpdateAsync` para claim/confirmación.
- `backend/src/Application/Services/OutboxRetryProcessor.cs`, blob `542c0a655a3ee0698e3d80f1854d43903b6470fa`, ya valida lote A/B y duplicados antes del primer efecto y aplica retry finito; no añade un HostedService/scheduler.
- `docs/v5/c11-03-m01-06-gates-rollback-outbox-eventos.md`, blob `fb068b1687e4c1d33fe2989ca9678ae3fb42e297`, fue revisado por A5 independiente; B01–B14 y RB01–RB10 continúan como barreras.
- `docs/v5/c11-03-m02-01-entidades-ownership-invariantes-outbox.md` define 14 elementos conceptuales/16 invariantes; `docs/v5/c11-03-m02-03-persistencia-indices-migraciones-fixtures-outbox.md` especifica 12 proyecciones/8 índices candidatos/6 etapas/12 fixtures. **La documentación no acredita pruebas de MySQL, un nuevo índice ni relay desplegado**.

## 2. Doce campos de sobre E01–E12 (contrato FUTURO, no DTO instalado)

| ID | Campo lógico | Restricción propuesta |
| --- | --- | --- |
| E01 | `EventId` | GUID generado en servidor y estable entre retries |
| E02 | `TenantId` | `EmpresaId > 0` obtenido/validado por autoridad backend |
| E03 | `EventType` | Catálogo allowlisted, no invocación arbitraria |
| E04 | `SchemaVersion` | Versión positiva y compatible; hoy no se añade propiedad al modelo |
| E05 | `AggregateType` | Tipo opcional tipado, sin autoridad por sí solo |
| E06 | `AggregateId` | Referencia opcional, comprobada contra ownership persistido |
| E07 | `IdempotencyKey` | Tenant-scoped, longitud acotada, misma clave durante retry |
| E08 | `CorrelationId` | Solo trazabilidad, rechazar suplantación como permiso |
| E09 | `CausationId` | **Propuesta futura** para causalidad; no columna creada |
| E10 | `OccurredAtUtc` | UTC validado de origen confiable, no del navegador |
| E11 | `DeliveryAttempt` | Entero del claim vigente, no autoridad autodeclarada |
| E12 | `PayloadReference/Hash` | **Propuesta futura**, hash y referencia sin exponer contenido sensible |

No se equipara el sobre propuesto con la entidad actual: campos E04/E09/E12 aún no son parte del modelo persistido y solo podrán agregarse con preflight de migración autorizado.

## 3. Dieciséis invariantes de idempotencia y seguridad ID01–ID16

| ID | Condición obligatoria para una futura implementación |
| --- | --- |
| ID01 | `(EmpresaId, ClaveIdempotencia)` separa tenants; coincidencia textual A/B no comparte autorización. |
| ID02 | Repetición de misma clave y solicitud idéntica devuelve el mismo efecto lógico, sin nuevo despacho. |
| ID03 | Mismo par tenant+clave con payload/operación distinta genera conflicto comprobable. |
| ID04 | Un EventoId nunca se interpreta sin verificar el EmpresaId del objeto persistido. |
| ID05 | Dos consumidores concurrentes requieren CAS/lock del mismo claim: uno gana, otro observa conflicto/no-op. |
| ID06 | Confirmar una entrega exige `Intentos` esperado; un antiguo claim stale no confirma el nuevo. |
| ID07 | El evento y mutación de negocio deben compartir unidad transaccional donde el contrato causal la requiere. |
| ID08 | Timeout de proveedor no es entrega: retry debe preservar su llave idempotente y evitar duplicado financiero. |
| ID09 | El batch se inspecciona por completo antes del primer efecto; mezcla de tenant o ID duplicado bloquea todo. |
| ID10 | El caller autenticado solo puede emitir tipos, acciones y recursos autorizados por el backend vigente. |
| ID11 | `CorrelationId`, `AggregateId` y cabeceras no se usan para autorizar ni localizar otro tenant. |
| ID12 | Versiones incompatibles y eventos fuera de orden se rechazan hasta que exista contrato versionado probado. |
| ID13 | Logs omiten payload, secretos, JWT, OTP, PII y error bruto de proveedor; solo códigos redacted. |
| ID14 | Intentos máximos, backoff, cuota y tamaño de mensaje son finitos; sin loops server-side infinitos. |
| ID15 | DeadLetter es terminal salvo autorización humana nueva y trazabilidad del reintento separado. |
| ID16 | Ningún check `queued/in_progress/skipped/cancelled/failed` es PASS requerido; A5 debe ser independiente. |

## 4. Diez límites contractuales L01–L10

| ID | Límite / ámbito futuro | Estado y prueba exigible |
| --- | --- | --- |
| L01 | Máximo de eventos por lote | Usar valores tipados validados; medir y configurar sin exceder límites de procesador existente |
| L02 | Tamaño de payload | No superar `MensajeOutbox.LongitudMaximaPayload`; rechazo antes de staging |
| L03 | Largo de idempotency key | No superar `LongitudMaximaClaveIdempotencia` normalizada |
| L04 | Retención y privacidad | Definir política por tenant, base legal y borrado/anonimización antes de activar almacenamiento nuevo |
| L05 | Reintentos | Usar política finita existente, no rearmar automáticamente DLQ |
| L06 | Timeout externo | Cancelación acotada y estado reconciliable; no declarar ACK presunto |
| L07 | Cuota por tenant | Contadores tenant-bound medidos y comportamiento fail-closed |
| L08 | Versiones de contrato | Rechazar consumidor incompatible; negociaciones documentadas |
| L09 | Correlación y trazabilidad | IDs opacos acotados, sin PII y sin influencia en auth |
| L10 | Frecuencia de automatizaciones | No conectar Tasks ChatGPT ni webhooks; no asumir ejecución perpetua del proveedor |

**Sin umbrales inventados:** un diseño futuro requiere mediciones, garantías contractuales y configuración tipada real antes de determinar números concretos de cuota, payload, TTL y throughput.

## 5. Diez señales documentales S01–S10, NO EMITIDAS

`S01 IntentStaged`, `S02 ClaimWon`, `S03 ClaimLost`, `S04 DeliveryAcknowledged`, `S05 RetryScheduled`, `S06 DeadLetterReached`, `S07 TenantMismatchDenied`, `S08 DuplicateKeyConflict`, `S09 SchemaVersionRejected` y `S10 StaleClaimRejected`. Estos son **nombres de oráculos conceptuales**, no un enum, publicación en bus, log emitido, métrica instrumentada o API añadida. Quedan sujetos a privacidad, vocabulario allowlisted y budget de cardinalidad.

## 6. Diez pares de aceptación futura P01/N01–P10/N10

| Par | Caso positivo futuro en CI pertinente | Caso negativo que debe denegarse |
| --- | --- | --- |
| P01/N01 | Staging por actor tenant A con membresía vigente | Actor B intenta registrar en A |
| P02/N02 | Replay mismo tenant+key+contenido sin efecto duplicado | Igual key y payload distinto |
| P03/N03 | Dos claims MySQL del mismo EventoId dan un único ganador | Doble procesamiento del mismo intento |
| P04/N04 | Retry con ACK durable confirma entrega | Timeout se marca `Entregado` |
| P05/N05 | DLQ conserva evidencia y termina retries | DLQ se reactiva en ciclo infinito |
| P06/N06 | A y B tienen quotas e idempotency scopes independientes | Cuota de A consume recursos de B |
| P07/N07 | Correlación cruza trazas permitidas de A | CorrelationId autoriza acceso a B |
| P08/N08 | SchemaVersion compatible se acepta | Versión desconocida invoca acción privilegiada |
| P09/N09 | Auditoría solo guarda metadata saneada | Log incluye JWT/PII/payload crudo |
| P10/N10 | Git exact SHA y A5 independiente permiten revisión | `cancelled`, falso SHA o autor se autoaprueba |

**FUTUROS_NO_EJECUTADOS:** estas son especificaciones para posteriores microtareas M03/M05 con cambios reales, xUnit, MySQL 8.4 efímero y seguridad; no reportan tests ejecutados por esta documentación.

## 7. Criterios de entrega A0–A5, impacto y rollback

**A0:** identidad, Skill/Scope Lock/HEAD, owner CAS, DAG F/G y COLA G/O, lectura del código/contratos. **A1:** cotejar 12 campos, 16 invariantes, 10 límites, 10 señales y 10 pares de pruebas futuras reales en Markdown. **A2:** revisar tenant A/B, RBAC/JWT y validación backend, idempotencia, privacidad y retry finito; no cambiar runtime. **A3:** compatibilidad de diseño con `MensajeOutboxService`, `MensajeOutboxRepository`, `OutboxRetryProcessor` y documentos M01/M02, sin invocar servicios; runtime .NET, Angular, MySQL y pruebas HTTP son `NO_APLICA` por diff Markdown, no PASS. **A4:** diff restringido a este documento y CHANGELOG_AI.md, encabezado y viñetas autocontenidos, commit fast-forward/CAS sobre HEAD, Scope Lock/V5 exact-SHA `completed/success`, BITACORA/PLAN/COLA/DAG readback. **A5:** Task Javier diferente del autor revisa el alcance y SHA; sin autoaprobación.

**Rollback:** git revert normal del changeset documental después de comprobar HEAD y datos institucionales. **No se activaron eventos**, sockets, colas, webhooks, dispatchers, worker, SQL, EF, secretos, migraciones, proveedores, cuentas, QA/main/PROD ni infraestructura.
