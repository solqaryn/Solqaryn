# N2.4.C.3 — Preflight de datos, reconciliación y rollback de FacturaProveedor

## Alcance y base

- Proyecto: Solqaryn / ERP-N2.4 FacturaProveedor.
- Rama: `Desarrollo`.
- Base lógica: `158161812e74fd9d6b91c9cdfba51f62671de5c4` y descendientes de N2.4.C hasta el failover actual.
- Scope exclusivo: estrategia de datos, preflight/postcheck, backfill si aplica, reconciliación y rollback lógico. No incluye ejecutar DDL/DML sobre Producción ni modificar runtime EF/API/frontend.

## Resultado ejecutivo

**REVIEW RESULT: APPROVED_WITH_NO_HEURISTIC_BACKFILL**

FacturaProveedor debe nacer como una autoridad documental nueva y separada de `OrdenCompra`, `RecepcionCompra` y `Compra` legacy. No existe una fuente autoritativa suficiente para fabricar facturas históricas de proveedor de forma automática. Por ello, el backfill inicial recomendado es **NO-OP explícito**: crear el esquema vacío y dejar que las facturas se registren mediante el flujo N2.4.

## Invariantes de datos

1. `OrdenCompra` representa lo comprado/ordenado.
2. `RecepcionCompra` representa lo físicamente recibido y es la autoridad de stock/Kardex.
3. `FacturaProveedor` representa lo facturado por el proveedor.
4. Registrar o migrar una FacturaProveedor **no debe** incrementar existencias, generar Kardex, modificar costeo ni crear pagos/egresos automáticamente.
5. La conciliación estricta comprado vs recibido vs facturado corresponde a N2.5; N2.4 sólo debe preservar las referencias y snapshots necesarios para hacerla posible después.
6. Una factura se identifica contextualmente por `ProveedorId + NumeroFactura`; no debe asumirse que `NumeroFactura` es globalmente único.
7. Dentro de una factura, una misma `OrdenCompraDetalleId` no puede repetirse.
8. Importes y cantidades deben conservar precisión `18,4` y no admitir negativos; el descuento no puede superar el bruto de línea.

## Preflight recomendado

Antes de aplicar la migración N2.4.C en un ambiente controlado:

- Confirmar existencia de tablas/dependencias: `Proveedores`, `OrdenesCompra`, `OrdenCompraDetalles`, `Productos`, `ProductoVariantes`.
- Confirmar ausencia de colisiones con `FacturasProveedor`, `FacturaProveedorDetalles` y nombres canónicos de índices/FKs/check constraints.
- Confirmar que `__EFMigrationsHistory` no contiene ya la migración N2.4.C bajo otro identificador.
- Verificar que no existe otra tabla legacy que el sistema esté tratando como autoridad de FacturaProveedor.
- Verificar que el entorno no contiene objetos parciales de un intento fallido; si existen, abortar y usar el recovery controlado en lugar de continuar a ciegas.
- No leer `Compra` legacy para crear FacturaProveedor automáticamente.

## Estrategia de backfill

### Decisión

**No ejecutar backfill heurístico.**

Motivos:

- `Compra` legacy mezcla documento, estado de pago, método de pago y otras responsabilidades; no es equivalente semántico a FacturaProveedor.
- Una `RecepcionCompra` demuestra recepción física, no demuestra que exista una factura fiscal ni su número, fecha, moneda, referencia fiscal, impuestos o vencimiento.
- Una `OrdenCompra` demuestra intención/compromiso de compra, no prueba que el proveedor haya facturado.
- Inventar `NumeroFactura`, fechas o importes rompería trazabilidad y three-way match futuro.

### Única excepción futura

Si se incorpora una fuente autoritativa externa con número fiscal, proveedor, orden vinculada, fechas e importes completos, el backfill debe implementarse como una migración/importación independiente, explícita, auditable e idempotente; no debe esconderse dentro de la migración de esquema N2.4.C.

