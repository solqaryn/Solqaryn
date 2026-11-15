# SOLQARYN V16 — C07-03.M02.05: contratos futuros de eventos, idempotencia, correlación y límites

**ID:** C07-03.M02.05, PLAN_MAESTRO fila 1092. **Owner/autor:** PRIMARY DEV 02 JAVIER (`6af16bb9492c8191b8e8c7d606f11507`). **Clasificación:** V15 R0 documental inerte. **Repositorio/rama:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT.

**Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; PLAN/COLA/DAG actuales. Resultado exigido: definir eventos, idempotencia, correlación y límites para inventario, compras, almacenes, recepciones, transferencias y conciliación. **NO se implementan ni publican eventos reales, handlers, colas, dispatcher, outbox, webhooks, API, DB, migraciones, flags o tareas automáticas**.

## 1. Dependencias, límites y fuentes de dominio

`C07-03.M01.06` fija BL01–BL16, RB01–RB10 y SAL01–SAL12; D01–D08 siguen sin certificar. `M02.01` describe E01–E12, INV01–INV16, REL01–REL10; `M02.02` contratos OP01–12, DTO01–12 y VAL01–16; `M02.03` persistencia conceptual P01–12, IDX01–12, MIG01–08 y fixtures F01–F12; `M02.04` opciones OPT01–14, sin habilitación runtime. Esta propuesta NO satisface los gates de código M03 ni los terminales M02.06 hasta revisión independiente A5 real.

El backend ASP.NET Core 10 sigue arquitectura Domain <- Application <- Infrastructure; API compone. La identidad empresa/sucursal/almacén se resuelve en backend con JWT/RBAC, pertenencia y estado; `NULL` de legado o IDs cliente nunca conceden acceso. Cualquier publicación real exige datos de `AppDbContext` y transacción MySQL 8.4 efímero probada cuando hay mutación de existencias, recepción o costos. No duplicar fronteras de autoridad del ERP.

## 2. Doce intenciones de eventos EV01–EV12 (FUTUROS, NO EMITIDOS)

| ID | Intención futura | Evidencia y condición causal |
| --- | --- | --- |
| EV01 | ReservaCreada | tenant, almacén, variante, reserva, versión y commit transaccional |
| EV02 | ReservaConsumida | consumo exacto una vez; clave de transición y saldo |
| EV03 | ReservaLiberada | liberación idempotente por expiración o cancelación autorizada |
| EV04 | OrdenCompraCreada | orden, proveedor, sucursal y fingerprint de idempotencia |
| EV05 | OrdenCompraAprobada | rol aprobador vigente, versión y audit trail |
| EV06 | RecepcionParcialRegistrada | orden, saldo pendiente, lotes y transacción completada |
| EV07 | RecepcionCerrada | todas las líneas conciliadas y sin duplicar movimiento |
| EV08 | TransferenciaIniciada | almacenes origen/destino distintos, ambos del tenant |
| EV09 | TransferenciaConfirmada | débitos y créditos atómicos con rollback probado |
| EV10 | ConteoBorradorCreado | no aplica ajuste de stock automáticamente |
| EV11 | AjusteInventarioConfirmado | RBAC explícito, correlación y kardex por transacción |
| EV12 | ConciliacionGenerada | scope empresa, corte temporal y redacción de costos |

**Sólo nombres candidatos:** no son mensajes, topics ni contratos registrados; publicar requiere revisión de consumidores y prueba de transacción con origen realmente confirmado. Un error antes del commit no puede producir evento de éxito.

## 3. Doce defensas de idempotencia IDEM01–IDEM12

| ID | Contrato de deduplicación y rechazo |
| --- | --- |
| IDEM01 | Scope de clave por empresa + operación + recurso + identidad verificada |
| IDEM02 | Fingerprint canónico de payload, sin secretos/PII |
| IDEM03 | Mismo key y fingerprint devuelve resultado estable, no segunda escritura |
| IDEM04 | Misma clave con fingerprint diferente rechaza conflicto |
| IDEM05 | Versionado optimista/CAS por reserva, orden y transferencia |
| IDEM06 | Retry en timeout reconcilia estado persistido antes de confirmar |
| IDEM07 | Double-delivery/consumer dedup no aplica movimiento dos veces |
| IDEM08 | La expiración de reserva no duplica liberaciones manuales |
| IDEM09 | Recepción replay no duplica kardex, lotes ni saldos |
| IDEM10 | Partial failure antes de commit revierte saldo y evento |
| IDEM11 | Revocación de rol/membresía invalida nuevos intents |
| IDEM12 | Recuperación de outbox futuro conserva causalidad y versión, no autoriza bypass |

Clave global o dedup compartido entre tenants **prohibido**. Requiere validación R2 si llega a existir infraestructura; documentos no prueban unicidad de DB.

## 4. Correlación y envelope futuro (COR01–COR10)

