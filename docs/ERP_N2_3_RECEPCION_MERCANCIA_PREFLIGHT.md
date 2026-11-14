# ERP-N2.3 — Recepción de mercancía — Auditoría y preflight

## Estado

- Proyecto: `SOLQARYN`
- Repositorio: `jmejia31/Solqaryn`
- Rama exclusiva: `Desarrollo`
- Punto: `N2.3.A — Auditoría y preflight`
- Dependencia: `N2.2.H — OrdenCompra` cerrada.
- Producción, `main`, merge/auto-merge del PR #2, ramas nuevas, force-push, secretos e infraestructura productiva: fuera de alcance.
- Scope Jules (`.github/workflows/vaep-jules-*.yml`, `docs/VAEP_JULES.md`, `vaep/jules/**`): excluido.

## Objetivo funcional

Materializar en N2.3 un documento empresarial `RecepcionCompra` separado de `OrdenCompra`, capaz de registrar una o varias recepciones totales/parciales contra una orden aprobada, incluyendo cantidades recibidas, faltantes, sobrantes y dañadas. **El stock físico sólo aumenta por recepción real**, nunca por aprobar la orden.

## Autoridades ya existentes

### Orden de compra

`OrdenCompra` es el compromiso comercial con el proveedor. Su lifecycle actual es `Borrador -> PendienteAprobacion -> Aprobada`, con cancelación controlada. Mantiene proveedor, moneda, fecha esperada, observaciones y detalles, pero no representa entrada física de inventario.

`OrdenCompraDetalle` mantiene por línea `ProductoId`, `ProductoVarianteId`, `CantidadOrdenada`, precio, descuento, impuesto y snapshots de producto. Es la fuente documental contra la cual debe reconciliarse la recepción.

### Stock físico

`ExistenciaVariante` es la autoridad de stock vivo por clave:

`ProductoVarianteId + AlmacenId + UbicacionAlmacenId?`

El incremento de recepción debe operar sobre `StockFisico` mediante `IExistenciaVarianteConcurrencyService`, con locks pesimistas y precondiciones; no debe decidir disponibilidad usando `Producto.Cantidad` o `ProductoVariante.Cantidad` legacy.

### Kardex

Toda entrada materializada por recepción debe generar movimiento de inventario con origen tipado de recepción, `CorrelationId` determinístico por operación y contexto físico completo. El movimiento debe registrar `StockAnterior`, `StockNuevo`, cantidad, costo y snapshots aplicables.

## Gap legacy crítico

`CompraService.ConfirmarAsync` todavía concentra responsabilidades históricas: al confirmar una `Compra` incrementa inventario, escribe Kardex y crea un `MovimientoFinanciero` de egreso dentro del mismo flujo.

N2.3 **no debe reutilizar esa semántica**. La nueva recepción debe separar al menos estas responsabilidades:

1. `OrdenCompra`: compromiso documental aprobado.
2. `RecepcionCompra`: evento físico que aumenta stock.
3. `FacturaProveedor` (N2.4): documento fiscal/obligación facturada.
4. Tesorería/CxP posterior: movimiento financiero/pago.

La implementación de N2.3 no debe crear automáticamente un egreso financiero por recibir mercancía.

## Alcance de N2.3

### Dentro de alcance

- `RecepcionCompra` y `RecepcionCompraDetalle` como agregados dedicados.
- Numeración documental propia e idempotencia durable en creación/confirmación cuando corresponda.
- Relación obligatoria con `OrdenCompra` aprobada.
- Múltiples recepciones por orden.
- Recepción total o parcial por línea.
- Cantidades explícitas: recibida, dañada, faltante y sobrante.
- Almacén y ubicación física de destino por línea/recepción según contrato final.
- Validación acumulada `recibido aceptado <= ordenado`, salvo decisión explícita de política para sobrantes.
- Incremento de `ExistenciaVariante.StockFisico` sólo por cantidad aceptada físicamente.
- Kardex tipado por recepción y trazabilidad hacia `OrdenCompra`.
- Estado documental fail-closed e idempotencia frente a reintentos.
- Auditoría transaccional de mutaciones críticas.
- API, RBAC, frontend, E2E, QA, documentación y rollback en microtareas posteriores B-H.

### Fuera de alcance de N2.3

- Factura de proveedor y cuentas por pagar: N2.4+.
- Three-way match Orden/Recepción/Factura: N2.5.
- Pago al proveedor o egreso financiero automático por recepción.
- Recalcular o mutar arbitrariamente una `OrdenCompra` aprobada.
- Reutilizar `CompraService.ConfirmarAsync` como motor de recepción.
- Modificar autoridad de stock fuera de `ExistenciaVariante`.

## Modelo propuesto para N2.3.B

### `RecepcionCompra`

Campos mínimos previstos:

- `Id`, auditoría base.
- `NumeroRecepcion` único.
- `OrdenCompraId` obligatorio.
- `Estado`: `Borrador`, `Recibida`, `Anulada` (o lifecycle equivalente definido en B).
- `FechaRecepcionUtc`.
- `RecibidaPorUsuarioId` y snapshot de nombre.
- `Observaciones`.
- `IdempotencyKey` + fingerprint durable cuando se defina el endpoint de creación/confirmación.
- colección de detalles.

### `RecepcionCompraDetalle`

Campos mínimos previstos:

- `RecepcionCompraId`.
- `OrdenCompraDetalleId` obligatorio.
- `ProductoId` / `ProductoVarianteId` y snapshots documentales.
- `AlmacenId` obligatorio.
- `UbicacionAlmacenId` nullable.
- `CantidadRecibida`.
- `CantidadDanada`.
- `CantidadFaltante`.
- `CantidadSobrante`.
- `CantidadAceptada` derivada/validada.
- costo unitario snapshot proveniente de la OC para costeo/Kardex.

