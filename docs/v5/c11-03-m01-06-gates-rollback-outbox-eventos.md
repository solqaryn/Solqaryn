# SOLQARYN V5 FINAL — C11-03.M01.06: bloqueo, rollback y liberación del contrato de outbox y eventos

**Autoria:** PRIMARY DEV 02, JAVIER_CHATGPT, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Repositorio:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Tipo:** documento terminal M01.06, sin servidor de eventos nuevo ni modificaciones ejecutables.

## 1. Dependencias y fuentes causales

La materia es C11-03, **outbox y eventos**: separación entre eventos de dominio/plataforma, relay, deduplicación, correlación y fallo parcial. Se conservan los componentes preexistentes `OutboxRetryProcessor`, `OutboxRetryPolicy` y `MensajeOutboxService`, sin cambiar sus contratos, invocar workers ni inferir que exista un bus distribuido general. Que exista un procesador de retry no acredita corrección para nuevos tipos de eventos.

Se verificaron `C11-03.M01.01–05` LISTO con cinco A5 independientes emitidas por PRIMARY DEV 04 (revisor TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`) sobre las obras de DEV02: objetivos RNF y oráculos, actores/interfaces/fronteras, 15 fuentes y brechas, riesgos/decisiones y aceptación funcional futura. También se verificó `C10-04.M01.06` LISTO con A5 de DEV03 distinta del autor DEV06, más requisitos por fases documentadas en `DAG_TECNICO`. La unidad presente se reclamó solo tras `PLAN H=PENDIENTE/I` vacío, `COLA G/O=SI`, `DAG F=VALIDADO/G=SI`, sin liberar ningún sucesor artificialmente.

**Evidencia que NO existe aquí:** un relay contratado, producer autenticado nuevo, migración MySQL ejecutada, garantía exact-once, restauración física, proveedor de mensajería en producción, métricas observadas, webhook entre Tasks ni pruebas de carga/orden reales de esta entrega.

## 2. Catorce bloqueos B01–B14 de salida

| ID | Condición NO_GO | Evidencia para futura liberación técnica |
| --- | --- | --- |
| B01 | Identidad y origen de evento provienen de request no autenticada | Claims y grant backend validados, actor auditable |
| B02 | Empresa A puede publicar/consumir objeto B | Pruebas negativas A/B y pertenencia de recurso persistida |
| B03 | Event type permite invocar operación privilegiada arbitraria | Catálogo/versionado allowlisted, permisos por operación |
| B04 | El retry duplica efecto de compra o pago | Llave idempotente tenant+operación+hash+versión y conflicto probado |
| B05 | Dos relays poseen el mismo evento a la vez | Lease CAS/lock transaccional con un solo ganador |
| B06 | Falla de red deja mensaje marcado ENTREGADO sin acuse | Confirmación durable y retry con backoff finito |
| B07 | Orden relativo de un agregado se rompe sin detección | Sequence por agregado, versión y test de out-of-order |
| B08 | Correlación usa ID de otro tenant | CorrelationId validada, nunca autoridad |
| B09 | Dead-letter se convierte en retry infinito | Límite finito, estado de cuarentena y revisión explícita |
| B10 | Logs o payload contienen JWT, OTP, DSN o PII privada | Redacción verificada y retención mínima |
| B11 | Evento se emite sin transacción/consistencia necesaria | Prueba real de outbox/commit o compensación causal |
| B12 | Se declara RPO/RTO o recuperación sin ensayo | Restore efímero, manifest/hash/controles tenant verificados |
| B13 | Check queued, cancelled, skipped, failure se cuenta PASS | Solo pruebas requeridas completed/success exact SHA |
| B14 | Autor se revisa su propio A5 o ocupa scope ajeno | TASK_ID independiente, exclusividad y readbacks |

La gravedad de cada B es preventiva: no se afirma que un defecto haya sido explotado o sea existente. Si B01–B14 no se satisface al momento técnico pertinente, aplicar NO_GO a esa operación, no bloquear arbitrariamente trabajo independiente.

## 3. Condiciones de reversión y contención RB01–RB10

| ID | Acción futura autorizable solo con pruebas |
| --- | --- |
| RB01 | Detener únicamente consumidores afectados con ownership acreditado |
| RB02 | No alterar estado ENTREGADO ya confirmado ni reemitir pago |
| RB03 | Conservar lote, EventId, tenant, hash, attempts y correlación redacted |
| RB04 | Recuperar un lease huérfano solo tras demostrar ausencia de worker activo |
| RB05 | Reprogramar con backoff, límite y autorización renovada |
| RB06 | Distinguir duplicado idéntico de conflicto mismo ID distinto payload |
| RB07 | Segregar DLQ por tenant, sin filtraciones ni reactivación automática |
| RB08 | Ensayar restauración en MySQL efímero y verificar integridad y FK |
| RB09 | Comprobar orden/versión por agregado antes de reanudar relay |
| RB10 | Registrar cierre con evidencia de checks, revisión A5 y rollback validado |

**Reversión del changeset documental actual:** `git revert` normal del commit en dev tras releer HEAD; no hay proceso ni base que restaurar porque no se ejecutó ninguna migración, relay, webhook ni worker.

## 4. Escenarios P/N y condiciones de aceptación — FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro | Negativo que debe fallar |
| --- | --- | --- |
| P01/N01 | Encolar evento A con actor y membresía vigentes | Tenant B o rol revocado intenta leer/publicar A |
| P02/N02 | Reintento con misma llave no duplica efecto | Misma llave con payload distinto duplica operación |
| P03/N03 | Un relay gana lease CAS y completa | Dos consumidores reclaman simultáneamente el mismo ID |
| P04/N04 | Outbox conserva transacción al fallo externo | Timeout produce falso ENTREGADO |
| P05/N05 | Secuencia de agregado se preserva | Evento fuera de orden sobrescribe versión posterior |
| P06/N06 | Backoff finito llega a estado cuarentena | Retry infinito supera cuotas |
| P07/N07 | Auditoría redactada traza actor/tenant/resultado | Log filtra JWT/PII o payload privado |
| P08/N08 | Contrato versionado rechaza tipo incompatible | Caller cambia EventType a operación privilegiada |
| P09/N09 | Restore de snapshot efímero verifica checksum | Manifiesto corrupto se declara recuperación exitosa |
| P10/N10 | A5 independiente y exact-SHA validan fase | Autoaprobación o CI cancelled da PASS |

Ninguna fila P/N es prueba ya ejecutada. M02 solo podrá iniciar como modelado documental después de A0–A4 y **A5 independiente de esta M01.06**, con readback de DAG/COLA y preflight propio; M03 debe implementar código real con xUnit, MySQL efímero, idempotencia/atomicidad y seguridad pertinente, sin sustituirlo por narrativa.

## 5. Revisión A0–A5, impacto y salida

**A0:** verificar autoridad V5, identidad, Scope Lock, predecesores LISTO+A5, SHA y superficie de dos Markdown. **A1:** cotejo real del documento, 14 bloqueos, diez medidas de reversión y diez pares P/N escritos, sin atribuir runtime. **A2:** política de aislamiento A/B, JWT/RBAC, PII, idempotencia, límites y fallo seguro. **A3:** compatibilidad de diseño con los servicios OutboxRetry existentes; CI .NET, Angular, MySQL, colas y proveedores son NO_APLICA a este changeset exclusivamente documental, no PASS técnico. **A4:** diff exacto, sección CHANGELOG con encabezado y viñetas juntas, commit fast-forward CAS, checks Scope Lock/V5 `completed/success` SHA exacto, BITACORA/PLAN/COLA/DAG readback. **A5:** revisión verdadera de otra de las cinco Tasks Javier antes de declarar terminada la salida a M02.

El contrato no otorga autorización comercial, de datos, administración, QA/main/PROD, Cloudflare, R2, cambios de secretos o llamadas perpetuas entre automatizaciones; conserva la preparación documental del backlog sin instalar eventos.
