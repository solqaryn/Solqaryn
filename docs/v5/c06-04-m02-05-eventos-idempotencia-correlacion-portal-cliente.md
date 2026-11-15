# SOLQARYN V5 FINAL — C06-04.M02.05: eventos, idempotencia, correlación y límites del portal cliente

**Especificación INERTE; NO se implementa dispatcher, publicador, cola, worker, webhook, integración externa ni evento runtime.** Autor PRIMARY DEV 02 JAVIER_CHATGPT, TASK_ID 6af16bb9492c8191b8e8c7d606f11507; único repo solqaryn/Solqaryn en dev, Scope Lock STRICT. Fuente V5 FINAL (Doc Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), PLAN fila 984, COLA G/O SI, DAG fila 945 VALIDADO/SI. Predecesor M01.06 LISTO y A5 independiente; M02.01 define ENT01–ENT16 e invariantes I01–I16, M02.02 contratos API, M02.03 persistencia conceptual.

## 1. Propósito y fronteras de confianza

Una señal de dominio/evento propuesta expresa un cambio **ya validado** por el servidor y no confiere permiso nuevo al consumidor. Actor de cliente, tenant y CustomerId deben proceder del backend, con separación estricta de identidad CUSTOMER frente a ERP. Ningún Host, slug, query, EmpresaId, header o body del navegador constituye prueba de tenant, permiso o relación con pedido/reserva/documento. Eventos A nunca pueden leerse/actuarse en B; clientes A1 y A2 de A tampoco comparten contenido. No serializar JWT, challenge/token, contraseñas, email privado, direcciones ni documentos en payloads.

Toda operación que necesite consistencia debe escribir estado e idempotencia/outbox en una transacción autorizada y hacer handoff eventual después del commit; cualquier bus futuro debe tener presupuesto de cuotas, retry acotado, DLQ y auditoría antes de encenderse. **No se crea ningún servicio de eventos ahora.**

## 2. Doce contratos de señales hipotéticas (NO_IMPLEMENTADAS)

| Código | Nombre conceptual | Momento / condición servidor |
| --- | --- | --- |
| EVT01 | CustomerProfileUpdated.v1 | Perfil propio validado y commit + versión CAS |
| EVT02 | CustomerVerifiedContactChanged.v1 | Contacto confirmado sin filtrar email ni código |
| EVT03 | CustomerRecoveryChallengeRequested.v1 | Solicitud aceptada con respuesta neutra, sin token |
| EVT04 | CustomerRecoveryCompleted.v1 | Challenge consumido de forma atómica y sesiones revocadas |
| EVT05 | CustomerSessionRevoked.v1 | Identidad/tenant vinculados y revocación persistida |
| EVT06 | CustomerOrderVisibilityChanged.v1 | Referencia propia, no total de compra ni datos ajenos |
| EVT07 | CustomerReservationStatusChanged.v1 | Estado de servidor confirmado, sin prometer cupo |
| EVT08 | CustomerMembershipEligibilityChanged.v1 | Estado/versión de membresía, beneficios revalidados |
| EVT09 | CustomerDocumentGrantCreated.v1 | Grant opaco/TTL, sin URL pública ni documento |
| EVT10 | CustomerDocumentGrantRevoked.v1 | Grant invalidado y caches seguros purgados |
| EVT11 | CustomerPreferenceChanged.v1 | Consentimiento con versión validada, audit trail mínimo |
| EVT12 | CustomerPortalAccessDenied.v1 | Código seguro de denegación, nunca resource metadata ajena |

La lista es un vocabulario de diseño, no contratos publicados en un broker. No disparar notificaciones, correos, colas o webhooks desde el frontend por leerla.

## 3. Envelope y campos permitidos

| Campo conceptual | Regla de confianza futura |
| --- | --- |
| eventId | UUID opaco generado servidor, único por hecho persistido |
| eventType + schemaVersion | Allowlist estricta; desconocido se aisla/rechaza |
| tenantRef + customerRef | Resueltos del principal backend, no del navegador |
| actorRef + principalKind | CUSTOMER distinto de ERP, sin token ni correo |
| aggregateRef + aggregateVersion | Referencia opaca propia y control CAS del estado |
| occurredAtUtc | Generado servidor, no orden ni autoridad absoluta |
| operationCode + outcomeCode | Catálogo limitado, sin payload de PII |
| correlationId | ASCII saneado y longitud acotada; observabilidad, NO idempotencia |
| causationId | Referencia a operación/causa conocida, sin reutilización como grant |
| idempotencyKeyDigest | Hash de clave ligada a tenant+cliente+operación+fingerprint |
| policyVersion | Política real validada; el consumidor no puede elevar grants |
| payloadDigest | Evidencia no reversible de proyección mínima, sin secret |

