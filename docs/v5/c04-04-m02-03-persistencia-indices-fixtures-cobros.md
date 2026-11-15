# SOLQARYN V5 — C04-04.M02.03: persistencia conceptual, índices, migraciones y fixtures para cobros

**Responsable:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-15. **Fuente institucional:** STANDARD V5 FINAL Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, Sheet PLAN/COLA/DAG `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Repositorio único:** `solqaryn/Solqaryn@dev` Scope Lock STRICT.

**Clase M02.03 DOCUMENTAL:** no se crean o alteran bases de datos, índices, migraciones, entidades EF ni saldos. No se ingresa a proveedores de cobro, se reciben webhooks ni se emiten eventos. La fuente M01.03 tiene 13 archivos de código/configuración/pruebas, pero la existencia de artefactos no certifica esquema MySQL real o pagos integrados.

## 1. Estado del sistema y fronteras

Las entidades existentes `PagoOnline.cs` (blob `5063b0dc667583f7403fbeec879b519b6ca216d2`) y `WebhookEntrante.cs` (blob `90294acb096f4ab228bc974fce3be7e7eadbd9d0`) ya modelan empresa, factura, proveedor, referencia y estados. Sus configuraciones EF `PagoOnlineConfiguration.cs` (`83a9c3676937c94d84ea3ef55017a489479bec1a`) y `WebhookEntranteConfiguration.cs` (`8c0f9d0af84bf9aea28d57fc50f54c27f56ca570`) declaran índices/constraints, y una migración versionada N7.8 `20260914200100_N7_8_D_PagoOnlineIdempotencia.cs` (`b1513b82bcc49349e1200174cc964e32ef34509a`) existe en código. **Ninguno fue ejecutado** por esta microtarea. Referencia: `docs/v5/c04-04-m01-03-auditoria-fuentes-contratos-cobros.md` blob `d8190f530416a4c6863cc8f7c1a1d11fdf03699f`.

`docs/v5/c04-04-m01-06-gates-rollback-liberacion-cobros.md` blob `66b50acc22599328f8f3eafcba7a0522260e41ae` documenta G01–G10, aprobación comercial pendiente, HMAC, conciliación y rollback. Predecesoras M01.01–06 LISTO+A5 sólo permiten diseñar M02; **no autoriza** migración en Aiven/MySQL, liquidación, cobro, credenciales, webhooks live o QA/main/PROD.

## 2. Entidades/relaciones adicionales propuestas (NO CREADAS)

| Contrato conceptual | Llave y propiedad | Seguridad contable e idempotencia |
| --- | --- | --- |
| `PaymentAttemptEvidence` | EmpresaId, invoiceRef, operationRef, providerRef, expectedVersion | Referencia a `PagoOnline`, sin duplicar factura ni efectivo |
| `InboundWebhookEvidence` | EmpresaId, providerKey, externalEventId, payloadDigest | Vincular `WebhookEntrante`, firma/timestamp verificados |
| `PaymentReconciliationResult` | EmpresaId, invoiceRef, providerTxnRef, reconciliationVersion | Decisión auditada, nunca por retorno navegador |
| `PaymentLedgerEffect` | EmpresaId, invoiceRef, financialEffectId, sequence | Referencia conceptual a ledger futuro, no asiento implementado |
| `RefundDecision` | EmpresaId, receiptRef, requestRef, approvalRef | Reversión como compensación, no borrado del cargo |
| `ProviderRetryLease` | EmpresaId, operationRef, expectedVersion, expiresAt | Necesidad de exclusión durable, no lock sólo en memoria |
| `PaymentAuditRecord` | EmpresaId, actorRef, eventRef, sourceSha, safeOutcome | Append-only saneado; sin PAN/CVV/secretos |
| `PaymentRollbackManifest` | scope, before/after digest, backupRef, targetVersion | Preparación de sandbox, no rollback productivo real |

No duplicar `PagoOnline`, `WebhookEntrante`, `Factura` ni su modelo de índice sin análisis EF. Nombres conceptuales deben cotejarse con esquema real antes de cualquier migración en M03 y con reglas de licenciamiento/contrato del proveedor si llegan a solicitarse.

## 3. Índices y constraints potenciales (NO SQL EJECUTADO)

| Consulta/efecto futuro | Índice/cota lógica candidata | Regla de rechazo |
| --- | --- | --- |
| Intento por empresa/factura | (EmpresaId,FacturaId,Estado) | A no consulta facturas B |
| Idempotencia de solicitud | (EmpresaId,ProviderKey,RequestHash) unique | Mismo key/payload distinto -> 409 |
| Webhook externo | (EmpresaId,Proveedor,EventoExternoId) unique | Replay no produce doble efecto |
| Transacción de proveedor | (EmpresaId,ProviderKey,ProviderTransactionRef) unique | Dos callbacks no concilian dos veces |
| Ledger por factura | (EmpresaId,InvoiceRef,EffectRef) unique | No duplicar asiento por reintento |
| Auditoría saneada | (EmpresaId,OccurredAtUtc,OperationRef) | Sin PAN/CVV, datos B o firma HMAC |
| Retry lease | (EmpresaId,OperationRef,ExpectedVersion) unique | CAS invalida escritor obsoleto |

La unicidad EF actual descrita en el inventario no debe duplicarse sin inventario real de columnas. No hay readback de MySQL 8.4 ni prueba de concurrencia multiinstancia; el `SemaphoreSlim` de `PagoOnlineService` solo sincroniza su proceso. El backend debe mantener JWT/RBAC, propiedad de empresa/factura y transacción persistente, nunca confiar en IDs o montos del browser.

## 4. Flujo futuro de estados, sin efectos

1. `REQUESTED`: identidad y factura revalidadas, importe/moneda calculados en servidor.
2. `LINK_PREPARED`: proveedor **autorizado** devuelve referencia opaca/URL segura, aún no pagado.
3. `INBOUND_VERIFIED`: webhook HMAC, timestamp, tamaño y deduplicación aceptados, sin acreditar automáticamente.
4. `RECONCILING`: consulta/fuente financiera aprobada confirma moneda, monto, factura y transacción.
5. `SETTLED`: efecto contable idempotente confirmado transaccionalmente.
6. `AMBIGUOUS/FAILED`: timeout o discrepancia retiene estado sin marcar pagado ni provocar cobro ciego.
7. `REVERSAL_REQUESTED/COMPENSATED`: autorización independiente, compensación y auditoría sin borrar historia.

Estos estados son **propuestos**. No se activan rutas ni eventos. No existe garantía de exactly-once distribuido por simples clases o índices; se requieren fixtures y tests reales bajo CI dirigida.

## 5. Diez fixtures conceptuales FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro | Negativo obligatorio |
| --- | --- | --- |
| F01 | Factura A válida inicia intento idempotente | Actor A inicia factura B |
| F02 | Reintento igual no duplica intento | Mismo key con otro monto crea cobro |
| F03 | Webhook HMAC/timestamp válido persiste evento | Firma inválida produce estado pagado |
| F04 | Evento duplicado recupera resultado original | Replay crea segundo asiento |
| F05 | Moneda y monto concuerdan tras fuente autorizada | Retorno navegador acredita sin conciliación |
| F06 | Dos procesos compiten y uno gana transacción | Dos instancias cargan simultáneamente |
| F07 | Timeout conserva estado indeterminado | Timeout dispara reintento ciego |
| F08 | Reversión autorizada agrega compensación | Borrar factura/pago para simular reembolso |
| F09 | MySQL efímero registra unicidad, índices, rollback | Migración parcial rompe historial |
| F10 | Logging depurado conserva ID opaco | Token, PAN/CVV o secreto persiste en logs |

Son escenarios y contratos de aceptación, **no test xUnit/MySQL/E2E ejecutados**. No inventar costos, monedas/fees aceptadas, reintentos, TTL, políticas de reembolso ni liquidación real; decisiones D01–D10 de M01 continúan abiertas.

## 6. Migración y rollback FUTUROS (no ejecutados)

MIG01 inventariar versión real y filas de PagoOnline/WebhookEntrante en un entorno autorizado; MIG02 elaborar backup sintético/restauración efímera y manifiesto; MIG03 diseñar migración EF aditiva con índices sin colisiones y rollback reproducible; MIG04 probar en MySQL efímero 8.4 con dos procesos, SHA exacto y concurrencia; MIG05 verificar A/B, firma/replay, cobro ambiguo, compensación contable y confidencialidad; MIG06 aprobar proveedor, permisos, costos y política comercial por separado antes de cualquier conexión live. No ejecutar esos pasos ni migrar datos de Aiven/PROD aquí.

## 7. Evidencia A0–A5

A0: solo Markdown y CHANGELOG, sin código, secretos ni datos financieros. A1: ocho contratos, siete consultas/índices candidatos, diez P/N y seis pasos MIG, todos futuros. A2: tenant A/B, HMAC, JWT/RBAC, idempotencia durable y no exposición de PAN/CVV explicados como límites de diseño. A3: compatibilidad de `PagoOnline`, `WebhookEntrante`, configuraciones EF, migración histórica y tests fuente, sin ejecutar API/MySQL (NO_APLICA a este diff). A4: Git fast-forward/CAS, controles `verify-solqaryn-project-scope` y V5 `completed/success` exact-SHA y readback institucional. A5: revisor independiente de otra Task Javier.

Revert de esta tarea: `git revert` normal del Markdown/CHANGELOG. **Sin** mover QA/main/PROD, migraciones, efectos financieros, proveedores, secretos ni webhooks.
