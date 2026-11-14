-- ERP-N1.8.C — Preflight read-only para reservas de inventario.
-- No realiza DDL/DML; falla por resultado observable y deja evidencia antes de migrar.

SELECT DATABASE() AS BaseDatos,
       NOW(6) AS EjecutadoUtc,
       @@sql_require_primary_key AS RequirePrimaryKey;

SELECT t.table_name AS TablaExistenteNoEsperada
FROM information_schema.tables t
WHERE t.table_schema = DATABASE()
  AND t.table_name IN ('ReservasInventario', 'ReservaInventarioDetalles');

SELECT required.Nombre,
       CASE WHEN t.table_name IS NULL THEN 'FALTA' ELSE 'OK' END AS Estado
FROM (
    SELECT 'Ventas' AS Nombre
    UNION ALL SELECT 'ProductoVariantes'
    UNION ALL SELECT 'Almacenes'
    UNION ALL SELECT 'UbicacionesAlmacen'
    UNION ALL SELECT 'ExistenciasVariante'
) required
LEFT JOIN information_schema.tables t
       ON t.table_schema = DATABASE()
      AND t.table_name = required.Nombre
ORDER BY required.Nombre;

SELECT tc.constraint_name,
       tc.constraint_type
FROM information_schema.table_constraints tc
WHERE tc.constraint_schema = DATABASE()
  AND tc.table_name = 'UbicacionesAlmacen'
  AND tc.constraint_name = 'AK_UbicacionesAlmacen_AlmacenId_Id';

SELECT COUNT(*) AS ExistenciasConStockInvalido
FROM ExistenciasVariante
WHERE StockFisico < 0
   OR StockReservado < 0
   OR StockReservado > StockFisico;
