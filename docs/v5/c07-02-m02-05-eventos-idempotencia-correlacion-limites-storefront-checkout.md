# SOLQARYN V17 — C07-02.M02.05: eventos, idempotencia, correlación y límites de storefront y checkout

**Microtarea:** C07-02.M02.05 · **autor:** PRIMARY DEV 05 — JAVIER_CHATGPT, Task ID `6adc2e55f47c81919f6374e561687076` · **PLAN_MAESTRO:** fila 1056 · **riesgo:** R0 documental sin runtime · **fecha:** 2026-11-15 · **repositorio:** solqaryn/Solqaryn@dev · **Scope Lock:** STRICT.

**Autoridad única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), V15/V16/V17 y PLAN/COLA/DAG vivos. Se definen eventos **conceptuales** para una implementación futura de tienda, carrito, cotización de checkout, pedidos y conciliación. **No** se introduce código .NET/Angular, interfaz HTTP, bus, cola, webhook, scheduler, trabajador, base de datos, migración, proveedor PSP, secreto, evento emitido ni configuración efectiva. Los criterios siguientes son contratos, no pruebas ejecutadas ni autorización de cobros.

## 1. Antecedentes y límites de autoridad

- `C07-02.M01.06` fija 16 puertas de salida, pruebas futuras y rollback; su A5 independiente está registrado. Ninguna puerta documental acredita por sí misma host/HMAC, stock, pedidos o pagos efectivos.
- `C07-02.M02.01` identifica modelo conceptual y ownership `Empresa`, `TiendaPublicada`, `CarritoCliente`, `CotizacionCheckout`, `OrdenComercial` e intentos PSP futuros, junto con 20 invariantes. Son propuestas, salvo entidades del ERP que deban comprobarse por código fresco.
- `C07-02.M02.04` documenta ámbitos, tipos y doce flags inertes, desactivados por defecto; este documento no los enciende ni crea nuevas rutas. Las decisiones anteriores D01–D08 permanecen sujetas a autorización y pruebas.
- La validación pública de checkout existente, citada por las preparaciones M01/M02, **no** equivale a orden duradera, reserva de existencias, pago confirmado ni factura. Un evento de cotización no autoriza por inferencia un efecto irreversible.
- Toda identidad `EmpresaId`, `StoreKey`, host confiable, actor, roles y recurso comercial debe ser resuelta y verificada **en backend**. Nombre de evento, JWT o flag frontend no sustituyen autorización. Sin contexto seguro, `DENY`.
- Los nombres de tipos/envelopes siguientes son **propuestos**; no hay clases, topics o contratos de proveedor publicados por esta entrega.

## 2. Doce eventos semánticos INERTES: intención, propietario y efecto

| Código | Evento propuesto | Dueño/contexto necesario | Condición de emisión futura y prohibición |
| --- | --- | --- | --- |
| E01 | `TiendaPublicacionSolicitada` | Empresa, tienda y miembro autorizado | Sólo orden autenticada; no publica hasta validación de host y contenido |
| E02 | `TiendaPublicacionConfirmada` | Empresa, tienda, versión y host verificados | Sólo después de commit durable; nunca por click/optimismo de UI |
| E03 | `CatalogoConsultado` | Empresa/host confiables, alcance público | Telemetría agregada sin IDs privados ni caché cruzada |
| E04 | `CarritoCreado` | Empresa, sesión anónima controlada o actor | Sólo sesión durable autorizada; no reserva existencias |
| E05 | `LineaCarritoValidada` | Empresa, carrito, variante y cantidades | Precio y límite recalculados en servidor; rechazo no emite éxito |
| E06 | `CotizacionCheckoutCalculada` | Empresa, cotización y versión UTC | Sólo resultado de validación; jamás orden, reserva o cargo |
| E07 | `CotizacionCheckoutExpirada` | Empresa, cotización y TTL verificable | No revierte una operación durable distinta ni expone PII |
| E08 | `OrdenCreacionSolicitada` | Empresa, actor/sesión, operación idempotente | **No disponible** hasta caso de uso R2 probado y autorizado |
| E09 | `OrdenRegistrada` | Empresa, orden durable y versión de transacción | Requiere commit MySQL real, no redirect ni resultado provisional |
| E10 | `ReservaExistenciasConfirmada` | Empresa, orden, producto, versión y transacción | Sólo transacción/rollback seguros, sin oversell A/B |
| E11 | `PagoNotificacionRecibida` | Empresa, proveedor y referencia opaca | No equivale a pago validado; firma/nonce pendientes |
| E12 | `PagoConciliado` | Empresa, orden, proveedor, firma, importe y estado | Sólo conciliación autorizada y reconciliada; sin PSP real no puede emitirse |

