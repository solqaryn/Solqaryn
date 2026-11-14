# N3.6.B — Devoluciones de cliente — Matriz de decisiones de dominio

**Estado:** PREWARM SAFE / PROMOTION_BLOCKED_BY_N3.6.A  
**Owner:** ChatGPT/VAEP v3.25 Closure Governor  
**Propósito:** convertir N3.6.B en cerrable rápidamente una vez que N3.6.A quede `LISTO`, sin convertir patrones observados en contratos inventados.

## Hechos vigentes

- No existe hoy un agregado `DevolucionCliente`.
- `Venta` conserva la autoridad comercial de la venta, sus líneas/importes, cliente y relación opcional con `Factura`.
- `Factura` conserva su propia autoridad documental, pagos y saldo.
- `DevolucionProveedor` es evidencia de patrones posibles de lifecycle/idempotencia, pero no define el contrato customer-side.
- El requerimiento rector de N3.6 menciona devolución total/parcial, cambio, reintegro y crédito a favor.

## Matriz de decisiones para cerrar N3.6.B

| Tema | Estado ahora | Regla de cierre fail-closed |
|---|---|---|
| Autoridad del documento | DECISION_PENDING | Definir un único agregado de devolución de cliente; no duplicar autoridad de `Venta`/`Factura`. |
| Documento origen | DECISION_PENDING | Definir si `VentaId` es obligatorio y cuándo `FacturaId` aplica; no exigir ambos por analogía. |
| Elegibilidad | DECISION_PENDING | Determinar estados de Venta/Factura aptos para devolver usando contrato explícito. |
| Lifecycle | DECISION_PENDING | Definir estados y transiciones; no copiar `DevolucionProveedor` automáticamente. |
| Devolución parcial | DECISION_PENDING | Fijar límite acumulado por línea/origen para impedir sobredevolución. |
| Cambio | DECISION_PENDING | Separar cantidad devuelta y cantidad/producto entregado en cambio; no inferir efecto financiero. |
| Reintegro | DECISION_PENDING | Definir autoridad financiera y método/origen; no deducir una caja concreta. |
| Crédito a favor | DECISION_PENDING | Definir si pertenece a CxC/documento de crédito/subledger; no escribir saldo en `Cliente` sin autoridad. |
| Inventario/Kardex | DECISION_PENDING | Definir qué transición materializa efecto físico y cómo evita doble stock por retry. |
| Factura/nota de crédito | DECISION_PENDING | No cancelar/modificar factura ni crear nota automáticamente hasta contrato explícito. |
| Idempotencia | DECISION_PENDING | Si existe mutación con efecto físico/financiero, definir key/fingerprint durable y replay semantics. |
| Concurrencia | DECISION_PENDING | Definir locking sobre venta/líneas/existencias cuando corresponda para evitar sobredevolución. |
| RBAC/auditoría | DECISION_PENDING | Definir operaciones y grants propios o reutilizados solo con evidencia; mutaciones críticas deben auditarse. |
| Anulación/reversión | DECISION_PENDING | Definir qué puede revertirse y qué documentos históricos deben preservarse. |

## Invariantes mínimas que el dominio debe demostrar antes de promoción

1. IDs de autoridad positivos y coherentes.
2. Al menos una línea cuando la operación tenga efecto físico.
3. Cantidades de devolución estrictamente positivas.
4. Acumulado devuelto no superior a la cantidad vendida elegible.
5. No duplicar una misma línea/origen dentro del documento salvo contrato explícito.
6. Transiciones de lifecycle fail-closed.
7. Separación explícita entre hecho físico y consecuencia financiera/documental.
8. No mutar `Venta` o `Factura` históricas de forma destructiva por simple confirmación de devolución.
9. Si se adopta idempotencia, clave y fingerprint deben persistirse atómicamente.
10. P0/P1 conocidos = 0 antes de declarar N3.6.B `LISTO`.

## Pruebas dirigidas previstas

- constructor/documento inválido;
- lifecycle permitido y transiciones inválidas;
- devolución total y parcial con límite acumulado;
- duplicidad de líneas/reintento;
- escenario de cambio sin suponer efecto financiero;
- anulación/reversión según contrato final;
- UTC y auditoría de actor cuando el dominio finalmente lo requiera.

## Fuera de alcance de esta matriz

Persistencia/DDL, endpoints, códigos HTTP, componentes Angular, permisos concretos, asientos/movimientos financieros, notas de crédito, selección de caja/método de pago y reglas fiscales. Esos elementos solo se materializan en B o parents posteriores cuando la autoridad funcional quede explícita y revisada.

## Gate de promoción

Este documento es evidence/prewarm. N3.6.B continúa `PENDIENTE` hasta `N3.6.A=LISTO`; al abrir B, ChatGPT/VAEP debe revalidar el HEAD, cerrar las decisiones anteriores con evidencia del Plan/arquitectura vigente y convertirlas en código/pruebas solo si son autoritativas.
