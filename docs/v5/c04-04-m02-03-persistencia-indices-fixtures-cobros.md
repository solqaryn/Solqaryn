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
| Idempotencia de solicitud | `UNIQUE(EmpresaId,ProveedorId,Operacion,IdempotencyKey)`; `PayloadDigestCanonico` almacenado APARTE del índice único | Misma clave y digest: mismo resultado sin segundo efecto; misma clave con digest distinto: 409 antes de un segundo cobro |
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
| F02 | Reintento autorizado con misma clave y payload canónico recupera un único intento | Misma clave con otro importe, moneda o factura devuelve 409, sin segunda fila ni cobro |
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

## 8. Corrección P2 de A5_FAIL: idempotencia durable y anticolisiones (2026-11-19)

**Hallazgo preservado.** La revisión A5 independiente observó que `UNIQUE(EmpresaId, ProviderKey, RequestHash)` permite dos filas cuando el mismo cliente reintenta la misma clave con un importe distinto, porque cambia `RequestHash`. Esto contradice el rechazo 409. Esta entrega corrige SOLO el diseño documental; no implementa tabla, constraint, MySQL, webhook, endpoint, pago, JWT ni proveedor. Autor histórico: DEV06; reparador: DEV01, que no puede aprobar su propia reparación A5.

**Identidad de operación.** `IdempotencyKey` es una clave opaca, obligatoria, estable para la operación lógica, distinta de la huella del contenido. La clave de unicidad futura será `(EmpresaId, ProveedorId, Operacion, IdempotencyKey)`. `Operacion` y `ProveedorId` se normalizan en servidor y se vinculan al tenant autenticado; ninguna dimensión de este índice incorpora `RequestHash`, importe, factura o digest. Claves ausentes/vacías, identificación de proveedor ambigua, operación no autorizada, tenant inválido o factura ajena se rechazan antes de cualquier mutación. Las mismas cadenas para empresas A/B no comparten acceso ni resultado: comprobar JWT/RBAC/propiedad en cada solicitud.

**Contenido canónico y conflicto.** Persistir junto al intento un `PayloadDigestCanonico` determinista calculado desde representación de factura, importe exacto en unidades menores, moneda, operación y destino en servidor; no registrar texto crudo, PAN/CVV, JWT, secreto HMAC ni datos de otra empresa. Un reintento de la misma clave, tenant y operación con huella idéntica devuelve estado/resultado del intento existente SIN otra llamada ni asiento. Con huella distinta: `409 IDEMPOTENCY_PAYLOAD_CONFLICT` antes de insertar o cobrar. El digest no autoriza: validar primero tenant/actor, RBAC, propiedad y revocación.

**Concurrencia y estados ambiguos.** Antes de implementar, diseñar operación MySQL transaccional con restricción UNIQUE realmente instalada; ante conflicto único, recuperar el intento del mismo tenant, comparar digest y devolver resultado/409. Dos workers paralelos deben competir sobre la misma unicidad persistente; `SemaphoreSlim` local y `SELECT` seguido de `INSERT` sin UNIQUE no bastan. Si el PSP hace timeout tras posible efecto, conservar `AMBIGUOUS` y exigir conciliación por referencia verificada antes de reintentar; no afirmar exactly-once distribuido.

| Caso P/N futuro (NO EJECUTADO) | Positivo requerido | Negativo/abuso que debe fallar cerrado |
| --- | --- | --- |
| P2-01 | Misma clave K, misma factura/importe/moneda: mismo AttemptId sin segundo efecto | Misma K con importe/factura distintos: 409, una fila, cero cargos adicionales |
| P2-02 | Tenant B puede usar su propia K tras verificación de permisos | Tenant A no lee ni modifica intento B aunque use K idéntica |
| P2-03 | Dos conexiones MySQL simultáneas con K+digest producen un intento | Dos procesos con K y digest distinto: un ganador, un 409, nunca dos cargos |
| P2-04 | Intento autorizado en operación distinta conserva identidad separada | Key vacía, proveedor ambiguo, actor revocado o firma inválida no crean intento |
| P2-05 | Webhook repetido con referencia auténtica sólo reconcilia un evento | Replay, timeout y retorno navegador no generan nuevo cargo ni SETTLED ficticio |

**Pruebas y gates.** A0: diff Markdown y CHANGELOG, sin runtime. A1/A2: los cinco pares anteriores son casos exigibles en trabajo futuro, NO pruebas ni PASS ejecutados. A3: MySQL/API/PSP = NO_APLICA a este cambio estrictamente documental; antes de migración se requieren EF/MySQL 8.4 efímero, múltiples conexiones, JWT/RBAC y aislamiento A/B con fallos adversariales. A4: Scope Lock y V5 exact-SHA más readback. El A5_FAIL previo permanece en PLAN/BITACORA hasta revisión independiente nueva por otra Task Javier distinta del autor DEV06 y reparador DEV01. NO modificar fórmulas DAG/COLA ni pasar QA/main/PROD.
