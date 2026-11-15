# SOLQARYN V5 — C04-04.M02.02: API, DTO, errores y compatibilidad de cobros por enlace

**Autor:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-15. **Autoridad:** STANDARD V5 FINAL Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN/COLA/DAG Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Repositorio:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Entrega:** diseño DOCUMENTAL; NO implementar endpoints, proveedor, webhook, cobro, factura ni migración.

## 1. Fuente y separación de autoridad

M01.03 `docs/v5/c04-04-m01-03-auditoria-fuentes-contratos-cobros.md` blob `d8190f530416a4c6863cc8f7c1a1d11fdf03699f` registra fuentes reales:
`IPagoOnlineProvider.cs` blob `50ad09788c3ad31a1a727f9ae6b154834813ee26`,
`PagoOnlineService.cs` blob `65272808514f2995f8c0abee5316aa872a9ce7a4`,
`PagosOnlineController.cs` blob `99962b2957f672a6dd435895daffaba7333f8a6b`,
`InboundWebhooksController.cs` blob `05bc86051f32c2de75b68877b79bf70b82c07078`,
`PagoOnline.cs` blob `5063b0dc667583f7403fbeec879b519b6ca216d2`, y `WebhookEntrante.cs` blob `90294acb096f4ab228bc974fce3be7e7eadbd9d0`. Son contratos actuales de pagos online/ingreso firmado; **no** evidencia de proveedor live ni settlement de esta fase.

M01.06 `docs/v5/c04-04-m01-06-gates-rollback-liberacion-cobros.md` blob `66b50acc22599328f8f3eafcba7a0522260e41ae` define puertas G01–G10, aprobación comercial abierta, y rollback futuro. El DAG de M02.02 requiere M01.01–06 LISTO+A5 independiente. No derivar autorización de cobrar de la disponibilidad de una fila.

## 2. Operaciones y contratos propuestos NO_IMPLEMENTADOS

| ID | Operación futura | Payload mínimo conceptual | Autoridad y resultado |
| --- | --- | --- | --- |
| OP01 | Consultar métodos de pago habilitables | empresa efectiva + contexto y moneda autorizada | Catálogo allowlisted; sin credenciales ni proveedor instalado |
| OP02 | Solicitar enlace de pago | facturaRef opaca, importe/moneda calculados servidor, idempotencyKey | JWT, RBAC, tenant A/B y saldo vigentes; no duplicar intento |
| OP03 | Consultar intento | paymentAttemptRef + versión | Sólo propietario de factura, resultado saneado |
| OP04 | Verificar estado de referencia | ref proveedor opaca + alcance autorizado | No dar `paid` por retorno navegador o callback aislado |
| OP05 | Recibir evento firmado de proveedor | body bytes, firma, timestamp y proveedor/empresa verificables | Anti-replay, validación HMAC y persistencia durable antes de efecto |
| OP06 | Conciliar pago e importe | facturaRef, providerTxnRef, moneda, monto y versión | Efecto idempotente contable sólo tras evidencia aprobada |
| OP07 | Solicitar reversión | referencia inmutable, motivo, autorización superior | No borrar historia; compensación auditada |
| OP08 | Consultar trazabilidad depurada | scope por empresa, fechas, cursor opaco | Sin PAN/CVV, secretos, token ni datos del tenant B |

OP01–OP08 son casos de uso **propuestos**, no rutas HTTP registradas ni transacciones habilitadas. El contrato `IPagoOnlineProvider` actual es referencia de compatibilidad, no permiso para ejecutar pagos o callbacks. El navegador sólo muestra un enlace; jamás autentica un cambio financiero por volver a una URL.

## 3. DTO conceptuales y validación

