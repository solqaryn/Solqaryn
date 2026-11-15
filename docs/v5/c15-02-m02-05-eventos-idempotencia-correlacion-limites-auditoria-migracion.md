# SOLQARYN V5 FINAL — C15-02.M02.05: contrato futuro de eventos, idempotencia, correlación y límites

**Fecha de alcance:** 2026-11-14 (America/Tegucigalpa). **Autor:** PRIMARY DEV 04 JAVIER, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. **Rama:** dev en solqaryn/Solqaryn. **Control:** PROJECT_SCOPE_LOCK=STRICT.

## 1. Alcance y autoridad

Contrato exclusivamente documental sobre arquitectura/migración C15-02, etapa M02.05. Referencias: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA, contrato terminal C15-02.M01.06 (bloqueos BL01–BL16 y rollback RB01–RB08), modelo M02.01 y contratos M02.02. Su DAG permite redactar esta microtarea independiente y NO libera cambios de infraestructura, runtime o terminal M02.06.

No se crea servicio, Task, webhook, dispatcher, workflow, worker, cola, migración, tabla, endpoint o suscripción. No existe mecanismo de eventos server-side infinito. Vercel/Render/Aiven/Cloudinary siguen declarados actuales; Hostinger/Cloudflare/R2 son objetivos no validados en vivo. La existencia de un sobre contractual no significa que un evento real se emita. Sólo BITACORA append-only registra las acciones institucionales ya autorizadas.

## 2. Sobre de evento futuro, con privacidad por defecto

Campos conceptuales: eventType (enum), eventId (identificador único), idempotencyKey (derivada por alcance), correlationId (opaco), sourceRevision (SHA exacto), actorTaskId del allowlist, ownerTenantId del contexto autenticado cuando corresponda, occurredAtUTC, schemaVersion, evidenceRef redactada, status y reasonCode. La autoridad nunca proviene de header, host, slug, body ni owner libre declarado. Cada campo sensible requiere autorización explícita antes de integración.

El resultado del contrato es READY_FOR_REVIEW o REJECTED de preparación; no genera side effects, no incrementa tareas, no altera G/O ni prueba restauración. El mismo evento no puede determinar A5_PASS sin revisor distinto y verificación del SHA y pruebas.

## 3. Catálogo de señales conceptuales EV01–EV14

| ID | Señal futura | Campos mínimos contractuales | Regla de denegación y límite |
| --- | --- | --- | --- |
| EV01 | AUDIT_SOURCE_OBSERVED | fuente,revisionSHA,owner,UTC | Sólo describe fuente leída, no certifica recurso real |
| EV02 | TENANT_SCOPE_CHECKED | userId,tenantContext,decision | No registrar token o PII en claro |
| EV03 | PROVIDER_REFERENCE_CHECKED | providerKind,opaqueRef,result | No activar proveedor citado |
| EV04 | DOMAIN_BINDING_CHECKED | hostRef,tenantRef,decision | No confiar en slug/host del cliente |
| EV05 | DATA_OWNER_CHECKED | dataRef,tenantRef,result | NO_GO ante NULL ambiguo |
| EV06 | BACKUP_MANIFEST_CHECKED | manifestRef,checksumRef,result | Backup no equivale a restore |
| EV07 | RESTORE_DRILL_OBSERVED | drillRef,ephemeralEnv,result | No declarar ensayo sin ejecución |
| EV08 | OBJECT_ACCESS_CHECKED | objectRef,tenantRef,ttlRef,result | No exponer URL firmada privada |
| EV09 | CI_CHECK_OBSERVED | commitSha,checkName,status,conclusion | CANCELLED/SKIPPED jamás PASS |
| EV10 | ROLLBACK_GATE_EVALUATED | planRef,checkpointRef,decision | No ejecutar revert por mero evento |
| EV11 | A5_REVIEW_OBSERVED | microId,authorTask,reviewerTask,decision | Autor distinto de revisor |
| EV12 | DISPATCH_REQUEST_BLOCKED | reason,scope,sourceSha | No crear Task/webhook/worker |
| EV13 | QUOTA_DECISION | scope,quota,usage,decision | Límite fail-closed, no doble consumo |
| EV14 | ERROR_RECORDED | errorCode,opaqueCorrId,UTC | Sin secretos/PII y sin retry automático |

Todas EV01–EV14 son nombres de diseño, no topics publicados, jobs o nuevas entradas de BITACORA. Los resultados NO_ACREDITADO permanecen como tales hasta lectura autorizada y evidencia causal.

