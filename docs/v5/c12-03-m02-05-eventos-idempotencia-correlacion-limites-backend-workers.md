# SOLQARYN V5 — C12-03.M02.05: contrato inerte de eventos, idempotencia, correlación y límites backend/workers

**Microtarea:** `C12-03.M02.05`. **Responsable:** PRIMARY DEV 05, `TASK_ID=6adc2e55f47c81919f6374e561687076`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA. **Scope:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT.

## 1. NO IMPLEMENTAR EVENTOS TODAVÍA — frontera real

Esta entrega solo escribe **contratos documentales** de la eventual separación API/worker. No crea eventos de plataforma, dispatcher, webhooks, conexiones entre Tasks, cron, HostedService, cola, productor, consumidor, migración, API pública, proveedor externo, secreto, DNS, infraestructura ni runtime de ninguna duración. Una Task ChatGPT no se interpreta como motor servidor de trabajos perpetuo.

Fuentes respaldadas por el árbol `dev`:
- `backend/src/Domain/Entities/MensajeOutbox.cs`, blob `891572626c19cd6cf8f522fca5ec876333286ffb`, entidad durable con EmpresaId, EventoId, ClaveIdempotencia, CorrelationId, TipoEvento, UTC e Intentos.
- `backend/src/Application/Services/MensajeOutboxService.cs`, blob `a00d353c474d7d7f785a58f443c66c5f12a542f7`, verifica usuario/membresía actual por empresa y replays de idempotencia al registrar, pero no confirma unidad de trabajo.
- `backend/src/Infrastructure/Repositories/MensajeOutboxRepository.cs`, blob `056d5aa380b53ed697ddcb6852df55394e6544a7`, usa filtros tenant y CAS condicional para claim/entrega.
- `backend/src/Application/Services/OutboxRetryProcessor.cs`, blob `542c0a655a3ee0698e3d80f1854d43903b6470fa`, chequea lote entero A/B y duplicados antes de un dispatcher inyectado, sin scheduler interno.
- `backend/src/API/Solqaryn.API.csproj`, blob `2a7b33a0974d1ebe13676593d84b458b1243e037`, API .NET10; un futuro worker **no** obtiene identidad JWT ni HttpContext del proceso API.
- `docs/v5/c12-03-m01-06-gates-rollback-backend-workers.md`, blob `3771d1eb465cdb165a3402dbfc673c325b23f78b`, B01–B16 y R01–R10, A5 independiente verificada.
- `docs/v5/c12-03-m02-01-entidades-ownership-backend-workers.md` y `docs/v5/c12-03-m02-03-persistencia-indices-migraciones-fixtures-backend-workers.md`, contratos anteriores exclusivamente Markdown, sin migraciones o deployment. No se infiere A5 de esos documentos.

## 2. Doce campos propuestos del sobre de trabajo E01–E12

| ID | Dato lógico | Tratamiento futuro requerido |
| --- | --- | --- |
| E01 | `EventId` | Identidad GUID server-side; mantener estable en retry |
| E02 | `EmpresaId` | Tenant persistido, validado por backend; no token del payload |
| E03 | `MessageType` | Tipo allowlisted, nunca invocar código por cadena libre |
| E04 | `MessageVersion` | Versión explícita **futura** y migración aditiva compatible |
| E05 | `AggregateType` | Recurso/entidad causal, validado por empresa |
| E06 | `AggregateId` | Identificador causal, no autoridad del consumidor |
| E07 | `ClaveIdempotencia` | Tupla empresa+clave, estable en redelivery |
| E08 | `CorrelationId` | Traza opaca limitada; no sustituye auth |
| E09 | `CausationId` | Campo futuro sin columna ni DTO creado |
| E10 | `CreatedAtUtc` | Fecha UTC de origen servidor, no reloj cliente |
| E11 | `Attempt` | Número del claim vigente que exige CAS |
| E12 | `EvidenceDigest` | Hash opaco futuro, sin copiar payload/PII |

El sobre es un modelo de compatibilidad, **no una modificación de MensajeOutbox ni un nuevo esquema**; E04/E09/E12 no se presumen implementados.

## 3. Dieciséis garantías G01–G16 de procesamiento seguro

| ID | Invariante fail-closed propuesta |
| --- | --- |
| G01 | El tenant es un hecho verificado por repositorio/identidad servidor, no del mensaje autopresentado. |
| G02 | Un worker sin HttpContext necesita un grant explícito y revocable para el tenant exacto. |
| G03 | La identidad de servicio jamás hereda automáticamente RBAC/JWT del caller API. |
| G04 | Una clave idempotente solo se compara dentro de EmpresaId y con el contenido/operación causal. |
| G05 | Replay idéntico no crea un segundo efecto; replay conflictivo se rechaza sin persistir. |
| G06 | Dos consumidores concurrentes obtienen un único claim por CAS/lock, no lectura secuencial simple. |
| G07 | ACK del intento anterior/stale jamás confirma el intento recuperado por otro consumidor. |
| G08 | Error o timeout externo no es entrega: conservar clave y permitir retry acotado. |
| G09 | DeadLetter conserva evidencia y requiere aprobación distinta para reactivar. |
| G10 | Antes del primer despacho, validar todo el lote y abortar mezcla A/B o id duplicado. |
| G11 | CorrelationId no autentica ni confiere acceso, aun si coincide entre empresas. |
| G12 | Tiempo UTC, tipo/versión y tamaño se validan antes de stagear/consumir. |
| G13 | Logs de API y worker excluyen JWT, secreto, PII, DSN y payload bruto, incluso ante excepción. |
| G14 | Error de proveedor se representa mediante código seguro, no respuesta externa cruda. |
| G15 | Cuotas, lotes, reintentos, jitter y tiempo de proceso finitos; nunca scheduler infinito implícito. |
| G16 | Check CI requerido debe ser completed/success exact SHA; A5 de revisor distinto no habilita QA/PROD. |