| DTO futuro | Campos mínimos sin secretos | Invariante |
| --- | --- | --- |
| `PaymentLinkRequestV1` | facturaRef, requestRef, intendedAction | Monto/moneda/Empresa se obtienen y revalidan en servidor |
| `PaymentAttemptViewV1` | attemptRef, safeState, currency, amount, expiresAtUtc | No exponer credenciales, email de otro tenant ni URL privilegiada |
| `PaymentProviderReceiptV1` | providerRef opaco, transactionRef, paidAmount, currency, evidenceRef | Fuente autenticada; no equivale a asiento contable |
| `WebhookIngressEvidenceV1` | payloadSha256, signatureResult, timestampResult, dedupRef | No almacenar HMAC secreto ni body sensible en auditoría |
| `ReconciliationDecisionV1` | invoiceRef, eventRef, authorizedOutcome, expectedVersion | Requiere regla contable, idempotencia DB y firma validada |
| `RefundRequestV1` | receiptRef, reasonCode, amount, currency, expectedVersion | Permiso de reversión independiente, no borrar factura |

Los DTO no existen en C# por este documento y sus nombres no sustituyen los DTO actuales. Cualquier API posterior deberá preservar forma/versionado de `ApiResponse`, límites ASP.NET Core 10, aislamiento empresa y serialización segura.

## 4. Errores E01–E14, fallos y no-regresión

| Código conceptual | Condición | Respuesta esperada futura |
| --- | --- | --- |
| E01 | Sin JWT/expirado | 401; sin intento nuevo |
| E02 | Sin permiso aplicar pagos | 403; no exponer recursos |
| E03 | Factura ajena tenant B | Denegación sin enumerar existencia |
| E04 | Empresa desactivada | No iniciar cobro |
| E05 | Saldo o monto incongruente | Rechazar sin cobrar |
| E06 | Moneda no admitida | Rechazo explícito |
| E07 | Provider no autorizado/configurado | NO_GO; no fallback oculto |
| E08 | URL HTTP insegura o retorno cliente manipulado | No acreditar cobro |
| E09 | Webhook con HMAC inválido | Rechazar antes de efectos |
| E10 | Timestamp caducado, replay o payload demasiado grande | Denegación idempotente |
| E11 | Mismo key con body distinto | Conflicto, nunca doble intento |
| E12 | Timeout del proveedor | Estado pendiente/indeterminado, no `paid` |
| E13 | Conflicto versión/factura ya conciliada | Sin doble asiento o reembolso |
| E14 | Log expone PAN/CVV/firma/secreto/PII | Sanear, no registrar payload privado |

**P/N futuros:** P01 pago permitido para factura A / N01 intentar factura B; P02 reintento idéntico no duplica / N02 payload cambiado duplica; P03 firma HMAC correcta se recibe sin acreditar / N03 HMAC incorrecto acredita; P04 conciliación con fuente aprobada / N04 retorno navegador acredita; P05 fallo timeout queda pendiente / N05 se marca pagado sin comprobación; P06 reversión autorizada y auditada / N06 usuario lector reembolsa; P07 límites de body/timestamp efectivos / N07 callback masivo/replay; P08 errores saneados / N08 fuga de PAN/CVV o secreto.

Estos son oráculos **FUTUROS_NO_EJECUTADOS**, no xUnit/E2E ni pagos reales. Se identificó un lock de idempotencia en memoria en código existente: no certificarlo como exclusión distribuida entre instancias; futura implementación exige unicidad transaccional, concorrencia multi-instancia y recuperación.

## 5. Seguridad, CI y rollback

A0: sólo docs/v5 y sección de CHANGELOG, sin secretos o código de pago. A1: ocho operaciones, seis DTO y catorce denegaciones más P/N, pruebas escritas no ejecutadas. A2: JWT/RBAC, tenant A/B, HMAC, replay, límites, estados, PII y exclusividad contable como requisitos de diseño, no seguridad ya demostrada. A3: correspondencia con API/Domain/Application/EF actual documentada en M01.03, NO_APLICA integración live para Markdown. A4: publicación fast-forward/CAS en dev; `verify-solqaryn-project-scope` y control V5 `completed/success` del SHA exacto; readback PLAN/COLA/DAG/BITACORA. A5: otra Task Javier distinta de autora antes de M02.06 y de cualquier entorno distinto.

Revertir por `git revert` normal sólo documentos, preservando cambios ajenos e historial institucional. **No** configurar proveedor, activar enlace, hacer pago, ejecutar webhook, compensar factura, tocar MySQL real, agregar worker, mover QA/main/PROD ni crear Tasks.
