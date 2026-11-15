# SOLQARYN V5 — C08-02.M02.05 — Contrato de eventos, idempotencia y correlación para restaurante

**Autor:** PRIMARY DEV 06 JAVIER (TASK_ID `6af16bbad26881918c283abe06e1f32a`). **Fecha:** 2026-11-15. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL; PLAN_MAESTRO C08-02.M02.05 (fila 1200). **Riesgo R0:** contrato documental sin efecto de ejecución. **Rama:** `solqaryn/Solqaryn@dev`; Scope Lock STRICT.

## 1. Alcance y dependencias

Proponer eventos futuros y reglas de idempotencia, correlación, límites y privacidad para mesas, QR, menú, pedidos internos y autoservicio. Esta entrega **NO** implementa mensajes, bus, dispatcher, webhooks, brokers, workers, colas, endpoints, tablas MySQL, migraciones, JWT, PSP ni pagos. No modifica el ERP, las reglas de precio/IVA/stock ni responsabilidades de caja.

Como antecedentes se comprobaron C08-02.M01.01–06 en LISTO con A5 independiente. M02.01 (modelo con ownership e invariantes) y M02.03 (persistencia conceptual) están documentadas. M02.02 y M02.04 son contratos independientes cuya autoría/owner no se modifica aquí. Al reclamar bajo V17, COLA G/O=SI y DAG F=VALIDADO/G=SI fueron condición real por ID, no autoridad sobre las sucesoras M03+. A5 independiente de esta M02.05 seguirá pendiente.

## 2. Catálogo de eventos conceptuales — EV01–EV16

| Evento propuesto | Agregado/actor | Carga mínima futura | No exponer / rechazo |
| --- | --- | --- | --- |
| EV01 MesaReferenciaResuelta | Mesa/visita pública | Tenant/sede/mesa opaca, version, correlationId | Token QR íntegro y PII |
| EV02 QrRechazado | Validador de alias | Motivo saneado y política de tasa | Diferencia observable de tenant B |
| EV03 SesionMesaIniciada | Sesión autorizada | SesionId opaco, mesa, expiración | JWT completo, secreto QR |
| EV04 SesionMesaRevocada | Revocación | Versión y motivo mínimo | Sesión B en ámbito A |
| EV05 MenuVersionPublicada | Catálogo/sucursal | Versión pública, fecha, sede | Producto oculto, costo interno |
| EV06 MenuVersionRetirada | Catálogo/sucursal | Versión revocada, fecha | Reexposición de menú antiguo |
| EV07 PedidoCreado | Pedido | PedidoId, estado, versión, fingerprint | Total cliente como autoridad |
| EV08 LineaPedidoAgregada | Pedido | LineaId, producto ERP referencial, versión | Precio manipulado y PII |
| EV09 PedidoEnviadoCocina | Transición | Estado/versión, destino interno | Cocina de tenant B |
| EV10 PreparacionActualizada | Cocina | Estado autorizado y versión | Rol revocado o retorno a terminal |
| EV11 PedidoCancelado | Pedido | Estado terminal, motivo saneado | Reapertura o doble compensación |
| EV12 CierrePedidoConfirmado | ERP/caja | Referencia ERP, versión final, operación | Pago sin conciliación real |
| EV13 CierrePedidoRechazado | ERP/caja | Código seguro y correlationId | Factura/stock de cliente ajeno |
| EV14 IdempotenciaConflictiva | Operación | KeyHash truncado, fingerprint y resultado | Key/secret completo |
| EV15 VersionObsoletaRechazada | Concurrencia | Agregado, versión esperada/actual saneada | Pérdida de escritura |
| EV16 ConciliacionPendiente | ERP | Operación, estado pendiente y retryBudget | Exponer PSP, monto o secreto |

Cada evento es una **propuesta futura NO_EMITIDA**. Evento "confirmado" sólo puede producirse después del commit de transacción y conciliación canónica. Nunca publicar evento de éxito desde la UI, una intención de pago o una respuesta de proveedor todavía inconclusa.

## 3. Envoltura futura y privacidad

```text
EventEnvelopeV1 {
  eventId: UUIDv7 u opaco generado en servidor;
  eventType: allowlist EV01..EV16;
  schemaVersion: 1;
  occurredAtUtc: ISO8601 UTC;
  empresaRef: identificador de tenant validado por servidor;
  sucursalRef: identificador de sede validado;
  aggregateRef: opaqueId;
  aggregateVersion: long positiva;
  correlationId: opaco;
  causationId: opaco opcional;
  actorRef: principal mínimo pseudonimizado;
  idempotencyFingerprint: hash no reversible;
  outcome: enum seguro;
  payload: allowlist estricta y versionada;
}
```

El envelope nunca debe transmitir JWT, refresh token, código QR original, tarjeta, PAN, contraseña, correo privado, teléfono o identificadores de otra empresa. El consumidor **no** debe tratar un evento como permiso de lectura/escritura y revalida tenant/sucursal/recurso/rol. Registrar correlación sana sin payload bruto; definir retención y borrado conforme al contrato posterior aprobado.

## 4. Invariantes de idempotencia, orden y aislamiento — INV01–INV16