Un consumidor siempre revalida la autorización y ownership al ejecutar una acción sensible; un evento antiguo no reemplaza membresía vigente ni A5. Marcar como no confiable cualquier mensaje externo no autenticado.

## 4. Diez reglas de deduplicación, entrega y límites FUTUROS

| ID | Regla |
| --- | --- |
| D01 | Unique idempotency scope TenantId+CustomerId+Operation+KeyDigest |
| D02 | Reintento mismo fingerprint retorna resultado estable sin efecto duplicado |
| D03 | Misma key con fingerprint distinto => rechazo 409 y auditoría mínima |
| D04 | Publicar solo después de transacción/commit, nunca antes de ownership |
| D05 | Consumidor deduplica eventId y version de aggregate; at-least-once asumido |
| D06 | Entrega desordenada o tardía no revierte versiones más recientes |
| D07 | Reintentos exponenciales acotados; agotados => cuarentena sin fuga |
| D08 | Cuota por tenant y operación, backpressure y control de volumen |
| D09 | CorrelationId saneado nunca concede permisos ni reemplaza idempotencyKey |
| D10 | Retención/borrado sólo por política aprobada, sin purga automática inventada |

Reglas de publicación de eventos, deduplicación, almacenamiento y retry son **requisitos de M03/M05**, no evidencia de sistemas activos. Prohibido aceptar código arbitrario, scripting, SQL, URL callback libre o secretos como configuración.

## 5. Ocho parejas de pruebas futuras FUTUROS_NO_EJECUTADOS

| ID | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| PN01 | Actualización de perfil A1 produce una sola señal tras commit | Retry duplicado no emite doble notificación |
| PN02 | A1 recupera sesión con token válido y consumido | Desafío A2/B o expirado no emite éxito |
| PN03 | Reserva A1 confirmada por servidor emite estado propio | Reserva ajena o sin cupo no emite confirmación |
| PN04 | Cambio de consentimiento A1 usa versión vigente | Flag no permitido o CAS obsoleto no emite |
| PN05 | Grant documento A1 revocado invalida acceso y cache | URL B o grant vencido no emite URL en payload |
| PN06 | IdempotencyKey+fingerprint repetidos dan efecto único | Misma key con fingerprint distinto da conflicto |
| PN07 | Desorden event v3 seguido de v2 conserva v3 | Mensaje antiguo no revierte revocación ni estado |
| PN08 | Fallo de proveedor activa retry acotado y auditoría | Error, cuota o timeout no se marca DELIVERY_SUCCESS |

Estas son ocho parejas de escenarios **no ejecutados**. Cuando M03/M05 sea autorizado, probar xUnit, integración transaccional MySQL 8.4 efímera, tenant A/B y A1/A2, falla parcial de proveedor, DLQ y retry; ningún test CI se infiere de este documento.

## 6. Arquitectura y protocolo de reversión

Domain define eventos inertes/versión; Application decide permisos antes de emitir; Infrastructure futura implementará outbox/consumidor sólo tras prueba y autorización; API no recibe eventId como grant ni expone secret; Angular puede escuchar estados sólo por endpoint autenticado, nunca modifica ownership. No se añade nueva frontera arquitectónica en esta entrega, y no se instala bus distribuido, Kafka/RabbitMQ, webhook, SMTP o Cloudflare.

Observabilidad futura: correlación sin PII, métricas de retraso, retry, dead-letter y fallos; cuota explícita por tenant, control de circuit breaker y protección de costes. Rollback de este cambio DOCUMENTAL: git revert normal, no force-push y BITACORA histórica intacta. Un futuro bus requerirá rollback probado de schema/version/outbox y estrategia de consumidor antiguo; no borrar eventos ya procesados.

## 7. Auditorías A0–A5

**A0:** Skill/canon, identidad Javier, HEAD, Scope Lock, PLAN H/I, COLA G/O y DAG F/G; claim exclusivo. **A1 documental:** doce tipos EVT, envelope y diez reglas, ocho parejas PN auditables, sin afirmar ejecución. **A2:** JWT/RBAC y tenancy A/B, A1/A2, idempotencia scope-bound, secrets fuera y fail-closed. **A3 documental:** compatibilidad con M02.01–04, Domain/Application/Infrastructure/API/Angular; runtime de outbox/MySQL/Node/Webhooks NO_APLICA por diff Markdown. **A4:** commit fast-forward con expected_sha y CHANGELOG autocontenido; verificar diff/blobs y checks exact SHA Scope Lock + procedencia V5 completed/success; BITACORA/PLAN/COLA/DAG readback. **A5:** Task Javier independiente distinta del autor, no autoaprobación ni QA/main/PROD.

**No implementar dispatcher ni interpretar el texto de automatizaciones ChatGPT como proceso server-side infinito.**