| ID | Campo o regla de contrato futura |
| --- | --- |
| COR01 | EventId único opaco |
| COR02 | SchemaVersion explícita |
| COR03 | OperationId / IdempotencyKey con protección de replay |
| COR04 | CorrelationId de operación/transaction |
| COR05 | CausationId del comando que efectivamente comprometió |
| COR06 | TenantId derivado de claims/membresía backend, no del cliente |
| COR07 | BranchId/WarehouseId validados contra tenant |
| COR08 | ResourceId y ResourceVersion/ETag |
| COR09 | CommitUtc y resultado estable; no confundir hora de emisión y commit |
| COR10 | Payload mínimo; costos/PII se omiten o se protegen por rol |

No incluir JWT, cabeceras de autorización, cadenas de conexión ni valores de proveedor en logs. Sin permisos Finanzas, nunca divulgar costo. Errores externos devuelven código estable y correlación sin detalles internos.

## 5. Límites y controles LIM01–LIM12 (propuestas fail-closed)

| ID | Control futuro |
| --- | --- |
| LIM01 | tamaño máximo de payload y listas, sin valores implícitos ilimitados |
| LIM02 | TTL de clave de dedup con política explicitada y validada |
| LIM03 | backoff acotado; cero retry infinito |
| LIM04 | tiempo máximo transaccional con rollback seguro |
| LIM05 | cola por tenant con cuota/aislamiento, nunca starvation de B |
| LIM06 | bloqueo por rol revocado o tenant suspendido |
| LIM07 | máximo de recepciones parciales sin saldo negativo |
| LIM08 | máximo de movimientos de transferencia por transacción |
| LIM09 | prohibición de publicar antes de commit |
| LIM10 | circuit breaker externo fail-closed y estado no confirmado |
| LIM11 | auditoría minimizada, correlación sin secretos |
| LIM12 | desactivación no elimina historia ni concede privilegios |

Estos valores no se codifican ni configuran aquí. Cualquier despliegue de broker/outbox/worker requiere permiso, capacidad y riesgos R2 adicionales según DAG.

## 6. Diez pares de aceptación futuros T01–T10 (NO EJECUTADOS)

| ID | Futuro positivo | Futuro negativo / error |
| --- | --- | --- |
| T01 | evento de reserva A después de commit válido | token B no puede emitirse por reserva A |
| T02 | misma idempotency key devuelve misma reserva | mismo key diferente fingerprint => conflicto |
| T03 | liberación única ajusta disponibilidad una vez | dos consumidores/retries no liberan doble |
| T04 | recepción parcial correcta publica su hecho tras commit | fallo transaccional no publica éxito |
| T05 | transferencia entre almacenes A conserva stock | destino B o mismo origen falla |
| T06 | autorización aprobada genera evento causal | rol revocado no dispara ajuste |
| T07 | correlación enlaza comando y movimiento | correlación cliente no suplanta tenant |
| T08 | costo visible al rol Finanzas autorizado | consumidor sin Finanzas recibe campo censurado |
| T09 | retry acotado recupera estado confirmado | timeout no genera doble movimiento |
| T10 | rollback compensatorio conserva auditoría | fallo en consumer no borra histórico ni marca éxito |

**Ejecución actual: 0 pruebas, 0 eventos, resultados runtime NO_MEDIDO.** La futura implementación requerirá tests xUnit positivos/negativos, tenant A/B y RBAC, MySQL 8.4 efímero `Category=Integration` si afecta transacciones, pruebas de replay, fallo parcial y rollback; Playwright solo si cambia interfaz.

## 7. A0–A5, aceptación y rollback

- **A0:** fuente V5, dependencia M01/M02, dos Markdown y cobertura documental EV01–12, IDEM01–12, COR01–10, LIM01–12, T01–10.
- **A1:** cotejar oráculos positivos, negativos, frontera/error y replay descritos; no afirmar tests automatizados ejecutados.
- **A2:** controles contractuales JWT/RBAC, revocación, propiedad de almacenes, aislamiento A/B, costos, PII e idempotencia; sin cambios de runtime.
- **A3:** cotejo documental con código y otros contratos; backend, MySQL, HTTP, frontend, proveedor, broker `NO_APLICA` al diff R0.
- **A4:** commit serial fast-forward/CAS sobre HEAD real dev; cheques `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en `completed/success` del SHA, BITACORA y PLAN/COLA/DAG readback. Checks `CANCELLED/SKIPPED/IN_PROGRESS` nunca PASS obligatorio.
- **A5:** revisión independiente por distinta Task Javier; no se autoaprueba. LISTO documental sin A5 no habilita runtime ni M02.06/M03 por atajo.

Rollback de esta especificación: `git revert` normal preservando BITACORA, sin force-push, sin tocar datos, secretos, main, QA ni PROD. D01–D08 y gates de pruebas futuras siguen pendientes.