## Postcheck recomendado

Después de aplicar la migración en MySQL efímero/controlado:

- `FacturasProveedor` y `FacturaProveedorDetalles` existen exactamente una vez.
- Índice único `ProveedorId + NumeroFactura` existe y es único.
- Índice único `FacturaProveedorId + OrdenCompraDetalleId` existe y es único.
- FKs de cabecera a proveedor/orden y de detalle a orden-detalle/producto/variante existen con `RESTRICT`.
- La FK cabecera→detalle usa `CASCADE` únicamente en esa relación.
- Los check constraints de IDs, estado, moneda, fechas, importes y descuento están presentes.
- Columnas monetarias/cantidad mantienen precisión `18,4`.
- Conteo de huérfanos en cada FK = 0.
- Conteo de duplicados por `ProveedorId + NumeroFactura` = 0.
- Conteo de duplicados por `FacturaProveedorId + OrdenCompraDetalleId` = 0.
- No se crean filas en existencias, Kardex, costeo o finanzas como efecto colateral de la migración.

## Reconciliación comprado / recibido / facturado

N2.4 debe conservar referencias que habiliten N2.5, pero no resolver el match todavía:

- `FacturaProveedor.OrdenCompraId` fija la orden documental de origen.
- `FacturaProveedorDetalle.OrdenCompraDetalleId` fija la línea ordenada asociada.
- Cantidad e importes facturados se preservan como snapshots documentales.
- `RecepcionCompra` permanece independiente; N2.5 podrá comparar las cantidades recibidas contra las facturadas por línea.
- No agregar una FK directa obligatoria Factura→Recepcion en N2.4: múltiples recepciones parciales por orden pueden existir y esa cardinalidad debe resolverse por el motor de conciliación, no por una relación 1:1 artificial.

## Rollback lógico y técnico

### Rollback técnico de migración

En ambientes no productivos/controlados:

1. Verificar que la migración objetivo es la última aplicada y que no existen migraciones dependientes posteriores.
2. Eliminar primero FKs/índices dependientes del detalle según el orden generado por EF.
3. Eliminar `FacturaProveedorDetalles`.
4. Eliminar FKs/índices dependientes de cabecera.
5. Eliminar `FacturasProveedor`.
6. Verificar `__EFMigrationsHistory` y ejecutar postcheck de ausencia de objetos huérfanos.

### Rollback funcional después del go-live

No borrar físicamente facturas registradas para “deshacer” una operación empresarial. El lifecycle ya contempla `Registrada → Anulada`; la anulación debe conservar snapshots, usuario, fecha y motivo. El rollback físico de tablas sólo pertenece a una reversión de despliegue antes de uso empresarial o a un procedimiento extraordinario controlado.

## Riesgos

- **P1 si se introduce backfill heurístico** desde `Compra`, `OrdenCompra` o `RecepcionCompra`: generaría documentos no autoritativos.
- **P1 si se acopla FacturaProveedor a stock/Kardex**: rompería la separación de autoridad física certificada en N2.3.
- **P1 si se impone unicidad global al número de factura**: puede rechazar facturas válidas de proveedores distintos.
- **P1 si se fuerza una recepción única por factura**: rompe órdenes con recepciones parciales/múltiples.

## P0/P1 del changeset revisado

- P0: 0.
- P1 actuales dentro del modelo revisado: 0.
- REQUIRED antes de cerrar N2.4.C: migración y snapshot canónicos, preflight/postcheck ejecutados en gate controlado, recovery MySQL verde y ausencia de pending model changes.

## Recomendación final

**C.3 puede marcarse LISTO** con estrategia `NO_HEURISTIC_BACKFILL`, postchecks de integridad y rollback documentados. El padre N2.4.C permanece bloqueado hasta que la migración/snapshot real de C.1/C se materialice y pase CI/DoD completo.
