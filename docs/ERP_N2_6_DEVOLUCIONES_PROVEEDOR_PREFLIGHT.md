# ERP-N2.6 — Devoluciones a proveedor — Preflight autoritativo

**Estado:** `LISTO` — QA takeover ChatGPT/VAEP

## Alcance rector
N2.6 debe implementar `DevolucionProveedor` con efectos en inventario, cuenta por pagar, impuestos y auditoría. Este preflight fija únicamente el punto de partida, riesgos y decisiones que deben resolverse en N2.6.B–H; no implementa producto ni anticipa contratos.

## CONFIRMED_FACT
- N2.5 Three-Way Match quedó certificado antes de este cierre; el bloqueo histórico de N2.6 por N2.5 ya no aplica.
- El repositorio contiene `OrdenCompra`, `RecepcionCompra` y `FacturaProveedor` como piezas existentes del flujo de compras.
- En `backend/src/Domain/Entities/` no existe actualmente una entidad `DevolucionProveedor`; por tanto N2.6 requiere materialización explícita en tareas posteriores.
- `TipoAlmacen` ya contiene `Devolucion = 4`. Esto es capacidad estructural existente; no define por sí sola el workflow de una devolución a proveedor.
- La fuente rectora exige que N2.6 cubra efectos de inventario, cuenta por pagar, impuestos y auditoría. El mecanismo exacto de cada efecto todavía debe diseñarse y probarse.
- Las tareas N2.6.B, C, D, E, F, G y H existen separadas para dominio/contratos, persistencia, aplicación/API, frontend/UX, seguridad/auditoría, QA/CI y documentación/certificación.

## DECISION_PENDING — no promover como contrato desde este preflight
- Aggregate, entidades de detalle y nombres exactos de estados/transiciones de `DevolucionProveedor`.
- Cardinalidades/FKs concretas hacia proveedor, orden, recepción, factura, variantes, almacenes o movimientos.
- Regla exacta de cantidad máxima retornable, devoluciones parciales y concurrencia entre devoluciones.
- Estrategia exacta de idempotencia, locking/concurrency y respuesta a reintentos.
- Semántica de movimiento físico/Kardex y elección concreta de almacén/ubicación de tránsito o devolución.
- Ajuste exacto de CxP, crédito del proveedor y relación con futuras notas de crédito N2.7.
- Tratamiento fiscal/impuestos y cualquier recalculo o documento fiscal asociado.
- Permisos RBAC, eventos de auditoría, endpoint/DTO/error contract, UI y observabilidad específicos.
- Esquema/migración, backfill, `Down()` y procedimiento de restore/rollback.

## Riesgos obligatorios a resolver en B–H
- Doble devolución o retorno por encima de lo efectivamente recibido/elegible.
- Desalineación inventario ↔ Kardex ↔ CxP ↔ impuestos ↔ auditoría ante fallos parciales.
- Reintentos que dupliquen efectos financieros o físicos.
- Concurrencia sobre stock, recepción o saldos retornables.
- Pérdida de trazabilidad por modificar documentos históricos en lugar de registrar eventos/entidades propios.
- Diseñar N2.6 acoplándolo prematuramente a N2.7 antes de que el contrato de notas de crédito sea autoritativo.

## Estrategia de validación posterior
- N2.6.B debe fijar invariantes y contratos antes de persistencia.
- N2.6.C debe demostrar migración desde cero/upgrade, constraints, precisión, índices, idempotencia y rollback/restore aplicables.
- N2.6.D debe demostrar atomicidad/orquestación, API, errores y reintentos seguros.
- N2.6.E/F deben demostrar UX, RBAC, auditoría, seguridad y trazabilidad.
- N2.6.G debe ejecutar regresión proporcional sobre inventario/compras/finanzas/impuestos y los gates CI aplicables.
- N2.6.H solo puede certificar con B–G `LISTO`, P0/P1=0 y evidencia causal suficiente.

## Rollback de este punto A
Este cambio es documentación/preflight. Su reversión no modifica esquema ni datos. No autoriza DDL/DML ni operaciones en Producción.

## Dictamen QA takeover
El ATTEMPT2 de Jules A quedó `JULES_RETRY_EXHAUSTED` por defectos de evidencia/protocolo y no se integra. ChatGPT/VAEP reconstruyó el preflight únicamente con hechos verificables y decisiones pendientes explícitas. **N2.6.A = LISTO**; N2.6.B queda habilitado para promoción sujeto a su propia QA/DoD. R3+ Jules permanece prohibido para N2.6.A.
