# SOLQARYN V5 FINAL — C14-04.M02.05: contrato conceptual de eventos, idempotencia y límites de puesta en marcha

**Autor:** PRIMARY DEV 03 JAVIER_CHATGPT, `TASK_ID=6adc2e4bd8a48191ac7d8641ed95a1a3`. **Ámbito:** `solqaryn/Solqaryn@dev` con `PROJECT_SCOPE_LOCK=STRICT`. **Entrega:** contrato DOCUMENTAL M02.05; **no se implementan eventos, webhooks, dispatcher, cola, scheduler, worker o reintentos automáticos**.

## 1. Autoridad y predecesores

Fuente única de backlog: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit); estados desde [PLAN_MAESTRO/COLA/DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), evidencia histórica BITACORA A:Z. Se aplicaron `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md`, skill y arquitectura actual. Preflight M02.05: M01.01–06 LISTO+A5 independiente, PLAN H=PENDIENTE/I vacía, COLA G/O=SI, DAG F=VALIDADO/G=SI. M02.01–04 son diseños LISTO A0–A4 sin certificar sus A5 propios.

Las fórmulas de Sheets recalculan elegibilidad en la misma corrida; **no generan eventos server-side** ni activan otras Tasks fuera de sus agendas. Esta tarea describe un modelo para futura autorización, separado del flujo de BITACORA y de cualquier servicio de eventos.

## 2. Catálogo de señales conceptuales, no emitidas

| EV | Hecho observable futuro | Correlación y límite de autoridad |
| --- | --- | --- |
| EV01 | ReleaseCandidatePrepared | releaseId + commitSHA; no despliega |
| EV02 | ArtifactManifestVerified | SHA + digest comprobados |
| EV03 | EnvironmentGrantVerified | grant externo específico, no A5 |
| EV04 | CheckStatusObserved | checkName + runId + headSHA |
| EV05 | AcceptanceCaseRecorded | caseId + expected/observed + hash |
| EV06 | HealthSignalObserved | serviceId + UTC, no SLO ficticio |
| EV07 | AlertConditionObserved | severity + ruleVersion + observationId |
| EV08 | IncidentOpened | incidentId + redacted correlationId |
| EV09 | IncidentEscalated | incidentId + authorizedRecipientRef |
| EV10 | BackupIntegrityObserved | snapshotId + checksum, no backup inventado |
| EV11 | RestoreTrialObserved | trialId + syntheticScope, no restore real sin permiso |
| EV12 | RollbackProcedureReviewed | runbookRef + owner, no reversión automática |
| EV13 | ReleaseTransitionRejected | reasonCode + scope + gate fallido |
| EV14 | ReleaseTransitionVerified | grant + SHA + check causales (evidencia futura) |

**EV01–EV14 = EVENTOS_PROPUESTOS_NO_EMITIDOS.** No se presupone sistema productor/consumidor, API nueva, transporte, almacenamiento de mensajes, webhook o dispatcher. Nadie obtiene permiso de QA/main/PROD mediante una señal conceptual.

## 3. Modelo de envelope y deduplicación futura

| ID | Campo lógico | Restricción |
| --- | --- | --- |
| CT01 | eventId | Opaco e irrepetible por fuente |
| CT02 | eventType | Enum allowlisted EV01–EV14 |
| CT03 | eventSchemaVersion | Rechazar versión desconocida |
| CT04 | occurredAtUtc | Momento observado, no hora reconstruida |
| CT05 | sourceCommitSha | SHA exacto del artefacto afectado |
| CT06 | correlationId | Permite unir release, incidente y test; sin PII |
| CT07 | idempotencyKey | Determinista por hecho y ámbito, no por retry |
| CT08 | scopeId | Ámbito validado backend |
| CT09 | tenantBinding | Contexto autorizado servidor, no input libre |
| CT10 | actorRef | Identidad/rol permitido, con redacción |
| CT11 | evidenceDigest | Referencia inmutable de prueba, sin secretos |
| CT12 | causationId | Enlace padre sólo si validado; previene loops |

**CT01–CT12 = DISEÑO_NO_IMPLEMENTADO.** Debe distinguirse `eventId` único de `idempotencyKey` estable para reintentos de la MISMA causa. La clave se acota por tipo, ámbito y versión de contrato; collisions o replay inter-tenant se rechazan y auditan. No escribir tokens, URL firmadas, payload con PII ni datos financieros en mensajes.

## 4. Políticas de idempotencia, cuota y manejo de errores