Todos son nombres lógicos futuros, **0 emisores implementados** y ningún efecto permitido en esta microtarea. Cualquier evento posterior que cambie estado, dinero o existencias requiere modelo transaccional y pruebas específicas. La telemetría de consulta no debe revelar precios internos, costos, PII, secretos ni señales de empresas ajenas.

## 3. Sobre de evento propuesto: catorce campos mínimos

| Campo | Contrato propuesto | Restricción de seguridad e idempotencia |
| --- | --- | --- |
| `SchemaVersion` | entero allowlisted | versión desconocida => rechazar, no degradar silenciosamente |
| `EventId` | ID opaco único por emisión confirmada | no reutilizar entre operaciones diferentes |
| `EventType` | enum cerrado E01–E12 | rechazar tipo desconocido y eventos sin capacidad |
| `EmpresaId` | ID autoritativo de servidor | nunca confiar en empresa de payload externo |
| `StoreId` | ID de tienda vinculado a Empresa | host/storeKey validado por servidor |
| `AggregateId` | carrito/quote/orden según evento | pertenencia compuesta Empresa + recurso |
| `AggregateVersion` | versión monotónica comprobada | compare-and-swap, rechazar doble transición |
| `OperationId` | ID correlativo de comando autorizado | no es una contraseña ni token compartible |
| `IdempotencyKeyHash` | huella contextual, no clave visible | liga tenant, actor, operación y payload |
| `CorrelationId` | ID opaco de una solicitud/proceso | trazabilidad sin JWT, PII o datos de tarjeta |
| `CausationId` | comando/evento predecesor válido | detectar ciclos, replay o causalidad no demostrada |
| `OccurredAtUtc` | fecha UTC de evento verificado | reloj, TTL, desfase y expiración acotados |
| `ActorReference` | sujeto/servicio autenticado y permitido | miembro revocado o rol ajeno => negar |
| `PayloadDigest` | hash canónico de datos permitidos | impedir colisión de clave con payload cambiado |

La versión incluye compatibilidad e intención de consumidores; no definir bus, transporte, formato binario, tabla outbox o SLA aún. El productor futuro debe capturar resultado confirmado en transacción y luego publicar de forma recuperable, sin acoplar confirmaciones de pago a la recepción de mensajes. Un mensaje desordenado o repetido nunca hace avanzar una orden por sí solo.

## 4. Catorce invariantes de deduplicación, correlación y orden

- **I01.** La identidad efectiva de Empresa viene del servidor y tiene precedencia sobre host/cabeceras/payload.
- **I02.** La clave deduplicadora incluye `EmpresaId + ActorId + OperationType + IdempotencyKeyHash` y digest de petición; una clave de A no produce resultado de B.
- **I03.** Misma clave, mismo scope y mismo payload puede devolver la misma respuesta confirmada; payload distinto debe rechazar con conflicto auditable.
- **I04.** Dos escritores concurrentes con mismo agregado y versión deben producir como máximo una transición válida mediante CAS/transacción, nunca dos cobros o reservas.
- **I05.** Una respuesta temporal o timeout antes de commit no permite certificar éxito ni emitir `OrdenRegistrada`.
- **I06.** `checkout/validar` no crea orden, reserva, pago, factura o movimiento de existencias como efecto colateral.
- **I07.** Cotización vencida, precio/impuesto/stock cambiado o moneda no autorizada requiere volver a validar.
- **I08.** Un callback externo debe probar origen, firma HMAC, algoritmo, clave versionada externa al repo, nonce, TTL, importe, moneda, tenant y pedido; sin evidencia, no cambia estado.
- **I09.** Replays, callbacks duplicados, falsos o fuera de orden no reactivan una operación revocada ni producen segunda conciliación.
- **I10.** Estados de orden son una máquina de estados explícita y versionada; redirects, flags UI y “mensaje recibido” no marcan `PAGADO`.
- **I11.** Causalidad y correlación son opacas, con retención mínima; logs, URLs, errores, caché y analítica nunca transportan JWT, HMAC key, PAN o PII excesiva.
- **I12.** Emisión y consumo se aíslan por Empresa/host/versión; lectura pública de A no consume ni invalida mensajes de B.
- **I13.** Fallo en publicación después de commit requiere recuperación transaccional futura basada en estado real, nunca inventar orden o invertir un cobro legítimo.
- **I14.** Cambiar, reintentar, expirar o deshabilitar una bandera no elimina historia financiera ni modifica automáticamente datos durables; rollback requiere reconciliación auditada.

