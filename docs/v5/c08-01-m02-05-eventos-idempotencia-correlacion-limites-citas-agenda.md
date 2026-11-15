# SOLQARYN V16 — C08-01.M02.05: eventos, idempotencia, correlación y límites de citas y agenda

**ID:** C08-01.M02.05; **PLAN_MAESTRO:** fila 1164; **ejecutor:** PRIMARY DEV 05 Javier, TASK_ID \`6adc2e55f47c81919f6374e561687076\`. Repositorio \`solqaryn/Solqaryn\`, rama \`dev\`, \`PROJECT_SCOPE_LOCK=STRICT\`. **V15:** R0 DOCUMENTAL, cero código o eventos emitidos.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`) y estados actuales PLAN/COLA/DAG. Resultado requerido de la unidad: **definir eventos, idempotencia, correlación y límites**. La fase C08-01 trata reservas de citas/horarios, disponibilidad, personal, recursos y cancelaciones; no sustituye \`ReservaInventario\` ni autoriza un proveedor comercial.

## 1. Precedentes y separación estricta

- C08-01.M01.06: dieciséis gates G01–G16 y decisiones D01–D08 todavía pendientes. No hay autorización para poner agendas en vivo, modificar ERP, establecer una cola, crear webhooks o tocar MySQL.
- C08-01.M02.01: E01–E14/INV01–INV18/REL01–REL12, entidades y reglas de pertenencia conceptuales.
- C08-01.M02.02: OP01–OP12/DTO01–DTO12/ERR01–ERR14/VAL01–VAL18, contratos de aplicación/HTTP **futuros**.
- C08-01.M02.03: P01–P12, IDX01–IDX12, MIG01–MIG08, fixtures F01–F12 **FUTUROS_NO_EJECUTADOS**, sin tablas ni DDL.
- C08-01.M02.04: OPT01–OPT18, FL01–FL12, VAL01–VAL16, todos flags de seguridad **propuestos**, con funciones OFF; no configuración runtime.

**Frontera:** Domain declara invariantes de eventos; Application decide transiciones e idempotencia; Infrastructure futura podría realizar persistencia/outbox transaccional en MySQL; API futura autenticaría y publicaría la intención mínima. No se introduce bus, broker, scheduler, event dispatcher, queue, webhook, clase C#, endpoint, migración, suscripción, worker o credencial. La salida de este documento no prueba que la infraestructura de idempotencia exista.

## 2. Intenciones de evento futuras EV01–EV12 (NINGUNA EMITIDA)

| ID | Intención semántica candidata | Condición de confirmación y privacidad |
| --- | --- | --- |
| EV01 | AgendaServicioHabilitado | política aprobada, rol/tenant verificados y versión |
| EV02 | AgendaProfesionalDisponibilidadCambiada | actor autorizado y profesional activo de la misma sede |
| EV03 | AgendaRecursoDisponibilidadCambiada | recurso y capacidad tenant-scoped, sin PII |
| EV04 | AgendaBloqueoCreado | bloqueo horario validado, trazabilidad mínima |
| EV05 | AgendaBloqueoRetirado | versión CAS y no prometer citas retroactivas |
| EV06 | CitaSolicitada | intención pendiente, nunca confirmación anticipada |
| EV07 | CitaConfirmada | solo después de transacción DB y exclusión de slot real |
| EV08 | CitaConfirmacionRechazada | conflicto/permiso/versión, sin filtrar agenda ajena |
| EV09 | CitaReprogramada | slot previo/nuevo intercambiados de forma atómica |
| EV10 | CitaCancelada | transición terminal idempotente y liberación única |
| EV11 | CitaExpirada | expiración con prueba de propietario/estado/versionado |
| EV12 | AgendaEntregaNotificacionSolicitada | intención redacted, no envío SMTP o webhook certificado |

**EV01–EV12:** propósitos de diseño; eventos reales publicados = **0/12**, métricas de entrega/replay **NO_MEDIDO**. \`CitaConfirmada\` no puede emitirse por cálculo de disponibilidad, respuesta UI ni callback externo sin commit causal. El evento no cambia existencia o saldos de \`ReservaInventario\`.

## 3. Idempotencia futura IDEM01–IDEM14

| ID | Regla que deberá probarse antes de runtime |
| --- | --- |
| IDEM01 | IdempotencyKey se interpreta dentro de EmpresaId y actor backend verificados |
| IDEM02 | Un comando/acción forma parte de la clave lógica y evita colisión |
| IDEM03 | Payload canonizado se compara por fingerprint estable; no guardar PII raw |
| IDEM04 | La misma clave y fingerprint devuelve un resultado único y recuperable |
| IDEM05 | Misma clave con payload distinto rechaza con error seguro, sin mutación |
| IDEM06 | Otra empresa, sucursal o actor jamás hereda un resultado previo |
| IDEM07 | Reserva confirmada requiere compromiso MySQL de todas las ocupaciones |
| IDEM08 | Conflicto concurrente evita doble booking de profesional y recursos |
| IDEM09 | Transición/version CAS impide volver de estado terminal |
| IDEM10 | Reprogramación fallida mantiene el slot anterior y no libera capacidad |
| IDEM11 | Cancelación repetida no libera recursos dos veces |
| IDEM12 | Retry de publisher no repite efecto de consumidor; dedupe durable futuro |
| IDEM13 | TTL/retención deben definirse por política aprobada, no arbitrariamente |
| IDEM14 | Fallo entre commit y publicación requiere reconciliación/outbox futura, no inventar éxito |

Un \`lock\` local, una prueba mock o un BUILD PASS no acredita IDEM07–IDEM14. Antes de cualquier implementación R2 se requieren xUnit reales, JWT/RBAC/tenant A/B, rechazo por revocación y migraciones efímeras MySQL 8.4 con pruebas de **64 solicitudes concurrentes** y rollback. Outbox/inbox se consideran opciones futuras, no autorización para instalarlos.

## 4. Correlación, minimización y límites COR01–COR12

| ID | Campo/política futura | Límite o regla de redacción |
| --- | --- | --- |
| COR01 | CorrelationId | aleatorio/scoped, no identificador de persona |
| COR02 | CausationId | vincula comando-evento sin registrar token |
| COR03 | CommandId | único por intento tenant/actor/acción |
| COR04 | EventId | estable por efecto comprometido |
| COR05 | TenantScopeHash | nunca exponer EmpresaId ajeno al consumidor no autorizado |
| COR06 | SucursalScope | validar pertenencia en backend y minimizar payload |
| COR07 | AggregateVersion | versión monotónica para descartar eventos antiguos |
| COR08 | SchemaVersion | compatibilidad incremental; consumidores desconocidos NO_GO |
| COR09 | OccurredAtUtc | UTC verdadero desde autoridad confiable; no usar hora local del browser |
| COR10 | RetryAttempt | contador acotado, no reintento infinito ni busy loop |
| COR11 | RetentionPolicy | ventana aprobada, borrado seguro y evidencia sin PII |
| COR12 | AuditOutcome | éxito/rechazo/error correlacionado; secretos, JWT y teléfonos censurados |

Toda zona IANA de sede, interpretación UTC, DST ambiguo/inexistente y offset se validará antes de crear reservas. El correlator por sí solo no concede autorización. Telemetría de eventos y notificaciones no puede ser base para filtrar tenants de frontend ni asumir que un mensaje fue entregado. Políticas de retries, tamaño de payload, tasa y expiración quedan **sin cifra final** hasta decisión D01–D08.

## 5. Fronteras de error y procesamiento (diseño, no implementación)

1. Una solicitud nueva recibe un ID causal y solo entra a la fase de confirmación si identidad, rol, membresía, sede, recurso, política y horario se acreditan.
2. La ventana es un instante único UTC + IANA de la sede; DST duplicado sin offset inequívoco o instante local inexistente genera rechazo, no reserva optimista.
3. La disponibilidad leída es **proyección**, no lease. En el mismo contexto transaccional futuro se revalida profesional, recursos, intervalos, buffers y versión, con orden determinista de bloqueos y rollback total.
4. Se registra el resultado idempotente y el evento de intención commit-consistente. Un error de publicación posterior puede requerir reconciliación, no crea otra cita.
5. Si entrega externa falla, se limita retry por cuota y política; las APIs nunca presentan éxito no confirmado ni filtran PII en errors/logs.
6. Cancelación o reprogramación comprometida modifica ocupación una sola vez; reintentos repetidos devuelven resultado seguro, sin estados terminales reversibles por accidente.
7. Una orden de desactivar agenda no borra citas existentes ni historial; conservar reversión y auditoría proporcional.
8. Cualquier interacción con inventario/finanzas/costos o proveedor externo es **NO_GO** sin scope, autorización y pruebas R2 separados.

## 6. Oráculos positivos/negativos futuros T01–T12 (NO_EJECUTADOS)

| ID | Positivo FUTURO | Negativo/fallo FUTURO |
| --- | --- | --- |
| T01 | Tenant A crea evento de política autorizada | token de B no puede leer/eventar política A |
| T02 | Cita pendiente genera intención, no confirmación | UI optimista o timeout no generan EV07 |
| T03 | Cita con profesional/recurso libres confirma una vez | 64 intentos simultáneos no duplican slot |
| T04 | Replay de mismo tenant/actor/comando/fingerprint retorna mismo resultado | key repetida con payload/tenant diferente rechaza |
| T05 | Horario UTC/IANA confirmado con offset válido | DST ambiguo o inexistente sin decisión se rechaza |
| T06 | Cancelación legítima libera capacidad una vez | dos cancelaciones no liberan doble |
| T07 | Reprogramación con CAS cambia slot atómicamente | error intermedio conserva slot original |
| T08 | Publisher reintenta evento commit-consistente | duplicado no produce segundo envío/efecto |
| T09 | Profesional autorizado visualiza mínimo | rol revocado o miembro B no lee agenda A |
| T10 | Recurso múltiple confirma todo junto | un recurso bloqueado aborta todos |
| T11 | Logs de correlación muestran IDs sintéticos mínimos | JWT, PII, teléfono y secretos jamás aparecen |
| T12 | Reserva de citas permanece separada del ERP | reintentos no cambian stock/\`ReservaInventario\` |

**Cobertura:** 12 pares escritos, 0/12 ejecutados; concurrencia, entrega efectiva, MySQL, HTTP, browser, SLO, p95, costos y tasas de retry **NO_MEDIDO**. Sólo usar fixtures sintéticos A/B; nunca datos de usuarios reales o infra productiva. Los eventos futuros requieren pruebas negativas y de integración de componente.

## 7. Pruebas, autorización, readback y reversión

- **A0:** se coteja unidad M02.05 contra Standard V5/PLAN/COLA/DAG, arquitectura Domain <- Application <- Infrastructure, y contratos M01/M02; diff un documento y \`CHANGELOG_AI.md\` únicamente.
- **A1:** revisar que EV01–12, IDEM01–14, COR01–12 y T01–12 cubran positivo/negativo, error, replay, CAS, DST y rollback; son requisitos contractuales, NO tests ejecutados.
- **A2:** inspección estática JWT/RBAC/revocación, aislamiento A/B, IDOR/BOLA, PII, recursos/versiones y operación fail-closed. No declarar prueba de seguridad runtime.
- **A3:** cotejo documental con M02 y Standard V5; .NET, MySQL, Angular, Playwright, HTTP, SMTP, broker y notificaciones **NO_APLICA** al diff puramente documental.
- **A4:** un commit fast-forward/CAS en dev con dos Markdown, checks exact-SHA Scope Lock y V5/procedencia \`completed/success\`, transiciones BITACORA y readback PLAN/COLA/DAG.
- **A5:** revisión independiente de otro TASK_ID Javier, distinto de DEV05, con SHA/diff/criterios y CI pertinente; LISTO_DEV no implica A5_PASS.

**Rollback:** únicamente \`git revert\` normal luego de verificar HEAD, exclusividad y efecto sobre cambios posteriores. No reescribir historia, forzar gate G/O, borrar BITACORA o tocar QA/main/PROD. Esta documentación no activa eventos, workers, dispatcher, colas, sistemas de cita ni proveedores.