| Invariante | Rechazo o frontera obligatorio |
| --- | --- |
| INV01 | Un evento pertenece a un tenant y sede del servidor, nunca a un ID recibido por QR |
| INV02 | `eventId` y (tenant, evento, aggregate, version) evitan doble consumo lógico |
| INV03 | Reentrega idéntica produce mismo efecto, no doble pedido/stock/factura |
| INV04 | Misma clave idempotente con fingerprint cambiado genera conflicto, no overwrite |
| INV05 | Key de operación se limita por tenant, sede, actor/sesión, acción y TTL |
| INV06 | Mensaje de B entregado al consumidor A se rechaza antes de cualquier side effect |
| INV07 | Evento de rol revocado no autoriza modificación aunque el caché diga permitido |
| INV08 | Reordenación de mensajes no reabre estado terminal ni rebaja versión |
| INV09 | Escritura concurrente usa CAS/versión en fuente autoritativa y máximo un ganador |
| INV10 | Cierre sólo tras commit y conciliación ERP; no emitir confirmado por intento |
| INV11 | Fallo parcial aplica compensación/rollback observable e idempotente |
| INV12 | Tiempo del cliente no sustituye TTL UTC ni revocación validada por servidor |
| INV13 | Retención de PII/token en envelope/log debe ser cero; solo referencias saneadas |
| INV14 | Consumidor no ejecuta lógica económica si falta autorización y estado canónico |
| INV15 | Reintentos son limitados con backoff y estado de cuarentena, sin bucle infinito |
| INV16 | Feature flag apagado mantiene rutas antiguas; integración no despliega por documento |

## 5. Contratos de entrega y límites operativos futuros

- **Entrega:** al menos una vez podría ser aceptable únicamente con consumidores verdaderamente idempotentes; **no** prometer entrega exactamente una vez entre DB/broker/PSP. Un outbox transaccional se evaluará en una etapa implementable específica, no se crea aquí.
- **Orden:** preservar orden por agregado/versión donde el caso de uso lo exija; si llega versión 7 antes de 6, rechazar/retener hasta reconciliar, nunca inferir estado nuevo sin fuente de verdad.
- **Errores y retries:** definir resultado `RETRYABLE`, `TERMINAL` y `QUARANTINED` con presupuesto finito, observabilidad y manejo de redelivery, sin trabajador perpetuo ni reintentos automáticos de Tasks.
- **Límites de consumo:** tamaño máximo de evento, frecuencia por tenant, cuota por sede, rate limit de QR, TTL y presupuesto de reintentos se especificarán por pruebas de capacidad, no inventar cantidades aquí.
- **Superficie privada:** sólo actor autenticado, rol, membresía, sede y estado vigentes permiten mutaciones; QR público no equivale a JWT ni rol. No permitir que consumidor de tenant A lea estado B.
- **Rollback y compatibilidad:** versionar esquema, soportar lectores compatibles durante transición y revertir rutas mediante feature flag/commit no destructivo; cualquier cutover real queda R3 y NO_GO sin orden expresa.

## 6. Matriz futura de aceptación positiva/negativa — P/N01–12

| ID | Positivo futuro | Negativo/frontera futura |
| --- | --- | --- |
| PN01 | Pedido creado A emite una intención canónica tras commit | Tenant B no recibe evento A |
| PN02 | Reintento idéntico de creación converge | Key igual con payload distinto se rechaza |
| PN03 | Cocina A recibe versión válida | Cocina B y rol revocado no consumen/alteran |
| PN04 | Pedido avanza de versión n a n+1 | Versión obsoleta o desordenada no altera estado |
| PN05 | Cierre ERP confirmado produce evento único | Replay/cierre paralelo no duplica factura |
| PN06 | Fallo parcial deja compensación rastreable | No emitir confirmado mientras operación pendiente |
| PN07 | QR A válido genera señal saneada | QR falso/B/expirado no revela mesa/tenant |
| PN08 | Revocación invalida sesión futura | Caché obsoleta no reautoriza |
| PN09 | Retries limitados finalizan correctamente | Mensaje venenoso no ejecuta loop infinito |
| PN10 | Logs contienen correlación opaca | Token, PAN o PII deben estar ausentes |
| PN11 | Migración de esquema compatible mantiene lector | Versión desconocida rechazada sin efectos |
| PN12 | Feature desactivada preserva operación vigente | Documento/evento no activa ruta ni pagos |

**Estado de P/N01–12: NO_EJECUTADOS, cero pruebas runtime en este scope.** Un CI posterior sobre MySQL EF, API/JWT, Angular o broker exige pruebas reales y rechazo adversarial. Un documento no certifica MySQL 8.4, colas ni idempotencia efectiva.

## 7. Impacto, auditorías y rollback de esta microtarea

A0: revisar autoridad institucional, scope R0, 16 contratos EV, 16 invariantes, 12 pares de escenarios, predecesores y diff. A1: revisar consistencia y casos negativos de diseño, no runtime. A2: diseño anti-IDOR tenant A/B, QR falso, revocación, replay, privacidad y cierre una sola vez. A3: MySQL/.NET/HTTP/Angular/Playwright/broker **NO_APLICA por diff solo Markdown**. A4: publicación individual en dev, Scope Lock/V5 `completed/success` por SHA exacto o equivalencia verificada, readbacks PLAN/BITACORA y CHANGELOG autocontenido. A5: otra de las cinco Tasks Javier, distinta de esta Task autor, revisa y puede rechazar; no autoaprobación.

Impacto actual: Domain/Application/Infrastructure/API/UI, DB, red, secretos, inventario, dinero y servicios externos **sin cambios**. Reversión documental: modificación aditiva o `git revert` normal bajo lectura HEAD/owner y BITACORA conservada. NO QA/main/PROD.
