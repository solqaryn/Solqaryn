# SOLQARYN V5 — C07-04.M02.05 eventos conceptuales, idempotencia, correlación y límites de pedidos/pagos

**Autor:** PRIMARY DEV 06, TASK_ID 6af16bbad26881918c283abe06e1f32a · **Fecha:** 2026-11-14 America/Tegucigalpa · **Repositorio:** solqaryn/Solqaryn@dev · **PROJECT_SCOPE_LOCK=STRICT**.

## Preflight y alcance exclusivo

STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA, PLAN_MAESTRO C07-04.M02.05 (fila 1128), COLA G/O=SI y DAG F=VALIDADO/G=SI antes del claim. M01.01–06 LISTO con A5 independiente. Los diseños M02.01–04 no certifican automáticamente un evento, pago o release.

**Todo este archivo es un contrato NO_IMPLEMENTADO.** No se emiten eventos, instalan webhooks/colas/servicios, ejecutan pagos, callbacks, PSP, migraciones, Workers, reintentos, Tasks ni cambios runtime. No se crean ni se usan secretos, y no se tocan QA/main/PROD. Un evento conceptual no es prueba de ejecución. Se preserva el modelo Domain → Application → Infrastructure → API, JWT/RBAC y aislamiento tenant A/B.

## 1. Eventos conceptuales EV01–EV14: NO_EMITIDOS

| ID | Señal lógica propuesta | Emisor/fuente e idempotencia exigidas |
| --- | --- | --- |
| EV01 | CotizacionCreada | Cambio confirmado de borrador propio; clave scoped tenant |
| EV02 | CotizacionEnviada | Transición de estado autorizada y auditada |
| EV03 | CotizacionAceptada | Una aceptación por versión con actor verificado |
| EV04 | CotizacionRechazada | Motivo y actor, sin información de otro tenant |
| EV05 | PedidoDerivado | Referencia única a cotización; replay no duplica pedido |
| EV06 | PedidoConfirmado | Pedido/cliente/stock y permisos coherentes |
| EV07 | ReservaStockConfirmada | Una reserva por clave de operación y transacción |
| EV08 | FacturaEmitida | Emisor fiscal y correlativo autorizados, snapshot protegido |
| EV09 | PagoSolicitado | PSP aprobado; no incluye tarjeta ni secreto |
| EV10 | PagoConciliado | Confirmación idempotente verificada, saldo coherente |
| EV11 | PagoRechazado | Motivo redactado, sin cambio a cobrado |
| EV12 | DocumentoAnulado | Permiso y reversión auditable sin hard-delete |
| EV13 | DocumentoExportado | Acceso propio y TTL/revocación de enlace |
| EV14 | CompensacionRegistrada | Reconciliación ante fallo parcial, sin cobro nuevo |

EV09–EV11 **no autorizan** ningún PSP, integración económica real ni webhook. El evento es una proyección de una transición durable futura, nunca el disparador por sí solo de una transición no confirmada.

## 2. Contrato conceptual CT01–CT14, sin DTO runtime

- CT01 eventId: identificador estable único, sin PII.
- CT02 eventType: tipo versionado allowlisted.
- CT03 schemaVersion: semver compatible y validación strict.
- CT04 aggregateId: documento propio o referencia opaca verificada.
- CT05 aggregateType: cotización, pedido, factura, pago o compensación.
- CT06 tenantScope: identidad derivada del servidor, no payload externo.
- CT07 originRevision: referencia a estado/version confirmado.
- CT08 occurredAtUtc: UTC monotónico auditable; no hora cliente.
- CT09 actorRef: actor autorizado con datos redactados.
- CT10 correlationId: correlación de operación/API/DB.
- CT11 causationId: origen lógico sin bucles de notificaciones.
- CT12 idempotencyFingerprint: hash canónico de payload permitido.
- CT13 outcomeCode: resultado seguro sin secreto o PAN/CVV.
- CT14 evidenceRef: prueba de commit, transacción, autorización y revisión externa.

No incorporar JWT, claves de API, números de tarjeta, correo/RTN completo ni PII no necesaria. No serializar una entidad EF sin proyección explícita. Validar pertenencia de cada objeto y jerarquía de permisos A/B antes de cualquier publicación de eventos.

## 3. Reglas de idempotencia ID01–ID12