| POL | Norma hipotética de implementación futura |
| --- | --- |
| ID01 | Exactamente una transición institucional confirmada por eventId/idempotencyKey lógico |
| ID02 | Reintentos no crean un segundo incidente, restore o grant |
| ID03 | Lease/lock exclusivo se verifica al procesar; Sheets lectura/escritura no equivale a CAS atómico |
| ID04 | Rechazar eventos ajenos a tenant autorizado; no usar `EmpresaId` cliente como permiso |
| ID05 | Status `queued/in_progress/cancelled/skipped/failed` jamás acredita PASS CI |
| ID06 | Versiones de schema desconocidas se rechazan, no traducen silenciosamente |
| ID07 | Límite de reintentos es finito y solo ante nueva evidencia/hipótesis |
| ID08 | Cupo por actor/scope/tenant y backpressure se miden antes de habilitar servicio |
| ID09 | Dead-letter no revela secretos y requiere revisión humana/operativa autorizada |
| ID10 | Reproducción de historial no reescribe BITACORA ni promueve PLAN automáticamente |
| ID11 | Entrega fuera de orden se concilia por SHA/versión, nunca por hora de arribo |
| ID12 | Fallo parcial conserva estado verificable y rollback no destructivo |

**ID01–ID12 son garantías deseadas pendientes de diseño/medición, no propiedades de un runtime existente**. De forma especial, no prometer procesamiento exactly-once real al combinar GitHub, Google Sheets y tareas nativas sin transacción distribuida.

## 5. Diez escenarios positivos/negativos FUTUROS_NO_EJECUTADOS

| PR | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| PN01 | EV01 con SHA verificable y grant explícito | SHA distinto bloquea release |
| PN02 | Clave idempotente evita duplicado en ensayo | Replay idéntico no dispara segunda acción |
| PN03 | Usuario tenant A accede solo a eventos A | Tenant A no recibe evento B |
| PN04 | EvidenceDigest y caseId confirman caso ejecutado | Spec sin test no se declara PASS |
| PN05 | Check completado success exact SHA se reconoce | Cancelled/skipped/queued no PASS |
| PN06 | Alerta correlacionada a observación real | Alerta conceptual no certifica SLO |
| PN07 | Incidente se escala a destinatario autorizado | Actor sin RBAC no recibe PII |
| PN08 | Restore sintético autorizado registra trial | Restore sin backup/grant bloqueado |
| PN09 | Versión de evento conocida se deserializa | Versión inválida rechaza |
| PN10 | Reintento finito conserva trazabilidad | Retry infinito/dispatcher perpetuo prohibido |

**PN01–PN10 no se probaron.** Nada aquí ejecuta .NET, MySQL, RabbitMQ, Redis, webhooks, cloud services, CI runtime, backup, restore o eventos reales.

## 6. Regla de transición y auditoría V5 (vigente, sin nuevos servicios)

Las seis Tasks Javier son el único runtime permitido. Después de PLAN H/I/K y evento idempotente BITACORA (A:Z), se releen inmediatamente las fórmulas de COLA G/O y DAG F/G y el HEAD; la evaluación es **por lectura en la corrida activa**, no por un trigger externo. PENDIENTE→EN_PROGRESO exige owner exclusivo; VALIDANDO no habilita fases técnicas; LISTO requiere A0–A4 causales; A5 depende de una Task distinta del autor y sin A5 no se libera gate dependiente. No crear Tasks, PR ni branches ni despachar acciones desatendidas.

## 7. Revisión A0–A5, impacto y rollback

- A0: enumerar EV01–EV14, CT01–CT12, ID01–ID12, PN01–PN10; Markdown íntegro sin secretos ni encabezados vacíos.
- A1: cada PN tiene resultado positivo/negativo verificable futuro; marcar `FUTUROS_NO_EJECUTADOS`.
- A2: JWT/RBAC/tenant A/B, actor, PII, secretos y no autorización de entornos; sin cambiar código ni configuración.
- A3: enlazar M01.06 bloqueo/rollback, M02.01 ownership, M02.02 API/DTO, M02.03 persistencia y M02.04 flags; mantener compatibilidad, no runtime.
- A4: diff estrictamente dos Markdown, commit dev fast-forward/CAS, Scope Lock y V5 exact-SHA `completed/success`; readback PLAN/COLA/DAG/BITACORA.
- A5: revisión independiente de otra Task Javier distinta de la autora, sin autoaprobación.

**Rollback de este changeset documental:** `git revert` regular sobre dev, comprobando HEAD y preservando BITACORA histórica. Sin QA, main, PROD, DNS, secretos, DB real ni eventos instalados.
