# SOLQARYN V5 — C04-02.M02.05: eventos futuros, idempotencia, correlación y límites

**Autor:** PRIMARY DEV 06 — JAVIER (TASK_ID 6af16bbad26881918c283abe06e1f32a). **Fecha:** 2026-11-15. **Ámbito:** solqaryn/Solqaryn, dev, PROJECT_SCOPE_LOCK=STRICT. **Tipo:** documentación M02.05. Ninguna clase de evento, publicación, worker, webhook, nueva cola, migración o pago es implementado en esta microtarea.

## 1. Autoridad, alcance y baseline

Plan institucional autorizado STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, ID 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA; PLAN_MAESTRO fila 624, ID C04-02.M02.05, «Definir eventos, idempotencia, correlación y límites». Claim se realizó exclusivamente cuando PLAN H=PENDIENTE/I vacío, COLA G/O=SI y DAG_TECNICO F=VALIDADO/G=SI y fue cambiado por CAS con readback a EN_PROGRESO/DEV06. Las entregas M02.01–04 están LISTO_DEV documentales, A5 sigue siendo revisión independiente distinta del autor. No habilitar M02.06 ni M03 con DOCUMENTOS sin sus propios predecesores A5.

El servicio backend/src/Application/Services/SuscripcionesSaaSService.cs (blob bb501285f11cd4b32709a58a2c51efa19c3664ea) usa idempotency key y la capa Infrastructure repositories usa SuscripcionSaaSIdempotencia dentro de una transacción, según blob f3340ca687867d6dc2d99dfff5452bbe5c9bbb79. Este mecanismo existe **para onboarding de suscripción técnica**, no certifica deduplicación de cobros, renovación contractual, eventos comerciales o entregas distribuidas. La entidad Domain Suscripcion es técnica y no contiene aceptación jurídica versionada. Cualquier motor futuro deberá introducirse solo bajo autoridad comercial D01–D08 y gates de CI por SHA.

## 2. Doce eventos contractuales conceptualizados (EVT01–EVT12, todos inertes)

| Evento propuesto | Hecho causal que se exigiría en fase runtime | Restricción |
| --- | --- | --- |
| EVT01 ContratoBorradorCreado | Borrador autorizado confirmado | No firmado, no cobrado |
| EVT02 VersionContractualPropuesta | Versión nueva y padre válido | No sobrescribir términos aceptados |
| EVT03 AceptacionContractualRegistrada | Representación, consentimiento y D01/D02 aprobados | Nunca por checkbox UI sin prueba |
| EVT04 ContratoActivado | Condiciones aceptadas y política completa | Activar no prueba pago |
| EVT05 RenovacionPropuesta | Periodo posterior y D04 validada | No autorrenovar ni facturar |
| EVT06 ContratoSuspendido | Estado válido y actor con permiso | Mora nunca por timeout |
| EVT07 ContratoReactivado | Reconciliación y política aprobada | Cancelada es terminal |
| EVT08 CancelacionSolicitada | Solicitud trazable con actor autorizado | No refund automático |
| EVT09 ContratoCancelado | D07, motivos y retención verificados | Historial append-only |
| EVT10 ContratoExpirado | Periodo/UTC y política autorizada | Sin borrado físico |
| EVT11 ConciliacionReferenciada | Ledger autorizado con fuente verificable | Callback no firmado no tiene efecto |
| EVT12 IncidenteContractualDetectado | Fallo de gate, evidencia y correlación segura | No exponer datos de empresa B |

Estos doce **no son eventos implementados o emitidos**, ni habilitan outbox, transacciones nuevas, brokers, webhooks o scheduler. Campos y condiciones son contrato de diseño para revisión futura.

## 3. Envelope propuesto y controles de privacidad

Campos mínimos de diseño: SchemaVersion, EventId opaco, EventType allowlisted, OccurredAtUtc, EmpresaId verificada server-side, AggregateId, AggregateVersion, CorrelationId, CausationId opcional, ActorReference redacted, SourceRevision, IdempotencyFingerprint y PayloadHash. El contenido del payload debe reducirse a IDs opacos de versión/estado y jamás contener token JWT, cookies, secretos, tarjeta bancaria, precio/moneda no aprobados, términos legales completos o PII de otra empresa.

Toda relación entre envelope y agregado se verifica en backend, no por claims reconstruidos del cliente. El eventual consumidor no otorgará permisos; se deberá autenticar, aplicar RBAC de proceso/servicio, tenant A/B y política de retención D08, registrar causalidad y denegar mensajes desconocidos.

## 4. Idempotencia, correlación, replay y entrega futura

