# ERP-N2.3 — Recepción de mercancía

## Estado
Documento canónico de implementación para N2.3. Baseline funcional certificado: `8b8b95ce0573653452cee7ca5024d82bdb184d88`.

## Objetivo funcional
RecepcionCompra materializa la recepción física contra una OrdenCompra aprobada y mantiene separado lo comprado, lo recibido y los efectos posteriores de facturación. La recepción puede ser parcial o completarse mediante múltiples recepciones. Solo la cantidad aceptada incrementa stock físico.

## Modelo y lifecycle
`RecepcionCompra` es una entidad independiente vinculada a `OrdenCompra` y contiene `RecepcionCompraDetalle`.

Estados efectivos:
- `Borrador`: editable y aún sin efecto de inventario.
- `Recibida`: confirmada y materializada en inventario/Kardex.
- `Anulada`: recepción revertida de forma controlada.

La confirmación exige al menos una cantidad físicamente recibida. Una recepción fuera de Borrador no puede editarse. La anulación exige motivo y solo es válida desde Recibida.

## Líneas y cantidades
Cada detalle referencia una línea de OrdenCompra y conserva snapshots del producto: SKU, nombre, marca, modelo, color, talla y costo unitario. Registra almacén, ubicación opcional, cantidad recibida, dañada, faltante y sobrante; la cantidad aceptada es la magnitud que impacta stock.

Dentro de una misma recepción no puede repetirse la misma combinación física `OrdenCompraDetalleId + AlmacenId + UbicacionAlmacenId`. El almacén debe existir y estar activo; cuando se informa ubicación, debe existir, estar activa y pertenecer al almacén indicado.

## Recepciones parciales y múltiples
Antes de confirmar se calcula la cantidad aceptada acumulada por línea de orden, excluyendo cuando corresponda la recepción actual. La suma `aceptada acumulada + aceptada actual` no puede superar `CantidadOrdenada`. El endpoint de saldo expone por línea cantidad ordenada, aceptada acumulada y pendiente.

## Idempotencia y concurrencia
La creación exige `Idempotency-Key`.
- Longitud máxima: 128 caracteres.
- Caracteres aceptados: alfanuméricos y `- _ . :`.
- Se persiste junto con un fingerprint SHA-256 del payload canónico.
- Reusar la misma clave con el mismo fingerprint devuelve la recepción existente.
- Reusar la clave con payload distinto produce conflicto.
- La restricción única `UX_RecepcionesCompra_IdempotencyKey` resuelve también carreras concurrentes.

Las mutaciones críticas se ejecutan en transacción y obtienen la recepción mediante lectura `FOR UPDATE`/bloqueo equivalente del repositorio. La confirmación valida nuevamente la orden y los acumulados dentro de la transacción.

## Inventario y Kardex
La creación/edición del borrador no modifica stock. Al confirmar:
1. se bloquea y valida la recepción;
2. se verifica que la OrdenCompra continúe Aprobada;
3. se validan recepciones múltiples y límites acumulados;
4. `RecepcionCompraExistenciaMaterializador` aplica el aumento de existencia por cantidad aceptada;
5. `RecepcionCompraKardexRegistrar` registra los movimientos tipados de recepción;
6. se cambia el estado a Recibida y se persiste auditoría estricta dentro del flujo transaccional.

La anulación revierte existencias y registra el movimiento inverso de Kardex. Antes de revertir se consulta `ExisteMovimientoPosteriorRecepcionAsync(recepcion.Id)`; si existen movimientos posteriores relacionados, la anulación se rechaza para preservar integridad histórica y stock.

## Seguridad y auditoría
Todos los endpoints requieren autenticación y permisos relacionales del módulo Compras:
- Ver: listado, detalle y saldo de orden.
- Crear: alta de recepción.
- Editar: modificación de borrador.
- Confirmar: materialización de stock.
- Anular: reversión controlada.

Las mutaciones usan auditoría estricta. El snapshot de auditoría contiene identificadores/estado/totales/fechas relevantes y deliberadamente no incorpora Observaciones ni claves de idempotencia. Las operaciones críticas fallan si no existe usuario autenticado válido.

## Frontera contable
RecepcionCompra no genera por sí sola factura de proveedor, pago, cuenta por pagar ni asiento contable. N2.4 mantiene separada FacturaProveedor para controlar comprado vs recibido vs facturado.

## Frontend
El módulo frontend expone navegación protegida, listado, filtros, formulario, detalle y UX de diferencias de recepción. Las acciones de crear/confirmar/anular/ver respetan los grants del backend y no sustituyen la autorización del servidor.

## Evidencia de QA
El baseline funcional `8b8b95ce...` quedó certificado por:
- M13 `#32320525485`: SUCCESS completo, incluyendo backend/MySQL/migraciones, frontend, higiene, seguridad HTTP, runtime, Playwright integral y dictamen automatizado.
- N2.3 frontend CI `#32320525445`: SUCCESS con 7/7 E2E.
- N2.3 unit frontend `#32320525478`: SUCCESS.
- Revisión independiente Jules C: 36/36 pruebas RecepcionCompra PASS y cero hallazgos P0/P1; evidencia integrada en `docs/qa/N2_3_G_INDEPENDENT_REVIEW_JULES_C.md`.

## Criterio de cierre
N2.3 puede cerrarse cuando H.1, H.2 y H.3 estén reconciliados, no existan P0/P1 abiertos, la documentación canónica refleje el comportamiento real y `TASKS.md`/`CHANGELOG_AI.md` queden sincronizados con el tablero operativo.