## 4. Diez políticas de presupuesto y aislamiento L01–L10, SIN CONFIGURACIÓN VIVA

| ID | Política futura | Evidencia requerida antes de activación |
| --- | --- | --- |
| L01 | `BatchSize` | Valor tipado positivo y límite de procesador real, medido |
| L02 | Máximo de intentos | Finito, con DeadLetter y contención |
| L03 | BaseDelay/MaxDelay | Backoff acotado, `BaseDelay <= MaxDelay` |
| L04 | StaleClaimAfter | UTC y prueba de lease/worker concurrente |
| L05 | Timeout proveedor | Cancelación y reconciliación, nunca ACK supuesto |
| L06 | Quota por tenant | Contadores sin interferencia A/B y límite global seguro |
| L07 | Tamaño del payload | Respetar máximos de entidad y no publicar PII |
| L08 | Versión/tipo de contrato | Denegación de desconocidos antes de efecto |
| L09 | Cardinalidad de telemetry | Solo códigos allowlisted, sin IDs sensibles como etiquetas |
| L10 | Permisos de entorno | Desarrollo sin autoridad para QA/main/PROD, secrets o gastos |

No se fijan cifras arbitrarias de throughput, SLO, cuota, TTL, capacidad, RTO/RPO ni costos; esos valores dependen de métricas reales, contratos y autorización posterior.

## 5. Diez señales documentales S01–S10 — NO EMITIDAS

`S01 JobIntentionAccepted`, `S02 WorkerAuthorityDenied`, `S03 TenantScopeMismatch`, `S04 ClaimWon`, `S05 ClaimSuperseded`, `S06 DeliveryAcknowledged`, `S07 RetryDeferred`, `S08 DeadLetter`, `S09 VersionRejected`, `S10 ReadyDependencyFailed`. Son etiquetas **conceptuales**, no llamadas, eventos nuevos, métricas insertadas, registros en BITACORA ni implementación de bus. Cualquier instrumentación futura exige control de permisos y presupuesto de cardinalidad.

## 6. Diez pruebas positivas/negativas P01/N01–P10/N10, NO EJECUTADAS

| Par | Futuro positivo causal | Futuro negativo a rechazar |
| --- | --- | --- |
| P01/N01 | API A registra intención con JWT/RBAC vigentes | API B actúa sobre recurso de A |
| P02/N02 | Worker independiente recibe grant válido A | Worker sin identidad usa tenant de payload |
| P03/N03 | Dos workers compiten y uno gana claim | Dos efectos de mismo intento |
| P04/N04 | Retry comparte key idempotente y termina | Timeout duplica cobro o crea loop |
| P05/N05 | ACK actual se confirma por CAS | ACK supersedido marca Entregado |
| P06/N06 | Stale se recupera sin escritor activo | Recuperación basada solo en edad |
| P07/N07 | CorrelationId conserva trazabilidad opaca | Permite leer B o filtra PII |
| P08/N08 | Versión compatible se consume con permiso | Evento desconocido ejecuta comando |
| P09/N09 | Readiness negativo con DB/secreto ausente | Liveness sin DB se publica ready |
| P10/N10 | A5 distinta y CI exact-SHA permiten revisar | Autor se autoaprueba o cancelled es PASS |

Estos oráculos requerirán tests .NET 10, integración MySQL 8.4 efímera, dos contextos de consumidor, controles JWT/RBAC/tenant A/B y rollback en su fase técnica autorizada. Aquí **no** se ejecutan ensayos, HTTP, Docker ni un proveedor real.

## 7. Gates A0–A5 e impacto/rollback

**A0:** identidad, Skill, Scope Lock, HEAD, PLAN/COLA/DAG y CAS exclusivo; review FIRST de propia VALIDANDO. **A1:** sobre E01–E12, G01–G16, L01–L10, S01–S10 y 10 pares P/N documentados, no ejecutados. **A2:** reglas server-side de identidad/tenant A/B, JWT/RBAC, idempotencia, CAS, privacidad, errores redacted y limites. **A3:** contraste con el outbox de Domain/Application/Infrastructure y fronteras API/worker; CI runtime .NET/Angular/MySQL `NO_APLICA` para diff exclusivamente Markdown, sin llamarla PASS. **A4:** publicar solo este doc y CHANGELOG con encabezados no vacíos, CAS fast-forward sobre dev, Scope Lock/V5 completed/success exact SHA y readbacks en PLAN/COLA/DAG/BITACORA. **A5:** revisión documental independiente por Task Javier distinta de la autora, nunca autoaprobación.

**Rollback:** `git revert` normal del changeset después de observar HEAD vivo. No existe trabajador, evento, conexión, dato, cola, migración, backup o secreto operativo que revertir debido a este documento. No autoriza webhooks, dispatcher de Tasks ni QA/main/PROD.
