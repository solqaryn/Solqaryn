-- ERP-N2.4.C — FacturaProveedor / postcheck de persistencia
-- Solo lectura. Ejecutar después de aplicar la migración N2.4.C en MySQL controlado.

SELECT table_name
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name IN ('FacturasProveedor','FacturaProveedorDetalles')
ORDER BY table_name;

SELECT table_name, index_name, non_unique, seq_in_index, column_name
FROM information_schema.statistics
WHERE table_schema = DATABASE()
  AND index_name IN (
      'UX_FacturasProveedor_Proveedor_NumeroFactura',
      'UX_FacturaProveedorDetalles_Factura_OrdenDetalle'
  )
ORDER BY table_name, index_name, seq_in_index;

SELECT table_name, constraint_name, constraint_type
FROM information_schema.table_constraints
WHERE constraint_schema = DATABASE()
  AND table_name IN ('FacturasProveedor','FacturaProveedorDetalles')
ORDER BY table_name, constraint_name;

SELECT COUNT(*) AS FacturasHuerfanasProveedor
FROM FacturasProveedor f
LEFT JOIN Proveedores p ON p.Id = f.ProveedorId
WHERE p.Id IS NULL;

SELECT COUNT(*) AS FacturasHuerfanasOrden
FROM FacturasProveedor f
LEFT JOIN OrdenesCompra o ON o.Id = f.OrdenCompraId
WHERE o.Id IS NULL;

SELECT COUNT(*) AS DetallesHuerfanosFactura
FROM FacturaProveedorDetalles d
LEFT JOIN FacturasProveedor f ON f.Id = d.FacturaProveedorId
WHERE f.Id IS NULL;

SELECT COUNT(*) AS DetallesHuerfanosOrden
FROM FacturaProveedorDetalles d
LEFT JOIN OrdenCompraDetalles od ON od.Id = d.OrdenCompraDetalleId
WHERE od.Id IS NULL;

SELECT COUNT(*) AS DetallesHuerfanosProducto
FROM FacturaProveedorDetalles d
LEFT JOIN Productos p ON p.Id = d.ProductoId
WHERE p.Id IS NULL;

SELECT COUNT(*) AS DetallesHuerfanosVariante
FROM FacturaProveedorDetalles d
LEFT JOIN ProductoVariantes pv ON pv.Id = d.ProductoVarianteId
WHERE d.ProductoVarianteId IS NOT NULL AND pv.Id IS NULL;

SELECT ProveedorId, NumeroFactura, COUNT(*) AS Duplicados
FROM FacturasProveedor
GROUP BY ProveedorId, NumeroFactura
HAVING COUNT(*) > 1;

SELECT FacturaProveedorId, OrdenCompraDetalleId, COUNT(*) AS Duplicados
FROM FacturaProveedorDetalles
GROUP BY FacturaProveedorId, OrdenCompraDetalleId
HAVING COUNT(*) > 1;

SELECT table_name, column_name, numeric_precision, numeric_scale
FROM information_schema.columns
WHERE table_schema = DATABASE()
  AND table_name = 'FacturaProveedorDetalles'
  AND column_name IN ('CantidadFacturada','PrecioUnitarioSnapshot','DescuentoSnapshot','ImpuestoSnapshot')
ORDER BY column_name;

SELECT migration_id
FROM __EFMigrationsHistory
WHERE migration_id = '20260820082500_N2_4_FacturaProveedorPersistencia';

-- Resultado esperado:
-- * ambas tablas presentes;
-- * dos índices únicos compuestos con sus dos columnas;
-- * FKs/checks canónicos presentes;
-- * todos los conteos de huérfanos = 0;
-- * ninguna fila de duplicados;
-- * precisión monetaria/cantidad = 18,4;
-- * migración registrada exactamente una vez;
-- * cero efectos colaterales sobre stock, Kardex, costeo o finanzas.