1. ID01 Key compuesta tenant verificado + tipo operación + clave externa normalizada.
2. ID02 Fingerprint incluye campos de efecto (importe, documento, moneda y versión) con canonicalización definida.
3. ID03 Repetición idéntica devuelve resultado estable sin nuevo pago, factura o stock.
4. ID04 Misma key con fingerprint divergente devuelve conflicto, no segundo efecto.
5. ID05 Versionado de esquema no permite reejecutar transacciones antiguas.
6. ID06 Unicidad de deduplicación debe ser durable y probada con MySQL efímero antes de implementar.
7. ID07 No confiar en memoria local de una Task o en SHEETS como cerradura transaccional distribuida.
8. ID08 Retries sólo para fallo transitorio clasificado y política/cuota vigente; sin bucle ilimitado.
9. ID09 Mensajes fuera de orden no retroceden estado financiero o fiscal.
10. ID10 Compensación no genera un segundo cobro automático.
11. ID11 Payload de tenant B no puede compartir key/resultado con tenant A.
12. ID12 Audit log append-only registra conflicto de key sin secretos.

## 4. Límites y recuperación LM01–LM12

| ID | Límite obligatorio | Rechazo/recuperación |
| --- | --- | --- |
| LM01 | Máximo de eventos/reintentos por operación con cuota autorizada | No iniciar despacho si excede |
| LM02 | Payload dimensionado y redactado | Rechazar datos excesivos o inseguros |
| LM03 | Límite temporal y monotonicidad de transición | No aplicar eventos obsoletos |
| LM04 | Límite tenant/empresa/sucursal/actor | Denegar cross-tenant antes de colas |
| LM05 | Política de estados y expectedVersion | Rechazar transición conflictiva |
| LM06 | Idempotencia durable | Rechazar key usada con fingerprint distinto |
| LM07 | Autenticidad de callback externo | Sin firma/PSP autorizado, no aplicar efectos |
| LM08 | Pago y factura: atomicidad de efectos | En fallo conservar estado y registrar compensación |
| LM09 | Reconciliación de stock/caja/saldo | No confirmar parcialidad silenciosa |
| LM10 | Retención y privacidad | No persistir secretos o PAN/CVV en outbox/log |
| LM11 | Sin eventos servidores actualmente | Diseño no despierta otra Task fuera de agenda |
| LM12 | Gate independiente A0–A5 + DAG + CI | VALIDANDO/A5 pendiente no libera sucesora |

## 5. Pares positivos y negativos PN01–PN12 — FUTUROS_NO_EJECUTADOS

| ID | Positivo futuro | Negativo obligatorio |
| --- | --- | --- |
| PN01 | Conversión cotización → pedido emite un evento único | Replay mismo key no crea dos pedidos |
| PN02 | Actor autorizado genera factura propia | Factura tenant B no es visible desde A |
| PN03 | Pago confirmado produce saldo conciliado una sola vez | Doble callback no hace doble cargo |
| PN04 | Reembolso autorizado genera compensación trazable | Reembolso sin permiso no genera efecto |
| PN05 | Reserva stock confirma una sola reducción | Carreras no venden stock duplicado |
| PN06 | Mensaje firmado coincide con PSP habilitado | Firma incorrecta no cambia estado |
| PN07 | Correlación sigue una transacción propia | CorrelationId de B no revela datos a A |
| PN08 | Evento serializado no contiene secretos | Campo JWT o PAN/CVV se rechaza |
| PN09 | Transición válida usa expectedVersion correcta | Versión obsoleta falla cerrado |
| PN10 | Evento tardío queda registrado sin retroceso | Fuera de orden no vuelve pago a cobrado |
| PN11 | Falla transitoria permite reintento acotado | Fallo permanente/cuota agotada no hace bucle |
| PN12 | Estado durable permite recuperación auditable | Crash no simula PASS ni borra historial |

Todos los PN son **futuros NO_EJECUTADOS** y requieren test xUnit/integración MySQL/HTTP/seguridad por SHA causal antes de configurar runtime. No existe dispatcher ni ejecución de webhooks bajo esta orden.

## 6. Dependencias, auditorías y rollback

M02.06 es terminal y exige M02.01–05 LISTO+A5 independiente para liberación de su propio DAG; documentar estas reglas no satisface M03 ni QA/main/PROD. D01–D08 decisiones comerciales/fiscales de M01 siguen abiertas.

**A0:** verificar fuente, 14 EV, 14 CT, 12 ID, 12 LM y 12 pares PN; **A1:** revisar positivos/negativos como contrato, NO_EJECUTADOS; **A2:** preservar JWT/RBAC, tenant A/B, privacidad, firma PSP y efectos económicos; **A3:** coherencia con M01, arquitectura y código Git, runtime .NET/MySQL/HTTP NO_APLICA para dos Markdown; **A4:** commit fast-forward CAS en dev, Scope Lock + V5 CHANGELOG exact-SHA completed/success y readbacks PLAN/COLA/DAG/BITACORA; **A5:** revisión real de otra Task Javier distinta del autor.

**Rollback documental:** git revert normal del commit y compensación append-only en BITACORA, sin force-push/reset. No instalar callbacks, eventos, scheduled Tasks, colas, proveedores ni permisos nuevos.