**Límites futuros propuestos:** un TTL acotado por operación, máximo de intentos con backoff finito, retención definida por finalidad y requisitos legales, máximo de tamaño/cantidad de mensajes y control de carga, todos sin valores productivos inventados. Ninguna Task de ChatGPT se utiliza como dispatcher persistente o worker entre ejecuciones.

## 5. Doce pares de aceptación futura (NO_EJECUTADOS)

| Caso | Positivo que se probaría con backend real | Negativo que debe fallar |
| --- | --- | --- |
| P01/N01 | tienda A resuelta por host confiable | host manipulado selecciona B |
| P02/N02 | evento autorizado de publicación de A | miembro revocado o rol insuficiente intenta publicar |
| P03/N03 | carrito A añade variante A válida | variante B, inactiva o cantidad fuera de rango |
| P04/N04 | cotización vigente recalcula servidor | cliente altera precio, IVA o moneda |
| P05/N05 | validar checkout retorna cotización sin mutar | se intenta crear orden/cobro sólo al validar |
| P06/N06 | mismo request idempotente repite un efecto | payload distinto con misma clave / replay |
| P07/N07 | dos comandos concurrentes producen único CAS | duplicación de reserva o gasto |
| P08/N08 | pedido confirmado después de commit real | error transaccional emite pedido ficticio |
| P09/N09 | HMAC y TTL válidos permiten verificar callback | firma falsa, nonce repetido, tenant B |
| P10/N10 | conciliación real reconoce una única transición | redirect o webhook sin firma marca PAGADO |
| P11/N11 | correlación opaca permite auditoría autorizada | logs contienen JWT, PAN, secretos o PII de B |
| P12/N12 | fallo temporal se recupera sin doble efecto | expiración/reintento fuera de orden altera estado |

**Estado de pruebas aquí:** 0 ejecutados / 12 pares previstos, desempeño runtime `NO_MEDIDO`. Estos son escenarios escritos en Markdown, no asserts xUnit, integración MySQL, HTTP, Playwright, callbacks PSP ni evidencia de seguridad implementada.

## 6. Gates A0–A5, evolución y rollback

**A0 — documento:** alcance C07-02.M02.05 separado de las otras microtareas, 12 tipos, 14 campos, 14 invariantes, 12 pares P/N propuestos, límites/rollback, A5 y dependencias. Cambios permitidos: este archivo y sección contigua de `CHANGELOG_AI.md`.

**A1 R0:** revisión estructural/semántica de tablas y consistencia entre emisión, idempotencia, expiración y confirmación; no pruebas funcionales ejecutadas. **A2 R0:** revisión estática del rechazo tenant A/B, JWT/RBAC/revocación, HMAC/replay, integridad de stock/pagos, privacidad y secretos; no sustituye tests de implementación. **A3 R0:** `NO_APLICA` para ASP.NET Core, API/HTTP, EF/MySQL, Angular/Playwright, PSP, cola y workers: ningún runtime cambia. **A4:** sólo tras publicación fast-forward en `dev`, check `verify-solqaryn-project-scope` y V5 procedencia en `completed/success` para SHA exacto, PLAN H/I, COLA/DAG y BITACORA readbacks verificados. Un check `queued`, `in_progress`, `cancelled`, `skipped` o `failure` requerido no es PASS. **A5:** Task Javier distinta de la autora revisará requisitos, SHA, blobs, diff, resultados y límites y puede rechazar; LISTO_DEV por A0–A4 no implica A5.

**Desarrollo posterior:** M03 R2 debe confirmar interfaces/consumidores, contratos de PSP realmente autorizado y migración/rollback en MySQL 8.4 efímero con pruebas `Category=Integration`, xUnit positivos/negativos y JWT/RBAC/tenant A/B; al modificar UI exigir Playwright/Chromium, accesibilidad y estados honestos. Sin permisos explícitos quedan fuera producción, QA/main, DNS, secretos y cobros externos. No implementar futuros eventos ni scheduler en este scope.

**Rollback:** corregir con commit aditivo o revert dirigido, nunca force-push, reset destructivo, borrar BITACORA ni atribuir A5 propia; ante CI fallida mantener VALIDANDO y registrar hipótesis; ante duda V17 lock/owner no escribir estados. Apagar flags futuros no revierte transacciones comerciales ya realizadas. Conservar actor, SHA, notas, readbacks e historial.
