# ERP-N2.6 — Devoluciones a proveedor

## Estado del documento

Documento canónico de arquitectura y comportamiento funcional de ERP-N2.6. La certificación final del punto H se mantiene separada en `CERTIFICACION_N2_6_DEVOLUCIONES_PROVEEDOR.md` y sólo puede cerrarse con gates causales terminales y sin P0/P1 pendientes.

## Objetivo

ERP-N2.6 formaliza la devolución de mercancía a proveedor como documento empresarial trazable, enlazado con la compra ya recibida y facturada. La devolución no reutiliza `Compra` ni convierte una anulación documental en una mutación implícita: su ciclo de vida, persistencia, stock físico, Kardex, seguridad y auditoría tienen contratos explícitos.

## Linaje documental

La devolución preserva referencias autoritativas hacia:

- `ProveedorId`.
- `OrdenCompraId`.
- `RecepcionCompraId`.
- `FacturaProveedorId`.
- En cada línea: `RecepcionCompraDetalleId`, `OrdenCompraDetalleId`, `ProductoId`, variante opcional, almacén y ubicación opcional.

El diseño evita reconstruir el origen mediante heurísticas. Las referencias persistidas y los snapshots son la evidencia de negocio del documento.

## Ciclo de vida

El agregado `DevolucionProveedor` utiliza tres estados persistidos:

1. `Borrador`.
2. `Confirmada`.
3. `Anulada`.

Reglas principales:

- Una devolución nace en `Borrador`.
- Sólo el borrador admite edición de cabecera y líneas.
- No puede confirmarse sin líneas.
- Confirmar una devolución ya confirmada es idempotente y no repite la transición.
- La anulación sólo procede desde `Confirmada` y exige motivo.
- Repetir la anulación de una devolución ya anulada es idempotente.
- Las líneas no pueden duplicar el mismo `RecepcionCompraDetalleId` dentro del documento.
- Cantidades deben ser estrictamente positivas; costos e impuestos snapshot no pueden ser negativos.
- Los importes monetarios del dominio se normalizan a cuatro decimales con `MidpointRounding.AwayFromZero`.

## Idempotencia

La creación exige `Idempotency-Key`. El dominio conserva `IdempotencyKey` y `IdempotencyFingerprint` como par atómico. La persistencia protege `IdempotencyKey` mediante índice único y la migración impide combinaciones parciales key/fingerprint.

El objetivo es que un retry del mismo comando no cree una segunda devolución y que reutilizar una clave con un payload incompatible falle cerrado.

## Autoridad de inventario

La autoridad física es `ExistenciaVariante`, no `ProductoVariante.Cantidad`.

La confirmación se ejecuta dentro de una unidad transaccional que:

- bloquea las claves físicas requeridas mediante `IExistenciaVarianteConcurrencyService`;
- materializa las demandas de devolución;
- delega la modificación física a `IDevolucionProveedorinventarioProcessor`;
- registra el movimiento correspondiente mediante `IDevolucionProveedorKardexWriter`;
- persiste el nuevo estado y la auditoría estricta en la misma frontera transaccional.

La anulación utiliza la misma estrategia para revertir de forma controlada una devolución confirmada. La entidad por sí sola no altera stock: la aplicación coordina dominio, locks, procesador físico y Kardex.

## API y RBAC

El controller canónico es `DevolucionesProveedorController`, protegido globalmente con `[Authorize]` y ruta base:

`/devoluciones-proveedor`

Contrato HTTP principal:

- `GET /devoluciones-proveedor` — `Compras/Ver`.
- `GET /devoluciones-proveedor/{id}` — `Compras/Ver`.
- `POST /devoluciones-proveedor` — `Compras/Crear` y header `Idempotency-Key` obligatorio.
- `PUT /devoluciones-proveedor/{id}` — `Compras/Editar`.
- `POST /devoluciones-proveedor/{id}/confirmar` — `Compras/Confirmar`.
- `POST /devoluciones-proveedor/{id}/anular` — `Compras/Anular`.

La ausencia de `Idempotency-Key` en creación se rechaza con ProblemDetails `400`. Una lectura inexistente retorna ProblemDetails `404`. Las excepciones de negocio continúan por el pipeline global de errores.

## Auditoría y seguridad

Las mutaciones de aplicación exigen usuario autenticado y auditoría estricta. La auditoría forma parte de la transacción de las operaciones que cambian estado o persistencia; un fallo de auditoría no debe convertirse en un éxito silencioso del cambio de negocio.

La autorización se basa en grants relacionales de `Compras`; no se admite un bypass por rol/administrador fuera del contrato RBAC vigente.

## Persistencia

Migración canónica:

`20260821173500_N2_6_DevolucionProveedorPersistencia`

Tablas:

- `DevolucionesProveedor`.
- `DevolucionProveedorDetalles`.

Propiedades estructurales relevantes:

- PKs autoincrementales.
- FKs restrictivas hacia proveedor, orden, recepción y factura.
- FKs restrictivas de detalle hacia recepción, orden, producto, variante, almacén y ubicación.
- `Cascade` únicamente desde cabecera de devolución hacia sus detalles.
- índice único de `NumeroDevolucion`.
- índice único de `IdempotencyKey`.
- unicidad de `(DevolucionProveedorId, RecepcionCompraDetalleId)`.
- cantidades y snapshots monetarios con precisión `decimal(18,4)`.
- checks de IDs, estado, moneda, idempotencia atómica, cantidades y costos.

La migración contiene guards de precondición y postcondición en `Up`. El comportamiento destructivo de `Down` está documentado por separado en el runbook.

## Fronteras del módulo

N2.6 no debe:

- inventar referencias documentales faltantes;
- usar `ProductoVariante.Cantidad` como autoridad física;
- crear movimientos de stock fuera del procesador/concurrencia transaccional;
- saltarse Kardex al confirmar o anular;
- transformar la anulación documental en una operación financiera implícita no definida;
- promover el módulo como cerrado únicamente porque exista documentación.

## Criterio de cierre

N2.6 queda listo únicamente cuando el paquete documental, QA, CI, migración, seguridad, UX y evidencia de rollback estén reconciliados y los gates causales exigidos terminen en verde con P0/P1=0.