## Invariantes obligatorias

1. Sólo una `OrdenCompra` aprobada es recepcionable.
2. Una recepción no puede mutar una orden cancelada o no aprobada.
3. Cada detalle debe corresponder a una línea real de la misma OC.
4. Todas las cantidades son `>= 0` y debe existir actividad física real para confirmar.
5. `CantidadAceptada` no puede incluir unidades dañadas.
6. El total acumulado aceptado por línea no puede superar lo ordenado salvo una política explícita de sobrantes; esa política debe ser visible, auditable y fail-closed.
7. La ubicación, si existe, debe pertenecer al almacén destino.
8. El stock físico se actualiza bajo lock de `ExistenciaVariante` y dentro de la misma transacción que la recepción/Kardex/auditoría crítica.
9. Un replay idempotente no duplica recepción, stock ni Kardex.
10. Anular una recepción ya aplicada requiere reversión física segura y debe fallar si existen movimientos posteriores incompatibles o si la reversión violaría reservas/invariantes.

## Concurrencia

- Ordenar y bloquear determinísticamente las existencias por `ProductoVarianteId + AlmacenId + UbicacionAlmacenId`.
- Revalidar cantidades acumuladas contra recepciones previas dentro de la misma transacción.
- Evitar dos recepciones concurrentes que sobrepasen la cantidad ordenada.
- La creación de una existencia física nueva debe tener una estrategia explícita; no inventar ubicación ni almacén.

## Persistencia prevista para N2.3.C

- Tablas dedicadas `RecepcionesCompra` y `RecepcionCompraDetalles`.
- FK restrictiva hacia `OrdenesCompra` y hacia `OrdenCompraDetalles`.
- FKs físicas hacia `Almacenes`, `UbicacionesAlmacen`, producto/variante según diseño final.
- Índice único de `NumeroRecepcion`.
- Índices para `OrdenCompraId`, estado y fecha.
- Constraints de cantidades no negativas.
- Snapshot EF, preflight, migración forward, postcheck y rollback seguro.

## Aplicación/API prevista para N2.3.D

- listado paginado/filtrado.
- detalle.
- crear/editar borrador.
- confirmar/materializar recepción.
- anular/revertir de forma segura.
- consulta de recepciones por `OrdenCompraId`.
- endpoint/contrato para cantidades ordenadas vs acumuladas recibidas/pendientes.
- `ProblemDetails` fail-closed e idempotencia donde corresponda.

## Seguridad y auditoría

Superficie sugerida sobre el módulo `Compras`:

- `Ver` — listar/detalle.
- `Crear` — crear recepción.
- `Editar` — modificar borrador.
- `Confirmar` — materializar stock.
- `Anular` — reversión controlada.

Las mutaciones físicas deben auditar actor, recepción, orden origen, almacén/ubicación y cantidades sin exponer secretos. La auditoría crítica debe participar en la unidad transaccional cuando su fallo deba impedir la operación.

## Matriz mínima de QA

- crear recepción contra OC aprobada.
- rechazar OC borrador/pendiente/cancelada.
- recepción parcial y segunda recepción complementaria.
- impedir sobre-recepción concurrente.
- registrar dañados/faltantes/sobrantes sin inflar stock aceptado.
- validar almacén/ubicación.
- incrementar sólo `StockFisico` autoritativo.
- Kardex con origen tipado e idempotencia sin duplicados.
- rollback completo ante fallo de stock/Kardex/auditoría.
- anulación/reversión segura.
- 401/403 y grants relacionales.
- E2E de recepción total/parcial y errores fail-closed.
- migración limpia + upgrade histórico + recovery MySQL.

## Riesgos principales

1. **Doble autoridad de entrada:** reutilizar `CompraService.ConfirmarAsync` produciría stock/finanzas fuera del nuevo flujo.
2. **Sobre-recepción:** dos sesiones podrían exceder la OC si no se revalida acumulado bajo transacción.
3. **Doble stock por retry:** confirmar dos veces sin idempotencia duplicaría existencias/Kardex.
4. **Daños/sobrantes:** una fórmula ambigua podría sumar unidades no aceptadas al stock disponible.
5. **Ubicación inválida:** permitir una ubicación de otro almacén rompería la clave física.
6. **Reversión insegura:** anular sin revisar movimientos posteriores/reservas puede llevar `StockFisico` a un estado inválido.
7. **Acoplamiento prematuro a factura:** N2.3 no debe resolver N2.4/N2.5 anticipadamente.

## Rollback de implementación

- N2.3.A es documental y puede revertirse sin tocar datos.
- B define únicamente dominio/contratos.
- C debe incluir rollback DDL explícito y bloqueos fail-closed cuando existan recepciones incompatibles con la reversión.
- D-F deben ser reversibles por commit mientras no exista migración aplicada fuera del ambiente controlado de Desarrollo.

## Criterio de cierre de N2.3.A

N2.3.A queda completo cuando:

- el límite OrdenCompra/Recepción/Factura/Pago está explícito;
- la autoridad física `ExistenciaVariante` está confirmada;
- el gap de `CompraService` legacy está documentado y excluido del diseño;
- están definidos alcance, fuera de alcance, invariantes, concurrencia, persistencia prevista, API, RBAC, QA, riesgos y rollback;
- no se ha modificado código funcional, DDL ni scope Jules.

Siguiente microtarea tras validar este preflight: **N2.3.B — Recepción de mercancía / Dominio y contratos**.
