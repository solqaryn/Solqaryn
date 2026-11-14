-- ERP-N2.4.C — FacturaProveedor / preflight de persistencia
-- Solo lectura. Debe ejecutarse antes de la migración 20260820082500_N2_4_FacturaProveedorPersistencia.

SELECT
    SUM(table_name = 'Proveedores') AS Proveedores,
    SUM(table_name = 'OrdenesCompra') AS OrdenesCompra,
    SUM(table_name = 'OrdenCompraDetalles') AS OrdenCompraDetalles,
    SUM(table_name = 'Productos') AS Productos,
    SUM(table_name = 'ProductoVariantes') AS ProductoVariantes
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name IN ('Proveedores','OrdenesCompra','OrdenCompraDetalles','Productos','ProductoVariantes');

SELECT table_name AS ColisionTabla
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name IN ('FacturasProveedor','FacturaProveedorDetalles');

SELECT migration_id
FROM __EFMigrationsHistory
WHERE migration_id = '20260820082500_N2_4_FacturaProveedorPersistencia';

SELECT table_name, index_name
FROM information_schema.statistics
WHERE table_schema = DATABASE()
  AND index_name IN (
      'UX_FacturasProveedor_Proveedor_NumeroFactura',
      'UX_FacturaProveedorDetalles_Factura_OrdenDetalle'
  );

SELECT table_name, constraint_name, constraint_type
FROM information_schema.table_constraints
WHERE constraint_schema = DATABASE()
  AND constraint_name IN (
      'CK_FacturasProveedor_IdsValidos',
      'CK_FacturasProveedor_EstadoValido',
      'CK_FacturasProveedor_MonedaIso3',
      'CK_FacturasProveedor_FechasValidas',
      'CK_FacturaProveedorDetalles_IdsValidos',
      'CK_FacturaProveedorDetalles_ImportesValidos',
      'CK_FacturaProveedorDetalles_DescuentoValido'
  );

-- Resultado esperado antes de migrar:
-- * Las 5 dependencias existen exactamente una vez.
-- * No existen FacturasProveedor / FacturaProveedorDetalles.
-- * La migración N2.4.C no aparece todavía en __EFMigrationsHistory.
-- * No hay colisiones con los índices/check constraints canónicos.
-- No se realiza backfill heurístico desde Compra/OrdenCompra/RecepcionCompra.