## 4. Idempotencia, límites, concurrencia e integridad ID01–ID12

| ID | Invariante contractual | Negativo exigible |
| --- | --- | --- |
| ID01 | idempotencyKey = scope + eventType + sourceRevision + subjectRef | Cambio de tenant o revisión no reutiliza key |
| ID02 | una clave no produce dos efectos verificados | Replay no genera operación nueva |
| ID03 | correlationId opaco y limitado no incluye token | No correlacionar cross-tenant |
| ID04 | source SHA de 40 caracteres y estado causal | SHA divergente bloquea derivación |
| ID05 | owner y Task auditados por allowlist Javier | Task externa no reclama ni revisa |
| ID06 | A5 exige authorTask distinto de reviewerTask | Autoaprobación denegada |
| ID07 | unknown resource no se traduce a éxito | Falta de prueba significa NO_ACREDITADO |
| ID08 | evento no autoriza transición de estado | VALIDANDO no habilita M03 sucesora |
| ID09 | límite tenant y tipo antes de publicación | Quota inválida rechazada |
| ID10 | reintento requiere permiso vigente y prueba negativa | Cancelado no se relanza ciegamente |
| ID11 | se conservan eventos BITACORA previos | Nunca DELETE/UPDATE historial |
| ID12 | ningún sobre de evento acepta secreto/PII real | Payload sensible se censura |

Al implementar en otra microfase autorizada, la reserva distribuida requerirá CAS atómico real y ledger dedicado; simple leer y escribir una fila Sheets no constituye exclusión mutua transaccional. En caso de conflicto, no tomar owner ni atribuir efecto. Quotas deben diferenciar por tenant, tipo de evento y periodo; su valor vivo no se inventa.

## 5. Pruebas positivas/negativas futuras T01–T10

| Caso | Comportamiento positivo requerido | Comportamiento negativo obligatorio |
| --- | --- | --- |
| T01 | evento con sourceSha/owner exactos | sin sourceSha o tenant ajeno rechazado |
| T02 | misma clave replay se reconoce sin repetir | cambio concurrente crea duplicado bloqueado |
| T03 | correlationId no personal y acotado | token/correo incrustado se rechaza |
| T04 | A5 de Task distinta y SHA original | autoaprobación o SHA viejo bloqueado |
| T05 | CI completed/success exact-SHA | CANCELLED/SKIPPED/FAIL denegado |
| T06 | backup/restore demostrados por separado | sólo manifest no se cuenta como restore |
| T07 | quota vigente permite operación documental | cupo excedido implica NO_GO |
| T08 | BITACORA append-only e idempotente | edición histórica o repetición de EVENT_ID bloqueada |
| T09 | revisión de host/tenant firmada | host arbitrario atribuye A/B bloqueado |
| T10 | evento de transición no genera otra Task | dispatcher automático prohibido |

Los diez pares son FUTUROS_NO_EJECUTADOS. No se han probado HTTP, workers, backpressure, reintentos externos, mensajes reales, proveedores, MySQL ni tenancy en ejecución como parte de esta entrega. Deben permanecer NO_ACREDITADO cuando falte evidencia.

## 6. Compatibilidad, seguridad y condición de salida

El contrato conserva roles JWT/RBAC, aislamiento tenant A/B, secret scanning y propiedad de datos. No cambia la identidad física, agendas ni seis TASK_ID de Javier. EVENT_ID y clave de idempotencia de las acciones reales registradas deben preservarse sin reescribir BITACORA. C15-02.M02.06 es una tarea terminal distinta que exige cierres y A5 independientes de sus predecesores, no se declara habilitada por este texto.

**A0:** contrastar STANDARD, DAG G/O, M01.06 y modelos M02 anteriores; verificar allowlist y Scope Lock. **A1:** inspección positiva/negativa documental del sobre, EV01–14, ID01–12 y T01–10, sin atribuir tests reales. **A2:** revisión de riesgos tenant, JWT/RBAC, secretos y denegación por defecto sin cambios runtime. **A3:** consistencia conceptual con .NET 10, Angular 22.2.1, MySQL/EF 10 y la BITACORA existente. **A4:** commit fast-forward con diff sólo Markdown y check runs Scope Lock y procedencia V5 completed/success en SHA exacto, más readback institucional. **A5:** revisión posterior independiente por otra Task Javier. Ningún A5 significa autorización de QA/main/PROD.

Rollback del documento sólo mediante nuevo git revert normal y evento compensatorio append-only. Sin force-push, modificación de datos vivos, secretos, DNS, VPS, QA/main/PROD ni servidores externos.