- **KEY01:** identidad durable por EmpresaId + tipo de operación + IdempotencyKey normalizada; payload hash determina equivalencia o conflicto.
- **KEY02:** EventId único y stable per event; reintentos del transporte no generan nueva decisión contractual.
- **KEY03:** Evento sólo se proyectará después de commit transaccional del cambio de estado, si algún día existe outbox aprobado y probado; publicar antes del commit está prohibido.
- **KEY04:** Concurrencia entre dos solicitudes sobre el mismo agregado requiere control de versión esperado y constraint durable compatible con MySQL 8.4.
- **KEY05:** No devolver PASS por timeout; reconsultar registro ganador o devolver estado incierto controlado.
- **KEY06:** CorrelationId propaga contexto causal entre API, service y ledger sin incluir tokens ni autorizaciones reutilizables.
- **KEY07:** CausationId solo encadena hechos autorizados; eventos no son instrucciones de ejecutar pagos.
- **KEY08:** Replay sin payload idéntico y dentro del mismo EmpresaId devuelve conflicto, no un nuevo resultado.
- **KEY09:** Un consumidor eventual deduplica con inbox durable por tenant+EventId; no suponer exactly-once desde red.
- **KEY10:** Retries exponenciales limitados y cuarentena no conceden autoridad; no reintentar cargo ciegamente.
- **KEY11:** Retención, purge y export de evidencia deben ser aprobados D08 antes de implementación.
- **KEY12:** Revisar firma/autenticidad de callbacks externos y contrato proveedor D06; en ausencia, denegar sin efectos.

Los controles son especificaciones inertes. Si se implementa transporte, se necesitará diseño arquitectónico separado aprobado, no añadir bus distribuido como derivación automática de esta tarea.

## 5. Límites de confianza, cupos y no efectos

El presupuesto de eventos por tenant, rate limits, tamaños de payload, retención, políticas de DLQ y alertas deben ser configurados por valores aprobados futuros; **no se inventan números comerciales**. Los registros de auditoría contienen mínima metadata y nunca autorizan lectura/escritura por sí mismos. Nadie podrá derivar permiso comercial a partir de estado Plan, feature flags, licencia o un webhook sin revisión. Un callback fallido o inalcanzable no significa impago o morosidad.

Rechazar cross-tenant en cada frontera; no aceptar EmpresaId, Rol o PrincipalId del body. Una empresa A nunca observa eventos, contratos o correlaciones de B. Un actor revocado no es rehabilitado por un reintento de evento.

## 6. Doce pares P/N FUTUROS (NO ejecutados)

| Positivo por implementar y probar | Negativo que deberá rechazarse |
| --- | --- |
| P01 Borrador autorizado emite evento después de persistencia | N01 emitir sin commit de negocio |
| P02 Versión monotónica correlacionada | N02 volver a emitir versión obsoleta |
| P03 Aceptación con consentimiento D01/D02 | N03 aceptación sin prueba de autoridad |
| P04 Activación tras gates | N04 activar por entitlement UI |
| P05 Propuesta renovación sin cobro | N05 autorrenovar con D04 sin aprobar |
| P06 Suspensión causal | N06 morosidad inferida de timeout |
| P07 Reactivación solo desde suspendida | N07 reactivar cancelada/expirada |
| P08 Cancelación con historial preservado | N08 borrar efectos anteriores |
| P09 Replay mismo payload es idempotente | N09 misma clave diferente payload |
| P10 Colisión concurrente un ganador | N10 duplicar contrato, cargo o evento |
| P11 Evento de empresa A aislado | N11 usar EventId de B o correlationId de B |
| P12 Callback firmado y policy aprobada | N12 callback anónimo altera estado/ledger |

**P01–P12 y N01–N12 son casos de aceptación pendientes de codificar y ejecutar.** No declarar xUnit, Category=Integration MySQL, broker, API, Playwright ni transportes como PASS por esta especificación.

## 7. Matriz de validación y rollback

**A0:** contraste con fuente V5, PLAN, COLA, DAG, servicio/repo de onboarding reales y límites de los diseños M02 anteriores; un documento nuevo y CHANGELOG. **A1:** coherencia documental de doce tipos, doce reglas y doce pares P/N, todos FUTUROS. **A2:** tenant A/B, JWT/RBAC, firma de callback, idempotencia, PII y secretos explícitamente fail-closed. **A3:** compatibilidad conceptual con Domain/Application/API/Infrastructure sin modificar runtime; tests .NET/MySQL/Angular/HTTP NO_APLICA por diff Markdown, no PASS. **A4:** commit serial dev fast-forward/CAS y verificaciones exact-SHA Scope Lock + procedencia V5 completed/success; PLAN/COLA/BITACORA readback. **A5:** revisión independiente exclusiva de otra Task Javier.

Rollback actual: git revert normal del documento/CHANGELOG. Ninguna reversión de base de datos, cobro o evento está autorizada por este documento. No alterar QA/main/PROD, migraciones, proveedor, secretos ni scheduling.
